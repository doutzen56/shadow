using Furion.Authorization;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 后台用户令牌的授权处理（Furion AppAuthorizeHandler）：黑名单、自动续期、按钮权限。
/// 宿主之间的内部签名调用直接通过；令牌有效期和按钮权限通过 <see cref="ITokenLifetimeProvider"/>、
/// <see cref="IPermissionChecker"/> 取，由后台应用实现，Hosting 不依赖任何后台业务服务。
/// </summary>
public class AdminJwtHandler : AppAuthorizeHandler
{
    private const int DefaultAccessTokenExpire = 20;
    private const int DefaultRefreshTokenExpire = 40;

    /// <summary>
    /// 授权入口：内部调用直接过；否则先查黑名单，再按有效期自动续期，续期成功才进入权限检查。
    /// </summary>
    /// <param name="context">授权上下文</param>
    /// <param name="httpContext">当前请求</param>
    /// <returns>异步任务</returns>
    public override async Task HandleAsync(AuthorizationHandlerContext context, DefaultHttpContext httpContext)
    {
        // 宿主之间的内部签名调用：权限已在调用方（如 Admin）检查过，这里不再走用户令牌和按钮权限
        if (context.User.HasClaim(c => c.Type == InternalCallDefaults.ClaimType))
        {
            foreach (var requirement in context.PendingRequirements.ToList())
                context.Succeed(requirement);
            return;
        }

        var services = httpContext.RequestServices;
        var state = services.GetRequiredService<ICacheStateStore>();
        var userId = context.User.FindFirst(ClaimConst.UserId)?.Value;
        var token = httpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

        // 黑名单：被踢下线的用户，或作废令牌（只按令牌摘要查）
        var userBlocked = !string.IsNullOrEmpty(userId) && state.Exists(AuthCacheKeys.UserBlacklist, userId);
        var tokenBlocked = !string.IsNullOrWhiteSpace(token) && state.Exists(AuthCacheKeys.TokenBlacklist, CacheIdHasher.Sha256(token));
        if (userBlocked || tokenBlocked)
        {
            context.Fail();
            context.GetCurrentHttpContext().SignoutToSwagger();
            return;
        }

        var lifetime = services.GetService<ITokenLifetimeProvider>();
        var tokenExpire = lifetime == null ? DefaultAccessTokenExpire : await lifetime.GetAccessTokenExpireAsync();
        var refreshTokenExpire = lifetime == null ? DefaultRefreshTokenExpire : await lifetime.GetRefreshTokenExpireAsync();
        if (JWTEncryption.AutoRefreshToken(context, context.GetCurrentHttpContext(), tokenExpire, refreshTokenExpire))
        {
            await AuthorizeHandleAsync(context);
        }
        else
        {
            context.Fail();
            var currentHttpContext = context.GetCurrentHttpContext();
            if (currentHttpContext == null) return;

            // 开放接口签名失败时已写好失败原因，不再清掉 Swagger 登录态
            if (currentHttpContext.Items.ContainsKey(SignatureAuthenticationDefaults.AuthenticateFailMsgKey)) return;
            currentHttpContext.SignoutToSwagger();
        }
    }

    /// <summary>
    /// 令牌已验证有效后的权限检查。
    /// </summary>
    /// <param name="context">授权上下文</param>
    /// <param name="httpContext">当前请求</param>
    /// <returns>允许访问返回 true</returns>
    public override Task<bool> PipelineAsync(AuthorizationHandlerContext context, DefaultHttpContext httpContext)
    {
        return CheckAuthorizeAsync(httpContext);
    }

    /// <summary>
    /// APP 登录、超级管理员直接过；其余把路由换成权限码（/api/system/user/page → system:user:page）交给 <see cref="IPermissionChecker"/>。
    /// 宿主没注册权限检查器时一律拒绝，避免没有权限数据的宿主被用户令牌访问。
    /// </summary>
    /// <param name="httpContext">当前请求</param>
    /// <returns>允许访问返回 true</returns>
    private static async Task<bool> CheckAuthorizeAsync(DefaultHttpContext httpContext)
    {
        if (App.User.FindFirst(ClaimConst.LoginMode)?.Value == ((int)LoginModeEnum.APP).ToString())
            return true;

        if (App.User.FindFirst(ClaimConst.AccountType)?.Value == ((int)AccountTypeEnum.SuperAdmin).ToString())
            return true;

        var checker = httpContext.RequestServices.GetService<IPermissionChecker>();
        if (checker == null) return false;

        var permissionCode = httpContext.Request.Path.StartsWithSegments("/api")
            ? httpContext.Request.Path.Value![5..].Replace("/", ":")
            : httpContext.Request.Path.Value![1..].Replace("/", ":");
        return await checker.IsGrantedAsync(permissionCode);
    }
}

/// <summary>
/// 从系统配置读令牌有效期，后台改了配置立即生效。
/// </summary>
public sealed class ConfigTokenLifetimeProvider : ITokenLifetimeProvider, IScoped
{
    private readonly SysConfigReader _configReader;

    /// <summary>
    /// 注入配置读取器。
    /// </summary>
    /// <param name="configReader">配置读取器</param>
    public ConfigTokenLifetimeProvider(SysConfigReader configReader)
    {
        _configReader = configReader;
    }

    /// <summary>
    /// 访问令牌有效期（分钟）。
    /// </summary>
    /// <returns>分钟数</returns>
    public Task<int> GetAccessTokenExpireAsync() => _configReader.GetTokenExpire();

    /// <summary>
    /// 刷新令牌有效期（分钟）。
    /// </summary>
    /// <returns>分钟数</returns>
    public Task<int> GetRefreshTokenExpireAsync() => _configReader.GetRefreshTokenExpire();
}
