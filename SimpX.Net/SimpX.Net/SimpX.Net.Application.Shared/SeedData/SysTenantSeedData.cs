namespace SimpX.Net.Application.Shared;

/// <summary>
/// 系统租户表种子数据。
/// </summary>
public class SysTenantSeedData : ISqlSugarEntitySeedData<SysTenant>
{
    /// <summary>
    /// 种子数据
    /// </summary>
    /// <returns></returns>
    public IEnumerable<SysTenant> HasData()
    {
        var defaultDbConfig = App.GetOptions<DbConnectionOptions>().ConnectionConfigs[0];
        var userList = new SysUserSeedData().HasData().ToList();
        var admin = userList.First(u => u.Account == "SimpX.Net");
        return new[]
        {
            // Host 可多域名：本机前端常用 127.0.0.1:8888 / localhost:8888，规范化后认 127.0.0.1 与 localhost。
            new SysTenant{ Id=SqlSugarConst.DefaultTenantId, OrgId=SqlSugarConst.DefaultTenantId, UserId=admin.Id, Host="localhost,127.0.0.1", TenantType=defaultDbConfig.TenantType, DbType=defaultDbConfig.DbType, Connection=defaultDbConfig.TenantType == TenantTypeEnum.Db ? defaultDbConfig.ConnectionString : null, ConfigId=defaultDbConfig.TenantType == TenantTypeEnum.Db ? SqlSugarConst.MainConfigId : null, Logo="/upload/logo.png", Title="SimpX.Net", ViceTitle="SimpX.Net", ViceDesc="站在巨人肩膀上的 .NET 通用权限开发框架", Watermark="SimpX.Net", Copyright="Copyright \u00a9 2021-present SimpX.Net All rights reserved.", Icp="省ICP备12345678号", IcpUrl="https://beian.miit.gov.cn", Remark="系统默认", CreateTime=DateTime.Parse("2022-02-10 00:00:00") },
        };
    }
}
