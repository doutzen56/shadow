namespace SimpX.Net.Feature.Auth;

/// <summary>
/// 认证组件：只接受宿主之间的内部签名调用（如 Job、渠道回调）。
/// 用户令牌能通过登录检查，但这些宿主没有权限检查器，按钮权限一律判为没有权限。
/// </summary>
public sealed class InternalCallAuthenticationComponent : IServiceComponent
{
    /// <summary>
    /// 注册 JWT（开启全局授权）和内部签名方案。
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="componentContext">组件上下文</param>
    public void Load(IServiceCollection services, ComponentContext componentContext)
    {
        services.AddJwtAuthentication<DefaultJwtHandler>().AddInternalCallAuthentication();
    }
}
