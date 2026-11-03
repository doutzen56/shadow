using System.Net.Http.Headers;
using System.Text;
using Furion.HttpRemote;

namespace SimpX.Net.Feature.Http;

/// <summary>
/// 默认 HTTP 调用实现：包装 Furion <see cref="IHttpRemoteService"/>。
/// </summary>
public sealed class HttpCaller : IHttpCaller, ITransient
{
    private const int ErrorBodyMaxLength = 500;
    private readonly IHttpRemoteService _httpRemote;

    /// <summary>
    /// 注入 Furion HttpRemote。
    /// </summary>
    /// <param name="httpRemote">Furion 远程 HTTP 服务</param>
    public HttpCaller(IHttpRemoteService httpRemote)
    {
        _httpRemote = httpRemote;
    }

    /// <inheritdoc />
    public async Task<HttpCallResponse> SendAsync(HttpCallRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null) throw Oops.Oh("HTTP 请求参数不能为空");
        if (string.IsNullOrWhiteSpace(request.Url)) throw Oops.Oh("HTTP 请求地址不能为空");
        if (request.Method == null) throw Oops.Oh("HTTP 方法不能为空");

        var builder = HttpRequestBuilder.Create(request.Method, request.Url);

        if (!string.IsNullOrWhiteSpace(request.HttpClientName))
            builder.SetHttpClientName(request.HttpClientName);

        if (request.Timeout is { } timeout)
            builder.SetTimeout(timeout);

        if (request.Headers is { Count: > 0 })
            builder.WithHeaders(request.Headers);

        ApplyBody(builder, request);

        HttpResponseMessage response = null;
        try
        {
            response = await _httpRemote.SendAsync(builder, cancellationToken);
            var body = response?.Content == null
                ? string.Empty
                : await response.Content.ReadAsStringAsync(cancellationToken);

            var status = response?.StatusCode ?? 0;
            if (request.EnsureSuccessStatusCode && response is { IsSuccessStatusCode: false })
            {
                throw Oops.Oh($"HTTP 调用失败：{(int)status} {TruncateForError(body)}");
            }

            return new HttpCallResponse
            {
                StatusCode = status,
                Body = body ?? string.Empty
            };
        }
        finally
        {
            response?.Dispose();
        }
    }

    /// <inheritdoc />
    public async Task<string> GetStringAsync(
        string url,
        IDictionary<string, string> headers = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        var response = await SendAsync(new HttpCallRequest
        {
            Method = HttpMethod.Get,
            Url = url,
            Headers = headers,
            Timeout = timeout
        }, cancellationToken);
        return response.Body;
    }

    /// <inheritdoc />
    public async Task<string> PostJsonAsync(
        string url,
        object body,
        IDictionary<string, string> headers = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        var response = await SendAsync(new HttpCallRequest
        {
            Method = HttpMethod.Post,
            Url = url,
            JsonContent = body,
            Headers = headers,
            Timeout = timeout
        }, cancellationToken);
        return response.Body;
    }

    /// <inheritdoc />
    public async Task<string> PostFormAsync(
        string url,
        string formUrlEncoded,
        IDictionary<string, string> headers = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        var response = await SendAsync(new HttpCallRequest
        {
            Method = HttpMethod.Post,
            Url = url,
            FormUrlEncodedContent = formUrlEncoded,
            Headers = headers,
            Timeout = timeout
        }, cancellationToken);
        return response.Body;
    }

    /// <summary>
    /// 按请求里的正文类型写入 Furion 构建器。
    /// </summary>
    private static void ApplyBody(HttpRequestBuilder builder, HttpCallRequest request)
    {
        if (request.JsonContent != null)
        {
            if (request.JsonContent is string jsonText)
                builder.SetJsonContent(jsonText, Encoding.UTF8);
            else
                builder.SetJsonContent(request.JsonContent);
            return;
        }

        if (!string.IsNullOrEmpty(request.FormUrlEncodedContent))
        {
            builder.SetContent(new StringContent(
                request.FormUrlEncodedContent,
                Encoding.UTF8,
                "application/x-www-form-urlencoded"));
            return;
        }

        if (!string.IsNullOrEmpty(request.RawContent))
        {
            var contentType = string.IsNullOrWhiteSpace(request.RawContentType)
                ? "text/plain"
                : request.RawContentType;
            var content = new StringContent(request.RawContent, Encoding.UTF8);
            content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
            builder.SetContent(content);
        }
    }

    /// <summary>
    /// 截断错误正文，避免把大段响应（可能含敏感信息）塞进异常。
    /// </summary>
    private static string TruncateForError(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return string.Empty;
        var trimmed = body.Trim();
        return trimmed.Length <= ErrorBodyMaxLength
            ? trimmed
            : trimmed[..ErrorBodyMaxLength] + "…";
    }
}
