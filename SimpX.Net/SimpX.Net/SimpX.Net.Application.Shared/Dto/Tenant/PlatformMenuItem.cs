namespace SimpX.Net.Application.Shared;

/// <summary>
/// 总后台授权用的菜单简表一行。
/// </summary>
public class PlatformMenuItem
{
    /// <summary>菜单 Id。</summary>
    public long Id { get; set; }

    /// <summary>上级 Id。</summary>
    public long Pid { get; set; }

    /// <summary>标题。</summary>
    public string Title { get; set; }

    /// <summary>类型（目录/菜单/按钮）。</summary>
    public MenuTypeEnum Type { get; set; }

    /// <summary>路由路径。</summary>
    public string Path { get; set; }
}
