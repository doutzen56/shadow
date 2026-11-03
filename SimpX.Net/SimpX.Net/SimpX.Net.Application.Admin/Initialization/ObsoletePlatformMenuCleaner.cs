namespace SimpX.Net.Application.Admin;

/// <summary>
/// 清理已废弃功能留下的菜单、区域表和相关配置。
/// 种子不再写入这些页；旧库残留会点进空页或留下无用表。
/// </summary>
public static class ObsoletePlatformMenuCleaner
{
    /// <summary>原平台管理目录 Id（种子历史值）。</summary>
    public const long PlatformDirId = 1300300000101;

    /// <summary>已从种子拿掉的根菜单 Id（工作台、开发工具、帮助文档、关于项目、合并前业务系统管理根）。</summary>
    private static readonly long[] ObsoleteRootIds =
    [
        1300100000101, // 工作台
        1300500000101, // 开发工具
        1300600000101, // 帮助文档
        1300700000101, // 关于项目
        1390300000000000195, // 业务系统管理根（已并进框架系统管理）
    ];

    /// <summary>已从前端拿掉、旧库仍可能残留的菜单路径前缀（小写比较）。</summary>
    private static readonly string[] RemovedPathPrefixes =
    [
        "/platform",
        "/system/region",
        "/system/dict",
        "/system/dicttype",
        "/system/dictdata",
        "/system/template",
        "/system/job",
        "/system/server",
        "/system/schedule",
        "/system/userregway",
        "/system/swagger",
        "/system/weChatUser",
        "/dashboard",
        "/develop",
        "/doc",
        "/about",
    ];

    /// <summary>行政区域同步配置编码（历史常量，实体已删）。</summary>
    private const string ObsoleteRegionSyncConfigCode = "sys_region_sync_level";

    /// <summary>
    /// 清理废弃菜单授权，并丢掉行政区域表与同步配置。
    /// </summary>
    /// <param name="db">SqlSugar 客户端</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>删掉的菜单条数</returns>
    public static async Task<int> CleanAsync(ISqlSugarClient db, CancellationToken cancellationToken = default)
    {
        var removedMenus = await CleanObsoleteMenusAsync(db, cancellationToken);
        await DropRegionArtifactsAsync(db, cancellationToken);
        return removedMenus;
    }

    /// <summary>
    /// 删除废弃菜单树以及已下线页面的菜单与授权关系。
    /// </summary>
    private static async Task<int> CleanObsoleteMenusAsync(ISqlSugarClient db, CancellationToken cancellationToken)
    {
        var allMenus = await db.Queryable<SysMenu>().ToListAsync(cancellationToken);
        if (allMenus.Count == 0)
            return 0;

        var exclude = new HashSet<long> { PlatformDirId };
        foreach (var id in ObsoleteRootIds)
            exclude.Add(id);

        CollectDescendants(allMenus, exclude);

        foreach (var menu in allMenus)
        {
            if (IsRemovedPath(menu.Path) || IsRemovedRegionTitle(menu.Title) || IsRemovedTitle(menu.Title))
                exclude.Add(menu.Id);
        }

        CollectDescendants(allMenus, exclude);

        if (exclude.Count == 0)
            return 0;

        var ids = exclude.ToList();
        await db.Deleteable<SysTenantMenu>().Where(u => ids.Contains(u.MenuId)).ExecuteCommandAsync(cancellationToken);
        await db.Deleteable<SysRoleMenu>().Where(u => ids.Contains(u.MenuId)).ExecuteCommandAsync(cancellationToken);
        await db.Deleteable<SysUserMenu>().Where(u => ids.Contains(u.MenuId)).ExecuteCommandAsync(cancellationToken);
        return await db.Deleteable<SysMenu>().Where(u => ids.Contains(u.Id)).ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 把已入选节点的全部下级一并收进集合。
    /// </summary>
    private static void CollectDescendants(List<SysMenu> allMenus, HashSet<long> exclude)
    {
        var grew = true;
        while (grew)
        {
            grew = false;
            foreach (var menu in allMenus)
            {
                if (exclude.Contains(menu.Pid) && exclude.Add(menu.Id))
                    grew = true;
            }
        }
    }

    /// <summary>
    /// 丢掉 SysRegion 表，并删掉行政区域同步配置行。
    /// </summary>
    private static async Task DropRegionArtifactsAsync(ISqlSugarClient db, CancellationToken cancellationToken)
    {
        try
        {
            if (db.DbMaintenance.IsAnyTable("SysRegion", false))
                db.DbMaintenance.DropTable("SysRegion");
        }
        catch
        {
            // 部分库驱动对不存在的表会抛错，忽略即可
        }

        await db.Deleteable<SysConfig>()
            .Where(u => u.Code == ObsoleteRegionSyncConfigCode)
            .ExecuteCommandAsync(cancellationToken);
        await db.Deleteable<SysTenantConfig>()
            .Where(u => u.Code == ObsoleteRegionSyncConfigCode)
            .ExecuteCommandAsync(cancellationToken);
    }

    private static bool IsRemovedPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;
        var normalized = path.Trim().ToLowerInvariant();
        return RemovedPathPrefixes.Any(prefix =>
            normalized == prefix
            || normalized.StartsWith(prefix + "/", StringComparison.Ordinal));
    }

    private static bool IsRemovedRegionTitle(string title)
        => !string.IsNullOrWhiteSpace(title)
           && title.Contains("行政区域", StringComparison.Ordinal);

    /// <summary>
    /// 按标题兜底清理（旧库可能改过 Path）。
    /// </summary>
    private static bool IsRemovedTitle(string title)
        => !string.IsNullOrWhiteSpace(title)
           && (title is "工作台" or "开发工具" or "帮助文档" or "关于项目"
               or "接口压测" or "系统接口" or "后台教程" or "前端教程" or "SqlSugar"
               or "库表管理" or "代码生成" or "站内信" or "三方账号");
}
