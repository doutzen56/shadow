namespace SimpX.Net.Feature.Http;

/// <summary>
/// 一次 HTTP 调用的请求参数。底层走 Furion HttpRemote，不用于内部宿主互调（互调用 Declarative）。
/// </summary>
public sealed class HttpCallRequest
{
    /// <summary>HTTP 方法。</summary>
    public HttpMethod Method { get; set; } = HttpMethod.Get;

    /// <summary>完整请求地址。</summary>
    public string Url { get; set; }

    /// <summary>请求头；不要把密钥写进日志，头里可以带 Authorization。</summary>
    public IDictionary<string, string> Headers { get; set; }

    /// <summary>JSON 正文（对象或已序列化字符串）；与 Form / Raw 三选一。</summary>
    public object JsonContent { get; set; }

    /// <summary>application/x-www-form-urlencoded 正文（已拼好的键值串）。</summary>
    public string FormUrlEncodedContent { get; set; }

    /// <summary>原始正文；需同时指定 <see cref="RawContentType"/>。</summary>
    public string RawContent { get; set; }

    /// <summary>原始正文的 Content-Type。</summary>
    public string RawContentType { get; set; }

    /// <summary>超时；不设则用 HttpClient 默认超时。</summary>
    public TimeSpan? Timeout { get; set; }

    /// <summary>命名 HttpClient；外部通道可用来配超时/代理，不要挂内部签名。</summary>
    public string HttpClientName { get; set; }

    /// <summary>非 2xx 时是否直接抛友好异常；默认 true。</summary>
    public bool EnsureSuccessStatusCode { get; set; } = true;
}
