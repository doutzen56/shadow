namespace SimpX.Net.Application.Shared;

public class TenantOutput : SysTenant
{
    /// <summary>
    /// 租户名称
    /// </summary>
    public virtual string Name { get; set; }

    /// <summary>
    /// 管理员账号
    /// </summary>
    public virtual string AdminAccount { get; set; }

    /// <summary>
    /// 电子邮箱
    /// </summary>
    public virtual string Email { get; set; }

    /// <summary>
    /// 电话
    /// </summary>
    public virtual string Phone { get; set; }
}