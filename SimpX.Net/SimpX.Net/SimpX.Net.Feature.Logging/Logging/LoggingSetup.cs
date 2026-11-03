namespace SimpX.Net.Feature.Logging;

public static class LoggingSetup
{
    /// <summary>
    /// 日志注册
    /// </summary>
    /// <param name="services"></param>
    public static void AddLoggingSetup(this IServiceCollection services)
    {
        // 日志监听：请求入参和返回值会原样写日志，密码、密钥、令牌这类字段一律不记
        services.AddMonitorLogging(options =>
        {
            options.IgnorePropertyNames = new[]
            {
                "Byte",
                "Password", "PasswordOld", "PasswordNew",
                "AccessSecret", "SecretKey", "PrivateKey",
                "AccessToken", "RefreshToken"
            };
            options.IgnorePropertyTypes = new[] { typeof(byte[]) };
        });

        // 控制台日志：写出前把 Bearer 令牌打码
        var consoleLog = App.GetConfig<bool>("Logging:Monitor:ConsoleLog", true);
        services.AddConsoleFormatter(options =>
        {
            options.DateFormat = "yyyy-MM-dd HH:mm:ss(zzz) dddd";
            //options.WithTraceId = true; // 显示线程Id
            //options.WithStackFrame = true; // 显示程序集
            options.WriteFilter = (logMsg) =>
            {
                return consoleLog;
            };
            options.MessageProcess = LoggingSecretMasker.Mask;
        });

        // 日志写入文件：写出前同样打码
        if (App.GetConfig<bool>("Logging:File:Enabled", true))
        {
            var loggingMonitorSettings = App.GetConfig<LoggingMonitorSettings>("Logging:Monitor", true);
            Array.ForEach(new[] { LogLevel.Information, LogLevel.Warning, LogLevel.Error }, logLevel =>
            {
                services.AddFileLogging(options =>
                {
                    options.WithTraceId = true; // 显示线程Id
                    options.WithStackFrame = true; // 显示程序集
                    options.FileNameRule = fileName => string.Format(fileName, DateTime.Now, logLevel.ToString()); // 每天创建一个文件
                    options.WriteFilter = logMsg => logMsg.LogLevel == logLevel; // 日志级别
                    options.MessageProcess = LoggingSecretMasker.Mask;
                    options.HandleWriteError = (writeError) => // 写入失败时启用备用文件
                    {
                        writeError.UseRollbackFileName(Path.GetFileNameWithoutExtension(writeError.CurrentFileName) + "-oops" + Path.GetExtension(writeError.CurrentFileName));
                    };
                    if (loggingMonitorSettings.JsonBehavior == JsonBehavior.OnlyJson)
                    {
                        options.MessageFormat = (logMsg) =>
                        {
                            var jsonString = logMsg.Context.Get("loggingMonitor");
                            return LoggingSecretMasker.Mask(jsonString?.ToString());
                        };
                    }
                });
            });
        }

        // 写入数据库 / ElasticSearch 的审计日志属于后台业务（日志表在后台），由后台应用自己注册写入器
    }
}