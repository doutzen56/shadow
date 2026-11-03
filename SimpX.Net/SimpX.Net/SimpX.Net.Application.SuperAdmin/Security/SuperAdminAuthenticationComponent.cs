namespace SimpX.Net.Application.SuperAdmin;

/// <summary>
/// 总后台宿主认证：复用后台用户令牌处理，并挂内部签名与开放接口签名。
/// </summary>
public sealed class SuperAdminAuthenticationComponent : IServiceComponent
{
    /// <summary>
    /// 注册认证方式。
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
