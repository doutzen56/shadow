namespace SimpX.Net.Application.SuperAdmin;

/// <summary>
/// 总后台登录菜单树节点（字段对齐租户后台 MenuOutput，给 Vue 动态路由用）。
/// </summary>
public class PlatformMenuOutput
{
    /// <summary>菜单 Id。</summary>
    public long Id { get; set; }

    /// <summary>父菜单 Id。</summary>
    public long Pid { get; set; }

    /// <summary>菜单类型。</summary>
    public MenuTypeEnum Type { get; set; }

    /// <summary>路由名。</summary>
    public string Name { get; set; }

    /// <summary>路由路径。</summary>
    public string Path { get; set; }

    /// <summary>前端组件路径。</summary>
    public string Component { get; set; }

    /// <summary>权限码。</summary>
    public string Permission { get; set; }

    /// <summary>重定向。</summary>
    public string Redirect { get; set; }

    /// <summary>排序。</summary>
    public int OrderNo { get; set; }

    /// <summary>状态。</summary>
    public StatusEnum Status { get; set; }

    /// <summary>备注。</summary>
    public string Remark { get; set; }

    /// <summary>展示用 Meta。</summary>
    public PlatformMenuMeta Meta { get; set; }

    /// <summary>子菜单。</summary>
    public List<PlatformMenuOutput> Children { get; set; }
}

/// <summary>
/// 总后台菜单 Meta（标题、图标、显隐等）。
/// </summary>
public class PlatformMenuMeta
{
    /// <summary>标题。</summary>
    public string Title { get; set; }

    /// <summary>图标。</summary>
    public string Icon { get; set; }

    /// <summary>是否内嵌。</summary>
    public bool IsIframe { get; set; }

    /// <summary>外链。</summary>
    public string IsLink { get; set; }

    /// <summary>是否隐藏。</summary>
    public bool IsHide { get; set; }

    /// <summary>是否缓存。</summary>
    public bool IsKeepAlive { get; set; }

    /// <summary>是否固定标签。</summary>
    public bool IsAffix { get; set; }
}
