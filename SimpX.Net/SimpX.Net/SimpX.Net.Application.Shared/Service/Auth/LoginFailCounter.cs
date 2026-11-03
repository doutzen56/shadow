using SimpX.Net.Caching;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 登录失败计数（跟旧版失败计数一样：最多 5 次、窗口 3600 秒）。
/// 用业务缓存存次数；键里带类型与主体（会员号或 IP 字符串）。
/// </summary>
public sealed class LoginFailCounter : ITransient
{
    /// <summary>旧版 Utils.FAILD_COUNT_MAX。</summary>
    public const int MaxCount = 5;

    /// <summary>旧版 Utils.FAILD_COUNT_TIME（秒）。</summary>
    public const int WindowSeconds = 3600;

    /// <summary>会员登录失败类型名。</summary>
    public const string TypeLogin = "Login";

    private readonly ISimpXCache _cache;

    /// <summary>
    /// 注入业务缓存。
    /// </summary>
    /// <param name="cache">缓存</param>
    public LoginFailCounter(ISimpXCache cache)
    {
        _cache = cache;
    }

    /// <summary>
    /// 若已达上限，返回剩余锁定秒数；未达上限返回 0。
    /// </summary>
    /// <param name="subject">会员号或客户端 IP</param>
    /// <param name="type">失败类型</param>
    /// <returns>剩余秒数；0 表示可继续试</returns>
    public int CheckLockedSeconds(string subject, string type = TypeLogin)
    {
        var state = _cache.Get<FailState>(SiteHostCacheKeys.LoginFail, BuildId(type, subject));
        if (state == null || state.Count < MaxCount)
            return 0;

        var left = (int)Math.Ceiling((state.ExpireAt - DateTime.UtcNow).TotalSeconds);
        return left > 0 ? left : 0;
    }

    /// <summary>
    /// 当前窗口内已失败次数；无记录返回 0。
    /// </summary>
    /// <param name="subject">会员号或客户端 IP</param>
    /// <param name="type">失败类型</param>
    /// <returns>失败次数</returns>
    public int GetCount(string subject, string type = TypeLogin)
    {
        var state = _cache.Get<FailState>(SiteHostCacheKeys.LoginFail, BuildId(type, subject));
        return state?.Count ?? 0;
    }

    /// <summary>
    /// 记一次失败，刷新窗口；返回「还可以再试几次」（对齐 SaveFaildCount 返回值）。
    /// </summary>
    /// <param name="subject">会员号或客户端 IP</param>
    /// <param name="type">失败类型</param>
    /// <returns>剩余可试次数（可为 0）</returns>
    public int SaveFail(string subject, string type = TypeLogin)
    {
        var id = BuildId(type, subject);
        var state = _cache.Get<FailState>(SiteHostCacheKeys.LoginFail, id) ?? new FailState();
        state.Count += 1;
        state.ExpireAt = DateTime.UtcNow.AddSeconds(WindowSeconds);
        _cache.Add(SiteHostCacheKeys.LoginFail, id, state, TimeSpan.FromSeconds(WindowSeconds));
        var left = MaxCount - state.Count;
        return left < 0 ? 0 : left;
    }

    /// <summary>
    /// 登录成功后清掉失败计数。
    /// </summary>
    /// <param name="subject">会员号或客户端 IP</param>
    /// <param name="type">失败类型</param>
    public void Clear(string subject, string type = TypeLogin)
    {
        _cache.Remove(SiteHostCacheKeys.LoginFail, BuildId(type, subject));
    }

    /// <summary>
    /// 把剩余秒数说成可读时长（给锁定提示用）。
    /// </summary>
    /// <param name="seconds">剩余秒数</param>
    /// <returns>如「59分钟」</returns>
    public static string FormatRemain(int seconds)
    {
        if (seconds <= 0)
            return "片刻";
        if (seconds < 60)
            return $"{seconds}秒";
        if (seconds < 3600)
            return $"{(seconds + 59) / 60}分钟";
        return $"{(seconds + 3599) / 3600}小时";
    }

    private static string BuildId(string type, string subject) =>
        $"{type}_{subject ?? string.Empty}";

    /// <summary>
    /// 缓存里存的失败状态。
    /// </summary>
    public sealed class FailState
    {
        /// <summary>已失败次数。</summary>
        public int Count { get; set; }

        /// <summary>窗口结束时间（UTC）。</summary>
        public DateTime ExpireAt { get; set; }
    }
}
