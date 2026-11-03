using System.Text.Json.Serialization;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 系统更新通知列表查询。
/// </summary>
public sealed class SystemUpdateNewsListInput
{
    /// <summary>语种；null 表示全部。</summary>
    public int? Language { get; set; }
}

/// <summary>
/// 系统更新通知列表行。
/// </summary>
public sealed class SystemUpdateNewsItemDto
{
    /// <summary>编号。</summary>
    [JsonPropertyName("ID")]
    public int ID { get; set; }

    /// <summary>标题。</summary>
    [JsonPropertyName("Title")]
    public string Title { get; set; }

    /// <summary>正文。</summary>
    [JsonPropertyName("Content")]
    public string Content { get; set; }

    /// <summary>语种。</summary>
    [JsonPropertyName("Language")]
    public int Language { get; set; }

    /// <summary>创建时间。</summary>
    [JsonPropertyName("CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>标识。</summary>
    [JsonPropertyName("Ident")]
    public string Ident { get; set; }
}

/// <summary>
/// 保存系统更新通知。
/// </summary>
public sealed class SystemUpdateNewsSaveInput
{
    /// <summary>0=新增。</summary>
    public int ID { get; set; }

    /// <summary>标题。</summary>
    public string Title { get; set; }

    /// <summary>正文。</summary>
    public string Content { get; set; }

    /// <summary>语种整型。</summary>
    public int Language { get; set; }

    /// <summary>同组标识。</summary>
    public string Ident { get; set; }
}
