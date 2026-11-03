using Furion.ConfigurableOptions;
using Microsoft.Extensions.Configuration;
using NewLife.Caching;

namespace SimpX.Net.Caching;

/// <summary>
/// 缓存配置，从配置里的 Cache 这一段读取。
/// </summary>
public sealed class CacheOptions : IConfigurableOptions<CacheOptions>
{
    /// <summary>
    /// 缓存键前缀，所有键都会自动加上，多个系统共用一个 Redis 时用来区分。
    /// </summary>
    public string Prefix { get; set; }

    /// <summary>
    /// 缓存类型：Memory 或 Redis，见 <see cref="CacheTypeEnum"/>。
    /// </summary>
    public string CacheType { get; set; }

    /// <summary>
    /// Redis 连接配置。
    /// </summary>
    public RedisOption Redis { get; set; }

    /// <summary>
    /// 读完配置后整理前缀：空值统一成空字符串，去掉首尾空格。
    /// </summary>
    /// <param name="options">缓存配置</param>
    /// <param name="configuration">配置</param>
    public void PostConfigure(CacheOptions options, IConfiguration configuration)
    {
        options.Prefix = string.IsNullOrWhiteSpace(options.Prefix) ? "" : options.Prefix.Trim();
    }
}

/// <summary>
/// Redis 连接配置。
/// </summary>
public sealed class RedisOption : RedisOptions
{
    /// <summary>
    /// 最大消息大小。
    /// </summary>
    public int MaxMessageSize { get; set; }
}
