namespace SimpX.Net.Application.Admin;

/// <summary>
/// 后台宿主的认证组件：后台用户令牌（含按钮权限）、宿主间内部签名、开放接口签名（密钥在后台“开放接口”表里）。
/// </summary>
public sealed class AdminAuthenticationComponent : IServiceComponent
{
    /// <summary>
    /// 注册三种认证方式。
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="componentContext">组件上下文</param>
    public void Load(IServiceCollection services, ComponentContext componentContext)
    {
        services.AddJwtAuthentication<AdminJwtHandler>()
            .AddInternalCallAuthentication()
            .AddSignatureAuthentication(options =>
            {
                options.Events = OpenAccessSignatureService.GetSignatureAuthenticationEventImpl();
            });
    }
}
