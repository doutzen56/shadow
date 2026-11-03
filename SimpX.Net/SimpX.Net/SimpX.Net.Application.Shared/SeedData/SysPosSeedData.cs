namespace SimpX.Net.Application.Shared;

/// <summary>
/// 系统职位表种子数据。
/// </summary>
public class SysPosSeedData : ISqlSugarEntitySeedData<SysPos>
{
    /// <summary>
    /// 种子数据
    /// </summary>
    /// <returns></returns>
    public IEnumerable<SysPos> HasData()
    {
        return new[]
        {
            new SysPos{ Id=1300000000106, Name="部门经理", Code="bmjl", CreateTime=DateTime.Parse("2022-02-10 00:00:00"), Remark="部门经理", TenantId=SqlSugarConst.DefaultTenantId },
        };
    }
}
