namespace SimpX.Net.Application.Admin;

/// <summary>
/// 系统菜单服务 🧩
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "menu", Order = 450)]
public class MenuAppService : IDynamicApiController, ITransient
{
    private readonly SqlSugarRepository<SysTenantMenu> _sysTenantMenuRep;
    private readonly SqlSugarRepository<SysMenu> _sysMenuRep;
    private readonly RoleMenuManager _roleMenuManager;
    private readonly UserRoleManager _userRoleManager;
    private readonly UserMenuAppService _userMenuAppService;
    private readonly ISimpXCache _cache;
    private readonly CacheInvalidationBatch _invalidations;
    private readonly UserManager _userManager;
    private readonly LangTextCacheManager _langTextCacheManager;
    private readonly LangTextAppService _langTextAppService;

    public MenuAppService(
        SqlSugarRepository<SysTenantMenu> sysTenantMenuRep,
        SqlSugarRepository<SysMenu> sysMenuRep,
        RoleMenuManager roleMenuManager,
        UserRoleManager userRoleManager,
        UserMenuAppService userMenuAppService,
        ISimpXCache cache,
        CacheInvalidationBatch invalidations,
        UserManager userManager,
        LangTextCacheManager langTextCacheManager,
        LangTextAppService langTextAppService)
    {
        _userManager = userManager;
        _sysMenuRep = sysMenuRep;
        _roleMenuManager = roleMenuManager;
        _userRoleManager = userRoleManager;
        _userMenuAppService = userMenuAppService;
        _sysTenantMenuRep = sysTenantMenuRep;
        _cache = cache;
        _invalidations = invalidations;
        _langTextCacheManager = langTextCacheManager;
        _langTextAppService = langTextAppService;
    }

    /// <summary>
    /// 获取登录菜单树 🔖
    /// </summary>
    /// <returns></returns>
    [DisplayName("获取登录菜单树")]
    public async Task<List<MenuOutput>> GetLoginMenuTree()
    {
        var sysDefaultLang = App.GetOptions<LocalizationSettingsOptions>().DefaultCulture;
        var langCode = _userManager.LangCode;
        var (query, _) = GetSugarQueryableAndTenantId(_userManager.TenantId);

        // 查询菜单主表（过滤非按钮和禁用）
        var menuQuery = query.Where(u => u.Type != MenuTypeEnum.Btn && u.Status == StatusEnum.Enable);

        if (!(_userManager.SuperAdmin || _userManager.SysAdmin))
        {
            var menuIdList = await GetMenuIdList();
            menuQuery = menuQuery.Where(u => menuIdList.Contains(u.Id));
        }

        // 查询主表（不再 LEFT JOIN）
        var menuList = await menuQuery
            .OrderBy(u => new { u.OrderNo, u.Id })
            .ToListAsync();

        // 租户后台不展示总后台业务菜单、已废弃的平台管理目录
        if (!HostTenantMode.IsPlatformHost)
            menuList = ExcludePlatformManageMenus(menuList);

        // 仅当用户语言和系统默认语言不同时，才进行翻译，避免不必要的性能开销
        if (langCode != sysDefaultLang)
        {
            // 调用缓存翻译：翻译 Title 字段
            var fields = new List<LangFieldMap<SysMenu>>
            {
                new LangFieldMap<SysMenu>
                {
                    EntityName = "SysMenu",
                    FieldName = "Title",
                    IdSelector = m => m.Id,
                    SetTranslatedValue = (m, val) => m.Title = val
                }
            };
            await _langTextCacheManager.TranslateMultiFields(menuList, fields, langCode);
        }

        // 构造树
        var menuTree = menuList.ToTree(
            it => it.Children, it => it.Pid, 0
        );

        // 转换为输出 DTO
        return menuTree.Adapt<List<MenuOutput>>();
    }

    /// <summary>
    /// 获取菜单列表 🔖
    /// </summary>
    /// <returns></returns>
    [DisplayName("获取菜单列表"), AllowAnonymous]
    public async Task<List<SysMenu>> GetList([FromQuery] MenuInput input)
    {
        var langCode = _userManager.LangCode;
        var menuIdList = _userManager.SuperAdmin || _userManager.SysAdmin ? new List<long>() : await GetMenuIdList();
        var (query, _) = GetSugarQueryableAndTenantId(input.TenantId);

        // 有条件直接查询菜单列表（带 Title、Type 过滤）
        if (!string.IsNullOrWhiteSpace(input.Title) || input.Type is > 0)
        {
            var menuList = await query
                .WhereIF(!string.IsNullOrWhiteSpace(input.Title), u => u.Title.Contains(input.Title))
                .WhereIF(input.Type is > 0, u => u.Type == input.Type)
                .WhereIF(menuIdList.Count > 0, u => menuIdList.Contains(u.Id))
                .OrderBy(u => new { u.OrderNo, u.Id })
                .ToListAsync();

            // 走缓存批量翻译
            var fields = new List<LangFieldMap<SysMenu>>
            {
                new LangFieldMap<SysMenu>
                {
                    EntityName = "SysMenu",
                    FieldName = "Title",
                    IdSelector = m => m.Id,
                    SetTranslatedValue = (m, val) => m.Title = val
                }
            };
            await _langTextCacheManager.TranslateMultiFields(menuList, fields, langCode);

            return menuList.Distinct().ToList();
        }

        // 无筛选条件则走全量树形结构（带权限）
        if (!(_userManager.SuperAdmin || _userManager.SysAdmin))
        {
            query = query.Where(u => menuIdList.Contains(u.Id));
        }

        var menuFullList = await query
            .OrderBy(u => new { u.OrderNo, u.Id })
            .ToListAsync();

        // 走缓存批量翻译
        var treeFields = new List<LangFieldMap<SysMenu>>
        {
            new LangFieldMap<SysMenu>
            {
                    EntityName = "SysMenu",
                    FieldName = "Title",
                    IdSelector = m => m.Id,
                    SetTranslatedValue = (m, val) => m.Title = val
            }
        };
        await _langTextCacheManager.TranslateMultiFields(menuFullList, treeFields, langCode);

        // 组装树
        var menuTree = menuFullList.ToTree(it => it.Children, it => it.Pid, 0);
        return menuTree.ToList();
    }

    /// <summary>
    /// 增加菜单 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("增加菜单")]
    public async Task<long> AddMenu(AddMenuInput input)
    {
        var (query, tenantId) = GetSugarQueryableAndTenantId(input.TenantId);

        var isExist = input.Type != MenuTypeEnum.Btn
            ? await query.AnyAsync(u => u.Title == input.Title && u.Pid == input.Pid)
            : await query.AnyAsync(u => u.Permission == input.Permission);
        if (isExist) throw Oops.Oh(ErrorCodeEnum.D4000);

        if (!string.IsNullOrWhiteSpace(input.Name) && await query.AnyAsync(u => u.Name == input.Name)) throw Oops.Oh(ErrorCodeEnum.D4009);

        if (input.Pid != 0 && await query.AnyAsync(u => u.Id == input.Pid && u.Type == MenuTypeEnum.Btn)) throw Oops.Oh(ErrorCodeEnum.D4010);

        // 校验菜单参数
        var sysMenu = input.Adapt<SysMenu>();
        CheckMenuParam(sysMenu);

        // 保存租户菜单权限
        await _sysMenuRep.InsertAsync(sysMenu);
        await _sysTenantMenuRep.InsertAsync(new SysTenantMenu { TenantId = tenantId, MenuId = sysMenu.Id });

        // 清除缓存
        DeleteMenuCache();

        return sysMenu.Id;
    }

    /// <summary>
    /// 更新菜单 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("更新菜单")]
    public async Task UpdateMenu(UpdateMenuInput input)
    {
        if (!_userManager.SuperAdmin && new SysMenuSeedData().HasData().Any(u => u.Id == input.Id)) throw Oops.Oh(ErrorCodeEnum.D4012);

        if (input.Id == input.Pid) throw Oops.Oh(ErrorCodeEnum.D4008);
        var (query, _) = GetSugarQueryableAndTenantId(input.TenantId);

        var isExist = input.Type != MenuTypeEnum.Btn
            ? await query.AnyAsync(u => u.Title == input.Title && u.Type == input.Type && u.Pid == input.Pid && u.Id != input.Id)
            : await query.AnyAsync(u => u.Permission == input.Permission && u.Id != input.Id);
        if (isExist) throw Oops.Oh(ErrorCodeEnum.D4000);

        if (!string.IsNullOrWhiteSpace(input.Name) && await query.AnyAsync(u => u.Id != input.Id && u.Name == input.Name)) throw Oops.Oh(ErrorCodeEnum.D4009);

        if (input.Pid != 0 && await query.AnyAsync(u => u.Id == input.Pid && u.Type == MenuTypeEnum.Btn)) throw Oops.Oh(ErrorCodeEnum.D4010);

        // 校验菜单参数
        var sysMenu = input.Adapt<SysMenu>();
        CheckMenuParam(sysMenu);

        await _sysMenuRep.AsTenant().UseTranAsync(async () =>
        {
            // 更新菜单
            await _sysMenuRep.AsUpdateable(sysMenu).ExecuteCommandAsync();

            // 同步更新翻译表
            var menuTranslation = await _langTextCacheManager.GetTranslationEntity("SysMenu", "Title", sysMenu.Id, _userManager.LangCode);
            if (!menuTranslation.IsNullOrEmpty())
            {
                await _langTextAppService.Update(new UpdateSysLangTextInput
                {
                    Id = menuTranslation.Id,
                    EntityName = "SysMenu",
                    EntityId = sysMenu.Id,
                    FieldName = "Title",
                    LangCode = _userManager.LangCode,
                    Content = sysMenu.Title
                });
            }
        }, err =>
        {
            Oops.Oh("更新数据时发生错误", err.Message);
        });

        // 清除缓存
        DeleteMenuCache();
    }

    /// <summary>
    /// 删除菜单 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [UnitOfWork]
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("删除菜单")]
    public async Task DeleteMenu(DeleteMenuInput input)
    {
        if (!_userManager.SuperAdmin && new SysMenuSeedData().HasData().Any(u => u.Id == input.Id)) throw Oops.Oh(ErrorCodeEnum.D4013);

        var menuTreeList = await _sysMenuRep.AsQueryable().ToChildListAsync(u => u.Pid, input.Id);
        var menuIdList = menuTreeList.Select(u => u.Id).ToList();

        await _sysMenuRep.DeleteAsync(u => menuIdList.Contains(u.Id));

        // 级联删除租户菜单数据
        await _sysTenantMenuRep.AsDeleteable().Where(u => menuIdList.Contains(u.MenuId)).ExecuteCommandAsync();

        // 级联删除角色菜单数据
        await _roleMenuManager.DeleteRoleMenuByMenuIdList(menuIdList);

        // 级联删除用户收藏菜单
        await _userMenuAppService.DeleteMenuList(menuIdList);

        // 清除缓存
        DeleteMenuCache();
    }

    /// <summary>
    /// 设置菜单状态 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [UnitOfWork]
    [DisplayName("设置菜单状态")]
    public virtual async Task<int> SetStatus(MenuStatusInput input)
    {
        if (_userManager.UserId == input.Id)
            throw Oops.Oh(ErrorCodeEnum.D1026);

        var menu = await _sysMenuRep.GetByIdAsync(input.Id) ?? throw Oops.Oh(ErrorCodeEnum.D1002);
        menu.Status = input.Status;
        var rows = await _sysMenuRep.AsUpdateable(menu).UpdateColumns(u => new { u.Status }).ExecuteCommandAsync();
        return rows;
    }

    /// <summary>
    /// 增加和编辑时检查菜单数据
    /// </summary>
    /// <param name="menu"></param>
    private static void CheckMenuParam(SysMenu menu)
    {
        var permission = menu.Permission;
        if (menu.Type == MenuTypeEnum.Btn)
        {
            menu.Name = null;
            menu.Path = null;
            menu.Component = null;
            menu.Icon = null;
            menu.Redirect = null;
            menu.OutLink = null;
            menu.IsHide = false;
            menu.IsKeepAlive = true;
            menu.IsAffix = false;
            menu.IsIframe = false;

            if (string.IsNullOrEmpty(permission)) throw Oops.Oh(ErrorCodeEnum.D4003);
            if (!permission.Contains(':')) throw Oops.Oh(ErrorCodeEnum.D4004);
        }
        else
        {
            menu.Permission = null;
        }
    }

    /// <summary>
    /// 获取用户拥有按钮权限集合（缓存） 🔖
    /// </summary>
    /// <returns></returns>
    [DisplayName("获取按钮权限集合")]
    public async Task<List<string>> GetOwnBtnPermList()
    {
        var userId = _userManager.UserId;
        var permissions = _cache.Get<List<string>>(PermissionCacheKeys.UserButton, userId);
        if (permissions != null) return permissions;

        var menuIdList = _userManager.SuperAdmin ? new() : await GetMenuIdList();
        if (menuIdList.Count <= 0 && !_userManager.SuperAdmin && !_userManager.SysAdmin)
            return new List<string>();

        permissions = await _sysMenuRep.AsQueryable()
            .InnerJoinIF<SysTenantMenu>(!_userManager.SuperAdmin, (u, t) => t.TenantId == _userManager.TenantId && u.Id == t.MenuId)
            .Where(u => u.Type == MenuTypeEnum.Btn)
            .WhereIF(menuIdList.Count > 0, u => menuIdList.Contains(u.Id))
            .Select(u => u.Permission).ToListAsync();

        _cache.Add(PermissionCacheKeys.UserButton, userId, permissions, TimeSpan.FromMinutes(30));

        return permissions;
    }

    /// <summary>
    /// 获取系统所有按钮权限集合（缓存）
    /// </summary>
    /// <returns></returns>
    [NonAction]
    public async Task<List<string>> GetAllBtnPermList()
    {
        return await _cache.GetOrAddAsync(PermissionCacheKeys.AllButton, "all", async _ =>
        {
            return await _sysMenuRep.AsQueryable()
                .Where(u => u.Type == MenuTypeEnum.Btn)
                .Select(u => u.Permission).ToListAsync();
        }, TimeSpan.FromHours(1));
    }

    /// <summary>
    /// 根据租户id获取构建菜单联表查询实例
    /// </summary>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    [NonAction]
    public (ISugarQueryable<SysMenu, SysTenantMenu> query, long tenantId) GetSugarQueryableAndTenantId(long tenantId)
    {
        if (!_userManager.SuperAdmin) tenantId = _userManager.TenantId;

        // 超管用户菜单范围：种子菜单 + 租户id菜单
        ISugarQueryable<SysMenu, SysTenantMenu> query;
        if (_userManager.SuperAdmin)
        {
            if (tenantId <= 0)
            {
                query = _sysMenuRep.AsQueryable().InnerJoinIF<SysTenantMenu>(false, (u, t) => true);
            }
            else
            {
                // 指定租户的菜单
                var menuIds = _sysTenantMenuRep.AsQueryable().Where(u => u.TenantId == tenantId).ToList(u => u.MenuId) ?? new();

                // 种子菜单
                //menuIds.AddRange(new SysMenuSeedData().HasData().Select(u => u.Id).ToList());

                menuIds = menuIds.Distinct().ToList();
                query = _sysMenuRep.AsQueryable().InnerJoinIF<SysTenantMenu>(false, (u, t) => true).Where(u => menuIds.Contains(u.Id));
            }
        }
        else
        {
            query = _sysMenuRep.AsQueryable().InnerJoinIF<SysTenantMenu>(tenantId > 0, (u, t) => t.TenantId == tenantId && u.Id == t.MenuId);
        }

        return (query, tenantId);
    }

    /// <summary>
    /// 清除菜单和按钮缓存
    /// </summary>
    [NonAction]
    public void DeleteMenuCache()
    {
        // 登记后由事务提交或请求成功时统一删除，避免库还没提交就把缓存清掉又被旧数据填回去
        _invalidations.Add(PermissionCacheKeys.UserButton, null, CacheRemoveMode.ByName);
        _invalidations.Add(PermissionCacheKeys.AllButton, "all", CacheRemoveMode.ByKey);
    }

    /// <summary>
    /// 获取当前用户菜单Id集合
    /// </summary>
    /// <returns></returns>
    [NonAction]
    public async Task<List<long>> GetMenuIdList()
    {
        var roleIdList = await _userRoleManager.GetUserRoleIdList(_userManager.UserId);
        return await _roleMenuManager.GetRoleMenuIdList(roleIdList);
    }

    /// <summary>
    /// 排除前端存在全选的父级菜单
    /// </summary>
    /// <returns></returns>
    [NonAction]
    public async Task<List<long>> ExcludeParentMenuOfFullySelected(List<long> menuIds)
    {
        // 获取当前用户菜单
        var (query, _) = GetSugarQueryableAndTenantId(0);
        var menuList = await query.ToListAsync();

        // 排除列表，防止前端全选问题
        var exceptList = new List<long>();
        foreach (var id in menuIds)
        {
            // 排除按钮菜单
            if (menuList.Any(u => u.Id == id && u.Type == MenuTypeEnum.Btn)) continue;

            // 如果没有子集或有全部子集权限
            var children = menuList.ToChildList(u => u.Id, u => u.Pid, id, false).ToList();
            if (children.Count == 0 || children.All(u => menuIds.Contains(u.Id))) continue;

            // 排除没有全部子集权限的菜单
            exceptList.Add(id);
        }
        return menuIds.Except(exceptList).ToList();
    }

    /// <summary>
    /// 从菜单列表里拿掉「平台管理」目录、总后台业务菜单（Remark=SystemAdminMenu）及其下级。
    /// </summary>
    /// <param name="menuList">待过滤的菜单</param>
    /// <returns>去掉平台管理后的列表</returns>
    private static List<SysMenu> ExcludePlatformManageMenus(List<SysMenu> menuList)
    {
        const long platformDirId = 1300300000101;
        var exclude = new HashSet<long> { platformDirId };
        foreach (var menu in menuList)
        {
            if (string.Equals(menu.Remark, "SystemAdminMenu", StringComparison.Ordinal))
                exclude.Add(menu.Id);
        }

        var grew = true;
        while (grew)
        {
            grew = false;
            foreach (var menu in menuList)
            {
                if (exclude.Contains(menu.Pid) && exclude.Add(menu.Id))
                    grew = true;
            }
        }

        return menuList.Where(u => !exclude.Contains(u.Id)).ToList();
    }
}