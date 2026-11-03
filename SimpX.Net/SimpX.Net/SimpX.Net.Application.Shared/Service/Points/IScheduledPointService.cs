namespace SimpX.Net.Application.Shared;

/// <summary>
/// 定时积分。对齐旧 PointsAgent.BirthdayPoint / BetPoint。
/// </summary>
public interface IScheduledPointService
{
    /// <summary>
    /// 当天生日会员按站点积分项发固定生日积分。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <param name="now">当前时间；测试可注入</param>
    /// <returns>发放人数</returns>
    Task<int> GrantBirthdayPointsAsync(CancellationToken cancellationToken = default, DateTime? now = null);

    /// <summary>
    /// 按业务日投注汇总发投注积分（默认昨日）。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <param name="now">当前时间；测试可注入</param>
    /// <returns>发放人数</returns>
    Task<int> GrantBettingPointsAsync(CancellationToken cancellationToken = default, DateTime? now = null);
}
