using System.Text.Json;
using SimpX.Net.Data.Entity.Reports;
using SimpX.Net.Data.Entity.Sites;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 纸飞机监控：对齐旧 TelegramJob（充值 / 投注阈值）。
/// </summary>
public interface ITelegramMonitorService
{
    /// <summary>
    /// 扫有 Telegram 配置的站点，best-effort 组装充值/投注监控消息。
    /// 默认只记日志不发 HTTP；投注路径缺游戏日汇总时 soft-fail。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>本轮摘要</returns>
    Task<TelegramMonitorResult> RunAsync(CancellationToken cancellationToken = default);
}

/// <summary>纸飞机监控一轮结果。</summary>
public sealed class TelegramMonitorResult
{
    /// <summary>扫到的站点数。</summary>
    public int Sites { get; set; }

    /// <summary>记入日志的消息条数。</summary>
    public int Logged { get; set; }

    /// <summary>摘要。</summary>
    public string Message { get; set; }
}

/// <summary>默认实现：Schedule 显式注册；不发真实 Telegram HTTP。</summary>
public sealed class TelegramMonitorService : ITelegramMonitorService
{
    /// <summary>充值资金类型（旧 MoneyType.Recharge）。</summary>
    public const int MoneyTypeRecharge = MoneyTypeNameResolver.MoneyTypeRecharge;

    private readonly ISqlSugarClient _db;
    private readonly ILogger<TelegramMonitorService> _logger;

    /// <summary>
    /// 注入业务库与日志。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="logger">日志</param>
    public TelegramMonitorService(IBizDbAccessor biz, ILogger<TelegramMonitorService> logger)
    {
        _db = biz.Db;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<TelegramMonitorResult> RunAsync(CancellationToken cancellationToken = default)
    {
        var configs = await _db.Queryable<SiteConfig>()
            .Where(c => c.TelegramString != null && c.TelegramString != "")
            .Select(c => new { c.SiteID, c.TelegramString })
            .ToListAsync(cancellationToken);

        var result = new TelegramMonitorResult { Sites = configs.Count };
        var today = DateTime.Today;
        foreach (var cfg in configs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                result.Logged += await LogRechargeAsync(cfg.SiteID, cfg.TelegramString, today, cancellationToken);
                // soft-fail：无游戏日汇总聚合时只记一条说明，不中断其它站。
                result.Logged += LogBetSoftFail(cfg.SiteID, cfg.TelegramString);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "纸飞机监控站失败 SiteId={SiteId}", cfg.SiteID);
            }
        }

        result.Message =
            $"纸飞机监控站 {result.Sites}，记日志消息 {result.Logged} 条（未发真实 HTTP；投注路径 soft-fail）";
        _logger.LogInformation("{Msg}", result.Message);
        return result;
    }

    /// <summary>充值监控：按日资金表扫达标会员，只记 would-send 载荷。</summary>
    private async Task<int> LogRechargeAsync(
        int siteId,
        string telegramJson,
        DateTime today,
        CancellationToken cancellationToken)
    {
        if (!TryReadChannel(telegramJson, "TelegramRecharge", "RechargeStatus", out var token, out var channel, out var title, out var thresholds)
            && !TryReadChannel(telegramJson, "recharge", "status", out token, out channel, out title, out thresholds))
            return 0;

        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(channel))
            return 0;

        if (thresholds.Count == 0)
            return 0;

        var rows = await _db.Queryable<UserDateMoney>()
            .Where(t => t.SiteID == siteId && t.Date == today && t.Type == MoneyTypeRecharge)
            .ToListAsync(cancellationToken);

        var logged = 0;
        foreach (var row in rows)
        {
            if (!thresholds.TryGetValue(row.Currency, out var min) || row.Money < min)
                continue;

            var payload =
                $"would-send Telegram recharge site={siteId} channel={channel} title={title} " +
                $"userId={row.UserID} currency={row.Currency} money={row.Money:F2} at={DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            _logger.LogInformation("{Payload}", payload);
            logged++;
        }

        return logged;
    }

    /// <summary>投注监控：缺游戏日汇总时 soft-fail。</summary>
    private int LogBetSoftFail(int siteId, string telegramJson)
    {
        if (!TryReadChannel(telegramJson, "TelegramBet", "BetStatus", out var token, out var channel, out var title, out _)
            && !TryReadChannel(telegramJson, "bet", "status", out token, out channel, out title, out _))
            return 0;

        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(channel))
            return 0;

        _logger.LogInformation(
            "would-send Telegram bet soft-fail site={SiteId} channel={Channel} title={Title} reason=缺少游戏日汇总聚合",
            siteId, channel, title);
        return 1;
    }

    /// <summary>
    /// 从 Telegram JSON 里尽量读某个频道：Token / Channel / Title / Value 阈值。
    /// </summary>
    public static bool TryReadChannel(
        string telegramJson,
        string sectionName,
        string statusName,
        out string token,
        out string channel,
        out string title,
        out Dictionary<int, decimal> thresholds)
    {
        token = null;
        channel = null;
        title = string.Empty;
        thresholds = new Dictionary<int, decimal>();
        if (string.IsNullOrWhiteSpace(telegramJson)) return false;

        try
        {
            using var doc = JsonDocument.Parse(telegramJson);
            var root = doc.RootElement;
            if (!root.TryGetProperty(sectionName, out var section)
                && !TryFindIgnoreCase(root, sectionName, out section))
                return false;

            if (!IsEnabled(section, statusName))
                return false;

            token = ReadString(section, "Token", "token");
            channel = ReadString(section, "Channel", "channel", "ChatId", "chatId");
            title = ReadString(section, "Title", "title") ?? string.Empty;
            thresholds = ReadThresholds(section);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>是否开启。</summary>
    private static bool IsEnabled(JsonElement section, string statusName)
    {
        foreach (var name in new[] { statusName, "Status", "status", "Enabled", "enabled" })
        {
            if (!section.TryGetProperty(name, out var prop)
                && !TryFindIgnoreCase(section, name, out prop))
                continue;
            if (prop.ValueKind == JsonValueKind.True) return true;
            if (prop.ValueKind == JsonValueKind.False) return false;
            if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out var n)) return n != 0;
            if (prop.ValueKind == JsonValueKind.String)
            {
                var s = prop.GetString();
                if (bool.TryParse(s, out var b)) return b;
                if (s == "1") return true;
            }
        }
        // 没有状态字段但有 Token/Channel 时也当作开启，方便宽松配置。
        return !string.IsNullOrWhiteSpace(ReadString(section, "Token", "token"))
            && !string.IsNullOrWhiteSpace(ReadString(section, "Channel", "channel"));
    }

    /// <summary>读币种阈值表 Value。</summary>
    private static Dictionary<int, decimal> ReadThresholds(JsonElement section)
    {
        var map = new Dictionary<int, decimal>();
        if (!section.TryGetProperty("Value", out var value)
            && !TryFindIgnoreCase(section, "Value", out value))
            return map;

        if (value.ValueKind == JsonValueKind.Object)
        {
            foreach (var p in value.EnumerateObject())
            {
                if (!int.TryParse(p.Name, out var currency)) continue;
                if (p.Value.ValueKind == JsonValueKind.Number && p.Value.TryGetDecimal(out var d))
                    map[currency] = d;
                else if (p.Value.ValueKind == JsonValueKind.String
                         && decimal.TryParse(p.Value.GetString(), out d))
                    map[currency] = d;
            }
        }

        return map;
    }

    /// <summary>读字符串。</summary>
    private static string ReadString(JsonElement el, params string[] names)
    {
        foreach (var name in names)
        {
            if (!el.TryGetProperty(name, out var prop) && !TryFindIgnoreCase(el, name, out prop))
                continue;
            if (prop.ValueKind == JsonValueKind.String) return prop.GetString();
            if (prop.ValueKind == JsonValueKind.Number) return prop.ToString();
        }
        return null;
    }

    /// <summary>忽略大小写找属性。</summary>
    private static bool TryFindIgnoreCase(JsonElement el, string name, out JsonElement prop)
    {
        prop = default;
        if (el.ValueKind != JsonValueKind.Object) return false;
        foreach (var p in el.EnumerateObject())
        {
            if (!string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)) continue;
            prop = p.Value;
            return true;
        }
        return false;
    }
}
