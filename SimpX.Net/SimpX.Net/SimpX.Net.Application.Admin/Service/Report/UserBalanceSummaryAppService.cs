using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 租户后台余额汇总（JWT，只读）。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "userBalanceSummary", Order = 535, Description = "余额汇总")]
public partial class UserBalanceSummaryAppService : IDynamicApiController, ITransient
{
    private readonly UserBalanceSummaryService _svc;

    /// <summary>
    /// 注入。
    /// </summary>
    /// <param name="svc">余额汇总</param>
    public UserBalanceSummaryAppService(UserBalanceSummaryService svc) => _svc = svc;

    /// <summary>主钱包按币种。</summary>
    /// <returns>行列表</returns>
    [ApiDescriptionSettings(Name = "ListWallet"), HttpPost]
    [DisplayName("主钱包余额汇总")]
    public Task<List<WalletBalanceItemDto>> ListWallet() =>
        _svc.ListWalletAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>游戏钱包按游戏。</summary>
    /// <returns>行列表</returns>
    [ApiDescriptionSettings(Name = "ListGame"), HttpPost]
    [DisplayName("游戏钱包余额汇总")]
    public Task<List<GameBalanceItemDto>> ListGame() =>
        _svc.ListGameAsync(App.HttpContext?.RequestAborted ?? default);

    // --- merged from UserBalanceSummaryAppService.WaveGaps.cs ---



    /// <summary>
    /// 对齐旧 balance。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Balance"), HttpPost]
    [DisplayName("balance")]
    public Task<object?> Balance(Dictionary<string, object?>? input = null) =>
        _svc.BalanceAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 balanceinfo。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Balanceinfo"), HttpPost]
    [DisplayName("balanceinfo")]
    public Task<object?> Balanceinfo(Dictionary<string, object?>? input = null) =>
        _svc.BalanceinfoAsync(input, App.HttpContext?.RequestAborted ?? default);
}
