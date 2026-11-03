using Furion.FriendlyException;
using Microsoft.AspNetCore.Http;
using SimpX.Net.Core;

namespace SimpX.Net.Data;

/// <summary>
/// 从 HttpContext.Items（按域名解析出的站点结果）读取当前站点。
/// </summary>
public sealed class HttpCurrentSiteAccessor : ICurrentSiteAccessor, IScoped
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// 注入 HTTP 上下文。
    /// </summary>
    /// <param name="httpContextAccessor">HTTP 上下文访问器</param>
    public HttpCurrentSiteAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public int? SiteId
    {
        get
        {
            var items = _httpContextAccessor.HttpContext?.Items;
            if (items != null && items.TryGetValue(SiteHostKeys.ResolvedSiteId, out var value) && value is int id)
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
            if (items != null && items.TryGetValue(SiteHostKeys.ResolvedHost, out var value) && value is string host)
                return host;
            return null;
        }
    }

    /// <inheritdoc />
    public int RequireSiteId()
    {
        // 复用「未定上下文」类错误码语义；消息走框架默认，业务兼容层可再包 ErrorType
        return SiteId ?? throw Oops.Oh(ErrorCodeEnum.Z1005);
    }
}
