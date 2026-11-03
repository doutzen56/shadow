namespace SimpX.Net.Application.SuperAdmin;

/// <summary>
/// 总后台代付接口。
/// </summary>
[ApiDescriptionSettings(Module = "platform", Name = "systemWithdraw", Order = 474, Description = "代付接口")]
public class PlatformSystemWithdrawAppService : IDynamicApiController, ITransient
{
    private readonly SystemWithdrawService _svc;

    /// <summary>
    /// 注入。
    /// </summary>
    /// <param name="svc">代付</param>
    public PlatformSystemWithdrawAppService(SystemWithdrawService svc) => _svc = svc;

    /// <summary>列表。</summary>
    /// <param name="input">筛选</param>
    /// <returns>行列表</returns>
    [ApiDescriptionSettings(Name = "List"), HttpPost]
    [DisplayName("平台代付接口列表")]
    public Task<List<SystemWithdrawListItemDto>> List(SystemWithdrawListInput input) =>
        _svc.ListAsync(input?.Name, App.HttpContext?.RequestAborted ?? default);

    /// <summary>详情。</summary>
    /// <param name="id">编号</param>
    /// <returns>详情</returns>
    [ApiDescriptionSettings(Name = "Detail"), HttpGet]
    [DisplayName("平台代付接口详情")]
    public Task<SystemWithdrawDetailDto> Detail([FromQuery] int id) =>
        _svc.DetailAsync(id, App.HttpContext?.RequestAborted ?? default);

    /// <summary>保存。</summary>
    /// <param name="input">表单</param>
    /// <returns>编号</returns>
    [ApiDescriptionSettings(Name = "Save"), HttpPost]
    [DisplayName("平台代付接口保存")]
    public Task<int> Save(SystemWithdrawSaveInput input) =>
        _svc.SaveAsync(input ?? new SystemWithdrawSaveInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>改名称。</summary>
    /// <param name="input">编号与名称</param>
    [ApiDescriptionSettings(Name = "UpdateName"), HttpPost]
    [DisplayName("平台代付改名")]
    public Task UpdateName(SystemWithdrawUpdateNameInput input) =>
        _svc.UpdateNameAsync(input?.ID ?? 0, input?.Name, App.HttpContext?.RequestAborted ?? default);

    /// <summary>改开放。</summary>
    /// <param name="input">编号与开关</param>
    [ApiDescriptionSettings(Name = "UpdateIsOpen"), HttpPost]
    [DisplayName("平台代付开关")]
    public Task UpdateIsOpen(SystemWithdrawUpdateOpenInput input) =>
        _svc.UpdateIsOpenAsync(input?.ID ?? 0, input?.IsOpen ?? false, App.HttpContext?.RequestAborted ?? default);
}

/// <summary>
/// 总后台提现黑名单。
/// </summary>
[ApiDescriptionSettings(Module = "platform", Name = "systemBankBlack", Order = 475, Description = "提现黑名单")]
public class PlatformSystemBankBlackAppService : IDynamicApiController, ITransient
{
    private readonly SystemBankBlackService _svc;

    /// <summary>
    /// 注入。
    /// </summary>
    /// <param name="svc">黑名单</param>
    public PlatformSystemBankBlackAppService(SystemBankBlackService svc) => _svc = svc;

    /// <summary>列表。</summary>
    /// <returns>行列表</returns>
    [ApiDescriptionSettings(Name = "List"), HttpPost]
    [DisplayName("平台提现黑名单列表")]
    public Task<List<SystemBankBlackListItemDto>> List() =>
        _svc.ListAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>新增。</summary>
    /// <param name="input">户名卡号</param>
    [ApiDescriptionSettings(Name = "Create"), HttpPost]
    [DisplayName("平台提现黑名单新增")]
    public Task Create(SystemBankBlackCreateInput input) =>
        _svc.CreateAsync(input ?? new SystemBankBlackCreateInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>删除。</summary>
    /// <param name="id">编号</param>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("平台提现黑名单删除")]
    public Task Delete([FromQuery] int id) =>
        _svc.DeleteAsync(id, App.HttpContext?.RequestAborted ?? default);
}
