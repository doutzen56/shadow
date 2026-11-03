namespace SimpX.Net.Contracts.Jobs;

/// <summary>
/// 定时任务看板（/schedule）的登录校验。由负责调度的宿主实现；没有实现时看板一律拒绝登录。
/// </summary>
public interface IScheduleDashboardLogin
{
    /// <summary>
    /// 校验看板登录账号。
    /// </summary>
    /// <param name="username">账号</param>
    /// <param name="password">密码</param>
    /// <returns>通过返回 true</returns>
    Task<bool> ValidateAsync(string username, string password);
}
