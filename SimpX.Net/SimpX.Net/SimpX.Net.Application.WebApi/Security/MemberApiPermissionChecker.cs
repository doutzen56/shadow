using SimpX.Net.Feature.Auth;

namespace SimpX.Net.Application.WebApi;

/// <summary>
/// 会员站权限检查器。Shared 里部分服务构造要注入 <see cref="IPermissionChecker"/>，
/// 会员宿主必须注册一个实现，否则进程起不来。会员接口走旧版登录与匿名白名单，不靠后台菜单权限码。
/// </summary>
public sealed class MemberApiPermissionChecker : IPermissionChecker, IScoped
{
    /// <summary>
    /// 一律放行。真正拦截靠会员令牌、旧版登录特性与匿名白名单。
    /// </summary>
    /// <param name="permissionCode">权限码（会员站一般用不到）</param>
    /// <returns>允许</returns>
    public Task<bool> IsGrantedAsync(string permissionCode) => Task.FromResult(true);
}
