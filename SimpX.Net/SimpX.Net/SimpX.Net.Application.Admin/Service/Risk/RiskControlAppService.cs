namespace SimpX.Net.Application.Admin;

/// <summary>
/// 风控列表（JWT）：会员盈利 / 游戏盈利 / 风险会员。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "riskControl", Order = 469, Description = "风控列表")]
public partial class RiskControlAppService : IDynamicApiController, ITransient
{
    private readonly RiskControlService _svc;

    /// <summary>
    /// 注入服务。
    /// </summary>
    /// <param name="svc">服务</param>
    public RiskControlAppService(RiskControlService svc) => _svc = svc;

    /// <summary>
    /// 会员盈利。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <returns>行</returns>
    [ApiDescriptionSettings(Name = "Profit"), HttpPost]
    [DisplayName("会员盈利")]
    public Task<RiskProfitOutput> Profit(RiskControlQueryInput input) =>
        _svc.ProfitAsync(input ?? new RiskControlQueryInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 游戏盈利。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <returns>行</returns>
    [ApiDescriptionSettings(Name = "GameProfit"), HttpPost]
    [DisplayName("游戏盈利")]
    public Task<RiskGameProfitOutput> GameProfit(RiskControlQueryInput input) =>
        _svc.GameProfitAsync(input ?? new RiskControlQueryInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 风险会员。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <returns>行</returns>
    [ApiDescriptionSettings(Name = "DangerUsers"), HttpPost]
    [DisplayName("风险会员")]
    public Task<List<RiskDangerUserRowDto>> DangerUsers(RiskControlQueryInput input) =>
        _svc.DangerUsersAsync(input ?? new RiskControlQueryInput(), App.HttpContext?.RequestAborted ?? default);
}
