using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 充值到账飞机通知。对齐旧 TelegramAgent.SendAsyncByInterval / SendAsyncByFRecharge。
/// 真实 Bot 配置未迁前可挂空实现，只保留调用点。
/// </summary>
public interface IRechargeTelegramNotifier
{
    /// <summary>
    /// 普通到账提醒（带间隔节流，由实现自己决定）。
    /// </summary>
    /// <param name="order">已入账订单</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task NotifyCreditedAsync(RechargeOrder order, CancellationToken cancellationToken = default);

    /// <summary>
    /// 首充提醒。
    /// </summary>
    /// <param name="order">已入账订单</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task NotifyFirstRechargeAsync(RechargeOrder order, CancellationToken cancellationToken = default);
}
