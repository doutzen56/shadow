namespace SimpX.Net.Application.SuperAdmin;

/// <summary>
/// 总后台菜单。对外 /api/platform/menu，给 Vue 框架 loginMenuTree 用。
/// </summary>
[ApiDescriptionSettings(Module = "platform", Name = "menu", Order = 450)]
public class PlatformMenuAppService : IDynamicApiController, ITransient
{
    private readonly SqlSugarRepository<SysMenu> _sysMenuRep;
    private readonly UserManager _userManager;

    /// <summary>
    /// 注入菜单仓储与当前用户。
    /// </summary>
    /// <param name="sysMenuRep">菜单仓储</param>
    /// <param name="userManager">当前登录用户</param>
    public PlatformMenuAppService(SqlSugarRepository<SysMenu> sysMenuRep, UserManager userManager)
    {
        _sysMenuRep = sysMenuRep;
        _userManager = userManager;
    }

    /// <summary>
    /// 登录后侧栏菜单树（不含按钮）。超管看全部启用菜单。
    /// </summary>
    /// <returns>菜单树</returns>
    [DisplayName("获取登录菜单树")]
    public async Task<List<PlatformMenuOutput>> GetLoginMenuTree()
    {
        EnsurePlatformHost();
        if (_userManager.UserId <= 0)
            throw Oops.Oh(ErrorCodeEnum.D1011).StatusCode(401);

        var menuList = await _sysMenuRep.AsQueryable()
            .ClearFilter()
            .Where(u => u.Type != MenuTypeEnum.Btn && u.Status == StatusEnum.Enable)
            .OrderBy(u => new { u.OrderNo, u.Id })
            .ToListAsync();

        // 总后台也不再展示租户侧已下线的框架菜单（工作台/开发工具/帮助文档/关于项目）
        menuList = ExcludeTenantRetiredFrameworkMenus(menuList);

        var tree = menuList.ToTree(it => it.Children, it => it.Pid, 0).ToList();
        return tree.Select(MapMenu).ToList();
    }

    /// <summary>
    /// 拿掉租户后台已废弃的框架菜单树（按历史根 Id 及其下级）。
    /// </summary>
    /// <param name="menuList">待过滤菜单</param>
    /// <returns>过滤后的列表</returns>
    private static List<SysMenu> ExcludeTenantRetiredFrameworkMenus(List<SysMenu> menuList)
    {
        var exclude = new HashSet<long>
        {
            1300100000101, // 工作台
            1300500000101, // 开发工具
            1300600000101, // 帮助文档
            1300700000101, // 关于项目
        };
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

    /// <summary>
    /// 把实体菜单树转成前端 DTO。
    /// </summary>
    /// <param name="menu">菜单实体（可带 Children）</param>
    /// <returns>前端菜单节点</returns>
    private static PlatformMenuOutput MapMenu(SysMenu menu)
    {
        return new PlatformMenuOutput
        {
            Id = menu.Id,
            Pid = menu.Pid,
            Type = menu.Type,
            Name = menu.Name,
            Path = menu.Path,
            Component = menu.Component,
            Permission = menu.Permission,
            Redirect = menu.Redirect,
            OrderNo = menu.OrderNo,
            Status = menu.Status,
            Remark = menu.Remark,
            Meta = new PlatformMenuMeta
            {
                Title = menu.Title,
                Icon = menu.Icon,
                IsIframe = menu.IsIframe,
                IsLink = menu.OutLink,
                IsHide = menu.IsHide,
                IsKeepAlive = menu.IsKeepAlive,
                IsAffix = menu.IsAffix,
            },
            Children = menu.Children?.Select(MapMenu).ToList() ?? [],
        };
    }

    /// <summary>
    /// 只允许总后台进程调用。
    /// </summary>
    private static void EnsurePlatformHost()
    {
        if (!HostTenantMode.IsPlatformHost)
            throw Oops.Oh(ErrorCodeEnum.SA001);
    }
}
