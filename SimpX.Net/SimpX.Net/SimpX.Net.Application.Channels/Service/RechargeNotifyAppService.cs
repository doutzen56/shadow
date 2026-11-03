namespace SimpX.Net.Application.Channels;

/// <summary>
/// 充值网关回调 HTTP 入口。对齐旧支付回调；含 IP 白名单与 Mock MD5 验签。
/// </summary>
[ApiDescriptionSettings(Module = "pay", Name = "rechargeNotify", Order = 101, Description = "充值网关回调")]
public class RechargeNotifyAppService : IDynamicApiController, ITransient
{
    private readonly RechargeNotifyService _svc;

    /// <summary>
    /// 注入回调服务。
    /// </summary>
    /// <param name="svc">回调入账</param>
    public RechargeNotifyAppService(RechargeNotifyService svc) => _svc = svc;

    /// <summary>
    /// 通道回调。正文示例：orderId=RO1&amp;systemId=GW1&amp;money=0.01&amp;success=true&amp;sign=...
    /// </summary>
    /// <param name="payId">站点支付通道编号</param>
    /// <returns>SUCCESS 或失败原因</returns>
    [AllowAnonymous]
    [HttpPost("{payId:int}")]
    [DisplayName("充值网关回调")]
    public async Task<string> Notify(int payId)
    {
        var http = App.HttpContext;
        using var reader = new StreamReader(http.Request.Body);
        var rawBody = await reader.ReadToEndAsync();
        if (string.IsNullOrWhiteSpace(rawBody) && http.Request.QueryString.HasValue)
            rawBody = http.Request.QueryString.Value!.TrimStart('?');

        http.Request.Headers.TryGetValue("X-Forwarded-For", out var xff);
        http.Request.Headers.TryGetValue("X-Real-IP", out var xreal);
        var clientIp = RechargeNotifySecurity.ResolveClientIp(
            xff.ToString(),
            xreal.ToString(),
            http.Connection.RemoteIpAddress?.ToString());

        return await _svc.NotifyAsync(payId, rawBody, clientIp, http.RequestAborted);
    }
}
