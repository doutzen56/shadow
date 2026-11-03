namespace SimpX.Net.Application.Admin;

/// <summary>
/// 按后台菜单表里的按钮权限检查：用户拥有该权限码就允许；
/// 权限码根本没登记成按钮权限的接口（如登录后的公共查询）也允许，只有登记了但用户没有的才拒绝。
/// </summary>
public sealed class MenuPermissionChecker : IPermissionChecker, IScoped
{
    private readonly MenuAppService _menuAppService;

    /// <summary>
    /// 注入菜单服务（按钮权限带缓存）。
    /// </summary>
    /// <param name="menuAppService">菜单服务</param>
    public MenuPermissionChecker(MenuAppService menuAppService)
    {
        _menuAppService = menuAppService;
    }

    /// <summary>
    /// 判断当前用户是否有权限码。
    /// </summary>
    /// <param name="permissionCode">权限码</param>
    /// <returns>允许访问返回 true</returns>
    public async Task<bool> IsGrantedAsync(string permissionCode)
    {
        var own = await _menuAppService.GetOwnBtnPermList();
        if (own.Exists(u => permissionCode.Equals(u, StringComparison.CurrentCultureIgnoreCase)))
            return true;

        var all = await _menuAppService.GetAllBtnPermList();
        return all.TrueForAll(u => !permissionCode.Equals(u, StringComparison.CurrentCultureIgnoreCase));
    }
}
