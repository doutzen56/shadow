namespace SimpX.Net.Application.Admin;

/// <summary>
/// 租户后台充值接口设定（JWT）。对齐旧版 /admin/recharge/settinglist。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "sitePayment", Order = 465, Description = "充值接口设定")]
public partial class SitePaymentAppService : IDynamicApiController, ITransient
{
    private readonly SitePaymentService _svc;

    /// <summary>
    /// 注入服务。
    /// </summary>
    /// <param name="svc">服务</param>
    public SitePaymentAppService(SitePaymentService svc) => _svc = svc;

    /// <summary>
    /// 列表。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <returns>行列表</returns>
    [ApiDescriptionSettings(Name = "List"), HttpPost]
    [DisplayName("充值接口设定列表")]
    public Task<List<SitePaymentListItemDto>> List(SitePaymentListInput input) =>
        _svc.ListAsync(input, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 详情。
    /// </summary>
    /// <param name="id">通道编号</param>
    /// <returns>详情</returns>
    [ApiDescriptionSettings(Name = "Info"), HttpGet]
    [DisplayName("充值接口详情")]
    public Task<SitePaymentDetailDto> Info([FromQuery] int id) =>
        _svc.InfoAsync(id, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 保存。对齐旧版 savesetting 核心校验。
    /// </summary>
    /// <param name="input">入参</param>
    /// <returns>任务</returns>
    [ApiDescriptionSettings(Name = "Save"), HttpPost]
    [DisplayName("保存充值接口")]
    public Task Save(SitePaymentSaveInput input) =>
        _svc.SaveAsync(input ?? new SitePaymentSaveInput(), App.HttpContext?.RequestAborted ?? default);

    // --- merged from SitePaymentAppService.WaveGaps.cs ---



    /// <summary>
    /// 对齐旧 checkorder。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Checkorder"), HttpPost]
    [DisplayName("checkorder")]
    public Task<object?> Checkorder(Dictionary<string, object?>? input = null) =>
        _svc.CheckorderAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 deletecategory。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Deletecategory"), HttpPost]
    [DisplayName("deletecategory")]
    public Task<object?> Deletecategory(Dictionary<string, object?>? input = null) =>
        _svc.DeletecategoryAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 deletepayment。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Deletepayment"), HttpPost]
    [DisplayName("deletepayment")]
    public Task<object?> Deletepayment(Dictionary<string, object?>? input = null) =>
        _svc.DeletepaymentAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 getpaymentsetting。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Getpaymentsetting"), HttpPost]
    [DisplayName("getpaymentsetting")]
    public Task<object?> Getpaymentsetting(Dictionary<string, object?>? input = null) =>
        _svc.GetpaymentsettingAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 getsyspaymentgroup。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Getsyspaymentgroup"), HttpPost]
    [DisplayName("getsyspaymentgroup")]
    public Task<object?> Getsyspaymentgroup(Dictionary<string, object?>? input = null) =>
        _svc.GetsyspaymentgroupAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 open。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Open"), HttpPost]
    [DisplayName("open")]
    public Task<object?> Open(Dictionary<string, object?>? input = null) =>
        _svc.OpenAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 paymenttest。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Paymenttest"), HttpPost]
    [DisplayName("paymenttest")]
    public Task<object?> Paymenttest(Dictionary<string, object?>? input = null) =>
        _svc.PaymenttestAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 stop。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Stop"), HttpPost]
    [DisplayName("stop")]
    public Task<object?> Stop(Dictionary<string, object?>? input = null) =>
        _svc.StopAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 updatecategorycount。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Updatecategorycount"), HttpPost]
    [DisplayName("updatecategorycount")]
    public Task<object?> Updatecategorycount(Dictionary<string, object?>? input = null) =>
        _svc.UpdatecategorycountAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 updateisrecommend。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Updateisrecommend"), HttpPost]
    [DisplayName("updateisrecommend")]
    public Task<object?> Updateisrecommend(Dictionary<string, object?>? input = null) =>
        _svc.UpdateisrecommendAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 updatepaymentstatus。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Updatepaymentstatus"), HttpPost]
    [DisplayName("updatepaymentstatus")]
    public Task<object?> Updatepaymentstatus(Dictionary<string, object?>? input = null) =>
        _svc.UpdatepaymentstatusAsync(input, App.HttpContext?.RequestAborted ?? default);
}
