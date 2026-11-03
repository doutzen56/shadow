using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 充值网关回调入账。对齐旧 PaymentAgent.ShowCallback：IP → 插件验签解析 → CheckRechargeOrder。
/// 插件：mock（默认）、http-md5（Setting.Plugin）。
/// </summary>
public sealed class RechargeNotifyService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly RechargeOrderCheckService _check;
    private readonly RechargeNotifyPluginRouter _plugins;

    /// <summary>
    /// 注入库、到账、回调插件路由。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="check">确认到账</param>
    /// <param name="plugins">回调插件路由</param>
    public RechargeNotifyService(
        IBizDbAccessor biz,
        RechargeOrderCheckService check,
        RechargeNotifyPluginRouter plugins)
    {
        _db = biz.Db;
        _check = check;
        _plugins = plugins;
    }

    /// <summary>
    /// 处理回调。成功返回 SUCCESS；失败返回原因串。
    /// </summary>
    /// <param name="payId">站点支付通道编号</param>
    /// <param name="rawBody">原始正文</param>
    /// <param name="clientIp">客户端 IP</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>应答正文</returns>
    public async Task<string> NotifyAsync(
        int payId,
        string rawBody,
        string clientIp = null,
        CancellationToken cancellationToken = default)
    {
        if (payId <= 0)
            return "未找到支付ID";

        var payments = await _db.Queryable<SitePayment>()
            .Where(p => p.ID == payId && !p.IsDeleted)
            .Take(1)
            .ToListAsync(cancellationToken);
        var payment = payments.FirstOrDefault();
        if (payment == null)
            return $"未找到支付ID:{payId}";

        var allowIps = await RechargeNotifySecurity.LoadCallbackIpsAsync(_db, payment, cancellationToken);
        if (!RechargeNotifySecurity.IsIpAllowed(allowIps, clientIp))
            return $"非法回调(IP:{clientIp ?? string.Empty})";

        var parsed = _plugins.ParseAndVerify(rawBody ?? string.Empty, payment);
        if (!parsed.Success)
            return string.IsNullOrEmpty(parsed.FailMessage) ? "回调失败" : parsed.FailMessage;
        if (string.IsNullOrWhiteSpace(parsed.OrderId))
            return "#订单编号错误#";
        if (string.IsNullOrWhiteSpace(parsed.SystemId))
            return "#网关编号错误#";
        if (parsed.Money <= 0)
            return "#充值金额错误#";

        var orders = await _db.Queryable<RechargeOrder>()
            .Where(o => o.ID == parsed.OrderId.Trim())
            .Take(1)
            .ToListAsync(cancellationToken);
        var order = orders.FirstOrDefault();
        if (order == null)
            return "#充值编号错误#";
        if (order.SiteID != payment.SiteID)
            return "#充值编号错误#";
        if (order.PayID != 0 && order.PayID != payId)
            return "#支付渠道不匹配#";

        var fee = SitePaymentFeeCalculator.GetFee(payment, parsed.Money);
        var cost = SitePaymentFeeCalculator.GetCost(payment, parsed.Money);

        try
        {
            await _check.CheckAsync(
                new RechargeOrderCheckInput
                {
                    ID = order.ID,
                    SystemID = parsed.SystemId.Trim(),
                    Paid = parsed.Money,
                    Fee = fee,
                    Cost = cost,
                    PayAccount = parsed.PayAccount,
                    Description = "gateway-notify"
                },
                adminId: 0,
                expectedPayId: payId,
                cancellationToken: cancellationToken,
                siteIdOverride: order.SiteID);
            return "SUCCESS";
        }
        catch (Exception ex)
        {
            var msg = ex.Message;
            if (string.IsNullOrWhiteSpace(msg))
                msg = "#充值入账失败#";
            return msg;
        }
    }
}
