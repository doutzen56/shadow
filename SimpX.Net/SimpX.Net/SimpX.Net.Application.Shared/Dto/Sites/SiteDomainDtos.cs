using System.Text.Json.Serialization;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 站点域名保存入参（对齐旧版 /admin/site/domain/save）。
/// </summary>
public sealed class SiteDomainSaveInput
{
    /// <summary>编号；0=新增。</summary>
    public int ID { get; set; }

    /// <summary>域名（不含 http）。</summary>
    public string Domain { get; set; }

    /// <summary>权重。</summary>
    public short Weights { get; set; }

    /// <summary>代理名；空=官网。</summary>
    public string AgentName { get; set; }

    /// <summary>是否显示。</summary>
    public bool IsVisible { get; set; }

    /// <summary>是否 https。</summary>
    public bool IsSSL { get; set; }

    /// <summary>是否 APP。</summary>
    public bool IsAPP { get; set; }

    /// <summary>是否单页代理。</summary>
    public bool IsSingle { get; set; }

    /// <summary>测速模板。</summary>
    public int Speed { get; set; }
}

/// <summary>
/// 站点域名详情。
/// </summary>
public sealed class SiteDomainDetailDto
{
    /// <summary>编号。</summary>
    [JsonPropertyName("ID")]
    public int ID { get; set; }

    /// <summary>域名。</summary>
    [JsonPropertyName("Domain")]
    public string Domain { get; set; }

    /// <summary>是否显示。</summary>
    [JsonPropertyName("IsVisible")]
    public bool IsVisible { get; set; }

    /// <summary>是否 https。</summary>
    [JsonPropertyName("IsSSL")]
    public bool IsSSL { get; set; }

    /// <summary>是否 APP。</summary>
    [JsonPropertyName("IsAPP")]
    public bool IsAPP { get; set; }

    /// <summary>测速。</summary>
    [JsonPropertyName("Speed")]
    public int Speed { get; set; }

    /// <summary>权重。</summary>
    [JsonPropertyName("Weights")]
    public short Weights { get; set; }

    /// <summary>是否单页。</summary>
    [JsonPropertyName("IsSingle")]
    public bool IsSingle { get; set; }

    /// <summary>代理名（官网时前端可空）。</summary>
    [JsonPropertyName("AgentName")]
    public string AgentName { get; set; }
}
