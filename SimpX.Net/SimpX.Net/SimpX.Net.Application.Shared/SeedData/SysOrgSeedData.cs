namespace SimpX.Net.Application.Shared;

/// <summary>
/// 系统机构表种子数据（默认租户根节点；站=组织节点时在此扩展）。
/// </summary>
[IgnoreUpdateSeed]
public class SysOrgSeedData : ISqlSugarEntitySeedData<SysOrg>
{
    /// <summary>
    /// 种子数据
    /// </summary>
    /// <returns></returns>
    public IEnumerable<SysOrg> HasData()
    {
        return new[]
        {
            new SysOrg{ Id=SqlSugarConst.DefaultTenantId, Pid=0, Name="系统默认", Code="1001", Type="101", Level=1, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), Remark="系统默认", TenantId=SqlSugarConst.DefaultTenantId },
        };
    }
}
