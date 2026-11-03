namespace SimpX.Net.Application.Admin;

/// <summary>
/// 注册方案输出参数
/// </summary>
public class UserRegWayOutput : SysUserRegWay
{
    /// <summary>
    /// 角色名称
    /// </summary>
    public string RoleName { get; set; }

    /// <summary>
    /// 机构名称
    /// </summary>
    public string OrgName { get; set; }

    /// <summary>
    /// 职位名称
    /// </summary>
    public string PosName { get; set; }
}