namespace SimpX.Net.Application.Shared;

/// <summary>
/// 系统用户表种子数据。
/// 基础包：只落一个超管 + 一个租户系统管理员；不迁全量旧站账号。
/// </summary>
[IgnoreUpdateSeed]
public class SysUserSeedData : ISqlSugarEntitySeedData<SysUser>
{
    /// <summary>
    /// 种子数据
    /// </summary>
    /// <returns></returns>
    public IEnumerable<SysUser> HasData()
    {
        var encryptPassword = CryptogramUtil.Encrypt(new SysConfigSeedData().HasData().First(u => u.Code == ConfigConst.SysPassword).Value);
        var posList = new SysPosSeedData().HasData().ToList();
        return new[]
        {
            new SysUser{ Id=1300000000101, Account="superSimpX.Net", Password=encryptPassword, NickName="超级管理员", RealName="超级管理员", Phone="18012345678", Birthday=DateTime.Parse("2000-01-01"), Sex=GenderEnum.Male, AccountType=AccountTypeEnum.SuperAdmin, Remark="平台超管（基础包唯一超管）", CreateTime=DateTime.Parse("2022-02-10 00:00:00"), TenantId=SqlSugarConst.DefaultTenantId },
            new SysUser{ Id=1300000000111, Account="SimpX.Net", Password=encryptPassword, NickName="系统管理员", RealName="系统管理员", Phone="18012345677", Birthday=DateTime.Parse("2000-01-01"), Sex=GenderEnum.Male, AccountType=AccountTypeEnum.SysAdmin, Remark="默认租户系统管理员", CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrgId=SqlSugarConst.DefaultTenantId, PosId=posList[0].Id, TenantId=SqlSugarConst.DefaultTenantId },
        };
    }
}
