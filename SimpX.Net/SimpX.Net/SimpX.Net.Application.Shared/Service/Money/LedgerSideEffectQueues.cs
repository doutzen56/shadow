using System.Collections.Concurrent;
using NewLife.Caching;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 账变后的旁路队列：报表账变列表、用户资金/VIP 有序集合。对齐旧 ReportCaching / MoneyCaching 入队。
/// </summary>
public interface ILedgerSideEffectQueues
{
    /// <summary>
    /// 把账变流水号推进报表账变列表（旧 MONEY_LOG，右进）。
    /// </summary>
    /// <param name="logId">账变流水号</param>
    void EnqueueMoneyLog(int logId);

    /// <summary>
    /// 把用户资金与 VIP 推进延迟有序集合（旧 QUEUE_USERMONEY / QUEUE_USERVIP）。
    /// </summary>
    /// <param name="userId">用户编号</param>
    /// <param name="currency">币种编号</param>
    /// <param name="skipVip">为 true 时不入 VIP 队列（对齐旧 Lock.Upgrade）</param>
    /// <param name="delayMinutes">延迟分钟，默认 0（立刻可被消费）</param>
    void EnqueueUserMoneyAndVip(int userId, int currency, bool skipVip = false, int delayMinutes = 0);

    /// <summary>
    /// 充值到账后推进报表充值列表（旧 RECHARGE_LOG，供活动/报表消费）。
    /// </summary>
    /// <param name="orderId">充值订单号</param>
    void EnqueueRechargeLog(string orderId);

    /// <summary>
    /// 代理上下分统计队列（旧 PROXY_MONEY_STS）。
    /// </summary>
    /// <param name="payloadJson">统计 JSON</param>
    void EnqueueProxyMoneySts(string payloadJson);

    /// <summary>
    /// 从充值到账列表左侧弹出一条（旧 LPOP RECHARGE_LOG）。
    /// </summary>
    /// <param name="orderId">订单号</param>
    /// <returns>弹出成功为 true</returns>
    bool TryDequeueRechargeLog(out string orderId);

    /// <summary>
    /// 从账变报表列表左侧弹出一条（旧 LPOP MONEY_LOG）。
    /// </summary>
    /// <param name="logId">账变流水号</param>
    /// <returns>弹出成功为 true</returns>
    bool TryDequeueMoneyLog(out int logId);

    /// <summary>
    /// 从代理上下分统计列表左侧弹出一条（旧 LPOP PROXY_MONEY_STS）。
    /// </summary>
    /// <param name="payloadJson">统计 JSON</param>
    /// <returns>弹出成功为 true</returns>
    bool TryDequeueProxyMoneySts(out string payloadJson);

    /// <summary>
    /// 取出已到期的 VIP 队列成员并移除（旧 QUEUE_USERVIP 按 score≤现在）。
    /// </summary>
    /// <param name="maxCount">最多条数</param>
    /// <returns>用户编号列表</returns>
    IReadOnlyList<int> TakeDueUserVip(int maxCount);

    /// <summary>
    /// 取出已到期的用户资金队列成员并移除（旧 QUEUE_USERMONEY，成员形如 userId_currency）。
    /// </summary>
    /// <param name="maxCount">最多条数</param>
    /// <returns>(UserId, Currency) 列表</returns>
    IReadOnlyList<(int UserId, int Currency)> TakeDueUserMoney(int maxCount);

    /// <summary>
    /// 把用户推进活动集合（旧 USER_ACTIVE，Redis SET）。
    /// </summary>
    /// <param name="userId">用户编号</param>
    void EnqueueUserActive(int userId);

    /// <summary>
    /// 取出活动集合全部成员并清空键（旧 GetUserActive：SetScan + KeyDelete）。
    /// </summary>
    /// <returns>用户编号列表</returns>
    IReadOnlyList<int> TakeDueUserActive();

    /// <summary>
    /// 把用户推进经验同步列表（旧 QUEUE_USEREXPER）。
    /// </summary>
    /// <param name="userId">用户编号</param>
    void EnqueueUserExper(int userId);

    /// <summary>
    /// 从经验同步列表左侧弹出最多 maxCount 条（旧 RPOP QUEUE_USEREXPER 循环）。
    /// </summary>
    /// <param name="maxCount">最多条数</param>
    /// <returns>用户编号列表（未去重）</returns>
    IReadOnlyList<int> TakeDueUserExper(int maxCount);
}

/// <summary>
/// 内存旁路队列，给单测和未接 Redis 时用。键名与旧站一致，方便断言。
/// </summary>
public sealed class MemoryLedgerSideEffectQueues : ILedgerSideEffectQueues
{
    /// <summary>账变流水号列表（模拟 RPUSH）。</summary>
    public ConcurrentQueue<int> MoneyLogs { get; } = new();

    /// <summary>用户资金有序集合：成员 → 可执行 Unix 秒。</summary>
    public ConcurrentDictionary<string, long> UserMoneyScores { get; } = new(StringComparer.Ordinal);

    /// <summary>用户 VIP 有序集合：成员 → 可执行 Unix 秒。</summary>
    public ConcurrentDictionary<string, long> UserVipScores { get; } = new(StringComparer.Ordinal);

    /// <summary>充值订单号列表。</summary>
    public ConcurrentQueue<string> RechargeLogs { get; } = new();

    /// <summary>代理上下分统计 JSON 列表。</summary>
    public ConcurrentQueue<string> ProxyMoneySts { get; } = new();

    /// <summary>活动用户集合（模拟 USER_ACTIVE）。</summary>
    public ConcurrentDictionary<int, byte> UserActiveIds { get; } = new();

    /// <summary>经验同步列表（模拟 QUEUE_USEREXPER）。</summary>
    public ConcurrentQueue<int> UserExperIds { get; } = new();

    /// <inheritdoc />
    public void EnqueueMoneyLog(int logId)
    {
        if (logId <= 0) return;
        MoneyLogs.Enqueue(logId);
    }

    /// <inheritdoc />
    public void EnqueueUserMoneyAndVip(int userId, int currency, bool skipVip = false, int delayMinutes = 0)
    {
        if (userId == 0) return;
        var executeAt = DateTimeOffset.UtcNow.AddMinutes(delayMinutes).ToUnixTimeSeconds();
        UserMoneyScores[$"{userId}_{currency}"] = executeAt;
        if (skipVip) return;
        UserVipScores[userId.ToString()] = executeAt;
    }

    /// <inheritdoc />
    public void EnqueueRechargeLog(string orderId)
    {
        if (string.IsNullOrWhiteSpace(orderId)) return;
        RechargeLogs.Enqueue(orderId.Trim());
    }

    /// <inheritdoc />
    public void EnqueueProxyMoneySts(string payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson)) return;
        ProxyMoneySts.Enqueue(payloadJson);
    }

    /// <inheritdoc />
    public bool TryDequeueRechargeLog(out string orderId) => RechargeLogs.TryDequeue(out orderId);

    /// <inheritdoc />
    public bool TryDequeueMoneyLog(out int logId) => MoneyLogs.TryDequeue(out logId);

    /// <inheritdoc />
    public bool TryDequeueProxyMoneySts(out string payloadJson) => ProxyMoneySts.TryDequeue(out payloadJson);

    /// <inheritdoc />
    public IReadOnlyList<int> TakeDueUserVip(int maxCount)
    {
        if (maxCount <= 0)
            return Array.Empty<int>();
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var due = UserVipScores
            .Where(kv => kv.Value <= now)
            .Select(kv => kv.Key)
            .Take(maxCount)
            .ToList();
        var result = new List<int>(due.Count);
        foreach (var key in due)
        {
            if (!UserVipScores.TryRemove(key, out _))
                continue;
            if (int.TryParse(key, out var userId) && userId != 0)
                result.Add(userId);
        }

        return result;
    }

    /// <inheritdoc />
    public IReadOnlyList<(int UserId, int Currency)> TakeDueUserMoney(int maxCount)
    {
        if (maxCount <= 0)
            return Array.Empty<(int, int)>();
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var due = UserMoneyScores
            .Where(kv => kv.Value <= now)
            .Select(kv => kv.Key)
            .Take(maxCount)
            .ToList();
        var result = new List<(int, int)>(due.Count);
        foreach (var key in due)
        {
            if (!UserMoneyScores.TryRemove(key, out _))
                continue;
            var parts = key.Split('_');
            if (parts.Length >= 2
                && int.TryParse(parts[0], out var userId) && userId != 0
                && int.TryParse(parts[1], out var currency))
                result.Add((userId, currency));
        }

        return result;
    }

    /// <inheritdoc />
    public void EnqueueUserActive(int userId)
    {
        if (userId == 0) return;
        UserActiveIds[userId] = 0;
    }

    /// <inheritdoc />
    public IReadOnlyList<int> TakeDueUserActive()
    {
        var ids = UserActiveIds.Keys.ToList();
        foreach (var id in ids)
            UserActiveIds.TryRemove(id, out _);
        return ids;
    }

    /// <inheritdoc />
    public void EnqueueUserExper(int userId)
    {
        if (userId == 0) return;
        UserExperIds.Enqueue(userId);
    }

    /// <inheritdoc />
    public IReadOnlyList<int> TakeDueUserExper(int maxCount)
    {
        if (maxCount <= 0)
            return Array.Empty<int>();
        var result = new List<int>(Math.Min(maxCount, 64));
        while (result.Count < maxCount && UserExperIds.TryDequeue(out var userId))
        {
            if (userId != 0)
                result.Add(userId);
        }

        return result;
    }
}

/// <summary>
/// Redis 旁路队列。键名与库号对齐旧站：MONEY_LOG@10、QUEUE_USERMONEY/QUEUE_USERVIP/USER_ACTIVE/QUEUE_USEREXPER@1，无业务缓存前缀。
/// </summary>
public sealed class LedgerSideEffectQueues : ITransient, ILedgerSideEffectQueues
{
    /// <summary>旧报表账变列表键。</summary>
    public const string MoneyLogKey = "MONEY_LOG";

    /// <summary>旧用户资金延迟队列键。</summary>
    public const string UserMoneyKey = "QUEUE_USERMONEY";

    /// <summary>旧用户 VIP 延迟队列键。</summary>
    public const string UserVipKey = "QUEUE_USERVIP";

    /// <summary>旧充值到账列表键（报表库）。</summary>
    public const string RechargeLogKey = "RECHARGE_LOG";

    /// <summary>旧代理上下分统计列表键（报表库）。</summary>
    public const string ProxyMoneyStsKey = "PROXY_MONEY_STS";

    /// <summary>旧活动用户集合键（资金库）。</summary>
    public const string UserActiveKey = "USER_ACTIVE";

    /// <summary>旧经验同步列表键（资金库）。</summary>
    public const string UserExperKey = "QUEUE_USEREXPER";

    private readonly LegacyLedgerRedisHolder _redis;

    /// <summary>
    /// 注入旧队列专用 Redis。
    /// </summary>
    /// <param name="redis">报表库 / 资金库持有者</param>
    public LedgerSideEffectQueues(LegacyLedgerRedisHolder redis)
    {
        _redis = redis;
    }

    /// <inheritdoc />
    public void EnqueueMoneyLog(int logId)
    {
        if (logId <= 0) return;
        try
        {
            var redis = _redis.Report;
            if (redis == null)
            {
                Log.Warning("账变报表队列未入队：未配置旧队列 Redis（Cache 非 Redis 或连接串空）");
                return;
            }

            redis.GetList<int>(MoneyLogKey).Add(logId);
        }
        catch (Exception ex)
        {
            // 入队失败不能把已入账打成失败。
            Log.Warning($"账变报表队列入队失败（加款已入账）：{ex.Message}");
        }
    }

    /// <inheritdoc />
    public void EnqueueUserMoneyAndVip(int userId, int currency, bool skipVip = false, int delayMinutes = 0)
    {
        if (userId == 0) return;
        try
        {
            var redis = _redis.Money;
            if (redis == null)
            {
                Log.Warning("用户资金/VIP 队列未入队：未配置旧队列 Redis（Cache 非 Redis 或连接串空）");
                return;
            }

            PushUserQueues(redis, userId, currency, skipVip, delayMinutes);
        }
        catch (Exception ex) when (ex.Message.Contains("WRONGTYPE", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var redis = _redis.Money;
                if (redis == null) return;
                redis.Remove(UserMoneyKey);
                redis.Remove(UserVipKey);
                PushUserQueues(redis, userId, currency, skipVip, delayMinutes);
            }
            catch (Exception retryEx)
            {
                Log.Warning($"用户资金/VIP 队列入队重试失败（加款已入账）：{retryEx.Message}");
            }
        }
        catch (Exception ex)
        {
            Log.Warning($"用户资金/VIP 队列入队失败（加款已入账）：{ex.Message}");
        }
    }

    /// <summary>
    /// 写入资金与 VIP 有序集合。
    /// </summary>
    private static void PushUserQueues(
        FullRedis redis,
        int userId,
        int currency,
        bool skipVip,
        int delayMinutes)
    {
        var executeAt = (double)DateTimeOffset.UtcNow.AddMinutes(delayMinutes).ToUnixTimeSeconds();
        redis.GetSortedSet<string>(UserMoneyKey).Add($"{userId}_{currency}", executeAt);
        if (skipVip) return;
        redis.GetSortedSet<string>(UserVipKey).Add(userId.ToString(), executeAt);
    }

    /// <inheritdoc />
    public void EnqueueRechargeLog(string orderId)
    {
        if (string.IsNullOrWhiteSpace(orderId)) return;
        try
        {
            var redis = _redis.Report;
            if (redis == null)
            {
                Log.Warning("充值报表队列未入队：未配置旧队列 Redis");
                return;
            }

            redis.GetList<string>(RechargeLogKey).Add(orderId.Trim());
        }
        catch (Exception ex)
        {
            Log.Warning($"充值报表队列入队失败（入账已完成）：{ex.Message}");
        }
    }

    /// <inheritdoc />
    public void EnqueueProxyMoneySts(string payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson)) return;
        try
        {
            var redis = _redis.Report;
            if (redis == null)
            {
                Log.Warning("代理上下分统计队列未入队：未配置旧队列 Redis");
                return;
            }

            redis.GetList<string>(ProxyMoneyStsKey).Add(payloadJson);
        }
        catch (Exception ex)
        {
            Log.Warning($"代理上下分统计队列入队失败（加款已入账）：{ex.Message}");
        }
    }

    /// <inheritdoc />
    public bool TryDequeueRechargeLog(out string orderId)
    {
        orderId = null;
        try
        {
            var redis = _redis.Report;
            if (redis == null) return false;
            orderId = redis.LPOP<string>(RechargeLogKey);
            return !string.IsNullOrWhiteSpace(orderId);
        }
        catch (Exception ex)
        {
            Log.Warning($"充值报表队列弹出失败：{ex.Message}");
            orderId = null;
            return false;
        }
    }

    /// <inheritdoc />
    public bool TryDequeueMoneyLog(out int logId)
    {
        logId = 0;
        try
        {
            var redis = _redis.Report;
            if (redis == null) return false;
            // 入队用 GetList int，弹出按 int；空队列常返回 0。
            logId = redis.LPOP<int>(MoneyLogKey);
            return logId > 0;
        }
        catch (Exception ex)
        {
            Log.Warning($"账变报表队列弹出失败：{ex.Message}");
            logId = 0;
            return false;
        }
    }

    /// <inheritdoc />
    public bool TryDequeueProxyMoneySts(out string payloadJson)
    {
        payloadJson = null;
        try
        {
            var redis = _redis.Report;
            if (redis == null) return false;
            payloadJson = redis.LPOP<string>(ProxyMoneyStsKey);
            return !string.IsNullOrWhiteSpace(payloadJson);
        }
        catch (Exception ex)
        {
            Log.Warning($"代理上下分统计队列弹出失败：{ex.Message}");
            payloadJson = null;
            return false;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<int> TakeDueUserVip(int maxCount)
    {
        if (maxCount <= 0)
            return Array.Empty<int>();
        try
        {
            var redis = _redis.Money;
            if (redis == null)
                return Array.Empty<int>();

            var now = (double)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var set = redis.GetSortedSet<string>(UserVipKey);
            var members = set.RangeByScore(0, now, 0, maxCount)?.ToList() ?? [];
            if (members.Count == 0)
                return Array.Empty<int>();

            var arr = members.ToArray();
            set.Remove(arr);
            var result = new List<int>(arr.Length);
            foreach (var m in arr)
            {
                if (int.TryParse(m, out var userId) && userId != 0)
                    result.Add(userId);
            }

            return result;
        }
        catch (Exception ex)
        {
            Log.Warning($"VIP 队列取出失败：{ex.Message}");
            return Array.Empty<int>();
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<(int UserId, int Currency)> TakeDueUserMoney(int maxCount)
    {
        if (maxCount <= 0)
            return Array.Empty<(int, int)>();
        try
        {
            var redis = _redis.Money;
            if (redis == null)
                return Array.Empty<(int, int)>();

            var now = (double)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var set = redis.GetSortedSet<string>(UserMoneyKey);
            var members = set.RangeByScore(0, now, 0, maxCount)?.ToList() ?? [];
            if (members.Count == 0)
                return Array.Empty<(int, int)>();

            var arr = members.ToArray();
            set.Remove(arr);
            var result = new List<(int, int)>(arr.Length);
            foreach (var m in arr)
            {
                var parts = (m ?? string.Empty).Split('_');
                if (parts.Length >= 2
                    && int.TryParse(parts[0], out var userId) && userId != 0
                    && int.TryParse(parts[1], out var currency))
                    result.Add((userId, currency));
            }

            return result;
        }
        catch (Exception ex)
        {
            Log.Warning($"QUEUE_USERMONEY 取出失败：{ex.Message}");
            return Array.Empty<(int, int)>();
        }
    }

    /// <inheritdoc />
    public void EnqueueUserActive(int userId)
    {
        if (userId == 0) return;
        try
        {
            var redis = _redis.Money;
            if (redis == null)
            {
                Log.Warning("USER_ACTIVE 未入队：未配置旧队列 Redis");
                return;
            }

            redis.GetSet<int>(UserActiveKey).Add(userId);
        }
        catch (Exception ex)
        {
            Log.Warning($"USER_ACTIVE 入队失败：{ex.Message}");
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<int> TakeDueUserActive()
    {
        try
        {
            var redis = _redis.Money;
            if (redis == null)
                return Array.Empty<int>();

            var set = redis.GetSet<int>(UserActiveKey);
            var members = set?.ToArray() ?? [];
            if (members.Length == 0)
                return Array.Empty<int>();

            redis.Remove(UserActiveKey);
            return members.Where(id => id != 0).Distinct().ToList();
        }
        catch (Exception ex)
        {
            Log.Warning($"USER_ACTIVE 取出失败：{ex.Message}");
            return Array.Empty<int>();
        }
    }

    /// <inheritdoc />
    public void EnqueueUserExper(int userId)
    {
        if (userId == 0) return;
        try
        {
            var redis = _redis.Money;
            if (redis == null)
            {
                Log.Warning("QUEUE_USEREXPER 未入队：未配置旧队列 Redis");
                return;
            }

            redis.GetList<int>(UserExperKey).Add(userId);
        }
        catch (Exception ex)
        {
            Log.Warning($"QUEUE_USEREXPER 入队失败：{ex.Message}");
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<int> TakeDueUserExper(int maxCount)
    {
        if (maxCount <= 0)
            return Array.Empty<int>();
        try
        {
            var redis = _redis.Money;
            if (redis == null)
                return Array.Empty<int>();

            var result = new List<int>(Math.Min(maxCount, 64));
            while (result.Count < maxCount)
            {
                var userId = redis.LPOP<int>(UserExperKey);
                if (userId == 0)
                    break;
                result.Add(userId);
            }

            return result;
        }
        catch (Exception ex)
        {
            Log.Warning($"QUEUE_USEREXPER 取出失败：{ex.Message}");
            return Array.Empty<int>();
        }
    }
}
