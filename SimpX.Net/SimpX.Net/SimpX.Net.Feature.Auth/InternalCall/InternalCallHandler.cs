using System.Net.Http;
using System.Text.Json;

namespace SimpX.Net.Feature.Auth;

/// <summary>
/// 内部调用的 HttpClient 处理器：发请求前按 InternalCall 配置签名；收到统一结果且 code 不是 200 时，用对方的提示信息抛友好异常。
/// 挂在命名 HttpClient 上，Furion 声明式请求（IHttpDeclarative）和 IHttpRemoteService 都会经过它。
/// </summary>
public sealed class InternalCallHandler : DelegatingHandler
{
    private readonly IOptionsMonitor<InternalCallOptions> _options;

    /// <summary>
    /// 注入内部调用配置。
    /// </summary>
    /// <param name="options">InternalCall 配置</param>
    public InternalCallHandler(IOptionsMonitor<InternalCallOptions> options)
    {
        _options = options;
    }

    /// <summary>
    /// 签名后发送；对方业务失败（统一结果 code 非 200）时直接抛出，调用方不用再判断 code。
    /// </summary>
    /// <param name="request">请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>响应</returns>
    /// <exception cref="AppFriendlyException">对方返回业务失败</exception>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var opt = _options.CurrentValue;
        var path = request.RequestUri!.AbsolutePath;
        foreach (var kv in InternalCallSigner.CreateHeaders(request.Method.Method, path, opt.AccessKey, opt.AccessSecret))
        {
            request.Headers.Remove(kv.Key);
            request.Headers.TryAddWithoutValidation(kv.Key, kv.Value);
        }

        var response = await base.SendAsync(request, cancellationToken);

        var mediaType = response.Content?.Headers.ContentType?.MediaType;
        if (mediaType == null || !mediaType.Contains("json", StringComparison.OrdinalIgnoreCase))
            return response;

        // ReadAsStringAsync 会先把内容缓存下来，后面的声明式客户端还能再读一次
        var text = await response.Content!.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(text)) return response;

        using var doc = JsonDocument.Parse(text);
        var root = doc.RootElement;
        if (root.ValueKind == JsonValueKind.Object
            && root.TryGetProperty("code", out var code) && code.TryGetInt32(out var codeValue)
            && root.TryGetProperty("type", out _)
            && codeValue != StatusCodes.Status200OK)
        {
            var message = root.TryGetProperty("message", out var m) ? m.ToString() : $"HTTP {(int)response.StatusCode}";
            response.Dispose();
            throw Oops.Oh(message);
        }

        return response;
    }
}
