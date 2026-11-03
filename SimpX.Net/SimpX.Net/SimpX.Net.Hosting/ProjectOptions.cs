using Furion;
using Microsoft.Extensions.DependencyInjection;
using SimpX.Net.Core;
using SimpX.Net.Data;
using SimpX.Net.Feature.Auth;
using SimpX.Net.Feature.Email;
using SimpX.Net.Feature.EventBus;

namespace SimpX.Net.Hosting;

/// <summary>
/// 全宿主共用的配置选项注册（不含随能力点选的上传等）。
/// </summary>
public static class ProjectOptions
{
    /// <summary>
    /// 注册各宿主都可能用到的配置段。
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddProjectOptions(this IServiceCollection services)
    {
        // 认证 / 数据 / 内核横切
        services.AddConfigurableOptions<InternalCallOptions>();
        services.AddConfigurableOptions<DbConnectionOptions>();
        services.AddConfigurableOptions<SnowIdOptions>();
        services.AddConfigurableOptions<ClusterOptions>();
        services.AddConfigurableOptions<EnumOptions>();
        services.AddConfigurableOptions<CryptogramOptions>();
        services.AddConfigurableOptions<LocalizationSettingsOptions>();

        // Feature 归属、但多宿主都会绑配置（告警邮件、事件总线）
        services.AddConfigurableOptions<EmailOptions>();
        services.AddConfigurableOptions<EventBusOptions>();

        // UploadOptions：随 FileStorage 能力在 AddFileStorage 里注册

        return services;
    }
}
