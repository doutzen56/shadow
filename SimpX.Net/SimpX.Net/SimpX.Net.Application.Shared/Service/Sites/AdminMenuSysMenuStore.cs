using Microsoft.Extensions.Logging;
using SimpX.Net.Data;
using SqlSugar;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 兼容后台菜单读写 SysMenu：空 Remark 时灌可读码种子；有数据则组树；失败或空则回退种子内存树。
/// </summary>
public sealed class AdminMenuSysMenuStore : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ILogger<AdminMenuSysMenuStore> _logger;

    /// <summary>
    /// 注入库与日志。
    /// </summary>
    /// <param name="db">库客户端</param>
    /// <param name="logger">日志</param>
    public AdminMenuSysMenuStore(ISqlSugarClient db, ILogger<AdminMenuSysMenuStore> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// 取站点后台菜单树：优先 SysMenu，否则 XML。
    /// </summary>
    /// <param name="permissionIds">权限 GUID；null 全选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>Layui 节点</returns>
    public Task<List<LayuiMenuNode>> GetSiteAdminMenuAsync(
        string[] permissionIds,
        CancellationToken cancellationToken = default) =>
        GetMenuAsync(
            LayuiMenuCatalog.SiteAdminRemark,
            () => new SiteAdminMenuSeedData().HasData(),
            permissionIds,
            () => LayuiMenuCatalog.BuildSiteAdminMenu(permissionIds),
            cancellationToken);

    /// <summary>
    /// 取总后台菜单树：优先 SysMenu，否则 XML。
    /// </summary>
    /// <param name="permissionIds">权限 GUID；null 全选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>Layui 节点</returns>
    public Task<List<LayuiMenuNode>> GetSystemAdminMenuAsync(
        string[] permissionIds,
        CancellationToken cancellationToken = default) =>
        GetMenuAsync(
            LayuiMenuCatalog.SystemAdminRemark,
            () => new SystemAdminMenuSeedData().HasData(),
            permissionIds,
            () => LayuiMenuCatalog.BuildSystemAdminMenu(permissionIds),
            cancellationToken);

    /// <summary>
    /// 读表组树；无行则尝试插入种子后再读；仍失败则回退 XML。
    /// </summary>
    /// <param name="remark">SysMenu.Remark</param>
    /// <param name="seedFactory">种子工厂</param>
    /// <param name="permissionIds">权限 GUID</param>
    /// <param name="xmlFallback">XML 回退</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>菜单树</returns>
    private async Task<List<LayuiMenuNode>> GetMenuAsync(
        string remark,
        Func<IEnumerable<SysMenu>> seedFactory,
        string[] permissionIds,
        Func<List<LayuiMenuNode>> xmlFallback,
        CancellationToken cancellationToken)
    {
        try
        {
            var rows = await LoadByRemarkAsync(remark, cancellationToken);
            if (rows.Count == 0)
            {
                await TryInsertSeedAsync(remark, seedFactory, cancellationToken);
                rows = await LoadByRemarkAsync(remark, cancellationToken);
            }

            if (rows.Count > 0)
                return LayuiMenuCatalog.BuildFromSysMenus(rows, permissionIds);

            _logger.LogWarning("SysMenu 无 {Remark} 行，回退嵌入 XML 菜单", remark);
            return xmlFallback();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "读/写 SysMenu({Remark}) 失败，回退嵌入 XML 菜单", remark);
            return xmlFallback();
        }
    }

    /// <summary>
    /// 按 Remark 加载启用菜单。
    /// </summary>
    /// <param name="remark">备注标记</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>菜单行</returns>
    private async Task<List<SysMenu>> LoadByRemarkAsync(string remark, CancellationToken cancellationToken)
    {
        return await _db.Queryable<SysMenu>()
            .Where(m => m.Remark == remark && m.Status == StatusEnum.Enable)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 该 Remark 尚无行时插入种子；已有行不覆盖。
    /// </summary>
    /// <param name="remark">备注标记</param>
    /// <param name="seedFactory">种子工厂</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    private async Task TryInsertSeedAsync(
        string remark,
        Func<IEnumerable<SysMenu>> seedFactory,
        CancellationToken cancellationToken)
    {
        var count = await _db.Queryable<SysMenu>()
            .Where(m => m.Remark == remark)
            .CountAsync(cancellationToken);
        if (count > 0)
            return;

        var rows = seedFactory().ToList();
        if (rows.Count == 0)
            return;

        await _db.Insertable(rows).ExecuteCommandAsync(cancellationToken);
        _logger.LogInformation("已写入 SysMenu 兼容菜单种子 {Remark}，共 {Count} 行", remark, rows.Count);
    }
}
