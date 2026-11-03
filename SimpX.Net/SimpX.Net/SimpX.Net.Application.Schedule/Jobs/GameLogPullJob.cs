namespace SimpX.Net.Application.Schedule;

/// <summary>
/// 游戏拉单骨架：按参数 SiteID 空跑一轮，不调厂商。
/// 对齐 M-JOB J2 / M-GAME G5；Schedule 不定站，必须带 SiteID。
/// </summary>
[JobDetail("job_game_log_pull", Description = "游戏拉单（按站）", GroupName = "DataDownload", Concurrent = false)]
[PeriodMinutes(5, TriggerId = "trigger_game_log_pull", Description = "每 5 分钟拉一轮", RunOnStart = false)]
public class GameLogPullJob : IJob
{
    private readonly ILogger<GameLogPullJob> _logger;

    /// <summary>
    /// 注入日志。
    /// </summary>
    /// <param name="logger">日志</param>
    public GameLogPullJob(ILogger<GameLogPullJob> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 读任务参数 SiteID；非法则失败；合法只写空跑结果，不调厂商。
    /// </summary>
    /// <param name="context">执行上下文</param>
    /// <param name="stoppingToken">取消令牌</param>
    public Task ExecuteAsync(JobExecutingContext context, CancellationToken stoppingToken)
    {
        // 在作业 Properties JSON 里配 SiteID，例如 {"SiteID":"1001"}
        var siteIdText = context.JobDetail?.GetProperty<string>("SiteID");
        if (!int.TryParse(siteIdText, out var siteId) || siteId <= 0)
        {
            var fail = "缺少或非法 SiteID 参数";
            _logger.LogWarning("游戏拉单失败：{Msg}", fail);
            context.Result = fail;
            throw Oops.Oh(fail);
        }

        var msg = $"空跑拉单 SiteID={siteId}";
        _logger.LogInformation("游戏拉单：{Msg}", msg);
        context.Result = msg;
        return Task.CompletedTask;
    }
}
