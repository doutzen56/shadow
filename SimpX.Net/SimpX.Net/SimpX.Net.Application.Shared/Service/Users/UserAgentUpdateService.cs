using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 代理层级变更后重算当月代理报表。对齐旧 UpdateUserAgentJob / UserAgentUpdateLogAgent。
/// </summary>
public interface IUserAgentUpdateService
{
    /// <summary>
    /// 扫本月未完成的 usr_AgentUpdateLog：按站点开放币种、从月初到今天逐日，
    /// 对旧/新上级并集调 rpt_AgentStatistics_Update，再把日志标成已完成。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>本轮处理的日志条数</returns>
    Task<int> ProcessPendingAsync(CancellationToken cancellationToken = default);
}

/// <summary>默认实现：Schedule 显式注册，不定站。</summary>
public sealed class UserAgentUpdateService : IUserAgentUpdateService
{
    private readonly ISqlSugarClient _db;
    private readonly ILogger<UserAgentUpdateService> _logger;

    /// <summary>
    /// 注入业务库与日志。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="logger">日志</param>
    public UserAgentUpdateService(IBizDbAccessor biz, ILogger<UserAgentUpdateService> logger)
    {
        _db = biz.Db;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<int> ProcessPendingAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.Now;
        var monthStart = new DateTime(now.Year, now.Month, 1);
        var today = now.Date;

        var logs = await _db.Queryable<UserAgentUpdateLog>()
            .Where(t => t.Status == false && t.CreateAt >= monthStart && t.CreateAt < now)
            .OrderBy(t => t.ID, OrderByType.Asc)
            .ToListAsync(cancellationToken);

        if (logs.Count == 0)
            return 0;

        var openCurrencies = await _db.Queryable<SiteCurrency>()
            .Where(t => t.IsOpen)
            .ToListAsync(cancellationToken);

        var done = 0;
        foreach (var log in logs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var siteCurrencies = openCurrencies.Where(c => c.SiteID == log.SiteID).ToList();
            foreach (var currency in siteCurrencies)
            {
                for (var day = monthStart; day <= today; day = day.AddDays(1))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await RunUpdateTaskAsync(
                        log.SiteID,
                        currency.Currency,
                        log.OldAgentParents,
                        log.NewAgentParents,
                        day,
                        cancellationToken);
                    _logger.LogInformation(
                        "{Date}-{SiteId}-{Currency}-{OldParents}-{NewParents}-run time {Now}",
                        day.ToString("yyyy-MM-dd"),
                        log.SiteID,
                        currency.Currency,
                        log.OldAgentParents,
                        log.NewAgentParents,
                        DateTime.Now);
                }
            }

            log.Status = true;
            log.FinishAt = DateTime.Now;
            await _db.Updateable(log)
                .UpdateColumns(t => new { t.Status, t.FinishAt })
                .ExecuteCommandAsync(cancellationToken);
            done++;
        }

        _logger.LogInformation("[{Now}]更新用户代理层级完成,本轮 {Count} 条", DateTime.Now, done);
        return done;
    }

    /// <summary>
    /// 对旧/新上级并集逐个调 rpt_AgentStatistics_Update（对齐旧 RunUpdateTask）。
    /// </summary>
    private async Task RunUpdateTaskAsync(
        int siteId,
        int currency,
        string oldAgentParents,
        string newAgentParents,
        DateTime date,
        CancellationToken cancellationToken)
    {
        var parents = ParseParentIds(oldAgentParents)
            .Union(ParseParentIds(newAgentParents))
            .Distinct()
            .ToList();

        foreach (var userId in parents)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await _db.Ado.UseStoredProcedure().ExecuteCommandAsync(
                    "rpt_AgentStatistics_Update",
                    new SugarParameter("@Date", date.Date),
                    new SugarParameter("@Currency", currency),
                    new SugarParameter("@UserID", userId),
                    new SugarParameter("@SiteID", siteId));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    "rpt_AgentStatistics_Update 失败 Date={Date} Currency={Currency} UserID={User} SiteID={Site}: {Msg}",
                    date.Date,
                    currency,
                    userId,
                    siteId,
                    ex.Message);
            }
        }
    }

    /// <summary>把逗号分隔的上级编号解析成整数列表；空段跳过。</summary>
    private static IEnumerable<int> ParseParentIds(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            yield break;

        foreach (var part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (int.TryParse(part, out var id) && id > 0)
                yield return id;
        }
    }
}
