using System.Reflection;
using System.Xml.Linq;
using SimpX.Net.Data;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 兼容后台菜单：可读码种子组树、Layui JSON；XML 仅作一次性迁入源的枚举。
/// permissionIds 为 null 表示全选（超管）。
/// </summary>
public static class LayuiMenuCatalog
{
    /// <summary>站点后台菜单资源名后缀。</summary>
    public const string SiteAdminResourceSuffix = "SiteAdmin.xml";

    /// <summary>总后台菜单资源名后缀。</summary>
    public const string SystemAdminResourceSuffix = "SystemAdmin.xml";

    /// <summary>站点后台写入 SysMenu 时的 Remark。</summary>
    public const string SiteAdminRemark = "SiteAdminMenu";

    /// <summary>总后台写入 SysMenu 时的 Remark。</summary>
    public const string SystemAdminRemark = "SystemAdminMenu";

    /// <summary>
    /// 构建站点后台菜单树（读可读码种子，不再走运行时 GUID 对照）。
    /// </summary>
    /// <param name="permissionIds">拥有的权限码；null 表示全部勾选</param>
    /// <returns>顶级菜单列表</returns>
    public static List<LayuiMenuNode> BuildSiteAdminMenu(string[] permissionIds) =>
        BuildFromSysMenus(new SiteAdminMenuSeedData().HasData(), permissionIds);

    /// <summary>
    /// 构建总后台菜单树（读可读码种子，不再走运行时 GUID 对照）。
    /// </summary>
    /// <param name="permissionIds">拥有的权限码；null 表示全部勾选</param>
    /// <returns>顶级菜单列表</returns>
    public static List<LayuiMenuNode> BuildSystemAdminMenu(string[] permissionIds) =>
        BuildFromSysMenus(new SystemAdminMenuSeedData().HasData(), permissionIds);

    /// <summary>
    /// 从已入库的 SysMenu 行组 Layui 树。对外 id 用 Permission（可读码）。
    /// </summary>
    /// <param name="menus">同一 Remark 下的菜单行</param>
    /// <param name="permissionIds">拥有的权限码；null 表示全部勾选</param>
    /// <returns>顶级菜单列表</returns>
    public static List<LayuiMenuNode> BuildFromSysMenus(IEnumerable<SysMenu> menus, string[] permissionIds)
    {
        var list = menus?
            .Where(m => m != null && m.Status == StatusEnum.Enable)
            .OrderBy(m => m.OrderNo)
            .ThenBy(m => m.Id)
            .ToList() ?? [];
        if (list.Count == 0)
            return [];

        var byPid = list.ToLookup(m => m.Pid);

        List<LayuiMenuNode> BuildChildren(long pid)
        {
            var children = new List<LayuiMenuNode>();
            foreach (var m in byPid[pid])
            {
                var id = m.Permission ?? m.Name ?? string.Empty;
                var node = new LayuiMenuNode
                {
                    Id = id,
                    Name = m.Title ?? string.Empty,
                    Href = m.Path,
                    Icon = m.Icon,
                    Checked = permissionIds == null
                        || (!string.IsNullOrEmpty(id) && permissionIds.Contains(id, StringComparer.OrdinalIgnoreCase))
                        ? 1
                        : 0,
                    Menu = BuildChildren(m.Id)
                };
                if (node.Menu.Count == 0)
                    node.Menu = null;
                children.Add(node);
            }

            return children;
        }

        return BuildChildren(0);
    }

    /// <summary>
    /// 把菜单树序列成旧版 info 用的 JSON 数组字符串。
    /// </summary>
    /// <param name="nodes">顶级节点</param>
    /// <returns>如 [{"name":...},...]</returns>
    public static string ToOldApiArrayJson(IEnumerable<LayuiMenuNode> nodes) =>
        "[" + string.Join(",", nodes.Select(n => n.ToOldApiJson())) + "]";

    /// <summary>
    /// 扁平枚举 XML 中所有带 ID 的节点（菜单+按钮），供一次性迁入脚本对照用。
    /// </summary>
    /// <param name="resourceSuffix">资源后缀</param>
    /// <returns>id,name,href,icon,isAction</returns>
    public static IEnumerable<(string Id, string Name, string Href, string Icon, bool IsAction)> EnumerateFlat(string resourceSuffix)
    {
        foreach (var item in EnumerateTree(resourceSuffix))
            yield return (item.Id, item.Name, item.Href, item.Icon, item.IsAction);
    }

    /// <summary>
    /// 带父子关系的树形枚举（XML 原始 GUID），供一次性生成脚本用。
    /// </summary>
    /// <param name="resourceSuffix">资源后缀</param>
    /// <returns>树节点（ParentId 空表示顶级）</returns>
    public static IEnumerable<AdminMenuXmlItem> EnumerateTree(string resourceSuffix)
    {
        var root = LoadXml(resourceSuffix);
        var order = new IntBox();
        foreach (var el in root.Elements())
        {
            foreach (var item in WalkTree(el, parentId: string.Empty, order))
                yield return item;
        }
    }

    /// <summary>可变序号盒，避免迭代器不能用 ref 参数。</summary>
    private sealed class IntBox
    {
        /// <summary>当前序号。</summary>
        public int Value;
    }

    private static IEnumerable<AdminMenuXmlItem> WalkTree(XElement el, string parentId, IntBox order)
    {
        var id = (string)el.Attribute("ID");
        if (string.IsNullOrEmpty(id))
        {
            foreach (var child in el.Elements())
            {
                foreach (var nested in WalkTree(child, parentId, order))
                    yield return nested;
            }

            yield break;
        }

        order.Value++;
        yield return new AdminMenuXmlItem(
            id,
            parentId,
            (string)el.Attribute("name") ?? string.Empty,
            (string)el.Attribute("href"),
            (string)el.Attribute("icon"),
            el.Name.LocalName.Equals("action", StringComparison.OrdinalIgnoreCase),
            order.Value * 10);

        foreach (var child in el.Elements())
        {
            foreach (var nested in WalkTree(child, id, order))
                yield return nested;
        }
    }

    private static XElement LoadXml(string resourceSuffix)
    {
        var asm = typeof(LayuiMenuCatalog).Assembly;
        var name = asm.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(resourceSuffix, StringComparison.OrdinalIgnoreCase));
        if (name == null)
            throw new InvalidOperationException($"未找到嵌入菜单资源：{resourceSuffix}");

        using var stream = asm.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"无法打开嵌入菜单资源：{name}");
        return XElement.Load(stream);
    }
}

/// <summary>
/// 嵌入菜单 XML 的一个节点（带父 GUID），仅供一次性迁入脚本枚举。
/// </summary>
/// <param name="Id">权限 GUID</param>
/// <param name="ParentId">父节点 GUID；顶级为空</param>
/// <param name="Name">显示名</param>
/// <param name="Href">Layui 路径</param>
/// <param name="Icon">图标</param>
/// <param name="IsAction">是否按钮</param>
/// <param name="OrderNo">排序</param>
public sealed record AdminMenuXmlItem(
    string Id,
    string ParentId,
    string Name,
    string Href,
    string Icon,
    bool IsAction,
    int OrderNo);
