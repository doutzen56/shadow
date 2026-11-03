namespace SimpX.Net.Application.Admin;

/// <summary>
/// 租户后台的会员管理。写库与校验走 Shared 的 MemberService，与 WebApi 共用同一套规则。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "member", Order = 450, Description = "会员管理")]
public class MemberAppService : IDynamicApiController, ITransient
{
    private readonly MemberService _members;

    /// <summary>
    /// 注入会员领域服务。
    /// </summary>
    /// <param name="members">会员领域服务</param>
    public MemberAppService(MemberService members)
    {
        _members = members;
    }

    /// <summary>
    /// 会员分页列表。
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <returns>分页结果</returns>
    [DisplayName("获取会员分页列表")]
    public Task<SqlSugarPagedList<MemberProfileOutput>> Page(PageMemberInput input) => _members.Page(input);

    /// <summary>
    /// 会员详情。
    /// </summary>
    /// <param name="id">会员 Id</param>
    /// <returns>资料</returns>
    [DisplayName("获取会员详情")]
    public async Task<MemberProfileOutput> GetDetail([FromQuery] long id)
        => MemberService.ToProfile(await _members.GetRequired(id));

    /// <summary>
    /// 启用或停用会员。
    /// </summary>
    /// <param name="input">会员 Id 与目标状态</param>
    [ApiDescriptionSettings(Name = "SetStatus"), HttpPost]
    [DisplayName("设置会员状态")]
    public Task SetStatus(MemberSetStatusInput input) => _members.SetStatus(input);

    /// <summary>
    /// 重置会员密码。
    /// </summary>
    /// <param name="input">会员 Id 与新密码</param>
    [ApiDescriptionSettings(Name = "ResetPassword"), HttpPost]
    [DisplayName("重置会员密码")]
    public Task ResetPassword(MemberResetPasswordInput input) => _members.ResetPassword(input);
}
