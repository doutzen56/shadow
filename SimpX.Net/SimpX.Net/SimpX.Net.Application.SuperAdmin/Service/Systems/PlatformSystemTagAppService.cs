namespace SimpX.Net.Application.SuperAdmin;

/// <summary>
/// 总后台标签管理（JWT）。
/// </summary>
[ApiDescriptionSettings(Module = "platform", Name = "systemTag", Order = 477, Description = "标签管理")]
public class PlatformSystemTagAppService : IDynamicApiController, ITransient
{
    private readonly SystemTagService _svc;

    /// <summary>
    /// 注入。
    /// </summary>
    /// <param name="svc">标签</param>
    public PlatformSystemTagAppService(SystemTagService svc) => _svc = svc;

    /// <summary>列表。</summary>
    /// <param name="input">筛选</param>
    /// <returns>行列表</returns>
    [ApiDescriptionSettings(Name = "List"), HttpPost]
    [DisplayName("平台标签列表")]
    public Task<List<SystemTagListItemDto>> List(SystemTagListInput input) =>
        _svc.ListAsync(input ?? new SystemTagListInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>详情。</summary>
    /// <param name="type">类型</param>
    /// <param name="code">码</param>
    /// <returns>详情</returns>
    [ApiDescriptionSettings(Name = "Detail"), HttpGet]
    [DisplayName("平台标签详情")]
    public Task<SystemTagDetailDto> Detail([FromQuery] byte type, [FromQuery] string code) =>
        _svc.DetailAsync(type, code, App.HttpContext?.RequestAborted ?? default);

    /// <summary>保存。</summary>
    /// <param name="input">表单</param>
    [ApiDescriptionSettings(Name = "Save"), HttpPost]
    [DisplayName("平台标签保存")]
    public Task Save(SystemTagSaveInput input) =>
        _svc.SaveAsync(input ?? new SystemTagSaveInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>删除。</summary>
    /// <param name="input">类型与码</param>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("平台标签删除")]
    public Task Delete(SystemTagDeleteInput input) =>
        _svc.DeleteAsync(input?.Type ?? 0, input?.Code, App.HttpContext?.RequestAborted ?? default);
}
