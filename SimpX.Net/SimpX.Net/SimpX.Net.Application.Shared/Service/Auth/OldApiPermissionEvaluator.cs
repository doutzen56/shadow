namespace SimpX.Net.Application.Shared;

/// <summary>
/// 兼容权限判定（纯逻辑，供过滤器和单测共用）。
/// </summary>
public static class OldApiPermissionEvaluator
{
    /// <summary>
    /// 站点管理员是否拥有全部指定权限。
    /// </summary>
    /// <param name="adminName">管理员名</param>
    /// <param name="groupId">分组；0 为最高权限</param>
    /// <param name="groupPermissionCsv">分组权限串</param>
    /// <param name="requiredPermissionIds">需要的 GUID</param>
    /// <returns>是否通过</returns>
    public static bool SiteAdminHasAll(
        string adminName,
        int groupId,
        string groupPermissionCsv,
        IEnumerable<string> requiredPermissionIds)
    {
        var required = requiredPermissionIds?.Where(x => !string.IsNullOrWhiteSpace(x)).ToList() ?? [];
        if (required.Count == 0)
            return true;

        if (string.Equals(adminName, "admin", StringComparison.OrdinalIgnoreCase) || groupId == 0)
            return true;

        var owned = (groupPermissionCsv ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return required.All(owned.Contains);
    }

    /// <summary>
    /// 总后台管理员是否拥有全部指定权限；Permission 空视为全权限。
    /// </summary>
    /// <param name="permissionCsv">权限串</param>
    /// <param name="requiredPermissionIds">需要的 GUID</param>
    /// <returns>是否通过</returns>
    public static bool SystemAdminHasAll(string permissionCsv, IEnumerable<string> requiredPermissionIds)
    {
        var required = requiredPermissionIds?.Where(x => !string.IsNullOrWhiteSpace(x)).ToList() ?? [];
        if (required.Count == 0)
            return true;

        if (string.IsNullOrWhiteSpace(permissionCsv))
            return true;

        var owned = permissionCsv
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return required.All(owned.Contains);
    }

    /// <summary>
    /// 权限拒绝文案；能解析到菜单标题则带中文名。
    /// </summary>
    /// <param name="permissionId">可读权限码或旧 GUID</param>
    /// <returns>提示文案</returns>
    public static string BuildPermissionDeniedMessage(string permissionId)
    {
        if (string.IsNullOrWhiteSpace(permissionId))
            return "#没有权限#";

        var title = new SiteAdminMenuSeedData().HasData()
            .FirstOrDefault(m => string.Equals(m.Permission, permissionId, StringComparison.OrdinalIgnoreCase))
            ?.Title;
        if (string.IsNullOrEmpty(title))
        {
            title = new SystemAdminMenuSeedData().HasData()
                .FirstOrDefault(m => string.Equals(m.Permission, permissionId, StringComparison.OrdinalIgnoreCase))
                ?.Title;
        }

        // 旧接口特性上仍可能挂 GUID：从嵌入 XML 取显示名
        if (string.IsNullOrEmpty(title))
        {
            title = LayuiMenuCatalog.EnumerateFlat(LayuiMenuCatalog.SiteAdminResourceSuffix)
                .Concat(LayuiMenuCatalog.EnumerateFlat(LayuiMenuCatalog.SystemAdminResourceSuffix))
                .FirstOrDefault(x => string.Equals(x.Id, permissionId, StringComparison.OrdinalIgnoreCase))
                .Name;
        }

        return string.IsNullOrEmpty(title) ? "#没有权限#" : $"您没有权限操作“{title}”";
    }
}
