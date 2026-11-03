namespace SimpX.Net.Application.SuperAdmin;

/// <summary>
/// 总后台货币汇率（JWT）。对齐旧版 /system/setting/config/exchangelist|updateExchange|updatecurrency。
/// </summary>
[ApiDescriptionSettings(Module = "platform", Name = "systemExchange", Order = 471, Description = "货币汇率")]
public class PlatformSystemExchangeAppService : IDynamicApiController, ITransient
{
    private readonly SystemExchangeService _svc;

    /// <summary>
    /// 注入服务。
    /// </summary>
    /// <param name="svc">服务</param>
    public PlatformSystemExchangeAppService(SystemExchangeService svc) => _svc = svc;

    /// <summary>
    /// 列表。对齐旧 exchangelist。
    /// </summary>
    /// <param name="input">基准币</param>
    /// <returns>行列表</returns>
    [ApiDescriptionSettings(Name = "List"), HttpPost]
    [DisplayName("平台货币汇率列表")]
    public Task<List<SystemExchangeListItemDto>> List(SystemExchangeListInput input) =>
        _svc.ListAsync(input ?? new SystemExchangeListInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 改市场价。对齐旧 updateExchange。
    /// </summary>
    /// <param name="input">编号与价格</param>
    /// <returns>更新时间</returns>
    [ApiDescriptionSettings(Name = "UpdatePrice"), HttpPost]
    [DisplayName("更新平台汇率")]
    public Task<SystemExchangeUpdatePriceResult> UpdatePrice(SystemExchangeUpdatePriceInput input) =>
        _svc.UpdateMarketPriceAsync(input, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 补齐币对。对齐旧 updatecurrency。
    /// </summary>
    /// <param name="input">基准币</param>
    [ApiDescriptionSettings(Name = "EnsurePairs"), HttpPost]
    [DisplayName("补齐平台汇率币对")]
    public Task EnsurePairs(SystemExchangeListInput input) =>
        _svc.EnsureCurrencyPairsAsync(input?.Currency ?? 0, App.HttpContext?.RequestAborted ?? default);
}
