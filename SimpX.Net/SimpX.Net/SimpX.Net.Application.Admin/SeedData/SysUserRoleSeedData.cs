namespace SimpX.Net.Application.Admin;

/// <summary>
/// 系统用户角色表种子数据。
/// 基础包阶段超管/系统管理员不靠演示角色挂菜单，这里不灌测试用户角色。
/// </summary>
public class SysUserRoleSeedData : ISqlSugarEntitySeedData<SysUserRole>
{
    /// <summary>
    /// 种子数据
    /// </summary>
    /// <returns></returns>
    public IEnumerable<SysUserRole> HasData() => [];
}
