namespace SimpX.Net.Application.SuperAdmin;

/// <summary>
/// 总后台跨站会员列表（JWT，不定站，必须传 SiteID）。
/// </summary>
[ApiDescriptionSettings(Module = "platform", Name = "userMember", Order = 476, Description = "跨站会员")]
public class PlatformUserMemberAppService : IDynamicApiController, ITransient
{
    private readonly UserMemberService _svc;

    /// <summary>
    /// 注入会员查询。
    /// </summary>
    /// <param name="svc">会员</param>
    public PlatformUserMemberAppService(UserMemberService svc) => _svc = svc;

    /// <summary>
    /// 指定站点会员分页。
    /// </summary>
    /// <param name="input">SiteID + 筛选</param>
    /// <returns>分页</returns>
    [ApiDescriptionSettings(Name = "Page"), HttpPost]
    [DisplayName("总后台会员列表")]
    public Task<UserMemberPageOutput> Page(PlatformUserMemberPageInput input)
    {
        input ??= new PlatformUserMemberPageInput();
        // 总后台不定站；联系方式不按租户按钮脱敏，仍勿在日志打明文。
        return _svc.PageBySiteAsync(
            input.SiteID,
            new UserMemberPageInput
            {
                Page = input.Page,
                PageSize = input.PageSize,
                UserName = input.UserName
            },
            maskContact: false,
            App.HttpContext?.RequestAborted ?? default);
    }
}
