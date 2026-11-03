using System.Text.Json.Serialization;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// IP 黑名单行（对齐旧 blacklist）。
/// </summary>
public sealed class IpBlackListItemDto
{
    /// <summary>编号。</summary>
    [JsonPropertyName("ID")]
    public int ID { get; set; }

    /// <summary>名称。</summary>
    [JsonPropertyName("Name")]
    public string Name { get; set; }

    /// <summary>创建时间。</summary>
    [JsonPropertyName("CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>起始 IP 点分文本。</summary>
    [JsonPropertyName("StartIP")]
    public string StartIP { get; set; }

    /// <summary>结束 IP 点分文本。</summary>
    [JsonPropertyName("EndIP")]
    public string EndIP { get; set; }

    /// <summary>备注。</summary>
    [JsonPropertyName("Remark")]
    public string Remark { get; set; }
}

/// <summary>
/// IP 黑名单保存入参（对齐旧版 saveblackip）。
/// </summary>
public sealed class IpBlackSaveInput
{
    /// <summary>编号；0=新增。</summary>
    public int ID { get; set; }

    /// <summary>名称。</summary>
    public string Name { get; set; }

    /// <summary>起始 IP 点分文本。</summary>
    public string StartIP { get; set; }

    /// <summary>结束 IP 点分文本；空则等同起始。</summary>
    public string EndIP { get; set; }

    /// <summary>备注。</summary>
    public string Remark { get; set; }
}
