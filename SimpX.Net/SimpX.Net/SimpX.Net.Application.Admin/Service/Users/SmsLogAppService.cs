namespace SimpX.Net.Application.Admin;

/// <summary>
/// 租户后台短信邮件日志（JWT）。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "smsLog", Order = 491, Description = "短信邮件")]
public class SmsLogAppService : IDynamicApiController, ITransient
{
    private readonly SmsLogService _svc;

    /// <summary>
    /// 注入。
    /// </summary>
    /// <param name="svc">短信日志</param>
    public SmsLogAppService(SmsLogService svc) => _svc = svc;

    /// <summary>分页。</summary>
    /// <param name="input">筛选</param>
    /// <returns>分页</returns>
    [ApiDescriptionSettings(Name = "Page"), HttpPost]
    [DisplayName("短信邮件日志分页")]
    public Task<SmsLogPageOutput> Page(SmsLogPageInput input) =>
        _svc.PageAsync(input ?? new SmsLogPageInput(), App.HttpContext?.RequestAborted ?? default);
}
