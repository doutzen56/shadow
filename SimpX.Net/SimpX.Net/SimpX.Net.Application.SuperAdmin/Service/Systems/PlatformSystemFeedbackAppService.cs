namespace SimpX.Net.Application.SuperAdmin;

/// <summary>
/// 总后台客户留言（JWT）。
/// </summary>
[ApiDescriptionSettings(Module = "platform", Name = "systemFeedback", Order = 485, Description = "客户留言")]
public class PlatformSystemFeedbackAppService : IDynamicApiController, ITransient
{
    private readonly SystemFeedbackService _svc;

    /// <summary>
    /// 注入。
    /// </summary>
    /// <param name="svc">留言</param>
    public PlatformSystemFeedbackAppService(SystemFeedbackService svc) => _svc = svc;

    /// <summary>列表。</summary>
    /// <param name="name">称呼模糊</param>
    /// <returns>行列表</returns>
    [ApiDescriptionSettings(Name = "List"), HttpGet]
    [DisplayName("平台客户留言列表")]
    public Task<List<SystemFeedbackListItemDto>> List([FromQuery] string name = null) =>
        _svc.ListAsync(name, App.HttpContext?.RequestAborted ?? default);

    /// <summary>详情。</summary>
    /// <param name="id">编号</param>
    /// <returns>详情</returns>
    [ApiDescriptionSettings(Name = "Detail"), HttpGet]
    [DisplayName("平台客户留言详情")]
    public Task<SystemFeedbackDetailDto> Detail([FromQuery] int id) =>
        _svc.DetailAsync(id, App.HttpContext?.RequestAborted ?? default);

    /// <summary>删除。</summary>
    /// <param name="input">编号</param>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("平台客户留言删除")]
    public Task Delete(SystemFeedbackDeleteInput input) =>
        _svc.DeleteAsync(input?.ID ?? 0, App.HttpContext?.RequestAborted ?? default);
}

/// <summary>删除留言入参。</summary>
public sealed class SystemFeedbackDeleteInput
{
    /// <summary>编号。</summary>
    public int ID { get; set; }
}
