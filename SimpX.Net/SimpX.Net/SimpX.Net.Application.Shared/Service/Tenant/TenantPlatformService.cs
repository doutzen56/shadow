namespace SimpX.Net.Application.Shared;

/// <summary>
/// 总后台租户平台能力（分页、增删改、建库、授权菜单等，无 DynamicApi）。
/// 不挂 ITransient：依赖登录发令牌与文件存储，只在 SuperAdmin 经 <see cref="SharedServiceRegistration"/> 显式注册，避免 Channels 等宿主 DI 校验失败。
/// </summary>
public sealed class TenantPlatformService
{
    private readonly SqlSugarRepository<SysUserExtOrg> _sysUserExtOrgRep;
    private readonly SqlSugarRepository<SysTenantMenu> _sysTenantMenuRep;
    private readonly SqlSugarRepository<SysRoleMenu> _sysRoleMenuRep;
    private readonly SqlSugarRepository<SysUserRole> _userRoleRep;
    private readonly SqlSugarRepository<SysTenant> _sysTenantRep;
    private readonly SqlSugarRepository<SysRole> _sysRoleRep;
    private readonly SqlSugarRepository<SysUser> _sysUserRep;
    private readonly SqlSugarRepository<SysOrg> _sysOrgRep;
    private readonly SqlSugarRepository<SysPos> _sysPosRep;
    private readonly RoleMenuManager _roleMenuManager;
    private readonly SysConfigReader _configReader;
    private readonly AuthLoginService _authLogin;
    private readonly ISimpXCache _cache;
    private readonly IFileStorage _fileStorage;

    /// <summary>
    /// 注入租户相关仓储、角色菜单、登录发令牌、配置、缓存和文件存储。
    /// </summary>
    public TenantPlatformService(
        SqlSugarRepository<SysUserExtOrg> sysUserExtOrgRep,
        SqlSugarRepository<SysTenantMenu> sysTenantMenuRep,
        SqlSugarRepository<SysRoleMenu> sysRoleMenuRep,
        SqlSugarRepository<SysUserRole> userRoleRep,
        SqlSugarRepository<SysTenant> sysTenantRep,
        SqlSugarRepository<SysUser> sysUserRep,
        SqlSugarRepository<SysRole> sysRoleRep,
        SqlSugarRepository<SysOrg> sysOrgRep,
        SqlSugarRepository<SysPos> sysPosRep,
        RoleMenuManager roleMenuManager,
        SysConfigReader configReader,
        AuthLoginService authLogin,
        ISimpXCache cache,
        IFileStorage fileStorage)
    {
        _sysUserExtOrgRep = sysUserExtOrgRep;
        _sysTenantMenuRep = sysTenantMenuRep;
        _sysRoleMenuRep = sysRoleMenuRep;
        _userRoleRep = userRoleRep;
        _sysTenantRep = sysTenantRep;
        _sysUserRep = sysUserRep;
        _sysRoleRep = sysRoleRep;
        _sysOrgRep = sysOrgRep;
        _sysPosRep = sysPosRep;
        _roleMenuManager = roleMenuManager;
        _configReader = configReader;
        _authLogin = authLogin;
        _cache = cache;
        _fileStorage = fileStorage;
    }

    /// <summary>
    /// 租户分页列表。仅总后台进程可调。
    /// </summary>
    /// <param name="input">分页与筛选</param>
    /// <returns>租户分页</returns>
    public async Task<SqlSugarPagedList<TenantOutput>> Page(PageTenantInput input)
    {
        EnsurePlatformTenantManage();

        return await _sysTenantRep.AsQueryable()
            .LeftJoin<SysUser>((u, a) => u.UserId == a.Id).ClearFilter()
            .LeftJoin<SysOrg>((u, a, b) => u.OrgId == b.Id).ClearFilter()
            .WhereIF(!string.IsNullOrWhiteSpace(input.Phone), (u, a) => a.Phone.Contains(input.Phone.Trim()))
            .WhereIF(!string.IsNullOrWhiteSpace(input.Name), (u, a, b) => b.Name.Contains(input.Name.Trim()))
            .OrderBy(u => new { u.OrderNo, u.Id })
            .Select((u, a, b) => new TenantOutput
            {
                Id = u.Id,
                OrgId = b.Id,
                Name = b.Name,
                UserId = a.Id,
                AdminAccount = a.Account,
                Phone = a.Phone,
                Host = u.Host,
                Email = a.Email,
                TenantType = u.TenantType,
                DbType = u.DbType,
                Connection = u.Connection,
                ConfigId = u.ConfigId,
                SlaveConnections = u.SlaveConnections,
                Status = u.Status,
                OrderNo = u.OrderNo,
                Remark = u.Remark,
                Title = u.Title,
                ViceTitle = u.ViceTitle,
                ViceDesc = u.ViceDesc,
                Watermark = u.Watermark,
                Copyright = u.Copyright,
                Icp = u.Icp,
                IcpUrl = u.IcpUrl,
                Logo = u.Logo,
                CreateTime = u.CreateTime,
                UpdateTime = u.UpdateTime,
            })
            .ToPagedListAsync(input.Page, input.PageSize);
    }

    /// <summary>
    /// 增加租户。
    /// </summary>
    /// <param name="input">租户入参</param>
    public async Task AddTenant(AddTenantInput input)
    {
        EnsurePlatformTenantManage();

        var isExist = await _sysOrgRep.IsAnyAsync(u => u.Name == input.Name);
        if (isExist) throw Oops.Oh(ErrorCodeEnum.D1300);

        input.Host = NormalizeAndEnsureUniqueHosts(input.Host);

        isExist = await _sysUserRep.AsQueryable().ClearFilter().AnyAsync(u => u.Account == input.AdminAccount);
        if (isExist) throw Oops.Oh(ErrorCodeEnum.D1301);

        if (!string.IsNullOrWhiteSpace(input.SlaveConnections) && !JSON.IsValid(input.SlaveConnections))
            throw Oops.Oh(ErrorCodeEnum.D1302);

        switch (input.TenantType)
        {
            case TenantTypeEnum.Id:
                var config = _sysTenantRep.AsSugarClient().CurrentConnectionConfig;
                input.DbType = config.DbType;
                input.Connection = config.ConnectionString;
                break;
            case TenantTypeEnum.Db:
                if (string.IsNullOrWhiteSpace(input.Connection))
                    throw Oops.Oh(ErrorCodeEnum.Z1004);
                break;
            default:
                throw Oops.Oh(ErrorCodeEnum.D3004);
        }

        if (input.EnableReg == YesNoEnum.N) input.RegWayId = null;
        var tenant = input.Adapt<TenantOutput>();
        await SetLogoUrl(tenant, input.LogoBase64, input.LogoFileName);
        tenant.Id = _sysTenantRep.InsertReturnEntity(tenant).Id;
        await InitNewTenant(tenant);
        await CacheTenant(tenant.Id);
    }

    /// <summary>
    /// 更新租户。
    /// </summary>
    /// <param name="input">租户入参</param>
    public async Task UpdateTenant(UpdateTenantInput input)
    {
        EnsurePlatformTenantManage();

        var isExist = await _sysOrgRep.IsAnyAsync(u => u.Name == input.Name && u.Id != input.OrgId);
        if (isExist) throw Oops.Oh(ErrorCodeEnum.D1300);

        input.Host = NormalizeAndEnsureUniqueHosts(input.Host, input.Id);

        if (!string.IsNullOrWhiteSpace(input.SlaveConnections) && !JSON.IsValid(input.SlaveConnections))
            throw Oops.Oh(ErrorCodeEnum.D1302);

        if (input.EnableReg == YesNoEnum.N) input.RegWayId = null;
        var tenant = input.Adapt<SysTenant>();
        await SetLogoUrl(tenant, input.LogoBase64, input.LogoFileName);
        await _sysTenantRep.AsUpdateable(tenant).IgnoreColumns(true).ExecuteCommandAsync();
        await _sysOrgRep.UpdateAsync(u => new SysOrg { Name = input.Name }, u => u.Id == input.OrgId);
        await _sysUserRep.UpdateAsync(u => new SysUser { Account = input.AdminAccount, Phone = input.Phone, Email = input.Email }, u => u.Id == input.UserId);
        await CacheTenant(input.Id);
    }

    /// <summary>
    /// 删除租户。
    /// </summary>
    /// <param name="input">租户 Id</param>
    public async Task DeleteTenant(DeleteTenantInput input)
    {
        EnsurePlatformTenantManage();

        if (input.Id <= 0) throw Oops.Oh(ErrorCodeEnum.D0009);
        if (input.Id.ToString() == SqlSugarConst.MainConfigId) throw Oops.Oh(ErrorCodeEnum.D1023);

        var isOpenAccessTenant = await _sysTenantRep.ChangeRepository<SqlSugarRepository<SysOpenAccess>>()
            .IsAnyAsync(u => u.BindTenantId == input.Id);
        if (isOpenAccessTenant) throw Oops.Oh(ErrorCodeEnum.D1031);

        await _sysTenantRep.DeleteAsync(u => u.Id == input.Id);
        await CacheTenant(input.Id);

        await _sysTenantMenuRep.AsDeleteable().Where(u => u.TenantId == input.Id).ExecuteCommandAsync();
        await _sysTenantRep.Context.Deleteable<SysTenantConfigData>().Where(u => u.TenantId == input.Id).ExecuteCommandAsync();

        var users = await _sysUserRep.AsQueryable().ClearFilter().Where(u => u.TenantId == input.Id).ToListAsync();
        var userIds = users.Select(u => u.Id).ToList();
        await _sysUserRep.AsDeleteable().Where(u => userIds.Contains(u.Id)).ExecuteCommandAsync();
        await _userRoleRep.AsDeleteable().Where(u => userIds.Contains(u.UserId)).ExecuteCommandAsync();
        await _sysUserExtOrgRep.AsDeleteable().Where(u => userIds.Contains(u.UserId)).ExecuteCommandAsync();
        await _sysTenantRep.Context.Deleteable<SysUserMenu>().Where(u => userIds.Contains(u.UserId)).ExecuteCommandAsync();
        await _sysTenantRep.Context.Deleteable<SysUserConfigData>().Where(u => userIds.Contains(u.UserId)).ExecuteCommandAsync();

        var roleIds = await _sysRoleRep.AsQueryable().ClearFilter().Where(u => u.TenantId == input.Id).Select(u => u.Id).ToListAsync();
        await _sysRoleRep.AsDeleteable().Where(u => u.TenantId == input.Id).ExecuteCommandAsync();
        await _sysRoleMenuRep.AsDeleteable().Where(u => roleIds.Contains(u.RoleId)).ExecuteCommandAsync();
        await _sysTenantRep.Context.Deleteable<SysRoleOrg>().Where(u => roleIds.Contains(u.RoleId)).ExecuteCommandAsync();
        await _sysOrgRep.AsDeleteable().Where(u => u.TenantId == input.Id).ExecuteCommandAsync();
        await _sysPosRep.AsDeleteable().Where(u => u.TenantId == input.Id).ExecuteCommandAsync();
    }

    /// <summary>
    /// 新增租户初始化机构、角色、职位、管理员与默认菜单。
    /// </summary>
    private async Task InitNewTenant(TenantOutput tenant)
    {
        var tenantId = tenant.Id;
        var tenantName = tenant.Name;

        var newOrg = new SysOrg { TenantId = tenantId, Pid = 0, Name = tenantName, Code = tenantName, Remark = tenantName };
        await _sysOrgRep.InsertAsync(newOrg);

        var newRole = new SysRole
        {
            TenantId = tenantId,
            Name = CommonConst.DefaultBaseRoleName,
            Code = CommonConst.DefaultBaseRoleCode,
            DataScope = DataScopeEnum.Self,
            Remark = "此角色为系统自动创建角色"
        };
        var baseRole = await _sysRoleRep.InsertReturnEntityAsync(newRole);
        var baseRoleMenuIdList = GetBaseRoleMenuIdList().ToList();
        await _roleMenuManager.GrantRoleMenu(new RoleMenuInput { Id = baseRole.Id, MenuIdList = baseRoleMenuIdList.Select(u => u.MenuId).ToList() });

        var newPos = new SysPos { TenantId = tenantId, Name = "管理员-" + tenantName, Code = tenantName, Remark = tenantName };
        await _sysPosRep.InsertAsync(newPos);

        var password = await _configReader.GetConfigValue<string>(ConfigConst.SysPassword);
        var newUser = new SysUser
        {
            TenantId = tenantId,
            Account = tenant.AdminAccount,
            Password = CryptogramUtil.Encrypt(password),
            NickName = "系统管理员",
            Email = tenant.Email,
            Phone = tenant.Phone,
            AccountType = AccountTypeEnum.SysAdmin,
            OrgId = newOrg.Id,
            PosId = newPos.Id,
            Birthday = DateTime.Parse("2000-01-01"),
            RealName = "系统管理员",
            Remark = "系统管理员" + tenantName,
        };
        await _sysUserRep.InsertAsync(newUser);
        await _sysTenantRep.UpdateAsync(u => new SysTenant { UserId = newUser.Id, OrgId = newOrg.Id }, u => u.Id == tenantId);

        var menuList = GetTenantDefaultMenuList().ToList();
        await GrantMenuCore(new TenantMenuInput { Id = tenantId, MenuIdList = menuList.Select(u => u.MenuId).ToList() });
    }

    /// <summary>
    /// 设置租户启用/禁用状态。
    /// </summary>
    /// <param name="input">租户 Id 与状态</param>
    /// <returns>更新行数</returns>
    public async Task<int> SetStatus(TenantInput input)
    {
        EnsurePlatformTenantManage();
        var tenant = await _sysTenantRep.GetFirstAsync(u => u.Id == input.Id);
        if (tenant == null || tenant.ConfigId == SqlSugarConst.MainConfigId) throw Oops.Oh(ErrorCodeEnum.Z1001);
        if (!Enum.IsDefined(typeof(StatusEnum), input.Status)) throw Oops.Oh(ErrorCodeEnum.D3005);
        tenant.Status = input.Status;
        return await _sysTenantRep.AsUpdateable(tenant).UpdateColumns(u => new { u.Status }).ExecuteCommandAsync();
    }

    /// <summary>
    /// 授权租户菜单。
    /// </summary>
    /// <param name="input">租户与菜单 Id 列表</param>
    public async Task GrantMenu(TenantMenuInput input)
    {
        EnsurePlatformTenantManage();
        await GrantMenuCore(input);
    }

    /// <summary>
    /// 获取租户已授权菜单 Id 列表。
    /// </summary>
    /// <param name="input">租户 Id</param>
    /// <returns>菜单 Id</returns>
    public async Task<List<long>> GetTenantMenuList(BaseIdInput input)
    {
        EnsurePlatformTenantManage();
        return await _sysTenantMenuRep.AsQueryable().Where(u => u.TenantId == input.Id).Select(u => u.MenuId).ToListAsync();
    }

    /// <summary>
    /// 全部菜单清单（供总后台授权勾选；不含已废弃的平台管理页）。
    /// </summary>
    /// <returns>菜单简表</returns>
    public async Task<List<PlatformMenuItem>> ListAllMenus()
    {
        EnsurePlatformTenantManage();
        return await _sysTenantRep.Context.Queryable<SysMenu>()
            .OrderBy(u => u.OrderNo)
            .Select(u => new PlatformMenuItem
            {
                Id = u.Id,
                Pid = u.Pid,
                Title = u.Title,
                Type = u.Type,
                Path = u.Path
            })
            .ToListAsync();
    }

    /// <summary>
    /// 重置租户管理员密码为系统默认密码，并返回明文默认密码。
    /// </summary>
    /// <param name="input">用户 Id</param>
    /// <returns>默认明文密码</returns>
    public async Task<string> ResetPwd(TenantUserInput input)
    {
        EnsurePlatformTenantManage();
        var password = await _configReader.GetConfigValue<string>(ConfigConst.SysPassword);
        var encryptPassword = CryptogramUtil.Encrypt(password);
        await _sysUserRep.UpdateAsync(u => new SysUser { Password = encryptPassword }, u => u.Id == input.UserId);
        return password;
    }

    /// <summary>
    /// 为独立库租户创建/初始化租户数据库。
    /// </summary>
    /// <param name="input">租户 Id</param>
    public async Task CreateDb(TenantInput input)
    {
        EnsurePlatformTenantManage();
        var tenant = await _sysTenantRep.GetSingleAsync(u => u.Id == input.Id);
        if (tenant == null) return;

        if (tenant.DbType == SqlSugar.DbType.Oracle)
            throw Oops.Oh(ErrorCodeEnum.Z1002);
        if (string.IsNullOrWhiteSpace(tenant.Connection) || tenant.Connection.Length < 10)
            throw Oops.Oh(ErrorCodeEnum.Z1004);

        var defaultConfig = App.GetOptions<DbConnectionOptions>().ConnectionConfigs.FirstOrDefault();
        var connectionString = ResolveTenantConnectionString(tenant.Connection);
        var config = new DbConnectionConfig
        {
            ConfigId = tenant.Id.ToString(),
            DbType = tenant.DbType,
            ConnectionString = connectionString,
            DbSettings = new DbSettings()
            {
                EnableInitDb = true,
                EnableDiffLog = false,
                EnableUnderLine = defaultConfig!.DbSettings.EnableUnderLine,
            }
        };
        var iTenant = App.GetRequiredService<ISqlSugarClient>().AsTenant();
        SqlSugarSetup.InitTenantDatabase(iTenant, config);

        if (tenant.DbType == SqlSugar.DbType.Sqlite && iTenant.IsAnyConnection(tenant.Id.ToString()))
        {
            var scope = iTenant.GetConnectionScope(tenant.Id.ToString());
            scope.Ado.Open();
            try
            {
                scope.Ado.ExecuteCommand("pragma user_version=1;");
            }
            finally
            {
                scope.Ado.Close();
            }
        }
    }

    /// <summary>
    /// 同步授权菜单（版本更新后补齐租户菜单）。
    /// </summary>
    /// <param name="input">租户 Id</param>
    public async Task SyncGrantMenu(BaseIdInput input)
    {
        EnsurePlatformTenantManage();
        var menuIdList = input.Id == SqlSugarConst.DefaultTenantId
            ? new SysMenuSeedData().HasData().Select(u => u.Id).ToList()
            : await _sysRoleRep.AsQueryable().ClearFilter()
                .InnerJoin<SysTenant>((u, t) => t.Id == input.Id && u.TenantId == t.Id)
                .InnerJoin<SysRoleMenu>((u, t, rm) => u.Id == rm.RoleId)
                .Select((u, t, rm) => rm.MenuId)
                .Distinct()
                .ToListAsync() ?? throw Oops.Oh(ErrorCodeEnum.D1019);

        var adminRole = await _sysRoleRep.AsQueryable().ClearFilter().FirstAsync(u => u.TenantId == input.Id && u.Code == "sys_admin");
        if (adminRole != null)
        {
            await _userRoleRep.AsDeleteable().Where(u => u.RoleId == adminRole.Id).ExecuteCommandAsync();
            await _sysRoleMenuRep.AsDeleteable().Where(u => u.RoleId == adminRole.Id).ExecuteCommandAsync();
            await _sysRoleRep.AsDeleteable().Where(u => u.Id == adminRole.Id).ExecuteCommandAsync();
        }

        await GrantMenuCore(new TenantMenuInput { Id = input.Id, MenuIdList = menuIdList });
    }

    /// <summary>
    /// 进入指定租户的管理员会话（签发该租户管理员令牌）。
    /// </summary>
    /// <param name="input">租户 Id</param>
    /// <returns>登录令牌</returns>
    public async Task<LoginOutput> GoTenant(BaseIdInput input)
    {
        EnsurePlatformTenantManage();
        var tenant = await _sysTenantRep.GetFirstAsync(u => u.Id == input.Id) ?? throw Oops.Oh(ErrorCodeEnum.D1002);
        var user = await _sysUserRep.GetFirstAsync(u => u.Id == tenant.UserId) ?? throw Oops.Oh(ErrorCodeEnum.D1002);
        return await _authLogin.CreateToken(user, skipSingleLogin: true);
    }

    /// <summary>
    /// 当前超管切换到指定租户上下文并重新发令牌。
    /// </summary>
    /// <param name="input">目标租户 Id</param>
    /// <returns>登录令牌</returns>
    public async Task<LoginOutput> ChangeTenant(BaseIdInput input)
    {
        EnsurePlatformTenantManage();
        var userId = (App.HttpContext?.User.FindFirst(ClaimConst.UserId)?.Value)?.ToLong();
        _ = await _sysTenantRep.GetFirstAsync(u => u.Id == input.Id) ?? throw Oops.Oh(ErrorCodeEnum.D1002);
        var user = await _sysUserRep.GetFirstAsync(u => u.Id == userId) ?? throw Oops.Oh(ErrorCodeEnum.D1002);
        user.TenantId = input.Id;
        return await _authLogin.CreateToken(user, skipSingleLogin: true);
    }

    /// <summary>
    /// 列出某租户下的用户。
    /// </summary>
    /// <param name="input">租户 Id</param>
    /// <returns>用户列表</returns>
    public async Task<List<SysUser>> UserList(TenantIdInput input)
    {
        EnsurePlatformTenantManage();
        return await _sysUserRep.AsQueryable().ClearFilter().Where(u => u.TenantId == input.TenantId).ToListAsync();
    }

    /// <summary>
    /// 授权租户菜单（内部，新增租户时也可调）。
    /// </summary>
    private async Task GrantMenuCore(TenantMenuInput input)
    {
        var menuList = await _sysTenantRep.Context.Queryable<SysMenu>()
            .Where(u => input.MenuIdList.Contains(u.Id))
            .ToListAsync();

        if (menuList.Where(u => u.Type != MenuTypeEnum.Btn).GroupBy(u => new { u.Pid, u.Title }).Any(u => u.Count() > 1)
            || menuList.Where(u => u.Type == MenuTypeEnum.Btn).GroupBy(u => u.Permission).Any(u => u.Count() > 1))
            throw Oops.Oh(ErrorCodeEnum.D1304);

        if (menuList.Where(u => !string.IsNullOrWhiteSpace(u.Name)).GroupBy(u => u.Name).Any(u => u.Count() > 1))
            throw Oops.Oh(ErrorCodeEnum.D4009);

        var tenantMenuList = input.Id == SqlSugarConst.DefaultTenantId
            ? await _sysTenantMenuRep.AsQueryable().Where(u => u.TenantId == input.Id).ToListAsync()
            : null;

        List<long> tenantIdList = [input.Id];
        if (input.TenantIdList?.Count > 0) tenantIdList.AddRange(input.TenantIdList);
        await _sysTenantMenuRep.AsDeleteable().Where(u => tenantIdList.Contains(u.TenantId)).ExecuteCommandAsync();

        var allIdList = await _sysTenantRep.Context.Queryable<SysMenu>().Select(u => new { u.Id, u.Pid }).ToListAsync();
        var pIdList = allIdList.ToChildList(u => u.Pid, u => u.Id, u => input.MenuIdList.Contains(u.Id)).Select(u => u.Pid).Distinct().ToList();
        input.MenuIdList = input.MenuIdList.Concat(pIdList).Distinct().Where(u => u != 0).ToList();

        List<SysTenantMenu> sysTenantMenuList = new();
        tenantIdList.ForEach(tenantId =>
        {
            sysTenantMenuList.AddRange(input.MenuIdList.Select(menuId => new SysTenantMenu { TenantId = tenantId, MenuId = menuId }));
        });

        if (tenantMenuList != null)
        {
            foreach (var item in sysTenantMenuList)
            {
                var tenantMenu = tenantMenuList.FirstOrDefault(u => u.TenantId == item.TenantId && u.MenuId == item.MenuId);
                if (tenantMenu != null) item.Id = tenantMenu.Id;
            }
        }
        await _sysTenantMenuRep.InsertRangeAsync(sysTenantMenuList);
        _cache.Remove(PermissionCacheKeys.UserButton, null);
        _cache.Remove(PermissionCacheKeys.AllButton, "all");
    }

    /// <summary>
    /// 获取租户默认菜单集合。
    /// </summary>
    private IEnumerable<SysTenantMenu> GetTenantDefaultMenuList(bool ignoreHome = false)
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
    private IEnumerable<SysTenantMenu> GetBaseRoleMenuIdList()
    {
        var menuList = new List<SysMenu>();
        var frameworkMenus = new SysMenuSeedData().HasData().ToList();
        var siteMenus = new SiteAdminMenuSeedData().HasData().ToList();

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
    /// 写租户图标。
    /// </summary>
    private async Task SetLogoUrl(SysTenant tenant, string logoBase64, string logoFileName)
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

    private static string ToStorageKey(string logo)
    {
        if (string.IsNullOrWhiteSpace(logo)) return null;
        return logo.Replace('\\', '/').TrimStart('/');
    }

    /// <summary>
    /// 刷新一个租户的缓存：丢掉库连接后按库里最新数据重写。
    /// </summary>
    /// <param name="tenantId">租户 Id</param>
    private async Task CacheTenant(long tenantId = 0)
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

        if (!string.IsNullOrWhiteSpace(tenant.Connection))
            tenant.Connection = CryptogramUtil.SM2Encrypt(tenant.Connection);
        _cache.Add(TenantCacheKeys.Tenant, tenantId, tenant, TimeSpan.FromHours(1));
    }

    /// <summary>
    /// 还原可用的租户库连接串。
    /// </summary>
    private static string ResolveTenantConnectionString(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return raw;
        if (LooksLikeDbConnectionString(raw))
            return raw;
        return CryptogramUtil.SM2Decrypt(raw);
    }

    /// <summary>
    /// 粗判是不是数据库连接串。
    /// </summary>
    private static bool LooksLikeDbConnectionString(string value)
    {
        return value.Contains("Data Source", StringComparison.OrdinalIgnoreCase)
            || value.Contains("DataSource", StringComparison.OrdinalIgnoreCase)
            || value.Contains("Server=", StringComparison.OrdinalIgnoreCase)
            || value.Contains("Host=", StringComparison.OrdinalIgnoreCase)
            || value.Contains("USER ID=", StringComparison.OrdinalIgnoreCase)
            || value.Contains("Uid=", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 规范化多域名并检查与其他租户不冲突。
    /// </summary>
    /// <param name="hostField">入参 Host</param>
    /// <param name="excludeTenantId">更新时排除自己</param>
    /// <returns>规范化后的入库字符串</returns>
    private string NormalizeAndEnsureUniqueHosts(string hostField, long excludeTenantId = 0)
    {
        var hosts = TenantHostBinding.SplitHosts(hostField);
        if (hosts.Count == 0)
            throw Oops.Oh(ErrorCodeEnum.Z1005);

        var normalized = TenantHostBinding.NormalizeStore(hosts);
        var others = _sysTenantRep.AsQueryable()
            .Where(u => !string.IsNullOrWhiteSpace(u.Host))
            .WhereIF(excludeTenantId > 0, u => u.Id != excludeTenantId)
            .Select(u => u.Host)
            .ToList();
        if (others.Any(u => TenantHostBinding.Overlaps(u, normalized)))
            throw Oops.Oh(ErrorCodeEnum.D1303);
        return normalized;
    }

    /// <summary>
    /// 租户增删改只允许在总后台进程做。
    /// </summary>
    private static void EnsurePlatformTenantManage()
    {
        if (!HostTenantMode.IsPlatformHost)
            throw Oops.Oh(ErrorCodeEnum.SA001);
    }
}
