using SimpX.Net.Data.Entity.Sites;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 站点第三方登录授权配置（只读）。
/// </summary>
public sealed class SiteAuthorizationService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;

    /// <summary>
    /// 注入依赖。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站</param>
    public SiteAuthorizationService(IBizDbAccessor biz, ICurrentSiteAccessor site)
    {
        _db = biz.Db;
        _site = site;
    }

    /// <summary>已开放的第三方登录类型名列表（对齐 /request/api/auth/gettype）。</summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>类型名数组</returns>
    public async Task<List<string>> ListOpenTypesAsync(CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<SiteAuthorization>()
            .Where(a => a.SiteID == siteId && a.IsOpen)
            .Select(a => a.Type)
            .ToListAsync(cancellationToken);
        var names = new List<string>();
        foreach (var t in rows)
        {
            var b = unchecked((byte)t);
            names.Add(Enum.IsDefined(typeof(SiteAuthType), b)
                ? ((SiteAuthType)b).ToString()
                : t.ToString());
        }
        names = names.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (names.Count == 0)
            throw Oops.Oh("#不存在第三方登录配置#");
        return names;
    }

    /// <summary>后台读取某类型配置（对齐 /admin/site/auth/getsetting）。</summary>
    /// <param name="type">AuthType</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>Type/IsOpen/Setting</returns>
    public async Task<object> GetSettingAsync(int type, CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<SiteAuthorization>()
            .Where(a => a.SiteID == siteId && a.Type == type).Take(1).ToListAsync(cancellationToken);
        var auth = rows.FirstOrDefault();
        if (auth == null)
        {
            var b = unchecked((byte)type);
            var label = Enum.IsDefined(typeof(SiteAuthType), b) ? ((SiteAuthType)b).ToString() : type.ToString();
            throw Oops.Oh($"系统暂不支持{label}");
        }
        return new
        {
            auth.Type,
            auth.IsOpen,
            Setting = auth.Setting ?? string.Empty
        };
    }

    /// <summary>后台保存某类型配置（对齐 /admin/site/auth/savesetting）。</summary>
    /// <param name="type">AuthType</param>
    /// <param name="isOpen">是否开启</param>
    /// <param name="setting">参数原文</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SaveSettingAsync(
        int type,
        bool isOpen,
        string setting,
        CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<SiteAuthorization>()
            .Where(a => a.SiteID == siteId && a.Type == type).Take(1).ToListAsync(cancellationToken);
        var auth = rows.FirstOrDefault();
        if (auth == null)
        {
            await _db.Insertable(new SiteAuthorization
            {
                SiteID = siteId,
                Type = type,
                IsOpen = isOpen,
                Setting = setting ?? string.Empty
            }).ExecuteCommandAsync(cancellationToken);
            return;
        }

        auth.IsOpen = isOpen;
        auth.Setting = setting ?? string.Empty;
        await _db.Updateable(auth).ExecuteCommandAsync(cancellationToken);
    }
}

/// <summary>旧 BW.Gateway.Authorization.AuthType。</summary>
public enum SiteAuthType : byte
{
    /// <summary>谷歌。</summary>
    Google = 0,

    /// <summary>Steam。</summary>
    Steam = 1,

    /// <summary>Line。</summary>
    Line = 2,

    /// <summary>Telegram。</summary>
    Telegram = 3,

    /// <summary>微信。</summary>
    Wechat = 8,

    /// <summary>QQ。</summary>
    QQ = 9,

    /// <summary>脸书。</summary>
    Facebook = 10
}
