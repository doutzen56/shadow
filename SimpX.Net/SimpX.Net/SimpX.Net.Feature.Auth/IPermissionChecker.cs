namespace SimpX.Net.Feature.Auth;

/// <summary>
/// 后台按钮权限检查。由管理后台所在的应用实现（权限数据在后台的菜单表里）；
/// 宿主没有注册实现时，用户令牌访问一律判为没有权限。
/// </summary>
public interface IPermissionChecker
{
    /// <summary>
    /// 当前登录用户是否有该权限码。
    /// </summary>
    /// <param name="permissionCode">权限码，由路由换算而来，如 system:user:page</param>
    /// <returns>允许访问返回 true</returns>
    Task<bool> IsGrantedAsync(string permissionCode);
}
