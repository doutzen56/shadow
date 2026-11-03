using Elastic.Clients.Elasticsearch;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// ES日志写入器
/// </summary>
public class ElasticSearchLoggingWriter : IDatabaseLoggingWriter, IDisposable
{
    private readonly IServiceScope _serviceScope;
    private readonly ElasticsearchClient _esClient;
    private readonly ConfigAppService _configAppService;

    public ElasticSearchLoggingWriter(IServiceScopeFactory scopeFactory)
    {
        _serviceScope = scopeFactory.CreateScope();
        _esClient = _serviceScope.ServiceProvider.GetRequiredService<ElasticsearchClient>();
        _configAppService = _serviceScope.ServiceProvider.GetRequiredService<ConfigAppService>();
    }

    /// <summary>
    /// 批量写入日志到 Elasticsearch
    /// </summary>
    /// <param name="batchLogMsgs">批量日志消息列表</param>
    /// <param name="flush">是否立即刷新（保留参数，便于扩展）</param>
    public async Task WriteAsync(IReadOnlyList<LogMessage> batchLogMsgs, bool flush)
    {
        if (batchLogMsgs == null || batchLogMsgs.Count == 0) return;

        // 是否启用操作日志
        var opLogEnabled = await _configAppService.GetConfigValueByCode<bool>(ConfigConst.SysOpLog);
        if (!opLogEnabled) return;

        // 操作日志集合
        List<SysLogOp> opLogs = [];

        // 遍历处理日志
        foreach (var logMsg in batchLogMsgs)
        {
            var opLog = await ProcessLogMessageAsync(logMsg);
            if (opLog != null)
                opLogs.Add(opLog);
        }

        // 批量写入 Elasticsearch
        await _esClient.IndexManyAsync(opLogs);
    }

    /// <summary>
    /// 处理单条日志消息
    /// </summary>
    /// <param name="logMsg">日志消息对象</param>
    /// <returns>日志实体</returns>
    private async Task<SysLogOp> ProcessLogMessageAsync(LogMessage logMsg)
    {
        // 从上下文中获取日志监控信息
        var jsonStr = logMsg.Context?.Get("loggingMonitor")?.ToString();
        if (string.IsNullOrWhiteSpace(jsonStr)) return null;

        // 反序列化为动态对象（便于处理）
        var loggingMonitor = JSON.Deserialize<LoggingMonitorDto>(jsonStr);

        // 不记录 userInfo 和 logout 操作的日志
        if (loggingMonitor.ActionName is "userInfo" or "logout") return null;

        // 解析用户信息（从授权声明中获取）
        var userInfo = CommonHelper.GetUserInfo(loggingMonitor);

        // 获取客户端真实IP（考虑代理转发场景）
        string remoteIPv4 = loggingMonitor.RequestHeaders?.FirstOrDefault(u => u.Key == "X-Forwarded-For").Value?.ToString() ?? loggingMonitor.RemoteIPv4;

        // 获取IP地理位置信息（省市、经纬度）
        var (ipLocation, longitude, latitude) = CommonHelper.GetIpAddress(remoteIPv4);

        // 序列化请求参数、返回结果、异常，再打码（避免空引用异常）
        var requestParam = loggingMonitor.Parameters?.FirstOrDefault()?.Value != null
            ? LoggingSecretMasker.Mask(JSON.Serialize(loggingMonitor.Parameters.First().Value))
            : null;
        var returnResult = loggingMonitor.ReturnInformation?.Value != null
            ? LoggingSecretMasker.Mask(JSON.Serialize(loggingMonitor.ReturnInformation.Value))
            : null;
        var exception = loggingMonitor.Exception != null
            ? LoggingSecretMasker.Mask(JSON.Serialize(loggingMonitor.Exception))
            : null;

        // 构建日志实体
        return new SysLogOp
        {
            Id = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),                     // 日志ID（使用时间戳）
            ControllerName = loggingMonitor.ControllerName,                          // 控制器名称
            ActionName = loggingMonitor.ActionTypeName,                              // 操作方法名称
            DisplayTitle = loggingMonitor.DisplayTitle,                              // 显示标题
            Status = loggingMonitor.ReturnInformation?.HttpStatusCode.ToString() ?? "200",    // HTTP状态码
            RemoteIp = remoteIPv4,                                                   // 客户端IP
            Location = ipLocation,                                                   // IP地理位置
            Longitude = (decimal)longitude,                                                   // 经度
            Latitude = (decimal)latitude,                                                     // 纬度
            Browser = loggingMonitor.UserAgent,                                      // 浏览器信息（原始UserAgent）
            Os = $"{loggingMonitor.OsDescription} {loggingMonitor.OsArchitecture}",  // 操作系统信息
            Elapsed = loggingMonitor.TimeOperationElapsedMilliseconds,               // 接口执行耗时（毫秒）
            LogDateTime = logMsg.LogDateTime,                                        // 日志时间
            Account = userInfo.Account,                                              // 用户账号
            RealName = userInfo.RealName,                                            // 用户真实姓名
            HttpMethod = loggingMonitor.HttpMethod,                                  // HTTP方法（GET/POST等）
            RequestUrl = loggingMonitor.RequestUrl,                                  // 请求URL
            RequestParam = requestParam,                                             // 请求参数
            ReturnResult = returnResult,                                             // 返回结果
            EventId = logMsg.EventId.Id,                                             // 事件ID
            ThreadId = logMsg.ThreadId,                                              // 线程ID
            TraceId = logMsg.TraceId,                                                // 链路追踪ID
            Exception = exception,                                                   // 异常详情
            Message = LoggingSecretMasker.Mask(logMsg.Message),                      // 日志消息内容（令牌已打码）
            CreateUserId = userInfo.UserId,                                          // 创建用户ID
            TenantId = userInfo.TenantId                                             // 租户ID
        };
    }

    /// <summary>
    /// 释放服务作用域
    /// </summary>
    public void Dispose()
    {
        _serviceScope.Dispose();
    }
}