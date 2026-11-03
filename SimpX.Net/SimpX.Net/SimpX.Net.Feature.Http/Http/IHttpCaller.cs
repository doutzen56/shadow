namespace SimpX.Net.Feature.Http;

/// <summary>
/// 外部 HTTP 调用入口。底层是 Furion HttpRemote；内部宿主互调请用 IHttpDeclarative，不要走这里。
/// </summary>
public interface IHttpCaller
{
    /// <summary>
    /// 按完整参数发一次请求。
    /// </summary>
    /// <param name="request">请求参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>响应</returns>
    Task<HttpCallResponse> SendAsync(HttpCallRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// GET，成功时返回正文。
    /// </summary>
    /// <param name="url">地址</param>
    /// <param name="headers">可选请求头</param>
    /// <param name="timeout">可选超时</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>响应正文</returns>
    Task<string> GetStringAsync(
        string url,
        IDictionary<string, string> headers = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// POST JSON，成功时返回正文。
    /// </summary>
    /// <param name="url">地址</param>
    /// <param name="body">对象或 JSON 字符串</param>
    /// <param name="headers">可选请求头</param>
    /// <param name="timeout">可选超时</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>响应正文</returns>
    Task<string> PostJsonAsync(
        string url,
        object body,
        IDictionary<string, string> headers = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// POST form-urlencoded，成功时返回正文。
    /// </summary>
    /// <param name="url">地址</param>
    /// <param name="formUrlEncoded">已拼好的表单串，如 a=1&amp;b=2</param>
    /// <param name="headers">可选请求头</param>
    /// <param name="timeout">可选超时</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>响应正文</returns>
    Task<string> PostFormAsync(
        string url,
        string formUrlEncoded,
        IDictionary<string, string> headers = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default);
}
