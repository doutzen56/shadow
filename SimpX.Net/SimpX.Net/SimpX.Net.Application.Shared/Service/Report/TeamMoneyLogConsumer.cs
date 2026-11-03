namespace SimpX.Net.Application.Shared;

/// <summary>
/// 调用 rpt_AgentStatistics_Money。SQLite/无存储过程环境会记警告并吞掉，避免拖垮 Job。
/// </summary>
public sealed class TeamMoneyLogConsumer : ITransient, ITeamMoneyLogConsumer
{
    private readonly ISqlSugarClient _db;

    /// <summary>
    /// 注入业务库。
    /// </summary>
    /// <param name="biz">业务库</param>
    public TeamMoneyLogConsumer(IBizDbAccessor biz)
    {
        _db = biz.Db;
    }

    /// <inheritdoc />
    public async Task ConsumeAsync(int logId, CancellationToken cancellationToken = default)
    {
        if (logId <= 0)
            return;
        try
        {
            await _db.Ado.ExecuteCommandAsync(
                "EXEC rpt_AgentStatistics_Money @LogID",
                new List<SugarParameter> { new("@LogID", logId) });
        }
        catch (Exception ex)
        {
            // 测试库 / 未部署 SP 时不能让队列 Job 整轮失败。
            try
            {
                Log.Warning($"团队账变统计失败 LogID={logId}：{ex.Message}");
            }
            catch
            {
                // 单测无 Furion 宿主时忽略日志
            }
        }
    }
}
