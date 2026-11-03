namespace SimpX.Net.Application.SuperAdmin;

/// <summary>
/// 总后台更新公告 CRUD（JWT）。
/// </summary>
[ApiDescriptionSettings(Module = "platform", Name = "systemUpdateNews", Order = 479, Description = "更新公告")]
public class PlatformSystemUpdateNewsAppService : IDynamicApiController, ITransient
{
    private readonly SystemUpdateNewsService _svc;

    /// <summary>
    /// 注入。
    /// </summary>
    /// <param name="svc">更新公告</param>
    public PlatformSystemUpdateNewsAppService(SystemUpdateNewsService svc) => _svc = svc;

    /// <summary>列表。</summary>
    /// <param name="language">语种</param>
    /// <param name="title">标题模糊</param>
    /// <returns>行列表</returns>
    [ApiDescriptionSettings(Name = "List"), HttpGet]
    [DisplayName("平台更新公告列表")]
    public Task<List<SystemUpdateNewsItemDto>> List(
        [FromQuery] int? language = null,
        [FromQuery] string title = null) =>
        _svc.ListAsync(language, title, App.HttpContext?.RequestAborted ?? default);

    /// <summary>详情。</summary>
    /// <param name="id">编号</param>
    /// <returns>详情</returns>
    [ApiDescriptionSettings(Name = "Detail"), HttpGet]
    [DisplayName("平台更新公告详情")]
    public Task<SystemUpdateNewsItemDto> Detail([FromQuery] int id) =>
        _svc.DetailAsync(id, App.HttpContext?.RequestAborted ?? default);

    /// <summary>保存。</summary>
    /// <param name="input">表单</param>
    /// <returns>编号</returns>
    [ApiDescriptionSettings(Name = "Save"), HttpPost]
    [DisplayName("平台更新公告保存")]
    public Task<int> Save(SystemUpdateNewsSaveInput input) =>
        _svc.SaveAsync(input ?? new SystemUpdateNewsSaveInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>删除。</summary>
    /// <param name="input">编号</param>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("平台更新公告删除")]
    public Task Delete(SystemUpdateNewsDeleteInput input) =>
        _svc.DeleteAsync(input?.ID ?? 0, App.HttpContext?.RequestAborted ?? default);
}

/// <summary>删除更新公告入参。</summary>
public sealed class SystemUpdateNewsDeleteInput
{
    /// <summary>编号。</summary>
    public int ID { get; set; }
}
