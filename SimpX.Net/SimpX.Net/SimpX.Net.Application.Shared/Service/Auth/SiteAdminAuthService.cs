using Microsoft.AspNetCore.Http;
using SimpX.Net.Data;
using SimpX.Net.Data.Entity.Sites;
using SqlSugar;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 站点后台登录/登出。在当前站点校验 site_Admin；会话 Cookie+响应头 TOKEN。
/// 错密累计与自动锁定跟旧版 AdminAgent.Login 一样（已知账号错满 5 次锁库；未知账号按 IP 每天最多 20 次）。
/// </summary>
public sealed class SiteAdminAuthService : ITransient
{
    /// <summary>正常状态（对齐 SiteAdmin.AdminStatus.Normal）。</summary>
    public const int StatusNormal = 0;

    /// <summary>锁定状态。</summary>
    public const int StatusLocked = 1;

    /// <summary>已知账号错密上限。</summary>
    public const int MaxFailKnown = 5;

    /// <summary>未知账号按 IP 日上限。</summary>
    public const int MaxFailUnknownIp = 20;

    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ISimpXCache _cache;

    /// <summary>
    /// 注入业务库、当前站点、HTTP 上下文和缓存。站点管理员在 BetWin30 业务库，不能走主库 Sqlite。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站点</param>
    /// <param name="httpContextAccessor">HTTP 上下文</param>
    /// <param name="cache">缓存</param>
    public SiteAdminAuthService(
        IBizDbAccessor biz,
        ICurrentSiteAccessor site,
        IHttpContextAccessor httpContextAccessor,
        ISimpXCache cache)
    {
        _db = biz.Db;
        _site = site;
        _httpContextAccessor = httpContextAccessor;
        _cache = cache;
    }

    /// <summary>
    /// 站点管理员登录。
    /// </summary>
    /// <param name="userName">AdminName</param>
    /// <param name="password">明文密码</param>
    /// <param name="code">谷歌验证码；未启用密钥时可空</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>旧版返回格式；成功时响应头带 TOKEN</returns>
    public async Task<OldApiResult> LoginAsync(
        string userName,
        string password,
        string code,
        CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        userName = userName?.Trim();
        var clientIp = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";

        if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))
            return OldApiResult.Fail("#用户名或者密码错误#");

        var rows = await _db.Queryable<SiteAdmin>()
            .Where(a => a.SiteID == siteId && a.AdminName == userName)
            .Take(1)
            .ToListAsync(cancellationToken);
        var admin = rows.FirstOrDefault();
        if (admin == null)
        {
            var unknownCount = IncrementUnknownFail(clientIp);
            if (unknownCount > MaxFailUnknownIp)
                return OldApiResult.Fail("错误次数过多,明日再试");
            return OldApiResult.Fail("#用户名或者密码错误#");
        }

        if (GetKnownFail(admin.ID) > MaxFailKnown)
        {
            if (admin.Status == StatusNormal)
            {
                admin.Status = StatusLocked;
                await _db.Updateable(admin)
                    .UpdateColumns(a => new { a.Status })
                    .ExecuteCommandAsync(cancellationToken);
            }

            return OldApiResult.Fail("错误次数过多,已锁定,请联系管理员");
        }

        if (admin.Status == StatusLocked)
            return OldApiResult.Fail("错误次数过多,已锁定,请联系管理员");

        if (!Md5PasswordHasher.Matches(password, admin.Password))
        {
            IncrementKnownFail(admin.ID);
            return OldApiResult.Fail("#用户名或者密码错误#");
        }

        if (admin.SecretKey != Guid.Empty)
        {
            if (!GoogleTotpValidator.Validate(admin.SecretKey.ToString("N"), code))
            {
                IncrementKnownFail(admin.ID);
                return OldApiResult.Fail("#验证码错误#");
            }
        }

        ClearKnownFail(admin.ID);

        var token = Guid.NewGuid();
        _cache.Add(SiteHostCacheKeys.AdminToken, token.ToString("N"), (int?)admin.ID, TimeSpan.FromDays(7));
        _cache.Add(SiteHostCacheKeys.AdminTokenSite, token.ToString("N"), (int?)siteId, TimeSpan.FromDays(7));

        var http = _httpContextAccessor.HttpContext;
        admin.LoginAt = DateTime.Now;
        admin.LoginIP = clientIp;
        await _db.Updateable(admin)
            .UpdateColumns(a => new { a.LoginAt, a.LoginIP })
            .ExecuteCommandAsync(cancellationToken);

        WriteTokenCookieAndHeader(http, token);
        return OldApiResult.Ok("#登录成功#");
    }

    /// <summary>
    /// 登出：清 TOKEN Cookie。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>旧版返回格式</returns>
    public Task<OldApiResult> LogoutAsync(CancellationToken cancellationToken = default)
    {
        var http = _httpContextAccessor.HttpContext;
        var tokenText = http?.Request.Cookies[AdminSessionKeys.Token];
        if (!string.IsNullOrEmpty(tokenText))
        {
            _cache.Remove(SiteHostCacheKeys.AdminToken, tokenText);
            _cache.Remove(SiteHostCacheKeys.AdminTokenSite, tokenText);
        }

        if (http != null)
        {
            http.Response.Cookies.Append(
                AdminSessionKeys.Token,
                string.Empty,
                new CookieOptions { HttpOnly = true, Expires = DateTimeOffset.UnixEpoch });
        }

        return Task.FromResult(OldApiResult.Ok(string.Empty));
    }

    /// <summary>
    /// 从 Cookie/头解析当前站管理员编号。
    /// </summary>
    /// <param name="siteId">站点编号</param>
    /// <returns>管理员编号；无效为 null</returns>
    public int? TryResolveAdminIdFromCookie(int siteId)
    {
        var http = _httpContextAccessor.HttpContext;
        if (http == null)
            return null;

        var tokenText = http.Request.Cookies[AdminSessionKeys.Token];
        if (string.IsNullOrWhiteSpace(tokenText))
            tokenText = http.Request.Headers[AdminSessionKeys.Token].ToString();
        if (string.IsNullOrWhiteSpace(tokenText))
            return null;

        var cachedSite = _cache.Get<int?>(SiteHostCacheKeys.AdminTokenSite, tokenText);
        if (cachedSite != siteId)
            return null;

        var adminId = _cache.Get<int?>(SiteHostCacheKeys.AdminToken, tokenText);
        return adminId is > 0 ? adminId : null;
    }

    /// <summary>
    /// 取当前站点已登录的后台管理员；未登录或站点和会话对不上返回 null。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>站点管理员；未登录为 null</returns>
    public async Task<SiteAdmin> TryGetCurrentAdminAsync(CancellationToken cancellationToken = default)
    {
        var siteId = _site.SiteId;
        if (siteId == null)
            return null;

        var adminId = TryResolveAdminIdFromCookie(siteId.Value);
        if (adminId == null)
            return null;

        var rows = await _db.Queryable<SiteAdmin>()
            .Where(a => a.ID == adminId.Value && a.SiteID == siteId.Value)
            .Take(1)
            .ToListAsync(cancellationToken);
        return rows.FirstOrDefault();
    }

    private int GetKnownFail(int adminId) =>
        _cache.Get<int?>(SiteHostCacheKeys.SiteAdminLoginFail, adminId.ToString()) ?? 0;

    private void IncrementKnownFail(int adminId)
    {
        var next = GetKnownFail(adminId) + 1;
        // 旧版这个键没有过期时间；这里给 7 天，避免脏数据一直堆着
        _cache.Add(SiteHostCacheKeys.SiteAdminLoginFail, adminId.ToString(), (int?)next, TimeSpan.FromDays(7));
    }

    private void ClearKnownFail(int adminId) =>
        _cache.Remove(SiteHostCacheKeys.SiteAdminLoginFail, adminId.ToString());

    private int IncrementUnknownFail(string ip)
    {
        var next = (_cache.Get<int?>(SiteHostCacheKeys.SiteAdminUnknownFail, ip) ?? 0) + 1;
        var untilMidnight = DateTime.Today.AddDays(1) - DateTime.Now;
        if (untilMidnight < TimeSpan.FromMinutes(1))
            untilMidnight = TimeSpan.FromMinutes(1);
        _cache.Add(SiteHostCacheKeys.SiteAdminUnknownFail, ip, (int?)next, untilMidnight);
        return next;
    }

    /// <summary>
    /// 种 TOKEN Cookie 并写响应头。
    /// </summary>
    private static void WriteTokenCookieAndHeader(HttpContext http, Guid token)
    {
        if (http == null)
            return;

        var value = token.ToString("N");
        http.Response.Cookies.Append(
            AdminSessionKeys.Token,
            value,
            new CookieOptions { HttpOnly = true, IsEssential = true, SameSite = SameSiteMode.Lax });
        http.Response.Headers[AdminSessionKeys.Token] = value;
    }
}
