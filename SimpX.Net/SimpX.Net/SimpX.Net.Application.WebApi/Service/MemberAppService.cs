using Furion.DataEncryption;

namespace SimpX.Net.Application.WebApi;

/// <summary>
/// 会员注册、登录和自己改资料。签发带 MemberId 的 JWT，与后台 UserId 体系隔离。
/// 写库与校验走 Shared 的 MemberService。
/// </summary>
[ApiDescriptionSettings(Name = "member", Order = 10)]
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
    /// 会员注册。
    /// </summary>
    /// <param name="input">账号、密码、昵称</param>
    /// <returns>新会员 Id</returns>
    [AllowAnonymous]
    [HttpPost("register")]
    [DisplayName("会员注册")]
    public Task<long> Register([FromBody] MemberRegisterInput input) => _members.Register(input);

    /// <summary>
    /// 会员登录，返回独立 JWT。
    /// </summary>
    /// <param name="input">账号、密码</param>
    /// <returns>令牌和会员信息</returns>
    [AllowAnonymous]
    [HttpPost("login")]
    [DisplayName("会员登录")]
    public async Task<MemberLoginOutput> Login([FromBody] MemberLoginInput input)
    {
        var member = await _members.Authenticate(input);
        var accessToken = JWTEncryption.Encrypt(new Dictionary<string, object>
        {
            { ClaimConst.MemberId, member.Id },
            { ClaimConst.Account, member.Account },
            { ClaimConst.NickName, member.NickName ?? member.Account },
            { ClaimConst.LoginMode, "Member" },
            { ClaimConst.TenantId, member.TenantId ?? 0 }
        });

        return new MemberLoginOutput
        {
            AccessToken = accessToken,
            MemberId = member.Id,
            Account = member.Account,
            NickName = member.NickName
        };
    }

    /// <summary>
    /// 当前登录会员资料。
    /// </summary>
    /// <returns>会员资料</returns>
    [HttpGet("profile")]
    [DisplayName("会员资料")]
    public async Task<MemberProfileOutput> Profile()
    {
        var member = await _members.GetRequired(RequireMemberId());
        return MemberService.ToProfile(member);
    }

    /// <summary>
    /// 会员自己改昵称、手机号。
    /// </summary>
    /// <param name="input">昵称、手机号</param>
    [HttpPost("updateProfile")]
    [DisplayName("修改会员资料")]
    public Task UpdateProfile([FromBody] MemberUpdateProfileInput input)
        => _members.UpdateOwnProfile(RequireMemberId(), input);

    /// <summary>
    /// 从登录令牌取会员 Id。
    /// </summary>
    /// <returns>会员 Id</returns>
    private static long RequireMemberId()
    {
        var memberIdText = App.User?.FindFirst(ClaimConst.MemberId)?.Value;
        if (!long.TryParse(memberIdText, out var memberId) || memberId <= 0)
            throw Oops.Oh("未登录或令牌无效");
        return memberId;
    }
}
