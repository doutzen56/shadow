namespace SimpX.Net.Application.Admin;

/// <summary>
/// 后台审计日志写入器注册：把监听到的操作、访问、异常日志写进后台日志表或 ElasticSearch。
/// </summary>
public static class AuditLoggingSetup
{
    /// <summary>
    /// 按 Logging:Database:Enabled、Logging:ElasticSearch:Enabled 注册对应写入器，只写 LoggingMonitor 产生的日志。
    /// </summary>
    /// <param name="services">服务集合</param>
    public static void AddAuditLogWriters(this IServiceCollection services)
    {
        if (App.GetConfig<bool>("Logging:ElasticSearch:Enabled", true))
        {
            services.AddDatabaseLogging<ElasticSearchLoggingWriter>(options =>
            {
                options.WithTraceId = true;
                options.WithStackFrame = true;
                options.IgnoreReferenceLoop = false;
                options.MessageFormat = LoggerFormatter.Json;
                options.WriteFilter = logMsg => logMsg.LogName == CommonConst.SysLogCategoryName;
            });
        }

        if (App.GetConfig<bool>("Logging:Database:Enabled", true))
        {
            services.AddDatabaseLogging<DatabaseLoggingWriter>(options =>
            {
                options.WithTraceId = true;
                options.WithStackFrame = true;
                options.IgnoreReferenceLoop = false;
                options.MessageFormat = logMsg =>
                {
                    var stringBuilder = new StringBuilder();
                    stringBuilder.AppendLine(logMsg.Message);
                    return stringBuilder.ToString();
                };
                options.WriteFilter = logMsg => logMsg.LogName == CommonConst.SysLogCategoryName;
            });
        }
    }
}
