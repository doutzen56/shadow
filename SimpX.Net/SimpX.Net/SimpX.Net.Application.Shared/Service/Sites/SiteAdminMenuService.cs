using SimpX.Net.Data.Entity.Sites;
using SqlSugar;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 站点后台 Layui 菜单。优先读 SysMenu（SiteAdminMenu），空表或失败回退嵌入 XML。
/// AdminName=admin 或 GroupID=0 → 全菜单勾选。
/// </summary>
public sealed class SiteAdminMenuService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly SiteAdminAuthService _auth;
    private readonly AdminMenuSysMenuStore _menuStore;

    /// <summary>
    /// 注入业务库、会话与菜单存储。管理员分组在 BetWin30 业务库；菜单仍走主库 SysMenu。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="auth">站点后台登录会话</param>
    /// <param name="menuStore">SysMenu 菜单存储</param>
    public SiteAdminMenuService(
        IBizDbAccessor biz,
        SiteAdminAuthService auth,
        AdminMenuSysMenuStore menuStore)
    {
        _db = biz.Db;
        _auth = auth;
        _menuStore = menuStore;
    }

    /// <summary>
    /// 当前登录管理员的左侧菜单（旧版 /admin/info/menu）。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>旧版返回格式；info 是菜单 JSON 数组字符串</returns>
    public async Task<OldApiResult> GetMenuAsync(CancellationToken cancellationToken = default)
    {
        var admin = await _auth.TryGetCurrentAdminAsync(cancellationToken);
        if (admin == null)
            return OldApiResult.NeedLogin();

        var permission = await ResolvePermissionIdsAsync(admin, cancellationToken);
        var nodes = await _menuStore.GetSiteAdminMenuAsync(permission, cancellationToken);
        var json = LayuiMenuCatalog.ToOldApiArrayJson(nodes);

        // 跟旧版一样：超管把某个谷歌相关权限 id 在展示时换成 noused
        if (string.Equals(admin.AdminName, "admin", StringComparison.OrdinalIgnoreCase))
            json = json.Replace("d2a7b863a51e923d8ea164d4e9e22d09", "noused");

        return OldApiResult.Ok(string.Empty, json);
    }

    /// <summary>
    /// 解析管理员权限 GUID 列表；null 表示全权限。
    /// </summary>
    /// <param name="admin">当前管理员</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限 GUID；全权限为 null</returns>
    private async Task<string[]> ResolvePermissionIdsAsync(SiteAdmin admin, CancellationToken cancellationToken)
    {
        if (string.Equals(admin.AdminName, "admin", StringComparison.OrdinalIgnoreCase) || admin.GroupID == 0)
            return null;

        var groups = await _db.Queryable<SiteAdminGroup>()
            .Where(g => g.ID == admin.GroupID && g.SiteID == admin.SiteID)
            .Take(1)
            .ToListAsync(cancellationToken);
        var csv = groups.FirstOrDefault()?.PermissionString;
        if (string.IsNullOrWhiteSpace(csv))
            return [];

        return csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
