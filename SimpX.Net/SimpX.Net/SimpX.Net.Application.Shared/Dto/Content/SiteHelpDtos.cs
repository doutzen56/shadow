using System.Text.Json.Serialization;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 帮助列表行（对齐旧 /admin/site/help/list）。
/// </summary>
public sealed class SiteHelpListItemDto
{
    /// <summary>编号。</summary>
    [JsonPropertyName("ID")]
    public int ID { get; set; }

    /// <summary>标题（优先中文）。</summary>
    [JsonPropertyName("Title")]
    public string Title { get; set; }

    /// <summary>栏目编号。</summary>
    [JsonPropertyName("ColID")]
    public int ColID { get; set; }

    /// <summary>栏目名。</summary>
    [JsonPropertyName("Column")]
    public string Column { get; set; }

    /// <summary>创建时间。</summary>
    [JsonPropertyName("CreateAt")]
    public DateTime CreateAt { get; set; }
}

/// <summary>
/// 帮助栏目行（对齐旧 columnlist）。
/// </summary>
public sealed class SiteHelpColumnItemDto
{
    /// <summary>编号。</summary>
    [JsonPropertyName("ID")]
    public int ID { get; set; }

    /// <summary>名称。</summary>
    [JsonPropertyName("Name")]
    public string Name { get; set; }

    /// <summary>文章数。</summary>
    [JsonPropertyName("Count")]
    public int Count { get; set; }

    /// <summary>排序。</summary>
    [JsonPropertyName("Sort")]
    public short Sort { get; set; }

    /// <summary>创建时间。</summary>
    [JsonPropertyName("CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>底部显示。</summary>
    [JsonPropertyName("IsFooter")]
    public bool IsFooter { get; set; }

    /// <summary>是否外链。</summary>
    [JsonPropertyName("IsUrl")]
    public bool IsUrl { get; set; }
}


/// <summary>
/// 帮助详情（对齐旧版 /admin/site/help/info）。
/// </summary>
public sealed class SiteHelpDetailDto
{
    /// <summary>编号。</summary>
    [JsonPropertyName("ID")]
    public int ID { get; set; }

    /// <summary>栏目。</summary>
    [JsonPropertyName("ColID")]
    public int ColID { get; set; }

    /// <summary>标题（多为多语言 JSON）。</summary>
    [JsonPropertyName("Title")]
    public string Title { get; set; }

    /// <summary>正文（多为多语言 JSON）。</summary>
    [JsonPropertyName("Content")]
    public string Content { get; set; }

    /// <summary>排序。</summary>
    [JsonPropertyName("Sort")]
    public short Sort { get; set; }

    /// <summary>创建时间。</summary>
    [JsonPropertyName("CreateAt")]
    public DateTime CreateAt { get; set; }
}

/// <summary>
/// 帮助保存入参（对齐旧版 /admin/site/help/save）。
/// </summary>
public sealed class SiteHelpSaveInput
{
    /// <summary>编号；0=新增。</summary>
    public int ID { get; set; }

    /// <summary>栏目。</summary>
    public int ColID { get; set; }

    /// <summary>标题。</summary>
    public string Title { get; set; }

    /// <summary>正文。</summary>
    public string Content { get; set; }

    /// <summary>排序。</summary>
    public short Sort { get; set; }
}

/// <summary>帮助栏目保存入参。</summary>
public sealed class SiteHelpColumnSaveInput
{
    /// <summary>编号；0=新增。</summary>
    public int ID { get; set; }

    /// <summary>名称（可为多语言 JSON）。</summary>
    public string Name { get; set; }

    /// <summary>排序。</summary>
    public short Sort { get; set; }

    /// <summary>外链。</summary>
    public string Url { get; set; }

    /// <summary>是否底部显示。</summary>
    public bool IsFooter { get; set; }
}
