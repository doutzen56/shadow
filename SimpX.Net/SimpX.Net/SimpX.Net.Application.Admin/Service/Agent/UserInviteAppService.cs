namespace SimpX.Net.Application.Admin;

/// <summary>
/// 租户后台邀请管理（JWT）。对齐旧版 /admin/user/agent/invite/*。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "userInvite", Order = 467, Description = "邀请管理")]
public class UserInviteAppService : IDynamicApiController, ITransient
{
    private readonly UserInviteService _svc;

    /// <summary>
    /// 注入服务。
    /// </summary>
    /// <param name="svc">服务</param>
    public UserInviteAppService(UserInviteService svc) => _svc = svc;

    /// <summary>
    /// 列表。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <returns>行列表</returns>
    [ApiDescriptionSettings(Name = "List"), HttpPost]
    [DisplayName("邀请码列表")]
    public Task<List<UserInviteListItemDto>> List(UserInviteListInput input) =>
        _svc.ListAsync(input ?? new UserInviteListInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 详情。
    /// </summary>
    /// <param name="id">邀请码</param>
    /// <returns>详情</returns>
    [ApiDescriptionSettings(Name = "Info"), HttpPost]
    [DisplayName("邀请码详情")]
    public Task<UserInviteDetailDto> Info([FromQuery] string id) =>
        _svc.InfoAsync(id, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 保存。
    /// </summary>
    /// <param name="input">入参</param>
    [ApiDescriptionSettings(Name = "Save"), HttpPost]
    [DisplayName("保存邀请码")]
    public Task Save(UserInviteSaveInput input) =>
        _svc.SaveAsync(input ?? new UserInviteSaveInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 删除。
    /// </summary>
    /// <param name="id">邀请码</param>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("删除邀请码")]
    public Task Delete([FromQuery] string id) =>
        _svc.DeleteAsync(id, App.HttpContext?.RequestAborted ?? default);
}
