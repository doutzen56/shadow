namespace SimpX.Net.Application.Shared;

/// <summary>
/// 充值活动参与日志写入。对齐旧 ResetOrderAddEvent / FirstOrderAddEvent。
/// 覆盖 EventType.Recharge / RechargeByLevel / FirstRecharge 的待审日志；审过加款见 EventLogCheckService。
/// </summary>
public interface IRechargePromoGrantService
{
    /// <summary>
    /// 按订单号尝试写入活动参与日志；已参与或 ActiveID 为 0 则跳过。
    /// </summary>
    /// <param name="orderId">充值订单号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>新写入的日志条数</returns>
    Task<int> GrantForOrderAsync(string orderId, CancellationToken cancellationToken = default);
}
