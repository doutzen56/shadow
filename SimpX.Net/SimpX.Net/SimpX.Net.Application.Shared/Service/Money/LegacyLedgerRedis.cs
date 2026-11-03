using System.Text.RegularExpressions;
using NewLife.Caching;
using SimpX.Net.Caching;
using Furion;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 旧站账变旁路队列用的 Redis：无前缀，库号对齐旧 ReportCaching(10) / MoneyCaching(1)。
/// 业务数据缓存仍走宿主带前缀的连接，两边不要混。
/// </summary>
public static class LegacyLedgerRedisFactory
{
    /// <summary>旧报表缓存库号。</summary>
    public const int ReportDbIndex = 10;

    /// <summary>旧资金/VIP 缓存库号。</summary>
    public const int MoneyDbIndex = 1;

    /// <summary>
    /// 把连接串里的 db 改成指定库号；没有 db 段就补上。
    /// </summary>
    /// <param name="configuration">原连接串</param>
    /// <param name="db">目标库号</param>
    /// <returns>改写后的连接串</returns>
    public static string ConfigurationWithDb(string configuration, int db)
    {
        if (string.IsNullOrWhiteSpace(configuration))
            return configuration;

        if (Regex.IsMatch(configuration, @"db=\d+", RegexOptions.IgnoreCase))
            return Regex.Replace(configuration, @"db=\d+", $"db={db}", RegexOptions.IgnoreCase);

        return configuration.TrimEnd(';') + $";db={db};";
    }

    /// <summary>
    /// 按缓存配置建一条无前缀、指定库号的 FullRedis；未配 Redis 时返回 null。
    /// </summary>
    /// <param name="cacheOptions">Cache 配置段</param>
    /// <param name="db">目标库号</param>
    /// <returns>客户端，或 null</returns>
    public static FullRedis? TryCreate(CacheOptions cacheOptions, int db)
    {
        if (cacheOptions == null)
            return null;
        if (!string.Equals(cacheOptions.CacheType, CacheTypeEnum.Redis.ToString(), StringComparison.OrdinalIgnoreCase))
            return null;
        if (cacheOptions.Redis == null || string.IsNullOrWhiteSpace(cacheOptions.Redis.Configuration))
            return null;

        var redis = new FullRedis(new RedisOptions
        {
            Configuration = ConfigurationWithDb(cacheOptions.Redis.Configuration, db),
            // 旧队列键是 MONEY_LOG / QUEUE_*，不能套 simpxnet_ 这类业务缓存前缀。
            Prefix = ""
        })
        {
            AutoDetect = false
        };
        if (cacheOptions.Redis.MaxMessageSize > 0)
            redis.MaxMessageSize = cacheOptions.Redis.MaxMessageSize;
        return redis;
    }
}

/// <summary>
/// 懒加载旧队列专用 Redis（报表库 / 资金库各一条，无前缀）。
/// </summary>
public sealed class LegacyLedgerRedisHolder : ISingleton
{
    private readonly Lazy<FullRedis?> _report;
    private readonly Lazy<FullRedis?> _money;

    /// <summary>
    /// 从 Cache 配置建客户端；未配 Redis 时属性为 null。
    /// </summary>
    public LegacyLedgerRedisHolder()
    {
        _report = new Lazy<FullRedis?>(() =>
            LegacyLedgerRedisFactory.TryCreate(
                App.GetConfig<CacheOptions>("Cache", true),
                LegacyLedgerRedisFactory.ReportDbIndex));
        _money = new Lazy<FullRedis?>(() =>
            LegacyLedgerRedisFactory.TryCreate(
                App.GetConfig<CacheOptions>("Cache", true),
                LegacyLedgerRedisFactory.MoneyDbIndex));
    }

    /// <summary>报表账变列表所在库（旧 DB 10）。</summary>
    public FullRedis? Report => _report.Value;

    /// <summary>用户资金/VIP 队列所在库（旧 DB 1）。</summary>
    public FullRedis? Money => _money.Value;
}
