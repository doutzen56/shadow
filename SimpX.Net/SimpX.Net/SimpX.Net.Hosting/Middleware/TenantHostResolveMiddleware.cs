using Furion.FriendlyException;
using Microsoft.AspNetCore.Http;
using SimpX.Net.Core;
using SimpX.Net.Data;
using System.Threading.Tasks;

namespace SimpX.Net.Hosting;

/// <summary>
/// 按请求域名解析租户并写入 HttpContext.Items；解析失败且配置要求失败时抛业务异常。
/// Admin、WebApi 等需要按域名确定租户的进程使用。
/// </summary>
public sealed class TenantHostResolveMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// 保存后续请求处理环节。
    /// </summary>
    /// <param name="next">下一个中间件</param>
    public TenantHostResolveMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// 按域名解析租户后，继续处理请求。
    /// </summary>
    /// <param name="context">当前请求</param>
    /// <param name="resolver">按域名解析租户的组件</param>
    /// <returns>异步任务</returns>
    public async Task InvokeAsync(HttpContext context, ITenantHostResolver resolver)
    {
        var caps = HostCapabilities.Current;
        if (!caps.ResolveTenantByHost)
        {
            await _next(context);
            return;
        }

        var host = context.Request.Host.Host;
        context.Items[TenantHostKeys.ResolvedHost] = host;

        var tenantId = await resolver.ResolveTenantIdAsync(host, context.RequestAborted);
        if (tenantId == null)
        {
            if (caps.FailIfTenantHostUnresolved)
                throw Oops.Oh(ErrorCodeEnum.Z1005);
            await _next(context);
            return;
        }

        context.Items[TenantHostKeys.ResolvedTenantId] = tenantId.Value;
        await _next(context);
    }
}
