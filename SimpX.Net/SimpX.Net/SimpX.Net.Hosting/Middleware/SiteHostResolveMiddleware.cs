using Furion.FriendlyException;
using Microsoft.AspNetCore.Http;
using SimpX.Net.Core;
using SimpX.Net.Data;
using System.Threading.Tasks;

namespace SimpX.Net.Hosting;

/// <summary>
/// 按请求主机名解析站点并写入 HttpContext.Items；跟按域名解析租户的中间件并列，禁止把 SiteID 当成 TenantId。
/// 优先读头 X-Forwarded-HostX，否则用 Request.Host（带端口的写法交给解析器规范化）。
/// </summary>
public sealed class SiteHostResolveMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// 保存后续请求处理环节。
    /// </summary>
    /// <param name="next">下一个中间件</param>
    public SiteHostResolveMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// 按域名解析当前站点后，继续处理请求。
    /// </summary>
    /// <param name="context">当前请求</param>
    /// <param name="resolver">按域名解析站点的组件</param>
    /// <returns>异步任务</returns>
    public async Task InvokeAsync(HttpContext context, ISiteHostResolver resolver)
    {
        var caps = HostCapabilities.Current;
        if (!caps.ResolveSiteByHost)
        {
            await _next(context);
            return;
        }

        var host = PickRequestHost(context.Request);
        context.Items[SiteHostKeys.ResolvedHost] = host;

        var siteId = await resolver.ResolveSiteIdAsync(host, context.RequestAborted);
        if (siteId == null)
        {
            if (caps.FailIfSiteHostUnresolved)
                throw Oops.Oh(ErrorCodeEnum.Z1005);
            await _next(context);
            return;
        }

        context.Items[SiteHostKeys.ResolvedSiteId] = siteId.Value;
        await _next(context);
    }

    /// <summary>
    /// 用来确定当前站点的主机：有 X-Forwarded-HostX 用头，否则用 Request.Host（Host:Port）。
    /// </summary>
    /// <param name="request">当前请求</param>
    /// <returns>主机字符串；可能为空</returns>
    public static string PickRequestHost(HttpRequest request)
    {
        if (request == null)
            return null;

        if (request.Headers.TryGetValue(SiteHostResolver.ForwardedHostHeaderName, out var forwarded))
        {
            var value = forwarded.ToString();
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }

        // HostString.Value 含端口，跟旧版 Url.Authority 一样
        return request.Host.Value;
    }
}
