namespace SimpX.Net.Application.Shared;

/// <summary>
/// 总后台站点列表 JWT 筛选。
/// </summary>
public sealed class PlatformBizSiteListInput
{
    /// <summary>站点编号；0 或不传表示全部。</summary>
    public int SiteID { get; set; }

    /// <summary>名称包含。</summary>
    public string Name { get; set; }

    /// <summary>状态；空不限。</summary>
    public string Status { get; set; }
}
