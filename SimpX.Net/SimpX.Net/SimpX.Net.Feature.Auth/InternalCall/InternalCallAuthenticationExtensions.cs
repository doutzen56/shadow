using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace SimpX.Net.Feature.Auth;

/// <summary>
/// 注册内部签名身份验证。复用 SignatureAuthenticationHandler 的验签和“同一请求不能重复提交”检查，密钥从 InternalCall 配置读，不查后台开放接口表。
/// </summary>
public static class InternalCallAuthenticationExtensions
{
    /// <summary>
    /// 添加 InternalSignature 方案。
    /// </summary>
    /// <param name="builder">身份验证构建器</param>
    /// <returns>同一个构建器</returns>
    public static AuthenticationBuilder AddInternalCallAuthentication(this AuthenticationBuilder builder)
    {
        return builder.AddScheme<SignatureAuthenticationOptions, SignatureAuthenticationHandler>(
            InternalCallDefaults.AuthenticationScheme,
            options =>
            {
                options.Events = new SignatureAuthenticationEvent
                {
                    OnGetAccessSecret = context =>
                    {
                        var opt = context.HttpContext.RequestServices.GetRequiredService<IOptionsMonitor<InternalCallOptions>>().CurrentValue;
                        var ok = !string.IsNullOrEmpty(opt.AccessKey) && string.Equals(context.AccessKey, opt.AccessKey, StringComparison.Ordinal);
                        return Task.FromResult(ok ? opt.AccessSecret ?? string.Empty : string.Empty);
                    },
                    OnValidated = context =>
                    {
                        var identity = (ClaimsIdentity)context.Principal!.Identity!;
                        identity.AddClaim(new Claim(InternalCallDefaults.ClaimType, context.AccessKey));
                        return Task.CompletedTask;
                    }
                };
            });
    }
}
