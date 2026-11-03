using System.Text.Json.Serialization;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 内容区域分页查询。
/// </summary>
public sealed class SiteRegionPageInput
{
    /// <summary>页码。</summary>
    public int Page { get; set; } = 1;

    /// <summary>页大小。</summary>
    public int PageSize { get; set; } = 20;

    /// <summary>区域名模糊。</summary>
    public string Name { get; set; }

    /// <summary>类型；null 不限。</summary>
    public int? Type { get; set; }
}

/// <summary>
/// 内容区域列表行。
/// </summary>
public sealed class SiteRegionListItemDto
{
    /// <summary>区域编号。</summary>
    [JsonPropertyName("ID")]
    public int ID { get; set; }

    /// <summary>名称。</summary>
    [JsonPropertyName("Name")]
    public string Name { get; set; }

    /// <summary>说明。</summary>
    [JsonPropertyName("Description")]
    public string Description { get; set; }

    /// <summary>类型。</summary>
    [JsonPropertyName("Type")]
    public int Type { get; set; }

    /// <summary>创建时间。</summary>
    [JsonPropertyName("CreateAt")]
    public DateTime CreateAt { get; set; }
}

/// <summary>
/// 内容区域分页结果。
/// </summary>
public sealed class SiteRegionPageOutput
{
    /// <summary>总条数。</summary>
    [JsonPropertyName("RecordCount")]
    public int RecordCount { get; set; }

    /// <summary>页码。</summary>
    [JsonPropertyName("page")]
    public int Page { get; set; }

    /// <summary>页大小。</summary>
    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }

    /// <summary>行。</summary>
    [JsonPropertyName("data")]
    public List<SiteRegionListItemDto> Data { get; set; }
}

/// <summary>
/// 内容区域详情。
/// </summary>
public sealed class SiteRegionDetailDto
{
    /// <summary>区域编号。</summary>
    [JsonPropertyName("ID")]
    public int ID { get; set; }

    /// <summary>名称。</summary>
    [JsonPropertyName("Name")]
    public string Name { get; set; }

    /// <summary>说明。</summary>
    [JsonPropertyName("Description")]
    public string Description { get; set; }

    /// <summary>内容 JSON。</summary>
    [JsonPropertyName("Content")]
    public string Content { get; set; }

    /// <summary>类型。</summary>
    [JsonPropertyName("Type")]
    public int Type { get; set; }

    /// <summary>是否站点动画。</summary>
    [JsonPropertyName("IsSiteGif")]
    public bool IsSiteGif { get; set; }

    /// <summary>动画类型。</summary>
    [JsonPropertyName("GifType")]
    public byte GifType { get; set; }

    /// <summary>创建时间。</summary>
    [JsonPropertyName("CreateAt")]
    public DateTime CreateAt { get; set; }
}

/// <summary>
/// 内容区域保存入参。
/// </summary>
public sealed class SiteRegionSaveInput
{
    /// <summary>编号；0=新增。</summary>
    public int ID { get; set; }

    /// <summary>名称。</summary>
    public string Name { get; set; }

    /// <summary>说明。</summary>
    public string Description { get; set; }

    /// <summary>内容 JSON。</summary>
    public string Content { get; set; }

    /// <summary>类型。</summary>
    public int Type { get; set; }

    /// <summary>是否站点动画。</summary>
    public bool IsSiteGif { get; set; }

    /// <summary>动画类型。</summary>
    public byte GifType { get; set; }
}

/// <summary>
/// 区域内容列表行。
/// </summary>
public sealed class SiteRegionContentItemDto
{
    /// <summary>内容编号。</summary>
    [JsonPropertyName("ID")]
    public int ID { get; set; }

    /// <summary>区域编号。</summary>
    [JsonPropertyName("RegionID")]
    public int RegionID { get; set; }

    /// <summary>编号串。</summary>
    [JsonPropertyName("AdNo")]
    public string AdNo { get; set; }

    /// <summary>标题。</summary>
    [JsonPropertyName("Title")]
    public string Title { get; set; }

    /// <summary>内容。</summary>
    [JsonPropertyName("Content")]
    public string Content { get; set; }

    /// <summary>备注。</summary>
    [JsonPropertyName("Remark")]
    public string Remark { get; set; }

    /// <summary>链接。</summary>
    [JsonPropertyName("Url")]
    public string Url { get; set; }

    /// <summary>排序。</summary>
    [JsonPropertyName("Sort")]
    public int Sort { get; set; }

    /// <summary>图片。</summary>
    [JsonPropertyName("Pic")]
    public string Pic { get; set; }

    /// <summary>是否显示。</summary>
    [JsonPropertyName("IsShow")]
    public bool IsShow { get; set; }

    /// <summary>失效时间。</summary>
    [JsonPropertyName("ExpireAt")]
    public DateTime ExpireAt { get; set; }

    /// <summary>创建时间。</summary>
    [JsonPropertyName("CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>链接类型。</summary>
    [JsonPropertyName("Type")]
    public int Type { get; set; }
}

/// <summary>
/// 区域内容保存入参。
/// </summary>
public sealed class SiteRegionContentSaveInput
{
    /// <summary>编号；0=新增。</summary>
    public int ID { get; set; }

    /// <summary>区域编号。</summary>
    public int RegionID { get; set; }

    /// <summary>编号串。</summary>
    public string AdNo { get; set; }

    /// <summary>标题。</summary>
    public string Title { get; set; }

    /// <summary>内容。</summary>
    public string Content { get; set; }

    /// <summary>备注。</summary>
    public string Remark { get; set; }

    /// <summary>链接。</summary>
    public string Url { get; set; }

    /// <summary>排序。</summary>
    public int Sort { get; set; }

    /// <summary>图片。</summary>
    public string Pic { get; set; }

    /// <summary>是否显示。</summary>
    public bool IsShow { get; set; } = true;

    /// <summary>失效时间。</summary>
    public DateTime? ExpireAt { get; set; }

    /// <summary>链接类型。</summary>
    public int Type { get; set; }
}
