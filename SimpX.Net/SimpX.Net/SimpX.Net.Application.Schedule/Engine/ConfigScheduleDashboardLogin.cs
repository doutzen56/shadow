using SimpX.Net.Contracts.Jobs;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;

namespace SimpX.Net.Application.Schedule;

/// <summary>
/// 按 ScheduleHost 配置里的看板账号校验 /schedule 登录，不再依赖后台账号体系。
/// </summary>
public sealed class ConfigScheduleDashboardLogin : IScheduleDashboardLogin
{
    private readonly IOptionsMonitor<ScheduleHostOptions> _options;

    /// <summary>
    /// 注入调度宿主配置。
    /// </summary>
    /// <param name="options">ScheduleHost 配置</param>
    public ConfigScheduleDashboardLogin(IOptionsMonitor<ScheduleHostOptions> options)
    {
        _options = options;
    }

    /// <summary>
    /// 账号密码都配置了且完全一致才通过；任一项没配直接判失败。
    /// </summary>
    /// <param name="username">账号</param>
    /// <param name="password">密码</param>
    /// <returns>通过返回 true</returns>
    public Task<bool> ValidateAsync(string username, string password)
    {
        var opt = _options.CurrentValue;
        if (string.IsNullOrEmpty(opt.DashboardUsername) || string.IsNullOrEmpty(opt.DashboardPassword))
            return Task.FromResult(false);

        var ok = FixedEquals(username, opt.DashboardUsername) & FixedEquals(password, opt.DashboardPassword);
        return Task.FromResult(ok);
    }

    /// <summary>
    /// 定长比较，避免按耗时猜出账号密码。
    /// </summary>
    /// <param name="input">输入值</param>
    /// <param name="expected">期望值</param>
    /// <returns>相等返回 true</returns>
    private static bool FixedEquals(string input, string expected)
    {
        var a = Encoding.UTF8.GetBytes(input ?? string.Empty);
        var b = Encoding.UTF8.GetBytes(expected);
        return CryptographicOperations.FixedTimeEquals(a, b);
    }
}
