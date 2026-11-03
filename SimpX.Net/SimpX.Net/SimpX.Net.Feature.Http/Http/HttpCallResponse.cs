namespace SimpX.Net.Feature.Http;

/// <summary>
/// 一次 HTTP 调用的响应。
/// </summary>
public sealed class HttpCallResponse
{
    /// <summary>状态码。</summary>
    public HttpStatusCode StatusCode { get; set; }

    /// <summary>响应正文（按 UTF-8 文本读出）。</summary>
    public string Body { get; set; }

    /// <summary>是否 2xx。</summary>
    public bool IsSuccessStatusCode => (int)StatusCode is >= 200 and <= 299;
}
