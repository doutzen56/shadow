using System.Text.Json;
using NewLife.Caching;
using SimpX.Net.Caching;
using SimpX.Net.Data.Entity.Games;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 系统游戏维护状态复位。对齐旧 GameSettingAgent.ReSetGameMaintainStatus。
/// </summary>
public interface IGameMaintainResetService
{
    /// <summary>
    /// 扫 Status&gt;=2 的 sys_GameSetting：Redis 维护键已过期，或 Maintenance 里写的结束时间已过，则复位为正常并清空 Maintenance。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>本轮复位条数</returns>
    Task<int> ResetExpiredAsync(CancellationToken cancellationToken = default);
}

/// <summary>默认实现：Schedule 显式注册；Redis 用旧游戏缓存库号 4、键 MAINTAIN_{Type}。</summary>
public sealed class GameMaintainResetService : IGameMaintainResetService
{
    /// <summary>旧版正常状态。</summary>
    public const int StatusNormal = 1;

    /// <summary>旧游戏缓存 Redis 库号。</summary>
    public const int GameCacheDbIndex = 4;

    /// <summary>旧维护键前缀（系统层无站点）。</summary>
    public const string MaintainKeyPrefix = "MAINTAIN_";

    private readonly ISqlSugarClient _db;
    private readonly ILogger<GameMaintainResetService> _logger;
    private readonly FullRedis _gameRedis;

    /// <summary>
    /// 注入业务库与日志；按需懒建游戏缓存 Redis（未配则只走库侧结束时间判断）。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="logger">日志</param>
    public GameMaintainResetService(IBizDbAccessor biz, ILogger<GameMaintainResetService> logger)
    {
        _db = biz.Db;
        _logger = logger;
        try
        {
            _gameRedis = LegacyLedgerRedisFactory.TryCreate(
                App.GetConfig<CacheOptions>("Cache", true),
                GameCacheDbIndex);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "游戏维护 Redis 未就绪，仅用库侧结束时间判断");
            _gameRedis = null;
        }
    }

    /// <inheritdoc />
    public async Task<int> ResetExpiredAsync(CancellationToken cancellationToken = default)
    {
        var list = await _db.Queryable<SystemGameSetting>()
            .Where(t => t.Status >= 2)
            .ToListAsync(cancellationToken);

        var count = 0;
        var now = DateTime.Now;
        foreach (var item in list)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!ShouldReset(item, now))
                continue;

            var n = await _db.Updateable<SystemGameSetting>()
                .SetColumns(t => new SystemGameSetting
                {
                    Status = StatusNormal,
                    Maintenance = string.Empty
                })
                .Where(t => t.Type == item.Type && t.Status >= 2)
                .ExecuteCommandAsync(cancellationToken);
            if (n > 0)
                count++;
        }

        return count;
    }

    /// <summary>
    /// 是否应复位：Redis 无维护键 → 可复位；有键 → 仍在维护。
    /// Redis 不可用时，若 Maintenance 能解析出结束时间且已过，则复位。
    /// </summary>
    private bool ShouldReset(SystemGameSetting item, DateTime now)
    {
        if (_gameRedis != null)
        {
            try
            {
                var key = MaintainKeyPrefix + item.Type;
                // 键还在 = 维护 TTL 未到，对齐旧 CheckGameStatus 返回失败 → 不复位
                if (_gameRedis.ContainsKey(key))
                    return false;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "读维护键失败 Type={Type}，改试库侧结束时间", item.Type);
            }
        }

        if (TryParseMaintenanceEnd(item.Maintenance, out var endAt) && endAt <= now)
            return true;

        return false;
    }

    /// <summary>
    /// 从 Maintenance 文本里尽量读结束时间（ResumeAt / EndAt / ExpireAt）；单测也会直接调。
    /// </summary>
    /// <param name="maintenance">维护说明或 JSON</param>
    /// <param name="endAt">解析出的结束时间</param>
    /// <returns>解析成功为 true</returns>
    public static bool TryParseMaintenanceEnd(string maintenance, out DateTime endAt)
    {
        endAt = default;
        if (string.IsNullOrWhiteSpace(maintenance))
            return false;

        var raw = maintenance.Trim();
        if (raw.StartsWith("{", StringComparison.Ordinal))
        {
            try
            {
                using var doc = JsonDocument.Parse(raw);
                var root = doc.RootElement;
                foreach (var name in new[] { "ResumeAt", "EndAt", "ExpireAt", "resumeAt", "endAt", "expireAt" })
                {
                    if (!root.TryGetProperty(name, out var prop))
                        continue;
                    if (prop.ValueKind == JsonValueKind.String
                        && DateTime.TryParse(prop.GetString(), out endAt))
                        return true;
                    if (prop.ValueKind == JsonValueKind.Number
                        && prop.TryGetInt64(out var unix)
                        && unix > 1_000_000_000L)
                    {
                        endAt = DateTimeOffset.FromUnixTimeSeconds(unix).LocalDateTime;
                        return true;
                    }
                }
            }
            catch
            {
                // 不是合法 JSON，再试整段当时间
            }
        }

        return DateTime.TryParse(raw, out endAt);
    }
}
