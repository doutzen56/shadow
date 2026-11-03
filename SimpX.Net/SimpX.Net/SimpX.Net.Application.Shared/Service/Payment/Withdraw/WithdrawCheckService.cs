using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 付款中提现订单定时核对。对齐旧 WithdrawOrderAgent.WithdrawCheckforService。
/// </summary>
public interface IWithdrawCheckService
{
    /// <summary>
    /// 扫描 Status=InPayment 订单：入款日志满 120 秒后查单；
    /// Failed→回 Wait；Success→ConfirmGateway；Paymenting→继续轮询。
    /// </summary>
    /// <param name="maxCount">本轮最多处理条数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>本轮扫到的付款中订单数（含跳过）</returns>
    Task<int> CheckInPaymentAsync(int maxCount = 200, CancellationToken cancellationToken = default);
}

/// <summary>默认实现：全站扫付款中（Schedule 不定站）。</summary>
public sealed class WithdrawCheckService : ITransient, IWithdrawCheckService
{
    /// <summary>旧逻辑：付款中日志未满 120 秒不查。</summary>
    public const int MinSecondsAfterInPayment = 120;

    private readonly ISqlSugarClient _db;
    private readonly IWithdrawGateway _gateway;
    private readonly WithdrawOrderOperateService _operate;
    private readonly ILogger<WithdrawCheckService> _logger;

    public WithdrawCheckService(
        IBizDbAccessor biz,
        IWithdrawGateway gateway,
        WithdrawOrderOperateService operate,
        ILogger<WithdrawCheckService> logger)
    {
        _db = biz.Db;
        _gateway = gateway;
        _operate = operate;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<int> CheckInPaymentAsync(int maxCount = 200, CancellationToken cancellationToken = default)
    {
        maxCount = Math.Clamp(maxCount <= 0 ? 200 : maxCount, 1, 500);
        var orders = await _db.Queryable<WithdrawOrder>()
            .Where(o => o.Status == WithdrawOrderOperateService.StatusInPayment)
            .OrderBy(o => o.ID, OrderByType.Asc)
            .Take(maxCount)
            .ToListAsync(cancellationToken);

        var scanned = orders.Count;
        foreach (var order in orders)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await CheckOneAsync(order, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "【站点: {SiteId}】【订单号: {OrderId}】【渠道号: {ChannelId}】,错误信息: {Msg}",
                    order.SiteID,
                    order.ID,
                    order.ChannelID,
                    ex.Message);
            }
        }

        return scanned;
    }

    private async Task CheckOneAsync(WithdrawOrder order, CancellationToken cancellationToken)
    {
        if (order.ChannelID <= 0)
            throw new InvalidOperationException("ChannelID=0");

        // 对齐旧：取付款中日志最早一条，未满 120s 跳过
        var logRows = await _db.Queryable<WithdrawOrderLog>()
            .Where(l => l.OrderID == order.ID && l.Status == WithdrawOrderOperateService.StatusInPayment)
            .OrderBy(l => l.CreateAt, OrderByType.Asc)
            .Take(1)
            .ToListAsync(cancellationToken);
        var payLog = logRows.FirstOrDefault();
        if (payLog == null
            || DateTime.Now.Subtract(payLog.CreateAt).TotalSeconds < MinSecondsAfterInPayment
            || order.Status != WithdrawOrderOperateService.StatusInPayment)
            return;

        var channels = await _db.Queryable<WithdrawChannel>()
            .Where(c => c.SiteID == order.SiteID && c.ID == order.ChannelID)
            .Take(1)
            .ToListAsync(cancellationToken);
        var channel = channels.FirstOrDefault()
            ?? throw new InvalidOperationException(
                $"【站点: {order.SiteID}】【订单号: {order.ID}】【渠道号: {order.ChannelID}】,错误信息: 系统配置不存在！");

        // 网关不填写不执行（对齐旧 GateWay 空跳过）
        if (!HasGatewayConfig(channel))
            return;

        var money = order.Paid > 0 ? order.Paid : order.Money - order.Fee;
        var result = await _gateway.QueryAsync(
            new WithdrawGatewayQueryRequest
            {
                ChannelID = order.ChannelID,
                OrderID = order.ID,
                Money = money,
                SystemID = order.SystemID ?? string.Empty,
                SysID = channel.SysID,
                ChannelSetting = channel.Setting,
                ChannelQuerySetting = channel.QuerySetting
            },
            cancellationToken);

        switch (result.Status)
        {
            case WithdrawGatewayStatus.Failed:
                if (string.IsNullOrWhiteSpace(result.Message))
                    throw new InvalidOperationException(
                        $"【站点: {order.SiteID}】【订单号: {order.ID}】【渠道号: {order.ChannelID}】,错误信息: #未知错误#");
                await _operate.FailGatewayAsync(
                    order.ID,
                    adminId: 0,
                    reason: result.Message,
                    cancellationToken,
                    siteIdOverride: order.SiteID);
                throw new InvalidOperationException(
                    $"【站点: {order.SiteID}】【订单号: {order.ID}】【渠道号: {order.ChannelID}】,错误信息: {result.Message}");

            case WithdrawGatewayStatus.Paymenting:
            case WithdrawGatewayStatus.RepeatOrder:
                throw new InvalidOperationException(
                    $"【站点: {order.SiteID}】【订单号: {order.ID}】【渠道号: {order.ChannelID}】,错误信息: {result.Message}");

            case WithdrawGatewayStatus.Success:
                await _operate.ConfirmGatewayAsync(
                    order.ID,
                    adminId: 0,
                    cancellationToken,
                    siteIdOverride: order.SiteID);
                _logger.LogInformation(
                    "提现订单 {OrderId} 核对成功,已成功到账 SiteID={SiteId}",
                    order.ID,
                    order.SiteID);
                break;
        }
    }

    /// <summary>渠道是否配置了可查单/可提交网关地址。</summary>
    private static bool HasGatewayConfig(WithdrawChannel channel)
    {
        var raw = string.IsNullOrWhiteSpace(channel.QuerySetting) ? channel.Setting : channel.QuerySetting;
        if (string.IsNullOrWhiteSpace(raw))
            return false;
        // JSON 含 Gateway / QueryGateway / gateway 任一非空即认为有网关
        var lower = raw.ToLowerInvariant();
        return lower.Contains("gateway") || lower.Contains("url");
    }
}
