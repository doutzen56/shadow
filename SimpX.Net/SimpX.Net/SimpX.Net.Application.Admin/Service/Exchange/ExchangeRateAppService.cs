namespace SimpX.Net.Application.Admin;

/// <summary>
/// 租户后台汇率（JWT）。对齐旧版 /admin/exchange/rate/*。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "exchangeRate", Order = 461, Description = "汇率设置")]
public partial class ExchangeRateAppService : IDynamicApiController, ITransient
{
    private readonly ExchangeRateService _svc;

    /// <summary>
    /// 注入服务。
    /// </summary>
    /// <param name="svc">服务</param>
    public ExchangeRateAppService(ExchangeRateService svc)
    {
        _svc = svc;
    }

    /// <summary>
    /// 列表。对齐旧 list。
    /// </summary>
    /// <param name="input">基准币</param>
    /// <returns>行列表</returns>
    [ApiDescriptionSettings(Name = "List"), HttpPost]
    [DisplayName("汇率列表")]
    public Task<List<ExchangeRateListItemDto>> List(ExchangeRateListInput input) =>
        _svc.ListAsync(input, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 基准币。对齐旧 getbasecurrency。
    /// </summary>
    /// <returns>基准币</returns>
    [ApiDescriptionSettings(Name = "BaseCurrency"), HttpGet]
    [DisplayName("站点基准币")]
    public Task<ExchangeBaseCurrencyDto> BaseCurrency() =>
        _svc.GetBaseCurrencyAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 详情。对齐旧 info。
    /// </summary>
    /// <param name="id">编号</param>
    /// <returns>详情</returns>
    [ApiDescriptionSettings(Name = "Detail"), HttpGet]
    [DisplayName("汇率详情")]
    public Task<ExchangeRateDetailDto> Detail([FromQuery] int id) =>
        _svc.DetailAsync(id, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 保存。对齐旧 saveinfo。
    /// </summary>
    /// <param name="input">入参</param>
    [ApiDescriptionSettings(Name = "Save"), HttpPost]
    [DisplayName("保存汇率")]
    public Task Save(ExchangeRateSaveInput input) =>
        _svc.SaveAsync(input, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 兑币设置列表。对齐旧 exchangelist。
    /// </summary>
    /// <param name="input">基准币与状态</param>
    /// <returns>行列表</returns>
    [ApiDescriptionSettings(Name = "ExchangeList"), HttpPost]
    [DisplayName("兑币设置列表")]
    public Task<List<ExchangeSettingListItemDto>> ExchangeList(ExchangeSettingListInput input) =>
        _svc.ExchangeListAsync(input, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 兑币设置详情。对齐旧 exchangeinfo。
    /// </summary>
    /// <param name="id">编号</param>
    /// <returns>详情</returns>
    [ApiDescriptionSettings(Name = "ExchangeInfo"), HttpGet]
    [DisplayName("兑币设置详情")]
    public Task<ExchangeSettingDetailDto> ExchangeInfo([FromQuery] int id) =>
        _svc.ExchangeInfoAsync(id, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 保存兑币设置。对齐旧 saveexchange。
    /// </summary>
    /// <param name="input">入参</param>
    [ApiDescriptionSettings(Name = "SaveExchange"), HttpPost]
    [DisplayName("保存兑币设置")]
    public Task SaveExchange(ExchangeSettingSaveInput input) =>
        _svc.SaveExchangeAsync(input, App.HttpContext?.RequestAborted ?? default);

    // --- merged from ExchangeRateAppService.WaveGaps.cs ---



    /// <summary>
    /// 对齐旧 getcurrency。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Getcurrency"), HttpPost]
    [DisplayName("getcurrency")]
    public Task<object?> Getcurrency(Dictionary<string, object?>? input = null) =>
        _svc.GetcurrencyAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 getcurrencyinfo。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Getcurrencyinfo"), HttpPost]
    [DisplayName("getcurrencyinfo")]
    public Task<object?> Getcurrencyinfo(Dictionary<string, object?>? input = null) =>
        _svc.GetcurrencyinfoAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 getfee。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Getfee"), HttpPost]
    [DisplayName("getfee")]
    public Task<object?> Getfee(Dictionary<string, object?>? input = null) =>
        _svc.GetfeeAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 getmode。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Getmode"), HttpPost]
    [DisplayName("getmode")]
    public Task<object?> Getmode(Dictionary<string, object?>? input = null) =>
        _svc.GetmodeAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 GetSysExchange。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "GetSysExchange"), HttpPost]
    [DisplayName("GetSysExchange")]
    public Task<object?> GetSysExchange(Dictionary<string, object?>? input = null) =>
        _svc.GetSysExchangeAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 updatesiterate。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Updatesiterate"), HttpPost]
    [DisplayName("updatesiterate")]
    public Task<object?> Updatesiterate(Dictionary<string, object?>? input = null) =>
        _svc.UpdatesiterateAsync(input, App.HttpContext?.RequestAborted ?? default);
}
