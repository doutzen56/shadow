using SimpX.Net.Data.Entity.Sites;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 自动爆奖。对齐旧 JackpotJob → JackpotAgent.CreateJackpot（壳：诚实部分实现）。
/// </summary>
public interface IJackpotAutoService
{
    /// <summary>
    /// 扫正常站 + Jackpot 配置；ES 未配则跳过；有 ES 时尝试 top-N，否则空跑说明依赖游戏拉取。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>本轮摘要</returns>
    Task<JackpotAutoResult> RunAsync(CancellationToken cancellationToken = default);
}

/// <summary>从 gamelogv2* 取站点爆奖候选（可选，Schedule 侧注入 ES 实现）。</summary>
public interface IJackpotEsTopQuery
{
    /// <summary>
    /// 按站点与时间窗取 Money 最高的若干条。
    /// </summary>
    /// <param name="siteId">站点</param>
    /// <param name="hours">回溯小时</param>
    /// <param name="topRank">条数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>查询结果</returns>
    Task<JackpotEsTopResult> QueryTopAsync(
        int siteId,
        int hours,
        int topRank,
        CancellationToken cancellationToken = default);
}

/// <summary>ES top-N 查询结果。</summary>
public sealed class JackpotEsTopResult
{
    /// <summary>业务 ES 未配置。</summary>
    public bool NotConfigured { get; set; }

    /// <summary>查询失败。</summary>
    public bool QueryFailed { get; set; }

    /// <summary>命中条数。</summary>
    public int HitCount { get; set; }

    /// <summary>说明。</summary>
    public string Message { get; set; }
}

/// <summary>自动爆奖一轮结果。</summary>
public sealed class JackpotAutoResult
{
    /// <summary>候选站点数。</summary>
    public int CandidateSites { get; set; }

    /// <summary>因 ES 未配跳过。</summary>
    public int SkippedNoEs { get; set; }

    /// <summary>有 ES 命中的站。</summary>
    public int QueriedWithHits { get; set; }

    /// <summary>有 ES 但空结果 / 失败的站。</summary>
    public int QueriedEmpty { get; set; }

    /// <summary>摘要。</summary>
    public string Message { get; set; }
}

/// <summary>默认实现：Schedule 显式注册；写站奖池留到有完整游戏目录映射后再接。</summary>
public sealed class JackpotAutoService : IJackpotAutoService
{
    /// <summary>站点正常状态。</summary>
    public const int SiteStatusNormal = 0;

    private readonly ISqlSugarClient _db;
    private readonly IJackpotEsTopQuery _es;
    private readonly ILogger<JackpotAutoService> _logger;

    /// <summary>
    /// 注入业务库、可选 ES top 查询与日志。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="es">ES top 查询；可空</param>
    /// <param name="logger">日志</param>
    public JackpotAutoService(
        IBizDbAccessor biz,
        IJackpotEsTopQuery es = null,
        ILogger<JackpotAutoService> logger = null)
    {
        _db = biz.Db;
        _es = es;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<JackpotAutoResult> RunAsync(CancellationToken cancellationToken = default)
    {
        var result = new JackpotAutoResult();
        var sites = await _db.Queryable<Site>()
            .Where(s => s.Status == SiteStatusNormal)
            .Select(s => s.ID)
            .ToListAsync(cancellationToken);
        if (sites.Count == 0)
        {
            result.Message = "自动爆奖：无正常站点";
            return result;
        }

        var configs = await _db.Queryable<SiteConfig>()
            .Where(c => sites.Contains(c.SiteID))
            .Where(c => c.JackpotSettingString != null && c.JackpotSettingString != "")
            .Select(c => new { c.SiteID, c.JackpotSettingString })
            .ToListAsync(cancellationToken);

        foreach (var cfg in configs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!TryReadJackpotSetting(cfg.JackpotSettingString, out var hours, out var topRank))
                continue;

            result.CandidateSites++;
            if (_es == null)
            {
                result.SkippedNoEs++;
                continue;
            }

            try
            {
                var esResult = await _es.QueryTopAsync(cfg.SiteID, hours, topRank, cancellationToken);
                if (esResult == null || esResult.NotConfigured)
                {
                    result.SkippedNoEs++;
                    continue;
                }

                if (esResult.QueryFailed || esResult.HitCount <= 0)
                {
                    result.QueriedEmpty++;
                    _logger?.LogInformation(
                        "自动爆奖空跑 SiteId={SiteId}：{Msg}（需要游戏拉取 / 注单进 ES）",
                        cfg.SiteID,
                        esResult.Message ?? "无命中");
                    continue;
                }

                result.QueriedWithHits++;
                // 残留：完整 WriteJackpotToDB（币种换算、目录映射、站点缓存）未移植；有命中只记日志不假成功写库。
                _logger?.LogInformation(
                    "自动爆奖 ES 命中 SiteId={SiteId} hits={Hits}（残留：未写 site_Jackpot / 缓存）",
                    cfg.SiteID, esResult.HitCount);
            }
            catch (Exception ex)
            {
                result.QueriedEmpty++;
                _logger?.LogWarning(ex, "自动爆奖站点失败 SiteId={SiteId}", cfg.SiteID);
            }
        }

        if (result.CandidateSites == 0)
        {
            result.Message = "自动爆奖：无有效 Jackpot 配置站";
            return result;
        }

        if (result.SkippedNoEs == result.CandidateSites)
        {
            result.Message =
                $"自动爆奖：候选 {result.CandidateSites} 站均跳过（业务 ES 未配置）；需要游戏拉取 / ElasticSearch:Business";
            return result;
        }

        result.Message =
            $"自动爆奖：候选 {result.CandidateSites}，ES 命中 {result.QueriedWithHits}，空跑/失败 {result.QueriedEmpty}，无 ES 跳过 {result.SkippedNoEs}" +
            "（残留：未完整写 site_Jackpot）";
        return result;
    }

    /// <summary>
    /// 从 Jackpot 设置读 TimeSpan / TopRank；两者都 &gt;0 才有效。
    /// </summary>
    /// <param name="raw">Jackpot 设置原文</param>
    /// <param name="hours">回溯小时</param>
    /// <param name="topRank">排名条数</param>
    /// <returns>有效为 true</returns>
    public static bool TryReadJackpotSetting(string raw, out int hours, out int topRank)
    {
        hours = 0;
        topRank = 0;
        var map = SiteSettingQuery.Parse(raw ?? string.Empty);
        if (!int.TryParse(SiteSettingQuery.Get(map, "TimeSpan"), out hours) || hours < 1)
            return false;
        if (!int.TryParse(SiteSettingQuery.Get(map, "TopRank"), out topRank) || topRank < 1)
            return false;
        return true;
    }
}
