using Microsoft.Extensions.DependencyInjection.Extensions;

namespace SimpX.Net.Feature.Storage;

/// <summary>
/// 文件存储注册。
/// </summary>
public static class FileStorageServiceCollectionExtensions
{
    /// <summary>
    /// 按 FileStorage:Provider 注册文件存储。Local 或缺省写到站点目录；Oss 注册尚未实现的对象存储，调用时会直接失败。
    /// 应用若已经注册过 <see cref="IFileStorage"/>，这里不再覆盖。
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    /// <exception cref="InvalidOperationException">Provider 不是 Local 或 Oss</exception>
    public static IServiceCollection AddFileStorage(this IServiceCollection services)
    {
        services.AddConfigurableOptions<FileStorageOptions>();
        // 上传校验（大小/格式/MD5）跟文件能力一起开，避免未开存储的宿主空绑 Upload 配置
        services.AddConfigurableOptions<UploadOptions>();

        var provider = App.GetConfig<string>("FileStorage:Provider", true);
        if (string.IsNullOrWhiteSpace(provider) || string.Equals(provider, "Local", StringComparison.OrdinalIgnoreCase))
            services.TryAddSingleton<IFileStorage, LocalFileStorage>();
        else if (string.Equals(provider, "Oss", StringComparison.OrdinalIgnoreCase))
            services.TryAddSingleton<IFileStorage, OssFileStorage>();
        else
            throw new InvalidOperationException($"FileStorage:Provider 只能是 Local 或 Oss，当前是 {provider}。");

        return services;
    }
}
