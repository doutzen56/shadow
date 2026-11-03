namespace SimpX.Net.Application.SuperAdmin;

/// <summary>
/// 总后台充值接口列表（JWT，不定站）。对齐旧 Web.System /setting/payment/ 列表主能力。
/// </summary>
[ApiDescriptionSettings(Module = "platform", Name = "systemPayment", Order = 473, Description = "充值接口")]
public class PlatformSystemPaymentAppService : IDynamicApiController, ITransient
{
    private readonly SystemPaymentService _svc;

    /// <summary>
    /// 注入服务。
    /// </summary>
    /// <param name="svc">服务</param>
    public PlatformSystemPaymentAppService(SystemPaymentService svc) => _svc = svc;

    /// <summary>
    /// 系统充值接口列表。
    /// </summary>
    /// <returns>行列表</returns>
    [ApiDescriptionSettings(Name = "List"), HttpPost]
    [DisplayName("平台充值接口列表")]
    public Task<List<SystemPaymentListItemDto>> List() =>
        _svc.ListAsync(App.HttpContext?.RequestAborted ?? default);
}
