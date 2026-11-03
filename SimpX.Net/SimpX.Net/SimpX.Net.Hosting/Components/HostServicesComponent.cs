using Furion;
using Furion.JsonSerialization;
using Furion.Logging;
using IPTools.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SimpX.Net.Caching;
using SimpX.Net.Core;
using SixLabors.ImageSharp.Web.DependencyInjection;
using System;
using System.Linq;
using System.Text.Json;

namespace SimpX.Net.Hosting;

/// <summary>
/// 宿主公共服务组件：内核能力所有宿主都有；验证码、图片、SignalR、文件存储、IP 内存库等按 <see cref="HostCapabilities.Current"/> 点选。
/// 不含认证和 MVC：认证由各宿主选择的认证组件注册，MVC 由 <see cref="HostMvcComponent"/> 注册。
/// </summary>
public sealed class HostServicesComponent : IServiceComponent
{
    /// <summary>
    /// 注册公共服务。
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="componentContext">组件上下文</param>
    public void Load(IServiceCollection services, ComponentContext componentContext)
    {
        var caps = HostCapabilities.Current;

        services.AddProjectOptions();
        services.AddCache();
        // 文件存储要在 SqlSugar 之前注册：种子数据里会解析应用服务，可能依赖 IFileStorage
        if (caps.FileStorage)
            services.AddFileStorage();
        services.AddSqlSugar();
        // Transient：SimpXCache 从根容器取回填作用域，不能注册成 Scoped
        services.AddTransient<ICacheReadThroughScope, SqlSugarCacheReadThroughScope>();
        services.AddCorsAccessor();
        services.AddHttpRemote();
        // 外部 HTTP 统一入口（薄封装 Furion HttpRemote）；内部宿主互调仍用 Declarative
        services.AddHttpCaller();
        services.AddTaskQueue();
        services.AddSensitiveDetection();
        // ElasticSearch 客户端不在这里注册：只有 Admin 写 ES 审计日志时才引用 SimpX.Net.ElasticSearch 并自行注册

        // Nginx 转发时取客户端真实 IP；负载均衡不在本机回环地址时要清空已知网络和代理
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.All;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });

        services.AddEventBus(options =>
        {
            options.UseUtcTimestamp = false;
            options.LogEnabled = false;
            options.AddExecutor<RetryEventHandlerExecutor>();
            options.UnobservedTaskExceptionHandler = (obj, args) =>
            {
                if (args.Exception?.Message != null)
                    Log.Error($"EeventBus 有未处理异常 ：{args.Exception?.Message} ", args.Exception);
            };
            options.AddMonitor<EventHandlerMonitor>();

            // 缓存用 Redis 时，事件源也走 Redis，多实例之间能互通
            var cacheOptions = App.GetConfig<CacheOptions>("Cache", true);
            if (cacheOptions.CacheType == CacheTypeEnum.Redis.ToString())
            {
                options.ReplaceStorer(serviceProvider =>
                {
                    var cacheProvider = serviceProvider.GetRequiredService<NewLife.Caching.ICacheProvider>();
                    return new RedisEventSourceStorer(cacheProvider, "simpxnet_eventsource_queue", 3000);
                });
            }
        });

        if (caps.ImageSharp)
            services.AddImageSharp();

        if (caps.ViewEngine)
            services.AddViewEngine();

        if (caps.SignalR)
        {
            services.AddSignalR(options =>
            {
                options.EnableDetailedErrors = true;
                options.KeepAliveInterval = TimeSpan.FromSeconds(15);
                options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
                options.MaximumReceiveMessageSize = 1024 * 1014 * 10;
            }).AddJsonProtocol(options => options.PayloadSerializerOptions = JSON.GetSerializerOptions<JsonSerializerOptions>());

            services.AddSingleton<HubInvocationBus>();
            services.AddSingleton<IHubInvocationBus>(sp => sp.GetRequiredService<HubInvocationBus>());
            services.AddHostedService(sp => sp.GetRequiredService<HubInvocationBus>());
        }

        services.AddLoggingSetup();

        if (caps.Captcha)
            services.AddCaptchaStateStorage();

        if (caps.ConsoleLogo)
            services.AddConsoleLogo();

        // IP 归属地：仅点选 IpLocation 的宿主初始化；整库进内存另看 IpLocationInMemory
        if (caps.IpLocation)
        {
            IpToolSettings.DefalutSearcherType = IpSearcherType.International;
            if (caps.IpLocationInMemory)
                IpToolSettings.LoadInternationalDbToMemory = true;
        }

        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
            options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
            [
                "text/html; charset=utf-8",
                "application/xhtml+xml",
                "application/atom+xml",
                "image/svg+xml"
            ]);
        });

        if (caps.VirtualFileServer)
            services.AddVirtualFileServer();
    }
}
