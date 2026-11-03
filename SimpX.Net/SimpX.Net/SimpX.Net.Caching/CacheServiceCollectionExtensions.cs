using Furion;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NewLife.Caching;
using NewLife.Caching.Services;

namespace SimpX.Net.Caching;

/// <summary>
/// 缓存注册。
/// </summary>
public static class CacheServiceCollectionExtensions
{
    /// <summary>
    /// 注册缓存：配置了 Redis 就用 Redis，否则用内存缓存。业务读写走统一缓存和状态存储。
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddCache(this IServiceCollection services)
    {
        services.AddConfigurableOptions<CacheOptions>();

        var cacheOptions = App.GetConfig<CacheOptions>("Cache", true);
        if (cacheOptions?.CacheType == CacheTypeEnum.Redis.ToString())
        {
            var redis = new FullRedis(new RedisOptions
            {
                Configuration = cacheOptions.Redis.Configuration,
                Prefix = cacheOptions.Redis.Prefix
            })
            {
                AutoDetect = App.GetConfig<bool>("Cache:Redis:AutoDetect", true)
            };
            if (cacheOptions.Redis.MaxMessageSize > 0)
                redis.MaxMessageSize = cacheOptions.Redis.MaxMessageSize;

            services.AddSingleton<ICacheProvider>(u => new RedisCacheProvider(u) { Cache = redis });
        }

        services.TryAddSingleton<ICacheProvider, CacheProvider>();

        RegisterSimpXCacheLayer(services);
        return services;
    }

    /// <summary>
    /// 注册缓存名表、读写、状态存储、失效清单与执行器。
    /// </summary>
    /// <param name="services">服务集合</param>
    private static void RegisterSimpXCacheLayer(IServiceCollection services)
    {
        services.TryAddSingleton<CacheRegistry>(_ =>
        {
            var registry = new CacheRegistry();
            CacheKeyScanner.RegisterAll(registry);
            return registry;
        });
        services.TryAddSingleton<CacheKeyBuilder>();
        services.TryAddSingleton<ISimpXCache, SimpXCache>();
        services.TryAddSingleton<ICacheStateStore, CacheStateStore>();
        services.TryAddSingleton<ICacheInvalidationExecutor, CacheInvalidationExecutor>();
        services.TryAddScoped<CacheInvalidationBatch>();
    }
}
