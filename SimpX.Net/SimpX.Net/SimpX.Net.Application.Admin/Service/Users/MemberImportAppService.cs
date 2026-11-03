namespace SimpX.Net.Application.Admin;

/// <summary>
/// 会员导入三页（JWT）。对齐旧 /admin/site/admin/import*。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "memberImport", Order = 475, Description = "会员导入")]
public class MemberImportAppService : IDynamicApiController, ITransient
{
    private readonly MemberImportService _svc;

    /// <summary>
    /// 注入导入服务。
    /// </summary>
    /// <param name="svc">导入</param>
    public MemberImportAppService(MemberImportService svc) => _svc = svc;

    /// <summary>注册会员导入列表。</summary>
    [ApiDescriptionSettings(Name = "PageUsers"), HttpPost]
    [DisplayName("注册会员导入列表")]
    public Task<MemberImportPageOutput<UserImportItemDto>> PageUsers(MemberImportPageInput input) =>
        _svc.PageUsersAsync(input ?? new MemberImportPageInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>保存注册会员导入。</summary>
    [ApiDescriptionSettings(Name = "SaveUser"), HttpPost]
    [DisplayName("注册会员导入保存")]
    public Task SaveUser(UserImportSaveInput input) =>
        _svc.SaveUserAsync(input ?? new UserImportSaveInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>删除注册会员导入。</summary>
    [ApiDescriptionSettings(Name = "DeleteUser"), HttpPost]
    [DisplayName("注册会员导入删除")]
    public Task DeleteUser([FromQuery] int id) =>
        _svc.DeleteUserAsync(id, App.HttpContext?.RequestAborted ?? default);

    /// <summary>申请写入正式会员。</summary>
    [ApiDescriptionSettings(Name = "ApplyUsers"), HttpPost]
    [DisplayName("注册会员导入申请")]
    public Task<MemberImportApplyOutput> ApplyUsers(MemberImportApplyInput input) =>
        _svc.ApplyUsersAsync(input ?? new MemberImportApplyInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>证件导入列表。</summary>
    [ApiDescriptionSettings(Name = "PageIdCards"), HttpPost]
    [DisplayName("证件导入列表")]
    public Task<MemberImportPageOutput<IdCardImportItemDto>> PageIdCards(MemberImportPageInput input) =>
        _svc.PageIdCardsAsync(input ?? new MemberImportPageInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>保存证件导入。</summary>
    [ApiDescriptionSettings(Name = "SaveIdCard"), HttpPost]
    [DisplayName("证件导入保存")]
    public Task SaveIdCard(IdCardImportSaveInput input) =>
        _svc.SaveIdCardAsync(input ?? new IdCardImportSaveInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>删除证件导入。</summary>
    [ApiDescriptionSettings(Name = "DeleteIdCard"), HttpPost]
    [DisplayName("证件导入删除")]
    public Task DeleteIdCard([FromQuery] int id) =>
        _svc.DeleteIdCardAsync(id, App.HttpContext?.RequestAborted ?? default);

    /// <summary>申请写入正式证件。</summary>
    [ApiDescriptionSettings(Name = "ApplyIdCards"), HttpPost]
    [DisplayName("证件导入申请")]
    public Task<MemberImportApplyOutput> ApplyIdCards(MemberImportApplyInput input) =>
        _svc.ApplyIdCardsAsync(input ?? new MemberImportApplyInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>银行导入列表。</summary>
    [ApiDescriptionSettings(Name = "PageBanks"), HttpPost]
    [DisplayName("银行导入列表")]
    public Task<MemberImportPageOutput<BankImportItemDto>> PageBanks(MemberImportPageInput input) =>
        _svc.PageBanksAsync(input ?? new MemberImportPageInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>保存银行导入。</summary>
    [ApiDescriptionSettings(Name = "SaveBank"), HttpPost]
    [DisplayName("银行导入保存")]
    public Task SaveBank(BankImportSaveInput input) =>
        _svc.SaveBankAsync(input ?? new BankImportSaveInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>删除银行导入。</summary>
    [ApiDescriptionSettings(Name = "DeleteBank"), HttpPost]
    [DisplayName("银行导入删除")]
    public Task DeleteBank([FromQuery] int id) =>
        _svc.DeleteBankAsync(id, App.HttpContext?.RequestAborted ?? default);

    /// <summary>申请写入正式银行帐户。</summary>
    [ApiDescriptionSettings(Name = "ApplyBanks"), HttpPost]
    [DisplayName("银行导入申请")]
    public Task<MemberImportApplyOutput> ApplyBanks(MemberImportApplyInput input) =>
        _svc.ApplyBanksAsync(input ?? new MemberImportApplyInput(), App.HttpContext?.RequestAborted ?? default);
}
