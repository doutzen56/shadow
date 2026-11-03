using System.Text.Json.Serialization;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 新闻分页查询（JWT）。筛选对齐旧版 /admin/site/news/list。
/// </summary>
public sealed class SiteNewsPageInput
{
    /// <summary>页码，从 1 起。</summary>
    public int Page { get; set; } = 1;

    /// <summary>页大小。</summary>
    public int PageSize { get; set; } = 20;

    /// <summary>标题模糊（库内 JSON 串 Contains）。</summary>
    public string Title { get; set; }

    /// <summary>栏目编号；0 表示不限。</summary>
    public int ColID { get; set; }

    /// <summary>弹出筛选：空不限；1=登录前或登录后弹出。</summary>
    public string IsTip { get; set; }

    /// <summary>1=仅置顶（Sort≠0）。</summary>
    public int IsTop { get; set; }

    /// <summary>1=列表带回正文 Content。</summary>
    public int Content { get; set; }
}

/// <summary>
/// 新闻列表行（字段对齐旧版 list data）。
/// </summary>
public sealed class SiteNewsListItemDto
{
    /// <summary>新闻编号（旧版 ID）。</summary>
    [JsonPropertyName("ID")]
    public int ID { get; set; }

    /// <summary>栏目名（多语言 JSON 原文；旧版按当前语言取文案）。</summary>
    [JsonPropertyName("Column")]
    public string Column { get; set; }

    /// <summary>标题（库内多语言 JSON）。</summary>
    [JsonPropertyName("Title")]
    public string Title { get; set; }

    /// <summary>封面。</summary>
    [JsonPropertyName("Cover")]
    public string Cover { get; set; }

    /// <summary>创建时间。</summary>
    [JsonPropertyName("CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>是否任一弹出。</summary>
    [JsonPropertyName("IsTip")]
    public bool IsTip { get; set; }

    /// <summary>正文（仅 Content=1 时有值）。</summary>
    [JsonPropertyName("Content")]
    public string Content { get; set; }

    /// <summary>是否置顶。</summary>
    [JsonPropertyName("IsTop")]
    public bool IsTop { get; set; }

    /// <summary>开始时间。</summary>
    [JsonPropertyName("StartAt")]
    public DateTime StartAt { get; set; }

    /// <summary>结束时间。</summary>
    [JsonPropertyName("EndAt")]
    public DateTime EndAt { get; set; }

    /// <summary>排序。</summary>
    [JsonPropertyName("Sort")]
    public int Sort { get; set; }
}

/// <summary>
/// 新闻分页结果。
/// </summary>
public sealed class SiteNewsPageOutput
{
    /// <summary>总条数。</summary>
    [JsonPropertyName("RecordCount")]
    public int RecordCount { get; set; }

    /// <summary>页码（旧版 PageIndex）。</summary>
    [JsonPropertyName("PageIndex")]
    public int PageIndex { get; set; }

    /// <summary>页大小。</summary>
    [JsonPropertyName("PageSize")]
    public int PageSize { get; set; }

    /// <summary>当前页。</summary>
    [JsonPropertyName("data")]
    public List<SiteNewsListItemDto> Data { get; set; }
}

/// <summary>
/// 新闻详情（对齐旧版 info）。
/// </summary>
public sealed class SiteNewsDetailDto
{
    /// <summary>编号；0 表示新建默认。</summary>
    [JsonPropertyName("ID")]
    public int ID { get; set; }

    /// <summary>栏目。</summary>
    [JsonPropertyName("ColID")]
    public int ColID { get; set; }

    /// <summary>标题 JSON。</summary>
    [JsonPropertyName("Title")]
    public string Title { get; set; }

    /// <summary>正文 JSON。</summary>
    [JsonPropertyName("Content")]
    public string Content { get; set; }

    /// <summary>创建时间字符串。</summary>
    [JsonPropertyName("CreateAt")]
    public string CreateAt { get; set; }

    /// <summary>登录后弹出。</summary>
    [JsonPropertyName("IsTipAfterLogin")]
    public bool IsTipAfterLogin { get; set; }

    /// <summary>登录前弹出。</summary>
    [JsonPropertyName("IsTipBeforeLogin")]
    public bool IsTipBeforeLogin { get; set; }

    /// <summary>封面 JSON 字符串。</summary>
    [JsonPropertyName("Cover")]
    public string Cover { get; set; }

    /// <summary>排序。</summary>
    [JsonPropertyName("Sort")]
    public int Sort { get; set; }

    /// <summary>开始。</summary>
    [JsonPropertyName("StartAt")]
    public string StartAt { get; set; }

    /// <summary>结束。</summary>
    [JsonPropertyName("EndAt")]
    public string EndAt { get; set; }
}

/// <summary>
/// 保存新闻入参（对齐旧版 save）。
/// </summary>
public sealed class SiteNewsSaveInput
{
    /// <summary>0=新增。</summary>
    public int ID { get; set; }

    /// <summary>标题 JSON。</summary>
    public string Title { get; set; }

    /// <summary>栏目。</summary>
    public int ColID { get; set; }

    /// <summary>创建时间。</summary>
    public DateTime CreateAt { get; set; }

    /// <summary>登录后弹出。</summary>
    public bool IsTipAfterLogin { get; set; }

    /// <summary>登录前弹出。</summary>
    public bool IsTipBeforeLogin { get; set; }

    /// <summary>开始。</summary>
    public DateTime StartAt { get; set; }

    /// <summary>结束。</summary>
    public DateTime EndAt { get; set; }

    /// <summary>封面 JSON。</summary>
    public string Cover { get; set; }

    /// <summary>正文 JSON。</summary>
    public string Content { get; set; }

    /// <summary>排序。</summary>
    public int Sort { get; set; }
}

/// <summary>
/// 栏目列表项（对齐 columnlist）。
/// </summary>
public sealed class SiteNewsColumnItemDto
{
    /// <summary>栏目编号。</summary>
    [JsonPropertyName("ID")]
    public int ID { get; set; }

    /// <summary>名称 JSON。</summary>
    [JsonPropertyName("Name")]
    public string Name { get; set; }

    /// <summary>代码。</summary>
    [JsonPropertyName("Code")]
    public string Code { get; set; }

    /// <summary>排序。</summary>
    [JsonPropertyName("Sort")]
    public short Sort { get; set; }
}

/// <summary>新闻栏目保存入参。</summary>
public sealed class SiteNewsColumnSaveInput
{
    /// <summary>编号；0=新增。</summary>
    public int ID { get; set; }

    /// <summary>名称（多为 Translate JSON）。</summary>
    public string Name { get; set; }

    /// <summary>代码。</summary>
    public string Code { get; set; }

    /// <summary>排序。</summary>
    public short Sort { get; set; }
}
