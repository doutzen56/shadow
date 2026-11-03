using SimpX.Net.Data.Entity.Systems;
using SqlSugar;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 总后台 Layui 菜单。优先读 SysMenu（SystemAdminMenu），空表或失败回退嵌入 XML。
/// 权限来自 sys_Admin.Permission。
/// </summary>
public sealed class SystemAdminMenuService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ISimpXCache _cache;
    private readonly AdminMenuSysMenuStore _menuStore;

    /// <summary>
    /// 注入业务库、请求上下文、缓存与菜单存储。总后台账号在 BetWin30，菜单仍读主库。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="httpContextAccessor">HTTP 上下文</param>
    /// <param name="cache">缓存</param>
    /// <param name="menuStore">SysMenu 菜单存储</param>
    public SystemAdminMenuService(
        IBizDbAccessor biz,
        IHttpContextAccessor httpContextAccessor,
        ISimpXCache cache,
        AdminMenuSysMenuStore menuStore)
    {
        _db = biz.Db;
        _httpContextAccessor = httpContextAccessor;
        _cache = cache;
        _menuStore = menuStore;
    }

    /// <summary>
    /// 当前总后台管理员菜单（对应旧版 /system/config/account/menu 一类接口）。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>旧版返回格式</returns>
    public async Task<OldApiResult> GetMenuAsync(CancellationToken cancellationToken = default)
    {
        var admin = await TryGetCurrentAdminAsync(cancellationToken);
        if (admin == null)
            return OldApiResult.NeedLogin();

        string[] permission = null;
        if (!string.IsNullOrWhiteSpace(admin.Permission))
            permission = admin.Permission.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        // 无 Permission 串时旧版常给出全树；有串则按串勾选
        var nodes = await _menuStore.GetSystemAdminMenuAsync(
            string.IsNullOrWhiteSpace(admin.Permission) ? null : permission,
            cancellationToken);
        var json = LayuiMenuCatalog.ToOldApiArrayJson(nodes);
        return OldApiResult.Ok(string.Empty, json);
    }

    /// <summary>
    /// 从 Cookie 取当前总后台管理员；未登录返回 null。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>总后台管理员</returns>
    public async Task<SystemAdmin> TryGetCurrentAdminAsync(CancellationToken cancellationToken = default)
    {
        var http = _httpContextAccessor.HttpContext;
        var tokenText = http?.Request.Cookies[SystemSessionKeys.Token];
        if (string.IsNullOrWhiteSpace(tokenText))
            return null;

        var adminId = _cache.Get<int?>(SiteHostCacheKeys.SystemAdminToken, tokenText);
        if (adminId is not > 0)
        {
            if (!Guid.TryParseExact(tokenText, "N", out var token) && !Guid.TryParse(tokenText, out token))
                return null;

            var tokens = await _db.Queryable<SystemAdminToken>()
                .Where(t => t.Token == token)
                .Take(1)
                .ToListAsync(cancellationToken);
            adminId = tokens.FirstOrDefault()?.AdminID;
            if (adminId is not > 0)
                return null;
        }

        var rows = await _db.Queryable<SystemAdmin>()
            .Where(a => a.ID == adminId.Value)
            .Take(1)
            .ToListAsync(cancellationToken);
        return rows.FirstOrDefault();
    }
}
