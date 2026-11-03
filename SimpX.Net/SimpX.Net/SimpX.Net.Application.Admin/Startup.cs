using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using SimpX.Net.ElasticSearch;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// Admin 应用启动：系统启动初始化、租户后台专属能力。作业远程管理已迁总后台。
/// </summary>
[AppStartup(100)]
public class Startup : AppStartup
{
    /// <summary>
    /// 注册 Admin 专属服务。
    /// </summary>
    /// <param name="services">服务集合</param>
    public void ConfigureServices(IServiceCollection services)
    {
        // Shared 登录服务不自动注入（见 SharedServiceRegistration），Admin 这里显式挂上
        services.AddSharedAdminAuthServices();
        // 业务推送：SignalR 通道实现（内容对齐旧 Notify）
        services.AddTransient<ISiteRealtimePublisher, SiteRealtimePublisher>();

        services.AddHostedService<SystemStartupInitializer>();
        services.AddScoped<ICacheWarmup, UserBlacklistWarmup>();

        // 审计日志仍看 Logging:ElasticSearch；注单明细看 ElasticSearch:Business，互不顶替。
        services.AddElasticSearch();
        services.AddTransient<IGameOrderEsQuery, GameOrderEsQuery>();

        // 审计日志写入器（数据库 / ElasticSearch）
        services.AddAuditLogWriters();

        // 第三方登录：具体平台实现 IExternalLoginProvider 后在这里注册；演示提供方谁都能登录，要在配置里明确打开
        services.AddConfigurableOptions<ExternalLoginOptions>();
        if (App.GetConfig<bool>("ExternalLogin:EnableDemo"))
            services.AddTransient<IExternalLoginProvider, DemoExternalLoginProvider>();

        // 短信：具体服务商实现 ISmsSender 后在这里注册；演示通道不真发短信、验证码固定，要在配置里明确打开
        services.AddConfigurableOptions<SmsOptions>();
        if (App.GetConfig<bool>("Sms:Demo:Enabled"))
            services.AddTransient<ISmsSender, DemoSmsSender>();

        // 文本翻译：配置了接口地址、密钥、模型才注册默认实现；要接别的翻译服务，实现 ITextTranslator 后在这里注册
        services.AddConfigurableOptions<TextTranslatorOptions>();
        if (App.GetConfig<TextTranslatorOptions>("TextTranslator", true)?.IsConfigured == true)
            services.AddTransient<ITextTranslator, ChatCompletionTextTranslator>();

        // 这里不要调 AddMvcCore/AddAuthorization，它们会抢先注册默认授权服务，让 Furion 的权限检查被跳过。
    }

    /// <summary>
    /// 请求处理链由 Hosting 统一配置，这里不追加。
    /// </summary>
    /// <param name="app">应用构建器</param>
    /// <param name="env">宿主环境</param>
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
    }
}
