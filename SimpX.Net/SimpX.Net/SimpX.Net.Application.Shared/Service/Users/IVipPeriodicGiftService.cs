namespace SimpX.Net.Application.Shared;

/// <summary>
/// VIP 周期礼金。对齐旧 ExeEveryWeek / ExeEveryMonth / ExeBirthday。
/// </summary>
public interface IVipPeriodicGiftService
{
    /// <summary>
    /// 发每周礼金。窗口：配置星期 + 整点起一小时内。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <param name="now">当前时间；测试可注入，默认本机现在</param>
    /// <returns>成功发放人数</returns>
    Task<int> GrantEveryWeekAsync(CancellationToken cancellationToken = default, DateTime? now = null);

    /// <summary>
    /// 发每月礼金。窗口：配置日 + 整点起一小时内。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <param name="now">当前时间；测试可注入</param>
    /// <returns>成功发放人数</returns>
    Task<int> GrantEveryMonthAsync(CancellationToken cancellationToken = default, DateTime? now = null);

    /// <summary>
    /// 发生日礼金（当天生日会员）。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <param name="now">当前时间；测试可注入</param>
    /// <returns>成功发放人数</returns>
    Task<int> GrantBirthdayAsync(CancellationToken cancellationToken = default, DateTime? now = null);
}
