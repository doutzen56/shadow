namespace SimpX.Net.Application.Admin;

/// <summary>
/// 风控列表旧动作名。
/// </summary>
public partial class RiskControlAppService
{
    /// <summary>对齐旧 control/game。</summary>
    /// <param name="input">筛选</param>
    /// <returns>行</returns>
    [ApiDescriptionSettings(Name = "Game"), HttpPost]
    [DisplayName("游戏盈利别名")]
    public Task<RiskGameProfitOutput> Game(RiskControlQueryInput input) =>
        _svc.GameProfitAsync(input ?? new RiskControlQueryInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>对齐旧 user/list。</summary>
    /// <param name="input">筛选</param>
    /// <returns>行</returns>
    [ApiDescriptionSettings(Name = "List"), HttpPost]
    [DisplayName("风险会员列表")]
    public Task<List<RiskDangerUserRowDto>> List(RiskControlQueryInput input) =>
        _svc.ListAsync(input ?? new RiskControlQueryInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>对齐旧 user/ip。</summary>
    /// <param name="riskId">风险编号</param>
    /// <returns>对比</returns>
    [ApiDescriptionSettings(Name = "Ip"), HttpPost]
    [DisplayName("风险IP对比")]
    public Task<RiskUserIpCompareDto> Ip([FromQuery] int riskId) =>
        _svc.IpAsync(riskId, App.HttpContext?.RequestAborted ?? default);

    /// <summary>对齐旧 user/info。</summary>
    /// <param name="riskId">风险编号</param>
    /// <returns>说明</returns>
    [ApiDescriptionSettings(Name = "Info"), HttpPost]
    [DisplayName("风险绑定说明")]
    public Task<RiskUserBindInfoDto> Info([FromQuery] int riskId) =>
        _svc.InfoAsync(riskId, App.HttpContext?.RequestAborted ?? default);

    /// <summary>对齐旧 user/ipdifuser。</summary>
    /// <param name="riskId">风险编号</param>
    /// <returns>说明</returns>
    [ApiDescriptionSettings(Name = "IpDifUser"), HttpPost]
    [DisplayName("同IP多用户")]
    public Task<RiskUserContextDto> IpDifUser([FromQuery] int riskId) =>
        _svc.IpDifUserAsync(riskId, App.HttpContext?.RequestAborted ?? default);

    /// <summary>对齐旧 user/userdifip。</summary>
    /// <param name="riskId">风险编号</param>
    /// <returns>说明</returns>
    [ApiDescriptionSettings(Name = "UserDifIp"), HttpPost]
    [DisplayName("同用户多IP")]
    public Task<RiskUserContextDto> UserDifIp([FromQuery] int riskId) =>
        _svc.UserDifIpAsync(riskId, App.HttpContext?.RequestAborted ?? default);

    /// <summary>对齐旧 user/login。</summary>
    /// <param name="riskId">风险编号</param>
    /// <returns>次数</returns>
    [ApiDescriptionSettings(Name = "Login"), HttpPost]
    [DisplayName("登录密码变更")]
    public Task<RiskUserPasswordDto> Login([FromQuery] int riskId) =>
        _svc.LoginAsync(riskId, App.HttpContext?.RequestAborted ?? default);

    /// <summary>对齐旧 user/amount。</summary>
    /// <param name="riskId">风险编号</param>
    /// <returns>次数</returns>
    [ApiDescriptionSettings(Name = "Amount"), HttpPost]
    [DisplayName("资金密码变更")]
    public Task<RiskUserPasswordDto> Amount([FromQuery] int riskId) =>
        _svc.AmountAsync(riskId, App.HttpContext?.RequestAborted ?? default);
}
