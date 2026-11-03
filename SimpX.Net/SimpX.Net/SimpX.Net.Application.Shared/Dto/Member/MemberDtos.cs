namespace SimpX.Net.Application.Shared;

/// <summary>
/// 会员注册入参。
/// </summary>
public class MemberRegisterInput
{
    /// <summary>账号。</summary>
    [Required(ErrorMessage = "账号不能为空")]
    public string Account { get; set; }

    /// <summary>密码明文（传输走 HTTPS，落库前加密）。</summary>
    [Required(ErrorMessage = "密码不能为空")]
    public string Password { get; set; }

    /// <summary>昵称。</summary>
    public string NickName { get; set; }
}

/// <summary>
/// 会员登录入参。
/// </summary>
public class MemberLoginInput
{
    /// <summary>账号。</summary>
    [Required(ErrorMessage = "账号不能为空")]
    public string Account { get; set; }

    /// <summary>密码明文。</summary>
    [Required(ErrorMessage = "密码不能为空")]
    public string Password { get; set; }
}

/// <summary>
/// 会员登录结果（令牌由 WebApi 签发后填入）。
/// </summary>
public class MemberLoginOutput
{
    /// <summary>访问令牌。</summary>
    public string AccessToken { get; set; }

    /// <summary>会员 Id。</summary>
    public long MemberId { get; set; }

    /// <summary>账号。</summary>
    public string Account { get; set; }

    /// <summary>昵称。</summary>
    public string NickName { get; set; }
}

/// <summary>
/// 会员资料。
/// </summary>
public class MemberProfileOutput
{
    /// <summary>会员 Id。</summary>
    public long Id { get; set; }

    /// <summary>账号。</summary>
    public string Account { get; set; }

    /// <summary>昵称。</summary>
    public string NickName { get; set; }

    /// <summary>手机号。</summary>
    public string Phone { get; set; }

    /// <summary>状态。</summary>
    public StatusEnum Status { get; set; }

    /// <summary>所属租户 Id。</summary>
    public long? TenantId { get; set; }
}

/// <summary>
/// 会员自己改资料入参。
/// </summary>
public class MemberUpdateProfileInput
{
    /// <summary>昵称。</summary>
    public string NickName { get; set; }

    /// <summary>手机号。</summary>
    public string Phone { get; set; }
}

/// <summary>
/// 后台查会员分页入参。
/// </summary>
public class PageMemberInput : BasePageInput
{
    /// <summary>账号（模糊）。</summary>
    public string Account { get; set; }

    /// <summary>手机号（模糊）。</summary>
    public string Phone { get; set; }

    /// <summary>状态；不传则不限。</summary>
    public StatusEnum? Status { get; set; }
}

/// <summary>
/// 后台设置会员状态入参。
/// </summary>
public class MemberSetStatusInput : BaseStatusInput
{
}

/// <summary>
/// 后台重置会员密码入参。
/// </summary>
public class MemberResetPasswordInput : BaseIdInput
{
    /// <summary>新密码明文。</summary>
    [Required(ErrorMessage = "密码不能为空")]
    public string Password { get; set; }
}
