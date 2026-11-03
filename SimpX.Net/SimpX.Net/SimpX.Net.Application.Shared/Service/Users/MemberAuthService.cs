using Microsoft.AspNetCore.Http;
using SimpX.Net.Data;
using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Users;
using SqlSugar;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 会员登录/登出/是否已登录。在当前站点的 Users 表里校验；会话 Cookie 为 USERINFO（Guid N 格式）。
/// 失败次数、用户名密码互含、单钱包币种、黑名单手机按旧版 UserAgent.Login。
/// </summary>
public sealed class MemberAuthService : ITransient, IMemberCookieUserIdResolver
{
    /// <summary>登录锁定位（跟旧版登录锁定位一样（值为 1））。</summary>
    public const int LockLoginFlag = 1;

    /// <summary>单一钱包（Site.Wallet = 1）。</summary>
    public const int WalletSingle = 1;

    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ISimpXCache _cache;
    private readonly LoginFailCounter _failCounter;

    /// <summary>
    /// 注入业务库、当前站点、HTTP、缓存与失败计数。
    /// </summary>
    /// <param name="biz">业务库（Users/Site 在 BetWin30）</param>
    /// <param name="site">当前站点</param>
    /// <param name="httpContextAccessor">HTTP 上下文</param>
    /// <param name="cache">业务缓存</param>
    /// <param name="failCounter">登录失败计数</param>
    public MemberAuthService(
        IBizDbAccessor biz,
        ICurrentSiteAccessor site,
        IHttpContextAccessor httpContextAccessor,
        ISimpXCache cache,
        LoginFailCounter failCounter)
    {
        _db = biz.Db;
        _site = site;
        _httpContextAccessor = httpContextAccessor;
        _cache = cache;
        _failCounter = failCounter;
    }

    /// <summary>
    /// 账号密码登录：校验当前站会员，写 usr_Token 与 Cookie，返回旧版格式。
    /// </summary>
    /// <param name="userName">用户名</param>
    /// <param name="password">明文密码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>旧版返回格式；成功时 info 含 Token；密码错时 info.count 是该 IP 的失败次数</returns>
    public async Task<OldApiResult> LoginAsync(
        string userName,
        string password,
        CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        userName = userName?.Trim();
        if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))
            return OldApiResult.Fail("#用户名/密码错误#");

        var lowerUser = userName.ToLowerInvariant();
        var lowerPass = password.Trim().ToLowerInvariant();
        if (lowerUser == lowerPass || lowerPass == "a123456")
            return OldApiResult.Fail("#用户名/密码错误#");
        if (lowerUser.Contains(lowerPass))
            return OldApiResult.Fail("#用户名不得包含密码字符串#");
        if (lowerPass.Contains(lowerUser))
            return OldApiResult.Fail("#密码不得包含用户名字符串#");

        var clientIp = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";

        var users = await _db.Queryable<User>()
            .Where(u => u.SiteID == siteId && u.UserName == userName)
            .Take(1)
            .ToListAsync(cancellationToken);
        var user = users.FirstOrDefault();
        if (user == null)
            return OldApiResult.Fail("#用户名/密码错误#");

        var userSubject = user.ID.ToString();
        var lockedUser = _failCounter.CheckLockedSeconds(userSubject);
        if (lockedUser > 0)
        {
            return OldApiResult.Fail(
                $"#密码重试次数已经达到上限#,#请#{LoginFailCounter.FormatRemain(lockedUser)}#后重新登录#");
        }

        if ((user.Lock & LockLoginFlag) != 0)
            return OldApiResult.Fail("#被锁定登录#");

        var siteRows = await _db.Queryable<Site>()
            .Where(s => s.ID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var site = siteRows.FirstOrDefault();
        if (site != null && site.Wallet == WalletSingle)
        {
            var currencyOpen = await _db.Queryable<SiteCurrency>()
                .Where(c => c.SiteID == siteId && c.IsOpen && c.Currency == user.Currency)
                .AnyAsync(cancellationToken);
            if (!currencyOpen)
                return OldApiResult.Fail("暂未运营，请联系客服");
        }

        if (!Md5PasswordHasher.Matches(password, user.Password))
        {
            _failCounter.SaveFail(userSubject);
            _failCounter.SaveFail(clientIp);
            var ipCount = _failCounter.GetCount(clientIp);
            return OldApiResult.Fail("#用户名/密码错误#", new { count = ipCount.ToString() });
        }

        if (!string.IsNullOrEmpty(user.Mobile) && user.Mobile.Length > 3)
        {
            if (await IsBlackMobileAsync(siteId, user.Mobile, cancellationToken))
                return OldApiResult.Fail("#该手机号已被限制登录#");
        }

        var token = Guid.NewGuid();
        await SaveTokenAsync(siteId, user.ID, token, cancellationToken);

        var http = _httpContextAccessor.HttpContext;
        user.LoginAt = DateTime.Now;
        user.LoginIP = clientIp;
        await _db.Updateable(user)
            .UpdateColumns(u => new { u.LoginAt, u.LoginIP })
            .ExecuteCommandAsync(cancellationToken);

        _failCounter.Clear(userSubject);
        _failCounter.Clear(clientIp);
        AppendUserInfoCookie(http, token);

        return OldApiResult.Ok("#登录成功#", new { Token = token.ToString("N") });
    }

    /// <summary>
    /// 登出：清 Cookie，并尽量作废当前 token。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>旧版返回格式</returns>
    public async Task<OldApiResult> LogoutAsync(CancellationToken cancellationToken = default)
    {
        var http = _httpContextAccessor.HttpContext;
        var tokenText = http?.Request.Cookies[MemberSessionKeys.UserInfoCookie];
        if (Guid.TryParseExact(tokenText, "N", out var token) || Guid.TryParse(tokenText, out token))
            await InvalidateTokenAsync(token, cancellationToken);

        if (http != null)
        {
            http.Response.Cookies.Append(
                MemberSessionKeys.UserInfoCookie,
                string.Empty,
                new CookieOptions
                {
                    HttpOnly = true,
                    Expires = DateTimeOffset.UnixEpoch
                });
        }

        return OldApiResult.Ok(string.Empty);
    }

    /// <summary>
    /// 是否已登录（Guest 可读）：看 Cookie 对应 token 是否落在当前站。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>旧版返回格式；info.login 是是否已登录</returns>
    public async Task<OldApiResult> IsLoginAsync(CancellationToken cancellationToken = default)
    {
        var siteId = _site.SiteId;
        if (siteId == null)
            return OldApiResult.Ok("#游客#", new { login = false });

        var userId = await TryResolveUserIdFromCookieAsync(siteId.Value, cancellationToken);
        if (userId == null)
            return OldApiResult.Ok("#游客#", new { login = false });

        return OldApiResult.Ok("#已登录#", new { login = true });
    }

    /// <summary>
    /// 从 Cookie 解析当前站会员编号；无效返回 null。供后续兼容鉴权使用。
    /// </summary>
    /// <param name="siteId">站点编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>会员编号；无效为 null</returns>
    public async Task<int?> TryResolveUserIdFromCookieAsync(int siteId, CancellationToken cancellationToken = default)
    {
        var http = _httpContextAccessor.HttpContext;
        var tokenText = http?.Request.Cookies[MemberSessionKeys.UserInfoCookie];
        if (string.IsNullOrWhiteSpace(tokenText))
            return null;

        if (!Guid.TryParseExact(tokenText, "N", out var token) && !Guid.TryParse(tokenText, out token))
            return null;

        var cached = _cache.Get<int?>(SiteHostCacheKeys.UserToken, token.ToString("N"));
        if (cached is > 0)
        {
            var sameSite = await _db.Queryable<User>()
                .Where(u => u.ID == cached.Value && u.SiteID == siteId)
                .AnyAsync(cancellationToken);
            return sameSite ? cached : null;
        }

        var rows = await _db.Queryable<UserToken>()
            .Where(t => t.SiteID == siteId && t.Token == token)
            .Take(1)
            .ToListAsync(cancellationToken);
        return rows.FirstOrDefault()?.UserID;
    }

    /// <summary>
    /// 黑名单手机：前缀 0 表示子串匹配去掉 0 后的内容，否则整串包含即中。
    /// </summary>
    private async Task<bool> IsBlackMobileAsync(int siteId, string mobile, CancellationToken cancellationToken)
    {
        var blacks = await _db.Queryable<BlackMobileImport>()
            .Where(b => b.SiteID == siteId)
            .ToListAsync(cancellationToken);
        foreach (var b in blacks)
        {
            var raw = b.BlackMobile;
            if (string.IsNullOrEmpty(raw))
                continue;
            if (raw[0] == '0')
            {
                var part = raw.Length > 1 ? raw[1..] : string.Empty;
                if (!string.IsNullOrEmpty(part) && mobile.Contains(part, StringComparison.Ordinal))
                    return true;
            }
            else if (mobile.Contains(raw, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 写入或更新 usr_Token，并缓存 token→userId。
    /// </summary>
    private async Task SaveTokenAsync(int siteId, int userId, Guid token, CancellationToken cancellationToken)
    {
        var now = DateTime.Now;
        var exists = await _db.Queryable<UserToken>()
            .Where(t => t.SiteID == siteId && t.UserID == userId)
            .AnyAsync(cancellationToken);

        if (exists)
        {
            await _db.Updateable<UserToken>()
                .SetColumns(t => new UserToken { Token = token, UpdateAt = now, Type = 0 })
                .Where(t => t.SiteID == siteId && t.UserID == userId)
                .ExecuteCommandAsync(cancellationToken);
        }
        else
        {
            await _db.Insertable(new UserToken
            {
                SiteID = siteId,
                UserID = userId,
                Token = token,
                UpdateAt = now,
                Type = 0
            }).ExecuteCommandAsync(cancellationToken);
        }

        _cache.Add(SiteHostCacheKeys.UserToken, token.ToString("N"), (int?)userId, TimeSpan.FromDays(7));
    }

    /// <summary>
    /// 作废 token 缓存；库行按 token 清空。
    /// </summary>
    private async Task InvalidateTokenAsync(Guid token, CancellationToken cancellationToken)
    {
        _cache.Remove(SiteHostCacheKeys.UserToken, token.ToString("N"));
        await _db.Updateable<UserToken>()
            .SetColumns(t => new UserToken { Token = Guid.Empty, UpdateAt = DateTime.Now })
            .Where(t => t.Token == token)
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 种 HttpOnly USERINFO Cookie（值 Guid N）。
    /// </summary>
    private static void AppendUserInfoCookie(HttpContext http, Guid token)
    {
        if (http == null)
            return;

        http.Response.Cookies.Append(
            MemberSessionKeys.UserInfoCookie,
            token.ToString("N"),
            new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax
            });
    }
}
