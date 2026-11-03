namespace SimpX.Net.Application.Channels;

/// <summary>
/// 代付网关回调 HTTP 入口。对齐旧 /request/withdrawcallback/{channelId}；不用登录。
/// </summary>
[ApiDescriptionSettings(Module = "pay", Name = "withdrawNotify", Order = 102, Description = "代付网关回调")]
public class WithdrawNotifyAppService : IDynamicApiController, ITransient
{
    private readonly WithdrawNotifyService _svc;

    /// <summary>
    /// 注入回调服务。
    /// </summary>
    /// <param name="svc">代付回调</param>
    public WithdrawNotifyAppService(WithdrawNotifyService svc) => _svc = svc;

    /// <summary>
    /// 渠道回调。正文示例：orderId=1&amp;money=50&amp;success=true&amp;sign=STUB
    /// </summary>
    /// <param name="channelId">站点提现渠道编号</param>
    /// <returns>SUCCESS 或失败原因</returns>
    [AllowAnonymous]
    [HttpPost("{channelId:int}")]
    [HttpPost("/request/withdrawcallback/{channelId:int}")]
    [DisplayName("代付网关回调")]
    public async Task<string> Notify(int channelId)
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

        return await _svc.NotifyAsync(channelId, rawBody, clientIp, http.RequestAborted);
    }
}
