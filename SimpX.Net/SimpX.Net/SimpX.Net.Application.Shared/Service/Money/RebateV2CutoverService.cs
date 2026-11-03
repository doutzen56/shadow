using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 自助反水 V2 到点切换：对齐旧 SiteAgent.ActivateAutoReturnV2BySchedule。
/// </summary>
public interface IRebateV2CutoverService
{
    /// <summary>
    /// 扫描全部站点，把预约到期的站切到 V2（幂等）；返回本轮新激活站点数。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>本轮新激活站点数</returns>
    Task<int> ActivateDueSitesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// 默认实现。Schedule 显式注册；总开关读 ScheduleHost / AppSettings。
/// </summary>
public sealed class RebateV2CutoverService : IRebateV2CutoverService
{
    private readonly ISqlSugarClient _db;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RebateV2CutoverService> _logger;

    /// <summary>
    /// 注入业务库、配置与日志。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="configuration">配置（可读总开关）</param>
    /// <param name="logger">日志</param>
    public RebateV2CutoverService(
        IBizDbAccessor biz,
        IConfiguration configuration = null,
        ILogger<RebateV2CutoverService> logger = null)
    {
        _db = biz.Db;
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<int> ActivateDueSitesAsync(CancellationToken cancellationToken = default)
    {
        if (!IsRuntimeEnabled(_configuration))
        {
            _logger?.LogDebug("AutoReturnV2RuntimeEnabled=false，跳过切换扫描");
            return 0;
        }

        var now = DateTime.Now;
        List<Site> sites;
        try
        {
            sites = await _db.Queryable<Site>().ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "读取 Site 失败，跳过 V2 切换");
            return 0;
        }

        var activated = 0;
        foreach (var site in sites)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (site == null) continue;

            var map = SiteSettingQuery.Parse(site.SettingString ?? string.Empty);
            if (!TryApplyCutover(map, now, out var watermarkAt, out var didActivate))
                continue;

            site.SettingString = SiteSettingQuery.ToQueryString(map);
            try
            {
                var n = await _db.Updateable(site)
                    .UpdateColumns(s => new { s.SettingString })
                    .Where(s => s.ID == site.ID)
                    .ExecuteCommandAsync(cancellationToken);
                if (n != 1)
                    continue;

                if (watermarkAt.HasValue)
                    await EnsureSyncWatermarkAsync(watermarkAt.Value, cancellationToken);

                if (didActivate)
                {
                    activated++;
                    _logger?.LogInformation(
                        "站点 {SiteId} 自助反水 V2 已到点启用 EffectiveAt={At}",
                        site.ID,
                        watermarkAt);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "站点 {SiteId} V2 切换写库失败", site.ID);
            }
        }

        return activated;
    }

    /// <summary>
    /// 幂等切换规则：已开未锁则补锁；预约到期则打开并锁定。
    /// </summary>
    /// <param name="setting">站点 Setting 键值（就地改）</param>
    /// <param name="now">当前业务本地时间</param>
    /// <param name="watermarkAt">需要初始化同步水位的生效时间；无需则为 null</param>
    /// <param name="didActivate">本轮是否从预约态切到已启用</param>
    /// <returns>是否需要写回 SettingString</returns>
    public static bool TryApplyCutover(
        IDictionary<string, string> setting,
        DateTime now,
        out DateTime? watermarkAt,
        out bool didActivate)
    {
        watermarkAt = null;
        didActivate = false;
        if (setting == null)
            return false;

        var isAuto = SiteSettingQuery.GetBool(setting, "IsAutoReturn");
        var locked = SiteSettingQuery.GetBool(setting, "AutoReturnV2Locked");
        setting.TryGetValue("AutoReturnV2EffectiveAt", out var effectiveRaw);
        var effectiveAt = TryParseEffectiveAt(effectiveRaw);

        if (isAuto && !locked)
        {
            setting["AutoReturnV2Locked"] = bool.TrueString;
            if (!effectiveAt.HasValue)
            {
                effectiveAt = now.Date;
                setting["AutoReturnV2EffectiveAt"] = FormatEffectiveAt(effectiveAt.Value);
            }
            watermarkAt = effectiveAt;
            return true;
        }

        if (!isAuto && effectiveAt.HasValue && effectiveAt.Value <= now)
        {
            setting["IsAutoReturn"] = bool.TrueString;
            setting["AutoReturnV2Locked"] = bool.TrueString;
            // 保留 AutoReturnV2EffectiveAt 作为 V2 起始水位，不清空
            watermarkAt = effectiveAt;
            didActivate = true;
            return true;
        }

        return false;
    }

    /// <summary>
    /// 总开关：ScheduleHost:AutoReturnV2RuntimeEnabled 或 AppSettings:AutoReturnV2RuntimeEnabled。
    /// </summary>
    /// <param name="configuration">配置</param>
    /// <returns>是否允许运行时切 V2</returns>
    public static bool IsRuntimeEnabled(IConfiguration configuration)
    {
        if (configuration != null)
        {
            var host = configuration["ScheduleHost:AutoReturnV2RuntimeEnabled"];
            if (IsTruthy(host))
                return true;
            var app = configuration["AppSettings:AutoReturnV2RuntimeEnabled"];
            if (IsTruthy(app))
                return true;
            return false;
        }

        try
        {
            if (App.GetConfig<bool?>("ScheduleHost:AutoReturnV2RuntimeEnabled") == true)
                return true;
            return App.GetConfig<bool>("AppSettings:AutoReturnV2RuntimeEnabled", false);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>解析生效时间；失败返回 null。</summary>
    public static DateTime? TryParseEffectiveAt(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;
        if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var at))
            return at;
        if (DateTime.TryParse(raw, out at))
            return at;
        return null;
    }

    /// <summary>生效时间写回格式，对齐 SiteInfoService。</summary>
    public static string FormatEffectiveAt(DateTime at) =>
        at.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

    private static bool IsTruthy(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return false;
        raw = raw.Trim();
        return string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase) || raw == "1";
    }

    /// <summary>
    /// 全局同步水位：表不存在或已有有效水位则跳过；无行时写入生效点转 UTC。
    /// </summary>
    private async Task EnsureSyncWatermarkAsync(DateTime effectiveAtLocal, CancellationToken cancellationToken)
    {
        try
        {
            var rows = await _db.Queryable<UserRebateV2SyncMark>()
                .Where(t => t.ID == 1)
                .Take(1)
                .ToListAsync(cancellationToken);
            var existing = rows.FirstOrDefault();
            if (existing != null && existing.LastDownloadTime > DateTime.MinValue.AddYears(1))
                return;

            var markUtc = DateTime.SpecifyKind(effectiveAtLocal, DateTimeKind.Local).ToUniversalTime();
            if (existing == null)
            {
                await _db.Insertable(new UserRebateV2SyncMark
                {
                    ID = 1,
                    LastDownloadTime = markUtc,
                    UpdateAt = DateTime.Now
                }).ExecuteCommandAsync(cancellationToken);
            }
            else
            {
                existing.LastDownloadTime = markUtc;
                existing.UpdateAt = DateTime.Now;
                await _db.Updateable(existing)
                    .UpdateColumns(t => new { t.LastDownloadTime, t.UpdateAt })
                    .Where(t => t.ID == 1)
                    .ExecuteCommandAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "初始化 usr_RebateV2SyncMark 失败（表可能未建）");
        }
    }
}
