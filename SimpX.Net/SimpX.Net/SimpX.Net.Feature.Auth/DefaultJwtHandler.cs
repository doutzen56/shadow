using Furion.Authorization;
using Furion.DataEncryption;

namespace SimpX.Net.Feature.Auth;

/// <summary>
/// 通用 JWT 授权处理：内部签名调用直接过；用户令牌查黑名单并自动续期；
/// 未注册权限检查器的宿主对用户令牌一律拒绝（适合 Schedule / Channels 等 Worker）。
/// </summary>
public class DefaultJwtHandler : AppAuthorizeHandler
{
    private const int DefaultAccessTokenExpire = 20;
    private const int DefaultRefreshTokenExpire = 40;

    /// <summary>
    /// 授权入口：内部调用直接过；否则先查黑名单，再按有效期自动续期。
    /// </summary>
    /// <param name="context">授权上下文</param>
    /// <param name="httpContext">当前请求</param>
    /// <returns>异步任务</returns>
    public override async Task HandleAsync(AuthorizationHandlerContext context, DefaultHttpContext httpContext)
    {
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
            if (currentHttpContext.Items.ContainsKey(SignatureAuthenticationDefaults.AuthenticateFailMsgKey)) return;
            currentHttpContext.SignoutToSwagger();
        }
    }

    /// <summary>
    /// 令牌有效后的权限检查：有检查器才按权限码判断，没有则拒绝用户令牌。
    /// </summary>
    /// <param name="context">授权上下文</param>
    /// <param name="httpContext">当前请求</param>
    /// <returns>允许访问返回 true</returns>
    public override Task<bool> PipelineAsync(AuthorizationHandlerContext context, DefaultHttpContext httpContext)
    {
        return CheckAuthorizeAsync(httpContext);
    }

    /// <summary>
    /// 把请求路径换成权限码交给 <see cref="IPermissionChecker"/>；没注册检查器则拒绝。
    /// </summary>
    /// <param name="httpContext">当前请求</param>
    /// <returns>允许访问返回 true</returns>
    private static async Task<bool> CheckAuthorizeAsync(DefaultHttpContext httpContext)
    {
        var checker = httpContext.RequestServices.GetService<IPermissionChecker>();
        if (checker == null) return false;

        var permissionCode = httpContext.Request.Path.StartsWithSegments("/api")
            ? httpContext.Request.Path.Value![5..].Replace("/", ":")
            : httpContext.Request.Path.Value![1..].Replace("/", ":");
        return await checker.IsGrantedAsync(permissionCode);
    }
}
