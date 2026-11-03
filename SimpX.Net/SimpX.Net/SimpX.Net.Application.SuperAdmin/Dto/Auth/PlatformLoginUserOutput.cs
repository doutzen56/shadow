namespace SimpX.Net.Application.SuperAdmin;

/// <summary>
/// 总后台当前登录用户信息（字段对齐租户后台 LoginUserOutput）。
/// </summary>
public class PlatformLoginUserOutput
{
    /// <summary>用户 Id。</summary>
    public long Id { get; set; }

    /// <summary>账号。</summary>
    public string Account { get; set; }

    /// <summary>姓名。</summary>
    public string RealName { get; set; }

    /// <summary>手机。</summary>
    public string Phone { get; set; }

    /// <summary>证件号。</summary>
    public string IdCardNum { get; set; }

    /// <summary>邮箱。</summary>
    public string Email { get; set; }

    /// <summary>账号类型。</summary>
    public AccountTypeEnum AccountType { get; set; } = AccountTypeEnum.NormalUser;

    /// <summary>头像。</summary>
    public string Avatar { get; set; }

    /// <summary>地址。</summary>
    public string Address { get; set; }

    /// <summary>签名。</summary>
    public string Signature { get; set; }

    /// <summary>机构 Id。</summary>
    public long OrgId { get; set; }

    /// <summary>机构名。</summary>
    public string OrgName { get; set; }

    /// <summary>机构类型。</summary>
    public string OrgType { get; set; }

    /// <summary>职位名。</summary>
    public string PosName { get; set; }

    /// <summary>按钮权限码。</summary>
    public List<string> Buttons { get; set; }

    /// <summary>角色 Id 列表。</summary>
    public List<long> RoleIds { get; set; }

    /// <summary>水印文字。</summary>
    public string WatermarkText { get; set; }

    /// <summary>租户 Id。</summary>
    public long? TenantId { get; set; }

    /// <summary>当前切换到的租户 Id。</summary>
    public long? CurrentTenantId { get; set; }

    /// <summary>语言代码。</summary>
    public string LangCode { get; set; }
}
