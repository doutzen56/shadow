namespace SimpX.Net.Application.WebApi;

/// <summary>
/// WebApi 宿主的认证组件：会员令牌（只认 MemberId，与后台用户体系隔离）。
/// </summary>
public sealed class MemberAuthenticationComponent : IServiceComponent
{
    /// <summary>
    /// 注册会员令牌认证。
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="componentContext">组件上下文</param>
    public void Load(IServiceCollection services, ComponentContext componentContext)
    {
        services.AddJwtAuthentication<MemberJwtHandler>();
    }
}
