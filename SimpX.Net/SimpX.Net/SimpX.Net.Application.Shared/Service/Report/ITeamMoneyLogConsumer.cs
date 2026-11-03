namespace SimpX.Net.Application.Shared;

/// <summary>
/// 消费 MONEY_LOG：团队账变统计。对齐旧 TeamReportAgent.SaveTeamMoneyLog。
/// </summary>
public interface ITeamMoneyLogConsumer
{
    /// <summary>
    /// 按账变流水号更新代理团队资金统计（存储过程 rpt_AgentStatistics_Money）。
    /// </summary>
    /// <param name="logId">账变流水号</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task ConsumeAsync(int logId, CancellationToken cancellationToken = default);
}
