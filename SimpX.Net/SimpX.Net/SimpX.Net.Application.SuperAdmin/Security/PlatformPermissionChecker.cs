using SimpX.Net.Feature.Auth;

namespace SimpX.Net.Application.SuperAdmin;

/// <summary>
/// 总后台权限检查器。Shared 里部分服务依赖 <see cref="IPermissionChecker"/>，
/// 总后台进程必须注册实现才能启动。总后台只接受超管登录，按钮级细权后续再对齐菜单服务。
/// </summary>
public sealed class PlatformPermissionChecker : IPermissionChecker, IScoped
{
    /// <summary>
    /// 当前先一律放行；真正拦截靠总后台登录身份与 JWT 处理链。
    /// </summary>
    /// <param name="permissionCode">权限码</param>
    /// <returns>允许</returns>
    public Task<bool> IsGrantedAsync(string permissionCode) => Task.FromResult(true);
}
