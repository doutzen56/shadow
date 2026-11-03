using System.Collections.Concurrent;
using NewLife.Caching;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 三分钟内不得提交相同充值金额。对齐旧 MoneyCaching Has/SaveWithdrawRecharge（资金库、无前缀；无 Redis 时用进程内窗口）。
/// </summary>
public sealed class RechargeDuplicateAmountChecker : ITransient
{
    /// <summary>旧缓存键前缀。</summary>
    public const string KeyPrefix = "USER_MONEY_WITHDRAW_RECHARGE";

    private static readonly ConcurrentDictionary<string, (string Csv, DateTime ExpireAt)> Memory = new(StringComparer.Ordinal);

    private readonly LegacyLedgerRedisHolder _redis;

    /// <summary>
    /// 注入旧资金库 Redis。
    /// </summary>
    /// <param name="redis">持有者</param>
    public RechargeDuplicateAmountChecker(LegacyLedgerRedisHolder redis) => _redis = redis;

    /// <summary>
    /// 是否已在三分钟窗口内提交过相同金额。
    /// </summary>
    public bool HasRecentSameAmount(int userId, string money)
    {
        if (userId <= 0 || string.IsNullOrEmpty(money))
            return false;

        var redis = TryMoneyRedis();
        if (redis != null)
        {
            try
            {
                var key = KeyPrefix + userId;
                if (!redis.ContainsKey(key))
                    return false;
                var csv = redis.Get<string>(key) ?? string.Empty;
                return ContainsAmount(csv, money);
            }
            catch
            {
                // 读失败放行，避免挡建单
            }
        }

        return Memory.TryGetValue(KeyPrefix + userId, out var row)
               && row.ExpireAt > DateTime.UtcNow
               && ContainsAmount(row.Csv, money);
    }

    /// <summary>
    /// 记下本次金额，窗口 3 分钟。
    /// </summary>
    public void Remember(int userId, string money)
    {
        if (userId <= 0 || string.IsNullOrEmpty(money))
            return;

        var redis = TryMoneyRedis();
        if (redis != null)
        {
            try
            {
                var key = KeyPrefix + userId;
                var csv = redis.ContainsKey(key) ? (redis.Get<string>(key) ?? string.Empty) : string.Empty;
                csv = string.IsNullOrEmpty(csv) ? money : csv + "," + money;
                redis.Set(key, csv, TimeSpan.FromMinutes(3));
                return;
            }
            catch
            {
                // 改写内存窗口
            }
        }

        var memKey = KeyPrefix + userId;
        Memory.AddOrUpdate(
            memKey,
            _ => (money, DateTime.UtcNow.AddMinutes(3)),
            (_, old) =>
            {
                var csv = old.ExpireAt > DateTime.UtcNow && !string.IsNullOrEmpty(old.Csv)
                    ? old.Csv + "," + money
                    : money;
                return (csv, DateTime.UtcNow.AddMinutes(3));
            });
    }

    private FullRedis? TryMoneyRedis()
    {
        try
        {
            return _redis?.Money;
        }
        catch
        {
            return null;
        }
    }

    private static bool ContainsAmount(string csv, string money) =>
        csv.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Any(x => string.Equals(x, money, StringComparison.Ordinal));
}
