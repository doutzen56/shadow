using System.Text.Json.Serialization;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 进游戏成功时 info 结构（对齐旧 LoginResult.ToString：url/method/data）。
/// </summary>
public sealed class GameLoginInfoDto
{
    /// <summary>打开地址。</summary>
    [JsonPropertyName("url")]
    public string Url { get; set; }

    /// <summary>HTTP 方法。</summary>
    [JsonPropertyName("method")]
    public string Method { get; set; }

    /// <summary>POST 数据；无则 null。</summary>
    [JsonPropertyName("data")]
    public Dictionary<string, string> Data { get; set; }
}
