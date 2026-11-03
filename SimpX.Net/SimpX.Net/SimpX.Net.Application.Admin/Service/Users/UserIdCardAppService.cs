namespace SimpX.Net.Application.Admin;

/// <summary>
/// 租户后台证件信息（JWT）。对齐旧版 list/save/delete（本刀无证件照）。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "userIdCard", Order = 473, Description = "证件信息")]
public class UserIdCardAppService : IDynamicApiController, ITransient
{
    private readonly UserIdCardService _svc;

    /// <summary>
    /// 注入证件服务。
    /// </summary>
    /// <param name="svc">证件服务</param>
    public UserIdCardAppService(UserIdCardService svc) => _svc = svc;

    /// <summary>
    /// 分页列表。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <returns>分页</returns>
    [ApiDescriptionSettings(Name = "Page"), HttpPost]
    [DisplayName("证件信息列表")]
    public Task<UserIdCardAdminPageOutput> Page(UserIdCardAdminPageInput input) =>
        _svc.PageAsync(input ?? new UserIdCardAdminPageInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 保存。
    /// </summary>
    /// <param name="input">入参</param>
    [ApiDescriptionSettings(Name = "Save"), HttpPost]
    [DisplayName("保存证件信息")]
    public Task Save(UserIdCardSaveInput input) =>
        _svc.SaveAsync(input ?? new UserIdCardSaveInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 删除。
    /// </summary>
    /// <param name="input">入参</param>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("删除证件信息")]
    public Task Delete(UserIdCardDeleteInput input) =>
        _svc.DeleteAsync(input?.ID ?? 0, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 详情。对齐旧 info。
    /// </summary>
    /// <param name="id">证件编号</param>
    /// <param name="userId">会员</param>
    /// <param name="status">状态</param>
    /// <returns>详情</returns>
    [ApiDescriptionSettings(Name = "Info"), HttpPost]
    [DisplayName("证件详情")]
    public Task<UserIdCardDetailDto> Info(
        [FromQuery] int id,
        [FromQuery] int userId,
        [FromQuery] int status) =>
        _svc.InfoAsync(id, userId, status, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 审核。对齐旧 updateinfo。
    /// </summary>
    /// <param name="input">入参</param>
    [ApiDescriptionSettings(Name = "UpdateInfo"), HttpPost]
    [DisplayName("证件审核")]
    public Task UpdateInfo(UserIdCardUpdateInfoInput input) =>
        _svc.UpdateInfoAsync(input ?? new UserIdCardUpdateInfoInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 按会员重置证件。对齐旧 reset。
    /// </summary>
    /// <param name="userId">会员</param>
    [ApiDescriptionSettings(Name = "Reset"), HttpPost]
    [DisplayName("重置会员证件")]
    public Task Reset([FromQuery] int userId) =>
        _svc.ResetAsync(userId, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 查会员。对齐旧 verifyuser。
    /// </summary>
    /// <param name="userName">用户名</param>
    /// <returns>编号</returns>
    [ApiDescriptionSettings(Name = "VerifyUser"), HttpGet]
    [DisplayName("证件查会员")]
    public Task<UserIdCardVerifyUserOutput> VerifyUser([FromQuery] string userName) =>
        _svc.VerifyUserAsync(userName, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 新建证件。对齐旧 createidcard。
    /// </summary>
    /// <param name="input">入参</param>
    [ApiDescriptionSettings(Name = "CreateIdCard"), HttpPost]
    [DisplayName("新建证件")]
    public Task CreateIdCard(UserIdCardSaveInput input) =>
        _svc.CreateIdCardAsync(input ?? new UserIdCardSaveInput(), App.HttpContext?.RequestAborted ?? default);
}
