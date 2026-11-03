using SimpX.Net.Data.Entity.Sites;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 代付网关匿名回调。对齐旧 WithdrawAgent.ShowCallback：按渠道定站 → 验签 → 完结付款中订单。
/// </summary>
public sealed class WithdrawNotifyService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly WithdrawOrderOperateService _operate;

    /// <summary>
    /// 注入业务库与提现完结服务。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="operate">提现完结</param>
    public WithdrawNotifyService(IBizDbAccessor biz, WithdrawOrderOperateService operate)
    {
        _db = biz.Db;
        _operate = operate;
    }

    /// <summary>
    /// 处理回调。成功返回 SUCCESS；失败返回原因串。
    /// </summary>
    /// <param name="channelId">站点提现渠道编号</param>
    /// <param name="rawBody">原始正文（表单或 query）</param>
    /// <param name="clientIp">客户端 IP（预留；本波 Stub 不校验白名单）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>应答正文</returns>
    public async Task<string> NotifyAsync(
        int channelId,
        string rawBody,
        string clientIp = null,
        CancellationToken cancellationToken = default)
    {
        _ = clientIp;
        if (channelId <= 0)
            return "未找到渠道ID";

        var channels = await _db.Queryable<WithdrawChannel>()
            .Where(c => c.ID == channelId && !c.IsDeleted)
            .Take(1)
            .ToListAsync(cancellationToken);
        var channel = channels.FirstOrDefault();
        if (channel == null)
            return $"未找到渠道ID:{channelId}";

        var raw = ParseBody(rawBody ?? string.Empty, channelId);
        try
        {
            await _operate.HandleGatewayCallbackAsync(
                raw,
                adminId: 0,
                cancellationToken: cancellationToken,
                siteIdOverride: channel.SiteID);
            return "SUCCESS";
        }
        catch (Exception ex)
        {
            var msg = ex.Message;
            if (string.IsNullOrWhiteSpace(msg))
                msg = "#代付回调失败#";
            return msg;
        }
    }

    /// <summary>
    /// 解析表单正文为回调包（供单测与 Stub 验签）。字段：orderId/money/sign/success|status。
    /// </summary>
    /// <param name="rawBody">正文</param>
    /// <param name="channelId">渠道号（写入 ChannelID）</param>
    /// <returns>回调原文包</returns>
    public static WithdrawGatewayCallbackRaw ParseBodyPublic(string rawBody, int channelId) =>
        ParseBody(rawBody, channelId);

    /// <summary>
    /// 解析表单。success=false 时 Status 写成 fail，交给 Stub 判失败。
    /// </summary>
    internal static WithdrawGatewayCallbackRaw ParseBody(string rawBody, int channelId)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var text = (rawBody ?? string.Empty).Trim();
        if (text.StartsWith('?'))
            text = text[1..];
        foreach (var part in text.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = part.Split('=', 2);
            if (kv.Length != 2)
                continue;
            map[Uri.UnescapeDataString(kv[0])] = Uri.UnescapeDataString(kv[1]);
        }

        string Pick(params string[] keys)
        {
            foreach (var k in keys)
            {
                if (map.TryGetValue(k, out var v) && !string.IsNullOrWhiteSpace(v))
                    return v;
            }
            return string.Empty;
        }

        var status = Pick("status", "Status");
        if (string.IsNullOrWhiteSpace(status) && map.TryGetValue("success", out var successRaw))
        {
            var ok = successRaw.Equals("true", StringComparison.OrdinalIgnoreCase) || successRaw == "1";
            status = ok ? "success" : "fail";
        }

        if (string.IsNullOrWhiteSpace(status))
            status = "success";

        return new WithdrawGatewayCallbackRaw
        {
            ChannelID = channelId,
            OrderID = Pick("orderId", "OrderID", "ID", "id", "order_no"),
            Money = Pick("money", "Money", "Paid", "paid", "amount"),
            Sign = Pick("sign", "Sign"),
            Status = status,
            Payload = rawBody ?? string.Empty
        };
    }
}
