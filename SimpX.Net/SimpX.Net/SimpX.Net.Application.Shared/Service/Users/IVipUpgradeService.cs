namespace SimpX.Net.Application.Shared;

/// <summary>
/// VIP 升级消费。对齐旧 VipConfigSettingAgent.ExeUpgrade（保级检查 + 条件升级 + 晋级礼金）。
/// </summary>
public interface IVipUpgradeService
{
    /// <summary>
    /// 对单个会员跑一轮升级：未开 VIP / 信誉网用户 / 升级锁 直接跳过。
    /// </summary>
    /// <param name="userId">会员编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否发生了等级变更</returns>
    Task<bool> TryUpgradeAsync(int userId, CancellationToken cancellationToken = default);
}
