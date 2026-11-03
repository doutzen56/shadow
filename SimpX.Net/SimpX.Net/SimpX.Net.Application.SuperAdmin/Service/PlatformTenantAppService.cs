namespace SimpX.Net.Application.SuperAdmin;

/// <summary>
/// 总后台租户管理。对外走 /api/platform/tenant，实现在 Shared 的 TenantPlatformService。
/// </summary>
[ApiDescriptionSettings(Module = "platform", Name = "tenant", Order = 30)]
public class PlatformTenantAppService : IDynamicApiController, ITransient
{
    private readonly TenantPlatformService _tenantPlatform;

    /// <summary>
    /// 注入共享租户平台服务。
    /// </summary>
    /// <param name="tenantPlatform">共享租户平台服务</param>
    public PlatformTenantAppService(TenantPlatformService tenantPlatform)
    {
        _tenantPlatform = tenantPlatform;
    }

    /// <summary>
    /// 租户分页列表。
    /// </summary>
    /// <param name="input">分页与筛选</param>
    /// <returns>租户分页</returns>
    [HttpPost]
    [DisplayName("获取租户分页列表")]
    public Task<SqlSugarPagedList<TenantOutput>> Page(PageTenantInput input)
    {
        EnsurePlatformHost();
        return _tenantPlatform.Page(input);
    }

    /// <summary>
    /// 新增租户。
    /// </summary>
    /// <param name="input">租户入参</param>
    [UnitOfWork]
    [HttpPost]
    [DisplayName("增加租户")]
    public Task Add(AddTenantInput input)
    {
        EnsurePlatformHost();
        return _tenantPlatform.AddTenant(input);
    }

    /// <summary>
    /// 更新租户。
    /// </summary>
    /// <param name="input">租户入参</param>
    [HttpPost]
    [DisplayName("更新租户")]
    public Task Update(UpdateTenantInput input)
    {
        EnsurePlatformHost();
        return _tenantPlatform.UpdateTenant(input);
    }

    /// <summary>
    /// 删除租户。
    /// </summary>
    /// <param name="input">租户 Id</param>
    [UnitOfWork]
    [HttpPost]
    [DisplayName("删除租户")]
    public Task Delete(DeleteTenantInput input)
    {
        EnsurePlatformHost();
        return _tenantPlatform.DeleteTenant(input);
    }

    /// <summary>
    /// 设置租户状态。
    /// </summary>
    /// <param name="input">租户 Id 与状态</param>
    /// <returns>更新行数</returns>
    [HttpPost]
    [DisplayName("设置租户状态")]
    public Task<int> SetStatus(TenantInput input)
    {
        EnsurePlatformHost();
        return _tenantPlatform.SetStatus(input);
    }

    /// <summary>
    /// 授权租户菜单。
    /// </summary>
    /// <param name="input">租户与菜单</param>
    [UnitOfWork]
    [HttpPost]
    [DisplayName("授权租户菜单")]
    public Task GrantMenu(TenantMenuInput input)
    {
        EnsurePlatformHost();
        return _tenantPlatform.GrantMenu(input);
    }

    /// <summary>
    /// 获取租户菜单 Id 集合。
    /// </summary>
    /// <param name="input">租户 Id</param>
    /// <returns>菜单 Id</returns>
    [HttpGet]
    [DisplayName("获取租户菜单Id集合")]
    public Task<List<long>> MenuList([FromQuery] BaseIdInput input)
    {
        EnsurePlatformHost();
        return _tenantPlatform.GetTenantMenuList(input);
    }

    /// <summary>
    /// 全部菜单清单（授权勾选用）。
    /// </summary>
    /// <returns>菜单简表</returns>
    [HttpGet]
    [DisplayName("获取全部菜单清单")]
    public Task<List<PlatformMenuItem>> AllMenus()
    {
        EnsurePlatformHost();
        return _tenantPlatform.ListAllMenus();
    }

    /// <summary>
    /// 重置租户管理员密码。
    /// </summary>
    /// <param name="input">用户 Id</param>
    /// <returns>默认明文密码</returns>
    [HttpPost]
    [DisplayName("重置租户管理员密码")]
    public Task<string> ResetPwd(TenantUserInput input)
    {
        EnsurePlatformHost();
        return _tenantPlatform.ResetPwd(input);
    }

    /// <summary>
    /// 创建租户数据库。
    /// </summary>
    /// <param name="input">租户 Id</param>
    [HttpPost]
    [DisplayName("创建租户数据库")]
    public Task CreateDb(TenantInput input)
    {
        EnsurePlatformHost();
        return _tenantPlatform.CreateDb(input);
    }

    /// <summary>
    /// 同步授权菜单。
    /// </summary>
    /// <param name="input">租户 Id</param>
    [UnitOfWork]
    [HttpPost]
    [DisplayName("同步授权菜单")]
    public Task SyncGrantMenu(BaseIdInput input)
    {
        EnsurePlatformHost();
        return _tenantPlatform.SyncGrantMenu(input);
    }

    /// <summary>
    /// 进入租管端（签发该租户管理员令牌）。
    /// </summary>
    /// <param name="input">租户 Id</param>
    /// <returns>登录令牌</returns>
    [HttpPost]
    [DisplayName("进入租管端")]
    public Task<LoginOutput> GoTenant(BaseIdInput input)
    {
        EnsurePlatformHost();
        return _tenantPlatform.GoTenant(input);
    }

    /// <summary>
    /// 切换租户上下文。
    /// </summary>
    /// <param name="input">目标租户 Id</param>
    /// <returns>登录令牌</returns>
    [HttpPost]
    [DisplayName("切换租户")]
    public Task<LoginOutput> ChangeTenant(BaseIdInput input)
    {
        EnsurePlatformHost();
        return _tenantPlatform.ChangeTenant(input);
    }

    /// <summary>
    /// 租户下用户列表。
    /// </summary>
    /// <param name="input">租户 Id</param>
    /// <returns>用户列表</returns>
    [HttpPost]
    [DisplayName("获取租户下的用户列表")]
    public Task<List<SysUser>> UserList(TenantIdInput input)
    {
        EnsurePlatformHost();
        return _tenantPlatform.UserList(input);
    }

    /// <summary>
    /// 只允许在总后台进程调用本接口。
    /// </summary>
    private static void EnsurePlatformHost()
    {
        if (!HostTenantMode.IsPlatformHost)
            throw Oops.Oh(ErrorCodeEnum.SA001);
    }
}
