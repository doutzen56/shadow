namespace SimpX.Net.Application.SuperAdmin;

/// <summary>
/// 总后台游戏标签（JWT）。
/// </summary>
[ApiDescriptionSettings(Module = "platform", Name = "gameTag", Order = 478, Description = "游戏标签")]
public class PlatformGameTagAppService : IDynamicApiController, ITransient
{
    private readonly GameTagService _svc;

    /// <summary>
    /// 注入。
    /// </summary>
    /// <param name="svc">游戏标签</param>
    public PlatformGameTagAppService(GameTagService svc) => _svc = svc;

    /// <summary>列表。</summary>
    /// <param name="tagName">名称模糊</param>
    /// <returns>行列表</returns>
    [ApiDescriptionSettings(Name = "List"), HttpGet]
    [DisplayName("平台游戏标签列表")]
    public Task<List<GameTagListItemDto>> List([FromQuery] string tagName = null) =>
        _svc.ListAsync(tagName, App.HttpContext?.RequestAborted ?? default);

    /// <summary>详情。</summary>
    /// <param name="id">编号</param>
    /// <returns>详情</returns>
    [ApiDescriptionSettings(Name = "Detail"), HttpGet]
    [DisplayName("平台游戏标签详情")]
    public Task<GameTagDetailDto> Detail([FromQuery] int id) =>
        _svc.DetailAsync(id, App.HttpContext?.RequestAborted ?? default);

    /// <summary>保存。</summary>
    /// <param name="input">表单</param>
    /// <returns>编号</returns>
    [ApiDescriptionSettings(Name = "Save"), HttpPost]
    [DisplayName("平台游戏标签保存")]
    public Task<int> Save(GameTagSaveInput input) =>
        _svc.SaveAsync(input ?? new GameTagSaveInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>删除。</summary>
    /// <param name="input">编号</param>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("平台游戏标签删除")]
    public Task Delete(GameTagDeleteInput input) =>
        _svc.DeleteAsync(input?.ID ?? 0, App.HttpContext?.RequestAborted ?? default);
}

/// <summary>删除游戏标签入参。</summary>
public sealed class GameTagDeleteInput
{
    /// <summary>编号。</summary>
    public int ID { get; set; }
}
