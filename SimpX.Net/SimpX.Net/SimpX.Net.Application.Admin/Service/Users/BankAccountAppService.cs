namespace SimpX.Net.Application.Admin;

/// <summary>
/// 租户后台银行信息（JWT）。对齐旧版 /admin/user/member/bank/*。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "bankAccount", Order = 474, Description = "银行信息")]
public class BankAccountAppService : IDynamicApiController, ITransient
{
    private readonly BankAccountService _svc;

    /// <summary>
    /// 注入服务。
    /// </summary>
    /// <param name="svc">银行信息</param>
    public BankAccountAppService(BankAccountService svc) => _svc = svc;

    /// <summary>
    /// 分页列表。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <returns>分页</returns>
    [ApiDescriptionSettings(Name = "Page"), HttpPost]
    [DisplayName("银行信息列表")]
    public Task<BankAccountAdminPageOutput> Page(BankAccountAdminPageInput input) =>
        _svc.PageAsync(input ?? new BankAccountAdminPageInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 详情。
    /// </summary>
    /// <param name="id">帐户编号</param>
    /// <returns>详情</returns>
    [ApiDescriptionSettings(Name = "Detail"), HttpGet]
    [DisplayName("银行信息详情")]
    public Task<BankAccountAdminDetailDto> Detail([FromQuery] int id) =>
        _svc.DetailAsync(id, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 按用户名确认会员。
    /// </summary>
    /// <param name="userName">用户名</param>
    /// <returns>会员编号</returns>
    [ApiDescriptionSettings(Name = "VerifyUser"), HttpGet]
    [DisplayName("银行信息查会员")]
    public Task<BankAccountVerifyUserOutput> VerifyUser([FromQuery] string userName) =>
        _svc.VerifyUserAsync(userName, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 审核状态。
    /// </summary>
    /// <param name="input">编号与状态</param>
    [ApiDescriptionSettings(Name = "UpdateStatus"), HttpPost]
    [DisplayName("银行信息审核")]
    public Task UpdateStatus(BankAccountUpdateStatusInput input) =>
        _svc.UpdateStatusAsync(input ?? new BankAccountUpdateStatusInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 删除。
    /// </summary>
    /// <param name="input">编号</param>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("银行信息删除")]
    public Task Delete(BankAccountDeleteInput input) =>
        _svc.DeleteAsync(input?.ID ?? 0, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 新增。
    /// </summary>
    /// <param name="input">表单</param>
    [ApiDescriptionSettings(Name = "Create"), HttpPost]
    [DisplayName("银行信息新增")]
    public Task Create(BankAccountCreateInput input) =>
        _svc.CreateAsync(input ?? new BankAccountCreateInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 新增（旧名 createbank）。
    /// </summary>
    /// <param name="input">表单</param>
    [ApiDescriptionSettings(Name = "CreateBank"), HttpPost]
    [DisplayName("银行信息新增createbank")]
    public Task CreateBank(BankAccountCreateInput input) =>
        Create(input);

    /// <summary>
    /// 审核（旧名 updateinfo）。
    /// </summary>
    /// <param name="input">编号与状态</param>
    [ApiDescriptionSettings(Name = "UpdateInfo"), HttpPost]
    [DisplayName("银行信息审核updateinfo")]
    public Task UpdateInfo(BankAccountUpdateStatusInput input) =>
        UpdateStatus(input);

    /// <summary>
    /// 银行类型列表。对齐旧 bankcountrylist。
    /// </summary>
    /// <returns>选项</returns>
    [ApiDescriptionSettings(Name = "BankCountryList"), HttpPost]
    [DisplayName("银行类型列表")]
    public Task<List<BankTypeOptionDto>> BankCountryList() =>
        _svc.BankCountryListAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 超商城市。对齐旧 bankcitylist。
    /// </summary>
    /// <param name="type">超商类型</param>
    /// <returns>选项</returns>
    [ApiDescriptionSettings(Name = "BankCityList"), HttpPost]
    [DisplayName("超商城市")]
    public Task<List<BankTypeOptionDto>> BankCityList([FromQuery] int type) =>
        _svc.BankCityListAsync(type, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 超商区县。对齐旧 bankdistrictlist。
    /// </summary>
    /// <param name="type">超商类型</param>
    /// <param name="city">城市</param>
    /// <returns>选项</returns>
    [ApiDescriptionSettings(Name = "BankDistrictList"), HttpPost]
    [DisplayName("超商区县")]
    public Task<List<BankTypeOptionDto>> BankDistrictList([FromQuery] int type, [FromQuery] string city) =>
        _svc.BankDistrictListAsync(type, city, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 超商地址。对齐旧 bankadresslist。
    /// </summary>
    /// <param name="type">超商类型</param>
    /// <param name="city">城市</param>
    /// <param name="district">区县</param>
    /// <returns>选项</returns>
    [ApiDescriptionSettings(Name = "BankAdressList"), HttpPost]
    [DisplayName("超商地址")]
    public Task<List<BankTypeOptionDto>> BankAdressList(
        [FromQuery] int type,
        [FromQuery] string city,
        [FromQuery] string district) =>
        _svc.BankAdressListAsync(type, city, district, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 银行国家信息。对齐旧 bankcountryinfo。
    /// </summary>
    /// <param name="country">国家</param>
    /// <param name="isVir">虚拟类型</param>
    /// <returns>组</returns>
    [ApiDescriptionSettings(Name = "BankCountryInfo"), HttpPost]
    [DisplayName("银行国家信息")]
    public Task<object> BankCountryInfo([FromQuery] string country, [FromQuery] string isVir) =>
        _svc.BankCountryInfoAsync(country, isVir, App.HttpContext?.RequestAborted ?? default);
}
