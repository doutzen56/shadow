using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 租户后台充提用户统计（JWT，只读）。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "rwUserStat", Order = 537, Description = "充提用户统计")]
public class RechargeWithdrawUserStatAppService : IDynamicApiController, ITransient
{
    private readonly RechargeWithdrawUserStatService _svc;

    /// <summary>
    /// 注入。
    /// </summary>
    /// <param name="svc">充提用户</param>
    public RechargeWithdrawUserStatAppService(RechargeWithdrawUserStatService svc) => _svc = svc;

    /// <summary>汇总。</summary>
    /// <param name="input">筛选</param>
    /// <returns>汇总</returns>
    [ApiDescriptionSettings(Name = "Get"), HttpPost]
    [DisplayName("充提用户统计")]
    public Task<RechargeWithdrawUserStatDto> Get(SiteFundDailyListInput input) =>
        _svc.GetAsync(input ?? new SiteFundDailyListInput(), App.HttpContext?.RequestAborted ?? default);
}
