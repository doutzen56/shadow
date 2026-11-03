using Microsoft.AspNetCore.Http;
using SimpX.Net.Data.Entity.Systems;
using SqlSugar;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 总后台登录/登出。不按域名确定当前站点；会话 Cookie SYSTEM_ADMIN_TOKEN；成功 info 带 Token。
/// </summary>
public sealed class SystemAdminAuthService : ITransient
{
    /// <summary>正常状态（对齐 SystemAdmin.AdminStatus.Normal = 0）。</summary>
    public const int StatusNormal = 0;

    private readonly ISqlSugarClient _db;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ISimpXCache _cache;

    /// <summary>
    /// 注入业务库、请求上下文与缓存。总后台管理员在 BetWin30。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="httpContextAccessor">HTTP 上下文</param>
    /// <param name="cache">缓存</param>
    public SystemAdminAuthService(
        IBizDbAccessor biz,
        IHttpContextAccessor httpContextAccessor,
        ISimpXCache cache)
    {
        _db = biz.Db;
        _httpContextAccessor = httpContextAccessor;
        _cache = cache;
    }

    /// <summary>
    /// 总后台管理员登录。
    /// </summary>
    /// <param name="userName">AdminName</param>
    /// <param name="password">明文密码</param>
    /// <param name="code">谷歌验证码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>旧版返回格式；成功时 info 带 Token</returns>
    public async Task<OldApiResult> LoginAsync(
        string userName,
        string password,
        string code,
        CancellationToken cancellationToken = default)
    {
        userName = userName?.Trim();
        if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))
            return OldApiResult.Fail("#用户名或密码错误#");

        var rows = await _db.Queryable<SystemAdmin>()
            .Where(a => a.AdminName == userName)
            .Take(1)
            .ToListAsync(cancellationToken);
        var admin = rows.FirstOrDefault();
        if (admin == null)
            return OldApiResult.Fail("#用户名或密码错误#");

        if (admin.Status != StatusNormal)
            return OldApiResult.Fail("#账号停止使用#");

        if (!Md5PasswordHasher.Matches(password, admin.Password))
            return OldApiResult.Fail("#用户名或密码错误#");

        if (admin.SecretKey != Guid.Empty)
        {
            if (!GoogleTotpValidator.Validate(admin.SecretKey.ToString("N"), code))
                return OldApiResult.Fail("#验证码错误#");
        }

        var token = Guid.NewGuid();
        await SaveTokenAsync(admin.ID, token, cancellationToken);
        _cache.Add(SiteHostCacheKeys.SystemAdminToken, token.ToString("N"), (int?)admin.ID, TimeSpan.FromDays(7));

        var http = _httpContextAccessor.HttpContext;
        admin.LoginAt = DateTime.Now;
        admin.LoginIP = http?.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
        await _db.Updateable(admin)
            .UpdateColumns(a => new { a.LoginAt, a.LoginIP })
            .ExecuteCommandAsync(cancellationToken);

        if (http != null)
        {
            http.Response.Cookies.Append(
                SystemSessionKeys.Token,
                token.ToString("N"),
                new CookieOptions { HttpOnly = true, IsEssential = true, SameSite = SameSiteMode.Lax });
        }

        return OldApiResult.Ok("#登录成功#", new { Token = token.ToString("N") });
    }

    /// <summary>
    /// 登出。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>旧版返回格式</returns>
    public async Task<OldApiResult> LogoutAsync(CancellationToken cancellationToken = default)
    {
        var http = _httpContextAccessor.HttpContext;
        var tokenText = http?.Request.Cookies[SystemSessionKeys.Token];
        if (Guid.TryParseExact(tokenText, "N", out var token) || Guid.TryParse(tokenText, out token))
        {
            _cache.Remove(SiteHostCacheKeys.SystemAdminToken, token.ToString("N"));
            await _db.Updateable<SystemAdminToken>()
                .SetColumns(t => new SystemAdminToken { Token = Guid.Empty, UpdateAt = DateTime.Now })
                .Where(t => t.Token == token)
                .ExecuteCommandAsync(cancellationToken);
        }

        if (http != null)
        {
            http.Response.Cookies.Append(
                SystemSessionKeys.Token,
                string.Empty,
                new CookieOptions { HttpOnly = true, Expires = DateTimeOffset.UnixEpoch });
        }

        return OldApiResult.Ok(string.Empty);
    }

    /// <summary>
    /// 写入或更新 sys_AdminToken。
    /// </summary>
    private async Task SaveTokenAsync(int adminId, Guid token, CancellationToken cancellationToken)
    {
        var now = DateTime.Now;
        var exists = await _db.Queryable<SystemAdminToken>()
            .Where(t => t.AdminID == adminId)
            .AnyAsync(cancellationToken);

        if (exists)
        {
            await _db.Updateable<SystemAdminToken>()
                .SetColumns(t => new SystemAdminToken { Token = token, UpdateAt = now })
                .Where(t => t.AdminID == adminId)
                .ExecuteCommandAsync(cancellationToken);
        }
        else
        {
            await _db.Insertable(new SystemAdminToken
            {
                AdminID = adminId,
                Token = token,
                UpdateAt = now
            }).ExecuteCommandAsync(cancellationToken);
        }
    }
}
