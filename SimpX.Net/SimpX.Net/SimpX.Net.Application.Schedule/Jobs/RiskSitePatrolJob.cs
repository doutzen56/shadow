using SimpX.Net.Data.Entity.Risk;
using SqlSugar;

namespace SimpX.Net.Application.Schedule;

/// <summary>
/// ???????????? SiteID ???????????????????????????????
/// ???? M-JOB J5??Schedule ???????????? SiteID??
/// </summary>
[JobDetail("job_risk_site_patrol", Description = "???????????", GroupName = "AutoCheck", Concurrent = false)]
[PeriodMinutes(30, TriggerId = "trigger_risk_site_patrol", Description = "?§³???????", RunOnStart = false)]
public class RiskSitePatrolJob : IJob
{
    private readonly ISqlSugarClient _db;
    private readonly ILogger<RiskSitePatrolJob> _logger;

    /// <summary>
    /// ???????????
    /// </summary>
    /// <param name="db">??</param>
    /// <param name="logger">???</param>
    public RiskSitePatrolJob(ISqlSugarClient db, ILogger<RiskSitePatrolJob> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// ????????? SiteID????? risk_UserDanger ??????????
    /// </summary>
    /// <param name="context">?????????</param>
    /// <param name="stoppingToken">???????</param>
    public async Task ExecuteAsync(JobExecutingContext context, CancellationToken stoppingToken)
    {
        // ????? Properties JSON ???? SiteID?????? {"SiteID":"1001"}
        var siteIdText = context.JobDetail?.GetProperty<string>("SiteID");
        if (!int.TryParse(siteIdText, out var siteId) || siteId <= 0)
        {
            var fail = "?????? SiteID ????";
            _logger.LogWarning("??????????{Msg}", fail);
            context.Result = fail;
            throw Oops.Oh(fail);
        }

        var day = DateTime.Now.Date;
        var count = await _db.Queryable<RiskUserDanger>()
            .Where(r => r.SiteID == siteId && r.Date >= day && r.Date < day.AddDays(1))
            .CountAsync(stoppingToken);

        var msg = $"SiteID={siteId} ???????????? {count} ??";
        _logger.LogInformation("??????{Msg}", msg);
        context.Result = msg;
    }
}
