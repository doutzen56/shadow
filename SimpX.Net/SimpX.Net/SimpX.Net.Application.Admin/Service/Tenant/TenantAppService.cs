namespace SimpX.Net.Application.Admin;

/// <summary>
/// 租户后台侧租户辅助：当前租户信息、下拉列表、库连接与种子菜单。
/// 租户增删改、建库、授权等总后台能力在 Shared TenantPlatformService / SuperAdmin。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "tenant", Order = 390)]
public class TenantAppService : IDynamicApiController, ITransient
{
    private readonly SqlSugarRepository<SysTenant> _sysTenantRep;
    private readonly ISimpXCache _cache;
    private readonly IFileStorage _fileStorage;
    private readonly ICurrentTenantAccessor _currentTenant;

    /// <summary>
    /// 注入租户仓储、缓存、文件存储和当前租户。
    /// </summary>
    public TenantAppService(
        SqlSugarRepository<SysTenant> sysTenantRep,
        ISimpXCache cache,
        IFileStorage fileStorage,
        ICurrentTenantAccessor currentTenant)
    {
        _sysTenantRep = sysTenantRep;
        _cache = cache;
        _fileStorage = fileStorage;
        _currentTenant = currentTenant;
    }

    /// <summary>
    /// 获取租户列表。按域名确定租户的进程只返回当前租户；否则返回全部启用租户。
    /// </summary>
    /// <returns>租户下拉项</returns>
    [AllowAnonymous]
    [DisplayName("获取租户列表"), HttpGet]
    public async Task<dynamic> GetList()
    {
        if (HostTenantMode.ResolveByHost)
        {
            var tenantId = _currentTenant.RequireTenantId();
            var row = await _sysTenantRep.AsQueryable()
                .LeftJoin<SysOrg>((u, a) => u.OrgId == a.Id).ClearFilter()
                .Where((u, a) => u.Id == tenantId && u.Status == StatusEnum.Enable)
                .Select((u, a) => new
                {
                    Label = SqlFunc.HasValue(u.Title) ? $"{u.Title}-{a.Name}" : a.Name,
                    Host = u.Host.ToLower(),
                    Value = u.Id,
                }).FirstAsync();
            _ = row ?? throw Oops.Oh(ErrorCodeEnum.Z1005);
            return new[] { row };
        }

        return await _sysTenantRep.AsQueryable()
           .LeftJoin<SysOrg>((u, a) => u.OrgId == a.Id).ClearFilter()
           .Where(u => u.Status == StatusEnum.Enable)
           .Select((u, a) => new
           {
               Label = SqlFunc.HasValue(u.Title) ? $"{u.Title}-{a.Name}" : a.Name,
               Host = u.Host.ToLower(),
               Value = u.Id,
           }).ToListAsync();
    }

    /// <summary>
    /// 获取当前租户系统信息。优先用按域名解析到的租户，其次用登录 Claim。
    /// </summary>
    /// <returns>当前租户</returns>
    [NonAction]
    public async Task<SysTenant> GetCurrentTenantSysInfo()
    {
        var tenantId = _currentTenant.TenantId
            ?? long.Parse(App.User?.FindFirst(ClaimConst.TenantId)?.Value ?? "0");
        if (tenantId <= 0)
            throw Oops.Oh(ErrorCodeEnum.Z1005);

        var tenant = await _sysTenantRep.GetFirstAsync(u => u.Id == tenantId && u.Status == StatusEnum.Enable);
        return tenant ?? throw Oops.Oh(ErrorCodeEnum.Z1005);
    }

    /// <summary>
    /// 获取库隔离的租户列表。
    /// </summary>
    /// <returns>独立库租户</returns>
    [NonAction]
    public async Task<List<SysTenant>> GetTenantDbList()
    {
        return await _sysTenantRep.GetListAsync(u => u.TenantType == TenantTypeEnum.Db && u.Status == StatusEnum.Enable);
    }

    /// <summary>
    /// 把租户图标写到文件存储，并记下访问地址。没传图标内容时不改。
    /// </summary>
    /// <param name="tenant">租户</param>
    /// <param name="logoBase64">图标的 data URL</param>
    /// <param name="logoFileName">原文件名</param>
    [NonAction]
    public async Task SetLogoUrl(SysTenant tenant, string logoBase64, string logoFileName)
    {
        if (tenant == null || string.IsNullOrWhiteSpace(logoBase64))
            return;

        var groups = Regex.Match(logoBase64, @"data:image/(?<type>.+?);base64,(?<data>.+)").Groups;
        var binData = Convert.FromBase64String(groups["data"].Value);
        var ext = string.IsNullOrWhiteSpace(logoFileName) ? ".png" : Path.GetExtension(logoFileName);
        var fileName = $"{tenant.ViceTitle}-logo{ext}".ToLower();
        var key = $"upload/{fileName}";

        await using var stream = new MemoryStream(binData);
        await _fileStorage.SaveAsync(key, stream);

        var oldKey = ToStorageKey(tenant.Logo);
        if (!string.IsNullOrEmpty(oldKey) && !string.Equals(oldKey, key, StringComparison.OrdinalIgnoreCase))
            await _fileStorage.DeleteAsync(oldKey);

        tenant.Logo = "/" + _fileStorage.GetUrl(key).TrimStart('/');
    }

    /// <summary>
    /// 获取租户默认菜单（种子用）。
    /// </summary>
    /// <param name="ignoreHome">是否排除首页</param>
    /// <returns>租户菜单行</returns>
    [NonAction]
    public IEnumerable<SysTenantMenu> GetTenantDefaultMenuList(bool ignoreHome = false)
    {
        // 框架菜单 + 站点业务菜单（可读码种子）；总后台业务菜单不进租户默认授权
        var menuList = new SysMenuSeedData().HasData()
            .Concat(new SiteAdminMenuSeedData().HasData())
            .ToList();
        if (ignoreHome)
            menuList = menuList.Where(u => !(u.Type == MenuTypeEnum.Menu && u.Name == "site_home_dashboard")).ToList();
        menuList = menuList.Distinct().ToList();

        return menuList.Select(u => new SysTenantMenu
        {
            Id = CommonUtil.GetFixedHashCode("" + SqlSugarConst.DefaultTenantId + u.Id, 1300000000000),
            TenantId = SqlSugarConst.DefaultTenantId,
            MenuId = u.Id
        });
    }

    /// <summary>
    /// 获取基础角色默认菜单。
    /// </summary>
    /// <returns>租户菜单行</returns>
    [NonAction]
    public IEnumerable<SysTenantMenu> GetBaseRoleMenuIdList()
    {
        var menuList = new List<SysMenu>();
        var frameworkMenus = new SysMenuSeedData().HasData().ToList();
        var siteMenus = new SiteAdminMenuSeedData().HasData().ToList();

        // 站点首页树（替代已删除的框架「工作台」）
        var siteHome = siteMenus.First(u => u.Type == MenuTypeEnum.Dir && u.Pid == 0 && u.Title == "系统首页");
        menuList.Add(siteHome);
        menuList.AddRange(siteMenus.ToChildList(u => u.Id, u => u.Pid, siteHome.Id));

        var systemMenu = frameworkMenus.First(u => u.Type == MenuTypeEnum.Dir && u.Title == "系统管理");
        menuList.Add(systemMenu);
        menuList.AddRange(frameworkMenus.ToChildList(u => u.Id, u => u.Pid, u => u.Pid == systemMenu.Id && new[] { "机构管理", "个人中心" }.Contains(u.Title)));
        menuList = menuList.Where(u => !new[] { "增加", "编辑", "删除" }.Contains(u.Title)).ToList();
        return menuList.Select(u => new SysTenantMenu
        {
            Id = CommonUtil.GetFixedHashCode("" + SqlSugarConst.DefaultTenantId + u.Id, 1300000000000),
            TenantId = SqlSugarConst.DefaultTenantId,
            MenuId = u.Id
        });
    }

    /// <summary>
    /// 刷新一个租户的缓存。不传或传 0 时不做全量预热。
    /// </summary>
    /// <param name="tenantId">租户 Id</param>
    [NonAction]
    public async Task CacheTenant(long tenantId = 0)
    {
        if (tenantId <= 0) return;
        await CacheTenantSingle(tenantId);
    }

    /// <summary>
    /// 刷新一个租户：去掉已打开的库连接，再写入或删掉这条租户缓存。
    /// </summary>
    /// <param name="tenantId">租户 Id</param>
    [NonAction]
    public async Task CacheTenantSingle(long tenantId)
    {
        if (tenantId <= 0) return;

        if (tenantId.ToString() != SqlSugarConst.MainConfigId)
            _sysTenantRep.AsTenant().RemoveConnection(tenantId);

        var tenant = await _sysTenantRep.GetFirstAsync(u => u.Id == tenantId);
        if (tenant == null)
        {
            _cache.Remove(TenantCacheKeys.Tenant, tenantId);
            return;
        }

        EncryptTenantConnection(tenant);
        _cache.Add(TenantCacheKeys.Tenant, tenantId, tenant, TimeSpan.FromHours(1));
    }

    /// <summary>
    /// 获取租户数据库连接。
    /// </summary>
    /// <param name="tenantId">租户 Id</param>
    /// <returns>连接作用域；Id 隔离时返回 null</returns>
    [NonAction]
    public SqlSugarScopeProvider GetTenantDbConnectionScope(long tenantId)
    {
        var iTenant = _sysTenantRep.AsTenant();
        if (iTenant.IsAnyConnection(tenantId.ToString())) return iTenant.GetConnectionScope(tenantId.ToString());

        lock (iTenant)
        {
            var tenant = _cache.GetOrAdd(TenantCacheKeys.Tenant, tenantId, () =>
            {
                var loaded = _sysTenantRep.AsQueryable().ClearFilter().Where(u => u.Id == tenantId).ToList().FirstOrDefault();
                if (loaded == null) return null;
                EncryptTenantConnection(loaded);
                return loaded;
            }, TimeSpan.FromHours(1));
            if (tenant == null) return null;
            if (tenant.TenantType == TenantTypeEnum.Id) return null;

            var dbOptions = App.GetOptions<DbConnectionOptions>();
            var mainConnConfig = dbOptions.ConnectionConfigs.First(u => u.ConfigId.ToString() == SqlSugarConst.MainConfigId);
            var tenantConnConfig = new DbConnectionConfig
            {
                ConfigId = tenant.Id.ToString(),
                DbType = tenant.DbType,
                TenantType = tenant.TenantType,
                IsAutoCloseConnection = true,
                ConnectionString = CryptogramUtil.SM2Decrypt(tenant.Connection),
                DbSettings = new DbSettings()
                {
                    EnableUnderLine = mainConnConfig.DbSettings.EnableUnderLine,
                },
                SlaveConnectionConfigs = JSON.IsValid(tenant.SlaveConnections)
                    ? JSON.Deserialize<List<SlaveConnectionConfig>>(tenant.SlaveConnections)
                    : null
            };
            iTenant.AddConnection(tenantConnConfig);

            var sqlSugarScopeProvider = iTenant.GetConnectionScope(tenantId.ToString());
            SqlSugarSetup.SetDbConfig(tenantConnConfig);
            SqlSugarSetup.SetDbAop(sqlSugarScopeProvider, dbOptions.EnableConsoleSql, dbOptions.SuperAdminIgnoreIDeletedFilter);
            return sqlSugarScopeProvider;
        }
    }

    /// <summary>
    /// 对租户库连接进行 SM2 加密（缓存中只存密文）。
    /// </summary>
    private static void EncryptTenantConnection(SysTenant tenant)
    {
        if (!string.IsNullOrWhiteSpace(tenant.Connection))
            tenant.Connection = CryptogramUtil.SM2Encrypt(tenant.Connection);
    }

    /// <summary>
    /// 把库里的图标地址收成存储键。
    /// </summary>
    private static string ToStorageKey(string logo)
    {
        if (string.IsNullOrWhiteSpace(logo))
            return null;
        return logo.Replace('\\', '/').TrimStart('/');
    }
}
