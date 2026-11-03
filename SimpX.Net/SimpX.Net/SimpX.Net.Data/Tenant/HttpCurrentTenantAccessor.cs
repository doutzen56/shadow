using Microsoft.AspNetCore.Http;
using SimpX.Net.Core;

namespace SimpX.Net.Data;

/// <summary>
/// 从 HttpContext.Items（按域名确定到的租户）读取当前租户；未登录前没有 Claim 也可用。
/// </summary>
public sealed class HttpCurrentTenantAccessor : ICurrentTenantAccessor, IScoped
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// 注入 HTTP 上下文。
    /// </summary>
    /// <param name="httpContextAccessor">HTTP 上下文访问器</param>
    public HttpCurrentTenantAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public long? TenantId
    {
        get
        {
            var items = _httpContextAccessor.HttpContext?.Items;
            if (items != null && items.TryGetValue(TenantHostKeys.ResolvedTenantId, out var value) && value is long id)
                return id;
            return null;
        }
    }

    /// <inheritdoc />
    public string Host
    {
        get
        {
            var items = _httpContextAccessor.HttpContext?.Items;
            if (items != null && items.TryGetValue(TenantHostKeys.ResolvedHost, out var value) && value is string host)
                return host;
            return null;
        }
    }

    /// <inheritdoc />
    public long RequireTenantId()
    {
        return TenantId ?? throw Oops.Oh(ErrorCodeEnum.Z1005);
    }
}
