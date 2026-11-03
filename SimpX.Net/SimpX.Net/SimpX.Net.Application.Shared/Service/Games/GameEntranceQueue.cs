using System.Collections.Concurrent;
using System.Text.Json;
using NewLife.Caching;
using SimpX.Net.Caching;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 游戏入口同步队列：对齐旧 GameCaching GAMEENTRANCE（库号 4，LPUSH / RPOP）。
/// </summary>
public interface IGameEntranceQueue
{
    /// <summary>
    /// 把入口 JSON 推进队列左侧（旧 ListLeftPush）。
    /// </summary>
    /// <param name="payloadJson">入口 JSON</param>
    void Enqueue(string payloadJson);

    /// <summary>
    /// 从队列右侧弹出一条（旧 ListRightPop）；空则 false。
    /// </summary>
    /// <param name="payloadJson">弹出的 JSON</param>
    /// <returns>弹出成功为 true</returns>
    bool TryDequeue(out string payloadJson);

    /// <summary>
    /// 连续弹出最多 maxCount 条。
    /// </summary>
    /// <param name="maxCount">最多条数</param>
    /// <returns>JSON 列表</returns>
    IReadOnlyList<string> Drain(int maxCount);
}

/// <summary>内存队列，给单测和未接 Redis 时用。</summary>
public sealed class MemoryGameEntranceQueue : IGameEntranceQueue
{
    /// <summary>模拟 GAMEENTRANCE 列表（左进右出）。</summary>
    private readonly ConcurrentQueue<string> _items = new();

    /// <summary>当前条数（断言用）。</summary>
    public int Count => _items.Count;

    /// <inheritdoc />
    public void Enqueue(string payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson)) return;
        _items.Enqueue(payloadJson.Trim());
    }

    /// <inheritdoc />
    public bool TryDequeue(out string payloadJson)
    {
        if (_items.TryDequeue(out payloadJson) && !string.IsNullOrWhiteSpace(payloadJson))
            return true;
        payloadJson = null;
        return false;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> Drain(int maxCount)
    {
        if (maxCount <= 0) return Array.Empty<string>();
        var list = new List<string>(Math.Min(maxCount, 64));
        while (list.Count < maxCount && TryDequeue(out var json))
            list.Add(json);
        return list;
    }
}

/// <summary>
/// Redis 实现：键 GAMEENTRANCE，库号对齐旧 GameCaching（4），无业务缓存前缀。
/// </summary>
public sealed class GameEntranceQueue : IGameEntranceQueue
{
    /// <summary>旧游戏入口同步队列键。</summary>
    public const string QueueKey = "GAMEENTRANCE";

    /// <summary>旧游戏缓存 Redis 库号。</summary>
    public const int GameCacheDbIndex = 4;

    private readonly FullRedis _redis;
    private readonly ILogger<GameEntranceQueue> _logger;

    /// <summary>
    /// 按 Cache 配置懒建游戏库 Redis；未配时入队/出队静默失败。
    /// </summary>
    /// <param name="logger">日志</param>
    public GameEntranceQueue(ILogger<GameEntranceQueue> logger)
    {
        _logger = logger;
        try
        {
            _redis = LegacyLedgerRedisFactory.TryCreate(
                App.GetConfig<CacheOptions>("Cache", true),
                GameCacheDbIndex);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "游戏入口队列 Redis 未就绪");
            _redis = null;
        }
    }

    /// <inheritdoc />
    public void Enqueue(string payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson)) return;
        try
        {
            if (_redis == null)
            {
                _logger.LogWarning("游戏入口队列未入队：未配置旧游戏缓存 Redis");
                return;
            }

            // 对齐旧 ListLeftPush
            _redis.LPUSH(QueueKey, payloadJson.Trim());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "游戏入口队列入队失败");
        }
    }

    /// <inheritdoc />
    public bool TryDequeue(out string payloadJson)
    {
        payloadJson = null;
        try
        {
            if (_redis == null) return false;
            // 对齐旧 ListRightPop
            payloadJson = _redis.RPOP<string>(QueueKey);
            return !string.IsNullOrWhiteSpace(payloadJson);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "游戏入口队列弹出失败");
            payloadJson = null;
            return false;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<string> Drain(int maxCount)
    {
        if (maxCount <= 0) return Array.Empty<string>();
        var list = new List<string>(Math.Min(maxCount, 64));
        while (list.Count < maxCount && TryDequeue(out var json))
            list.Add(json);
        return list;
    }

    /// <summary>
    /// 把入口 JSON 尽量解析成同步载荷；单测也会直接调。
    /// </summary>
    /// <param name="json">队列原文</param>
    /// <param name="payload">解析结果</param>
    /// <returns>解析成功为 true</returns>
    public static bool TryParsePayload(string json, out GameEntrancePayload payload)
    {
        payload = null;
        if (string.IsNullOrWhiteSpace(json)) return false;
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var type = ReadInt(root, "Type", "type");
            var code = ReadString(root, "Code", "GameCode", "code", "gameCode");
            if (type <= 0 || string.IsNullOrWhiteSpace(code))
                return false;

            payload = new GameEntrancePayload
            {
                Type = type,
                Code = code.Trim(),
                Name = ReadString(root, "Name", "GameName", "name", "gameName") ?? code,
                Category = ReadInt(root, "Category", "category"),
                Status = ReadInt(root, "Status", "status"),
                Cover = ReadString(root, "Cover", "cover") ?? string.Empty,
                Sort = (short)ReadInt(root, "Sort", "sort"),
                GameTag = ReadInt(root, "GameTag", "gameTag")
            };
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>读整型属性（多别名）。</summary>
    private static int ReadInt(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (!root.TryGetProperty(name, out var prop)) continue;
            if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out var n))
                return n;
            if (prop.ValueKind == JsonValueKind.String && int.TryParse(prop.GetString(), out n))
                return n;
        }
        return 0;
    }

    /// <summary>读字符串属性（多别名）。</summary>
    private static string ReadString(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (!root.TryGetProperty(name, out var prop)) continue;
            if (prop.ValueKind == JsonValueKind.String)
                return prop.GetString();
            if (prop.ValueKind == JsonValueKind.Number)
                return prop.ToString();
        }
        return null;
    }
}

/// <summary>游戏入口同步载荷（Redis 队列 JSON）。</summary>
public sealed class GameEntrancePayload
{
    /// <summary>游戏类型。</summary>
    public int Type { get; set; }

    /// <summary>游戏代码。</summary>
    public string Code { get; set; }

    /// <summary>游戏名字。</summary>
    public string Name { get; set; }

    /// <summary>分类。</summary>
    public int Category { get; set; }

    /// <summary>状态：0 关闭 / 1 开放。</summary>
    public int Status { get; set; }

    /// <summary>封面。</summary>
    public string Cover { get; set; }

    /// <summary>排序。</summary>
    public short Sort { get; set; }

    /// <summary>标签。</summary>
    public int GameTag { get; set; }

    /// <summary>是否开放。</summary>
    public bool IsOpen => Status == 1;
}
