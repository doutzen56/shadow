using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 租户后台充提/盈亏日统计（JWT，只读）。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "siteFundDaily", Order = 534, Description = "充提盈亏日统计")]
public partial class SiteFundDailyStatAppService : IDynamicApiController, ITransient
{
    private readonly SiteFundDailyStatService _svc;

    /// <summary>
    /// 注入。
    /// </summary>
    /// <param name="svc">日统计</param>
    public SiteFundDailyStatAppService(SiteFundDailyStatService svc) => _svc = svc;

    /// <summary>充提按日。</summary>
    /// <param name="input">筛选</param>
    /// <returns>行列表</returns>
    [ApiDescriptionSettings(Name = "ListRechargeWithdraw"), HttpPost]
    [DisplayName("充提日统计")]
    public Task<List<SiteFundDailyItemDto>> ListRechargeWithdraw(SiteFundDailyListInput input) =>
        _svc.ListRechargeWithdrawAsync(input ?? new SiteFundDailyListInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>盈亏按日。</summary>
    /// <param name="input">筛选</param>
    /// <returns>行列表</returns>
    [ApiDescriptionSettings(Name = "ListProfit"), HttpPost]
    [DisplayName("盈亏日统计")]
    public Task<List<SiteFundDailyItemDto>> ListProfit(SiteFundDailyListInput input) =>
        _svc.ListProfitAsync(input ?? new SiteFundDailyListInput(), App.HttpContext?.RequestAborted ?? default);

    // --- merged from SiteFundDailyStatAppService.WaveGaps.cs ---



    /// <summary>
    /// 对齐旧 activeusers。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Activeusers"), HttpPost]
    [DisplayName("activeusers")]
    public Task<object?> Activeusers(Dictionary<string, object?>? input = null) =>
        _svc.ActiveusersAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 betamount。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Betamount"), HttpPost]
    [DisplayName("betamount")]
    public Task<object?> Betamount(Dictionary<string, object?>? input = null) =>
        _svc.BetamountAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 chartlist。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Chartlist"), HttpPost]
    [DisplayName("chartlist")]
    public Task<object?> Chartlist(Dictionary<string, object?>? input = null) =>
        _svc.ChartlistAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 gamecreditlog。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Gamecreditlog"), HttpPost]
    [DisplayName("gamecreditlog")]
    public Task<object?> Gamecreditlog(Dictionary<string, object?>? input = null) =>
        _svc.GamecreditlogAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 getplatformbalance。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Getplatformbalance"), HttpPost]
    [DisplayName("getplatformbalance")]
    public Task<object?> Getplatformbalance(Dictionary<string, object?>? input = null) =>
        _svc.GetplatformbalanceAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 ordercount。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Ordercount"), HttpPost]
    [DisplayName("ordercount")]
    public Task<object?> Ordercount(Dictionary<string, object?>? input = null) =>
        _svc.OrdercountAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 profitofindex。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Profitofindex"), HttpPost]
    [DisplayName("profitofindex")]
    public Task<object?> Profitofindex(Dictionary<string, object?>? input = null) =>
        _svc.ProfitofindexAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 rebateactive。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Rebateactive"), HttpPost]
    [DisplayName("rebateactive")]
    public Task<object?> Rebateactive(Dictionary<string, object?>? input = null) =>
        _svc.RebateactiveAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 rechargeandwitdraw。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Rechargeandwitdraw"), HttpPost]
    [DisplayName("rechargeandwitdraw")]
    public Task<object?> Rechargeandwitdraw(Dictionary<string, object?>? input = null) =>
        _svc.RechargeandwitdrawAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 rechargeusers。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Rechargeusers"), HttpPost]
    [DisplayName("rechargeusers")]
    public Task<object?> Rechargeusers(Dictionary<string, object?>? input = null) =>
        _svc.RechargeusersAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 rechargeusersdetails。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Rechargeusersdetails"), HttpPost]
    [DisplayName("rechargeusersdetails")]
    public Task<object?> Rechargeusersdetails(Dictionary<string, object?>? input = null) =>
        _svc.RechargeusersdetailsAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 systemdatareport。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Systemdatareport"), HttpPost]
    [DisplayName("systemdatareport")]
    public Task<object?> Systemdatareport(Dictionary<string, object?>? input = null) =>
        _svc.SystemdatareportAsync(input, App.HttpContext?.RequestAborted ?? default);
}
