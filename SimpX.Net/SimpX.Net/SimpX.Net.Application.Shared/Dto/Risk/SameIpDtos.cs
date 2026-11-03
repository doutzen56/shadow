using System.Text.Json.Serialization;

namespace SimpX.Net.Application.Shared;

/// <summary>同 IP / 同设备查询入参。</summary>
public sealed class SameRiskQueryInput
{
    /// <summary>会员用户名（可空，与 IP/设备二选一）。</summary>
    [JsonPropertyName("User")]
    public string User { get; set; } = string.Empty;

    /// <summary>IP（同 IP 查询用）。</summary>
    [JsonPropertyName("IP")]
    public string IP { get; set; } = string.Empty;

    /// <summary>设备号（同设备查询用）。</summary>
    [JsonPropertyName("Device")]
    public string Device { get; set; } = string.Empty;

    /// <summary>开始时间。</summary>
    [JsonPropertyName("StartAt")]
    public DateTime? StartAt { get; set; }

    /// <summary>结束时间。</summary>
    [JsonPropertyName("EndAt")]
    public DateTime? EndAt { get; set; }
}

/// <summary>同 IP / 同设备一行。</summary>
public sealed class SameRiskListItemDto
{
    /// <summary>会员编号。</summary>
    [JsonPropertyName("UserID")]
    public int UserID { get; set; }

    /// <summary>会员名。</summary>
    [JsonPropertyName("UserName")]
    public string UserName { get; set; } = string.Empty;

    /// <summary>命中次数。</summary>
    [JsonPropertyName("Count")]
    public int Count { get; set; }
}
