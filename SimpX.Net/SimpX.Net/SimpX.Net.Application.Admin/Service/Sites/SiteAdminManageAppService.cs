using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 租户后台管理员 / 分组 / 日志 / 页面配置（JWT）。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "siteAdminManage", Order = 550, Description = "站点管理员")]
public class SiteAdminManageAppService : IDynamicApiController, ITransient
{
    private readonly SiteAdminManageService _svc;

    /// <summary>
    /// 注入。
    /// </summary>
    /// <param name="svc">管理</param>
    public SiteAdminManageAppService(SiteAdminManageService svc) => _svc = svc;

    /// <summary>管理员分页。</summary>
    /// <param name="input">筛选</param>
    /// <returns>分页</returns>
    [ApiDescriptionSettings(Name = "PageAdmins"), HttpPost]
    [DisplayName("管理员分页")]
    public Task<SiteAdminPageOutput> PageAdmins(SiteAdminPageInput input) =>
        _svc.PageAdminsAsync(input ?? new SiteAdminPageInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>保存管理员。</summary>
    /// <param name="input">表单</param>
    /// <returns>编号</returns>
    [ApiDescriptionSettings(Name = "SaveAdmin"), HttpPost]
    [DisplayName("保存管理员")]
    public Task<int> SaveAdmin(SiteAdminSaveInput input) =>
        _svc.SaveAdminAsync(input ?? new SiteAdminSaveInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>重置密码。</summary>
    /// <param name="input">表单</param>
    [ApiDescriptionSettings(Name = "ResetPassword"), HttpPost]
    [DisplayName("重置管理员密码")]
    public Task ResetPassword(SiteAdminResetPwdInput input) =>
        _svc.ResetPasswordAsync(input ?? new SiteAdminResetPwdInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>清除谷歌。</summary>
    /// <param name="id">编号</param>
    [ApiDescriptionSettings(Name = "ClearTotp"), HttpPost]
    [DisplayName("清除谷歌验证")]
    public Task ClearTotp(int id) =>
        _svc.ClearTotpAsync(id, App.HttpContext?.RequestAborted ?? default);

    /// <summary>分组列表。</summary>
    /// <returns>行列表</returns>
    [ApiDescriptionSettings(Name = "ListGroups"), HttpPost]
    [DisplayName("权限分组列表")]
    public Task<List<SiteAdminGroupItemDto>> ListGroups() =>
        _svc.ListGroupsAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>保存分组。</summary>
    /// <param name="input">表单</param>
    /// <returns>编号</returns>
    [ApiDescriptionSettings(Name = "SaveGroup"), HttpPost]
    [DisplayName("保存权限分组")]
    public Task<int> SaveGroup(SiteAdminGroupSaveInput input) =>
        _svc.SaveGroupAsync(input ?? new SiteAdminGroupSaveInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>删除分组。</summary>
    /// <param name="id">编号</param>
    [ApiDescriptionSettings(Name = "DeleteGroup"), HttpPost]
    [DisplayName("删除权限分组")]
    public Task DeleteGroup(int id) =>
        _svc.DeleteGroupAsync(id, App.HttpContext?.RequestAborted ?? default);

    /// <summary>操作日志。</summary>
    /// <param name="input">筛选</param>
    /// <returns>分页</returns>
    [ApiDescriptionSettings(Name = "PageLogs"), HttpPost]
    [DisplayName("操作日志分页")]
    public Task<SiteAdminLogPageOutput> PageLogs(SiteAdminLogPageInput input) =>
        _svc.PageLogsAsync(input ?? new SiteAdminLogPageInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>页面配置。</summary>
    /// <returns>行列表</returns>
    [ApiDescriptionSettings(Name = "ListPageSettings"), HttpPost]
    [DisplayName("页面配置列表")]
    public Task<List<SitePageSettingItemDto>> ListPageSettings() =>
        _svc.ListPageSettingsAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>保存页面配置。</summary>
    /// <param name="input">表单</param>
    [ApiDescriptionSettings(Name = "SavePageSetting"), HttpPost]
    [DisplayName("保存页面配置")]
    public Task SavePageSetting(SitePageSettingSaveInput input) =>
        _svc.SavePageSettingAsync(input ?? new SitePageSettingSaveInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>删除管理员。对齐 delete（不是删分组）。</summary>
    /// <param name="id">编号</param>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("删除管理员")]
    public Task Delete(int id) =>
        _svc.DeleteAdminAsync(id, App.HttpContext?.RequestAborted ?? default);

    /// <summary>管理员详情。对齐 info。</summary>
    /// <param name="id">编号</param>
    [ApiDescriptionSettings(Name = "Info"), HttpPost]
    [DisplayName("管理员详情")]
    public Task<SiteAdminItemDto> Info(int id) =>
        _svc.GetAdminAsync(id, App.HttpContext?.RequestAborted ?? default);

    /// <summary>管理员列表别名。对齐 list。</summary>
    /// <param name="input">筛选</param>
    [ApiDescriptionSettings(Name = "List"), HttpPost]
    [DisplayName("管理员列表")]
    public Task<SiteAdminPageOutput> List(SiteAdminPageInput input) =>
        _svc.PageAdminsAsync(input ?? new SiteAdminPageInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>分组详情。对齐 groupinfo。</summary>
    /// <param name="id">编号</param>
    [ApiDescriptionSettings(Name = "GroupInfo"), HttpPost]
    [DisplayName("分组详情")]
    public Task<SiteAdminGroupItemDto> GroupInfo(int id) =>
        _svc.GetGroupAsync(id, App.HttpContext?.RequestAborted ?? default);

    /// <summary>分组列表别名。对齐 grouplist。</summary>
    [ApiDescriptionSettings(Name = "GroupList"), HttpPost]
    [DisplayName("分组列表")]
    public Task<List<SiteAdminGroupItemDto>> GroupList() =>
        _svc.ListGroupsAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>刷新分组人数。对齐 refreshgroup。</summary>
    [ApiDescriptionSettings(Name = "RefreshGroup"), HttpPost]
    [DisplayName("刷新分组人数")]
    public Task RefreshGroup() =>
        _svc.RefreshGroupAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>清除谷歌别名。对齐 clearcode。</summary>
    /// <param name="id">编号</param>
    [ApiDescriptionSettings(Name = "ClearCode"), HttpPost]
    [DisplayName("清除谷歌验证码")]
    public Task ClearCode(int id) =>
        _svc.ClearTotpAsync(id, App.HttpContext?.RequestAborted ?? default);

    /// <summary>管理员提示音。对齐 sound。</summary>
    [ApiDescriptionSettings(Name = "Sound"), HttpPost]
    [DisplayName("管理员提示音")]
    public Task<Dictionary<string, object>> Sound() =>
        _svc.GetAdminSoundAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>保存管理员提示音。对齐 savesound。</summary>
    /// <param name="input">type/url</param>
    [ApiDescriptionSettings(Name = "SaveSound"), HttpPost]
    [DisplayName("保存管理员提示音")]
    public Task SaveSound(SiteAdminSoundSaveInput input) =>
        _svc.SaveAdminSoundAsync(input ?? new SiteAdminSoundSaveInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>注册导入列表。</summary>
    /// <param name="input">筛选</param>
    [ApiDescriptionSettings(Name = "ImportUserList"), HttpPost]
    [DisplayName("注册会员导入列表")]
    public Task<MemberImportPageOutput<UserImportItemDto>> ImportUserList(MemberImportPageInput input) =>
        _svc.ImportUserListAsync(input ?? new MemberImportPageInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>证件导入列表。</summary>
    /// <param name="input">筛选</param>
    [ApiDescriptionSettings(Name = "ImportIdCardList"), HttpPost]
    [DisplayName("证件导入列表")]
    public Task<MemberImportPageOutput<IdCardImportItemDto>> ImportIdCardList(MemberImportPageInput input) =>
        _svc.ImportIdCardListAsync(input ?? new MemberImportPageInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>银行导入列表。</summary>
    /// <param name="input">筛选</param>
    [ApiDescriptionSettings(Name = "ImportBankList"), HttpPost]
    [DisplayName("银行导入列表")]
    public Task<MemberImportPageOutput<BankImportItemDto>> ImportBankList(MemberImportPageInput input) =>
        _svc.ImportBankListAsync(input ?? new MemberImportPageInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>活动导入列表。</summary>
    /// <param name="input">筛选</param>
    [ApiDescriptionSettings(Name = "ImportEventList"), HttpPost]
    [DisplayName("活动导入列表")]
    public Task<SimpleImportPageOutput> ImportEventList(SimpleImportPageInput input) =>
        _svc.ImportEventListAsync(input ?? new SimpleImportPageInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>号码导入列表。</summary>
    /// <param name="input">筛选</param>
    [ApiDescriptionSettings(Name = "ImportMobileList"), HttpPost]
    [DisplayName("号码导入列表")]
    public Task<SimpleImportPageOutput> ImportMobileList(SimpleImportPageInput input) =>
        _svc.ImportMobileListAsync(input ?? new SimpleImportPageInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>申请写入正式会员。</summary>
    /// <param name="input">编号</param>
    [ApiDescriptionSettings(Name = "CreateUser"), HttpPost]
    [DisplayName("注册会员导入申请")]
    public Task<MemberImportApplyOutput> CreateUser(MemberImportApplyInput input) =>
        _svc.CreateUserAsync(input ?? new MemberImportApplyInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>申请写入正式证件。</summary>
    /// <param name="input">编号</param>
    [ApiDescriptionSettings(Name = "CreateIdCard"), HttpPost]
    [DisplayName("证件导入申请")]
    public Task<MemberImportApplyOutput> CreateIdCard(MemberImportApplyInput input) =>
        _svc.CreateIdCardAsync(input ?? new MemberImportApplyInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>申请写入正式银行。</summary>
    /// <param name="input">编号</param>
    [ApiDescriptionSettings(Name = "CreateBank"), HttpPost]
    [DisplayName("银行导入申请")]
    public Task<MemberImportApplyOutput> CreateBank(MemberImportApplyInput input) =>
        _svc.CreateBankAsync(input ?? new MemberImportApplyInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>活动导入追加。</summary>
    /// <param name="input">文本</param>
    [ApiDescriptionSettings(Name = "CreateEvent"), HttpPost]
    [DisplayName("活动导入追加")]
    public Task<int> CreateEvent(SimpleImportBatchInput input) =>
        _svc.CreateEventAsync(input ?? new SimpleImportBatchInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>号码导入追加。</summary>
    /// <param name="input">文本</param>
    [ApiDescriptionSettings(Name = "CreateMobile"), HttpPost]
    [DisplayName("号码导入追加")]
    public Task<int> CreateMobile(SimpleImportBatchInput input) =>
        _svc.CreateMobileAsync(input ?? new SimpleImportBatchInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>保存注册导入。对齐 saveuser / edituser（单行）。</summary>
    /// <param name="input">行</param>
    [ApiDescriptionSettings(Name = "SaveUser"), HttpPost]
    [DisplayName("保存注册导入")]
    public Task SaveUser(UserImportSaveInput input) =>
        _svc.SaveUserImportAsync(input ?? new UserImportSaveInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>编辑注册导入别名。</summary>
    /// <param name="input">行</param>
    [ApiDescriptionSettings(Name = "EditUser"), HttpPost]
    [DisplayName("编辑注册导入")]
    public Task EditUser(UserImportSaveInput input) =>
        _svc.SaveUserImportAsync(input ?? new UserImportSaveInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>保存证件导入。</summary>
    /// <param name="input">行</param>
    [ApiDescriptionSettings(Name = "SaveIdCard"), HttpPost]
    [DisplayName("保存证件导入")]
    public Task SaveIdCard(IdCardImportSaveInput input) =>
        _svc.SaveIdCardImportAsync(input ?? new IdCardImportSaveInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>编辑证件导入别名。</summary>
    /// <param name="input">行</param>
    [ApiDescriptionSettings(Name = "EditIdCard"), HttpPost]
    [DisplayName("编辑证件导入")]
    public Task EditIdCard(IdCardImportSaveInput input) =>
        _svc.SaveIdCardImportAsync(input ?? new IdCardImportSaveInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>保存银行导入。</summary>
    /// <param name="input">行</param>
    [ApiDescriptionSettings(Name = "SaveBank"), HttpPost]
    [DisplayName("保存银行导入")]
    public Task SaveBank(BankImportSaveInput input) =>
        _svc.SaveBankImportAsync(input ?? new BankImportSaveInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>编辑银行导入别名。</summary>
    /// <param name="input">行</param>
    [ApiDescriptionSettings(Name = "EditBank"), HttpPost]
    [DisplayName("编辑银行导入")]
    public Task EditBank(BankImportSaveInput input) =>
        _svc.SaveBankImportAsync(input ?? new BankImportSaveInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>活动导入追加别名 saveevent。</summary>
    /// <param name="input">文本</param>
    [ApiDescriptionSettings(Name = "SaveEvent"), HttpPost]
    [DisplayName("保存活动导入")]
    public Task<int> SaveEvent(SimpleImportBatchInput input) =>
        _svc.CreateEventAsync(input ?? new SimpleImportBatchInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>活动导入编辑占位（按 ID 删除后重加由前端处理；本接口等同 CreateEvent）。</summary>
    /// <param name="input">文本</param>
    [ApiDescriptionSettings(Name = "EditEvent"), HttpPost]
    [DisplayName("编辑活动导入")]
    public Task<int> EditEvent(SimpleImportBatchInput input) =>
        _svc.CreateEventAsync(input ?? new SimpleImportBatchInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>号码导入追加别名。</summary>
    /// <param name="input">文本</param>
    [ApiDescriptionSettings(Name = "SaveMobile"), HttpPost]
    [DisplayName("保存号码导入")]
    public Task<int> SaveMobile(SimpleImportBatchInput input) =>
        _svc.CreateMobileAsync(input ?? new SimpleImportBatchInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>号码导入编辑别名。</summary>
    /// <param name="input">文本</param>
    [ApiDescriptionSettings(Name = "EditMobile"), HttpPost]
    [DisplayName("编辑号码导入")]
    public Task<int> EditMobile(SimpleImportBatchInput input) =>
        _svc.CreateMobileAsync(input ?? new SimpleImportBatchInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>删除注册导入。</summary>
    /// <param name="id">编号</param>
    [ApiDescriptionSettings(Name = "DeleteUser"), HttpPost]
    [DisplayName("删除注册导入")]
    public Task DeleteUser(int id) =>
        _svc.DeleteUserImportAsync(id, App.HttpContext?.RequestAborted ?? default);

    /// <summary>删除证件导入。</summary>
    /// <param name="id">编号</param>
    [ApiDescriptionSettings(Name = "DeleteIdCard"), HttpPost]
    [DisplayName("删除证件导入")]
    public Task DeleteIdCard(int id) =>
        _svc.DeleteIdCardImportAsync(id, App.HttpContext?.RequestAborted ?? default);

    /// <summary>删除银行导入。</summary>
    /// <param name="id">编号</param>
    [ApiDescriptionSettings(Name = "DeleteBank"), HttpPost]
    [DisplayName("删除银行导入")]
    public Task DeleteBank(int id) =>
        _svc.DeleteBankImportAsync(id, App.HttpContext?.RequestAborted ?? default);

    /// <summary>删除活动导入。</summary>
    /// <param name="id">编号</param>
    [ApiDescriptionSettings(Name = "DeleteEvent"), HttpPost]
    [DisplayName("删除活动导入")]
    public Task DeleteEvent(int id) =>
        _svc.DeleteEventImportAsync(id, App.HttpContext?.RequestAborted ?? default);

    /// <summary>删除号码导入。</summary>
    /// <param name="id">编号</param>
    [ApiDescriptionSettings(Name = "DeleteMobile"), HttpPost]
    [DisplayName("删除号码导入")]
    public Task DeleteMobile(int id) =>
        _svc.DeleteMobileImportAsync(id, App.HttpContext?.RequestAborted ?? default);

    /// <summary>批量删除注册导入。</summary>
    /// <param name="input">编号</param>
    [ApiDescriptionSettings(Name = "BatchDeleteUser"), HttpPost]
    [DisplayName("批量删除注册导入")]
    public Task BatchDeleteUser(SiteAdminBatchIdsInput input) =>
        _svc.BatchDeleteUserImportAsync(input ?? new SiteAdminBatchIdsInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>批量删除证件导入。</summary>
    /// <param name="input">编号</param>
    [ApiDescriptionSettings(Name = "BatchDeleteIdCard"), HttpPost]
    [DisplayName("批量删除证件导入")]
    public Task BatchDeleteIdCard(SiteAdminBatchIdsInput input) =>
        _svc.BatchDeleteIdCardImportAsync(input ?? new SiteAdminBatchIdsInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>批量删除银行导入。</summary>
    /// <param name="input">编号</param>
    [ApiDescriptionSettings(Name = "BatchDeleteBank"), HttpPost]
    [DisplayName("批量删除银行导入")]
    public Task BatchDeleteBank(SiteAdminBatchIdsInput input) =>
        _svc.BatchDeleteBankImportAsync(input ?? new SiteAdminBatchIdsInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>批量删除活动导入。</summary>
    /// <param name="input">编号</param>
    [ApiDescriptionSettings(Name = "BatchDeleteEvent"), HttpPost]
    [DisplayName("批量删除活动导入")]
    public Task BatchDeleteEvent(SiteAdminBatchIdsInput input) =>
        _svc.BatchDeleteEventImportAsync(input ?? new SiteAdminBatchIdsInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>批量删除号码导入。</summary>
    /// <param name="input">编号</param>
    [ApiDescriptionSettings(Name = "BatchDeleteMobile"), HttpPost]
    [DisplayName("批量删除号码导入")]
    public Task BatchDeleteMobile(SiteAdminBatchIdsInput input) =>
        _svc.BatchDeleteMobileImportAsync(input ?? new SiteAdminBatchIdsInput(), App.HttpContext?.RequestAborted ?? default);
}
