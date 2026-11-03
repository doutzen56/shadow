using System.Net;
using SimpX.Net.Feature.Http;

namespace SimpX.Net.Test;

/// <summary>
/// 测试用 IHttpCaller：按委托回包，不碰真实 HttpClient。
/// </summary>
internal sealed class FakeHttpCaller : IHttpCaller
{
    private readonly Func<HttpCallRequest, HttpCallResponse> _handler;

    /// <summary>
    /// 注入处理委托。
    /// </summary>
    /// <param name="handler">请求 → 响应</param>
    public FakeHttpCaller(Func<HttpCallRequest, HttpCallResponse> handler) =>
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));

    /// <inheritdoc />
    public Task<HttpCallResponse> SendAsync(HttpCallRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(_handler(request));

    /// <inheritdoc />
    public async Task<string> GetStringAsync(
        string url,
        IDictionary<string, string> headers = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default) =>
        (await SendAsync(new HttpCallRequest
        {
            Method = HttpMethod.Get,
            Url = url,
            Headers = headers,
            Timeout = timeout
        }, cancellationToken)).Body;

    /// <inheritdoc />
    public async Task<string> PostJsonAsync(
        string url,
        object body,
        IDictionary<string, string> headers = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default) =>
        (await SendAsync(new HttpCallRequest
        {
            Method = HttpMethod.Post,
            Url = url,
            JsonContent = body,
            Headers = headers,
            Timeout = timeout
        }, cancellationToken)).Body;

    /// <inheritdoc />
    public async Task<string> PostFormAsync(
        string url,
        string formUrlEncoded,
        IDictionary<string, string> headers = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default) =>
        (await SendAsync(new HttpCallRequest
        {
            Method = HttpMethod.Post,
            Url = url,
            FormUrlEncodedContent = formUrlEncoded,
            Headers = headers,
            Timeout = timeout
        }, cancellationToken)).Body;

    /// <summary>固定成功空响应（认领类单测用）。</summary>
    public static FakeHttpCaller Noop() =>
        new(_ => new HttpCallResponse { StatusCode = HttpStatusCode.OK, Body = "{}" });
}
