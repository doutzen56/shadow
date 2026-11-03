using SimpX.Net.Feature.Http;

namespace SimpX.Net.Application.Schedule;

/// <summary>
/// HTTP 请求作业处理程序。出站走 <see cref="IHttpCaller"/>（Furion HttpRemote 薄封装）。
/// </summary>
[SuppressSniffer]
public class HttpJob : IJob
{
    /// <summary>
    /// 无效 HTTP 请求错误消息
    /// </summary>
    private const string INVALID_HTTP_ERROR_MESSAGE = "Invalid HTTP job request. (Parameter 'RequestUri')";

    private readonly IHttpCaller _httpCaller;
    private readonly IScheduleLogger _logger;

    /// <summary>
    /// 注入 HTTP 调用与作业日志。
    /// </summary>
    /// <param name="httpCaller">外部 HTTP 调用</param>
    /// <param name="logger">作业调度器日志</param>
    public HttpJob(IHttpCaller httpCaller, IScheduleLogger logger)
    {
        _httpCaller = httpCaller;
        _logger = logger;
    }

    /// <summary>
    /// 具体处理逻辑
    /// </summary>
    /// <param name="context">作业执行前上下文</param>
    /// <param name="stoppingToken">取消任务 Token</param>
    public async Task ExecuteAsync(JobExecutingContext context, CancellationToken stoppingToken)
    {
        var jobDetail = context.JobDetail;
        var httpJobMessage = JSON.Deserialize<HttpJobMessage>(jobDetail.GetProperty<string>(nameof(HttpJob)));

        if (httpJobMessage == null || string.IsNullOrWhiteSpace(httpJobMessage.RequestUri))
        {
            _logger.LogWarning(INVALID_HTTP_ERROR_MESSAGE);
            context.Result = INVALID_HTTP_ERROR_MESSAGE;
            return;
        }

        var headers = httpJobMessage.Headers is { Count: > 0 }
            ? new Dictionary<string, string>(httpJobMessage.Headers)
            : new Dictionary<string, string>();
        if (!headers.ContainsKey("User-Agent"))
            headers["User-Agent"] = UserAgents.Chrome.PC;

        var request = new HttpCallRequest
        {
            Method = httpJobMessage.HttpMethod,
            Url = httpJobMessage.RequestUri,
            Headers = headers,
            HttpClientName = httpJobMessage.ClientName,
            Timeout = httpJobMessage.Timeout != null
                ? TimeSpan.FromMilliseconds(httpJobMessage.Timeout.Value)
                : null,
            EnsureSuccessStatusCode = httpJobMessage.EnsureSuccessStatusCode
        };

        if (httpJobMessage.HttpMethod != HttpMethod.Get
            && httpJobMessage.HttpMethod != HttpMethod.Head
            && !string.IsNullOrWhiteSpace(httpJobMessage.Body))
        {
            request.JsonContent = httpJobMessage.Body;
        }

        var response = await _httpCaller.SendAsync(request, stoppingToken);

        string responseContent;
        if (httpJobMessage.PrintResponseContent)
        {
            responseContent = response.Body;
            _logger.LogInformation(
                $"Received HTTP response body with a length of <{responseContent.Length}> output as follows - {(int)response.StatusCode}{Environment.NewLine}{responseContent}");
        }
        else
        {
            responseContent = "COMPLETED";
        }

        context.Result = JSON.Serialize(new
        {
            response.StatusCode,
            Body = responseContent
        });
    }
}
