using Furion.Authorization;
using Furion.DataEncryption;
using Microsoft.AspNetCore.Http;

namespace SimpX.Net.Application.WebApi;

/// <summary>
/// 会员鉴权。只认 MemberId，不走后台菜单权限。由会员认证组件注册。
/// </summary>
public class MemberJwtHandler : AppAuthorizeHandler
{
    /// <summary>
    /// 校验会员令牌是否有效；无效则失败。
    /// </summary>
    /// <param name="context">授权上下文</param>
    /// <param name="httpContext">当前请求</param>
    /// <returns>异步任务</returns>
    public override async Task HandleAsync(AuthorizationHandlerContext context, DefaultHttpContext httpContext)
    {
        var memberId = context.User.FindFirst(ClaimConst.MemberId)?.Value;
        if (string.IsNullOrWhiteSpace(memberId))
        {
            context.Fail();
            return;
        }

        // 自动刷新；过期则失败
        if (JWTEncryption.AutoRefreshToken(context, context.GetCurrentHttpContext(), 10080, 20160))
            await AuthorizeHandleAsync(context);
        else
            context.Fail();
    }

    /// <summary>
    /// 会员只要登录即可，不做按钮权限校验。
    /// </summary>
    /// <param name="context">授权上下文</param>
    /// <param name="httpContext">当前请求</param>
    /// <returns>始终允许</returns>
    public override Task<bool> PipelineAsync(AuthorizationHandlerContext context, DefaultHttpContext httpContext)
    {
        return Task.FromResult(true);
    }
}
