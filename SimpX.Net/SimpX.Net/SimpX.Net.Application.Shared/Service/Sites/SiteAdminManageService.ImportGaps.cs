using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 站点管理员缺口：管理员详情/分组详情/声音，以及导入批删与申请别名（接到既有导入服务）。
/// </summary>
public sealed partial class SiteAdminManageService
{
    /// <summary>删除管理员。对齐 delete。</summary>
    /// <param name="id">编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task DeleteAdminAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw Oops.Oh("#参数错误#");
        var siteId = _site.RequireSiteId();
        var n = await _db.Deleteable<SiteAdmin>()
            .Where(a => a.SiteID == siteId && a.ID == id)
            .ExecuteCommandAsync(cancellationToken);
        if (n != 1)
            throw Oops.Oh("#删除失败#");
    }

    /// <summary>管理员详情。对齐 info。</summary>
    /// <param name="id">编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>详情</returns>
    public async Task<SiteAdminItemDto> GetAdminAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw Oops.Oh("#参数错误#");
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<SiteAdmin>()
            .Where(a => a.SiteID == siteId && a.ID == id).Take(1).ToListAsync(cancellationToken);
        var a = rows.FirstOrDefault() ?? throw Oops.Oh("#记录不存在#");
        return new SiteAdminItemDto
        {
            ID = a.ID,
            AdminName = a.AdminName,
            NickName = a.NickName,
            Status = a.Status,
            GroupID = a.GroupID,
            LoginAt = a.LoginAt,
            LoginIP = a.LoginIP,
            CreateAt = a.CreateAt,
            HasTotp = a.SecretKey != Guid.Empty
        };
    }

    /// <summary>分组详情。对齐 groupinfo。</summary>
    /// <param name="id">编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分组</returns>
    public async Task<SiteAdminGroupItemDto> GetGroupAsync(int id, CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        if (id <= 0)
            return new SiteAdminGroupItemDto { ID = 0, Name = string.Empty, PermissionString = string.Empty };
        var rows = await _db.Queryable<SiteAdminGroup>()
            .Where(g => g.SiteID == siteId && g.ID == id).Take(1).ToListAsync(cancellationToken);
        var g = rows.FirstOrDefault() ?? throw Oops.Oh("#记录不存在#");
        return new SiteAdminGroupItemDto
        {
            ID = g.ID,
            Name = g.Name,
            PermissionString = g.PermissionString,
            Member = g.Member,
            CreateAt = g.CreateAt
        };
    }

    /// <summary>刷新分组人数。对齐 refreshgroup。</summary>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task RefreshGroupAsync(CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var groups = await _db.Queryable<SiteAdminGroup>()
            .Where(g => g.SiteID == siteId).ToListAsync(cancellationToken);
        foreach (var g in groups)
        {
            g.Member = await _db.Queryable<SiteAdmin>()
                .Where(a => a.SiteID == siteId && a.GroupID == g.ID)
                .CountAsync(cancellationToken);
            await _db.Updateable(g).UpdateColumns(x => x.Member).ExecuteCommandAsync(cancellationToken);
        }
    }

    /// <summary>管理员提示音。对齐 sound。</summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>Setting</returns>
    public async Task<Dictionary<string, object>> GetAdminSoundAsync(CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        // 旧版取当前登录管理员；这里取站内第一条管理员声音作兼容占位，前端应按会话补管理员编号。
        var rows = await _db.Queryable<SiteAdmin>()
            .Where(a => a.SiteID == siteId).OrderBy(a => a.ID).Take(1).ToListAsync(cancellationToken);
        var raw = rows.FirstOrDefault()?.WarningSoundString ?? string.Empty;
        return SiteSettingQuery.ToSettingObject(SiteSettingQuery.Parse(raw));
    }

    /// <summary>保存管理员提示音。对齐 savesound。</summary>
    /// <param name="input">type/url；可选 AdminID</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SaveAdminSoundAsync(SiteAdminSoundSaveInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (string.IsNullOrWhiteSpace(input.Type))
            throw Oops.Oh("#参数错误#");
        var siteId = _site.RequireSiteId();
        SiteAdmin row;
        if (input.AdminID > 0)
        {
            var rows = await _db.Queryable<SiteAdmin>()
                .Where(a => a.SiteID == siteId && a.ID == input.AdminID).Take(1).ToListAsync(cancellationToken);
            row = rows.FirstOrDefault() ?? throw Oops.Oh("#记录不存在#");
        }
        else
        {
            var rows = await _db.Queryable<SiteAdmin>()
                .Where(a => a.SiteID == siteId).OrderBy(a => a.ID).Take(1).ToListAsync(cancellationToken);
            row = rows.FirstOrDefault() ?? throw Oops.Oh("#记录不存在#");
        }

        var map = SiteSettingQuery.Parse(row.WarningSoundString);
        map[input.Type.Trim()] = input.Url ?? string.Empty;
        row.WarningSoundString = SiteSettingQuery.ToQueryString(map);
        await _db.Updateable(row).UpdateColumns(a => a.WarningSoundString).ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>注册会员导入列表。对齐 importuserlist。</summary>
    public Task<MemberImportPageOutput<UserImportItemDto>> ImportUserListAsync(
        MemberImportPageInput input,
        CancellationToken cancellationToken = default) =>
        _memberImport.PageUsersAsync(input ?? new MemberImportPageInput(), cancellationToken);

    /// <summary>证件导入列表。对齐 importidcardlist。</summary>
    public Task<MemberImportPageOutput<IdCardImportItemDto>> ImportIdCardListAsync(
        MemberImportPageInput input,
        CancellationToken cancellationToken = default) =>
        _memberImport.PageIdCardsAsync(input ?? new MemberImportPageInput(), cancellationToken);

    /// <summary>银行导入列表。对齐 importbanklist。</summary>
    public Task<MemberImportPageOutput<BankImportItemDto>> ImportBankListAsync(
        MemberImportPageInput input,
        CancellationToken cancellationToken = default) =>
        _memberImport.PageBanksAsync(input ?? new MemberImportPageInput(), cancellationToken);

    /// <summary>活动导入列表。对齐 importeventlist。</summary>
    public Task<SimpleImportPageOutput> ImportEventListAsync(
        SimpleImportPageInput input,
        CancellationToken cancellationToken = default) =>
        _eventImport.PageAsync(input ?? new SimpleImportPageInput(), cancellationToken);

    /// <summary>号码导入列表。对齐 importmobilelist。</summary>
    public Task<SimpleImportPageOutput> ImportMobileListAsync(
        SimpleImportPageInput input,
        CancellationToken cancellationToken = default) =>
        _mobileImport.PageAsync(input ?? new SimpleImportPageInput(), cancellationToken);

    /// <summary>申请写入正式会员。对齐 createuser。</summary>
    public Task<MemberImportApplyOutput> CreateUserAsync(
        MemberImportApplyInput input,
        CancellationToken cancellationToken = default) =>
        _memberImport.ApplyUsersAsync(input ?? new MemberImportApplyInput(), cancellationToken);

    /// <summary>申请写入正式证件。对齐 createidcard。</summary>
    public Task<MemberImportApplyOutput> CreateIdCardAsync(
        MemberImportApplyInput input,
        CancellationToken cancellationToken = default) =>
        _memberImport.ApplyIdCardsAsync(input ?? new MemberImportApplyInput(), cancellationToken);

    /// <summary>申请写入正式银行。对齐 createbank。</summary>
    public Task<MemberImportApplyOutput> CreateBankAsync(
        MemberImportApplyInput input,
        CancellationToken cancellationToken = default) =>
        _memberImport.ApplyBanksAsync(input ?? new MemberImportApplyInput(), cancellationToken);

    /// <summary>活动导入批量追加。对齐 createevent / saveevent（文本）。</summary>
    public Task<int> CreateEventAsync(SimpleImportBatchInput input, CancellationToken cancellationToken = default) =>
        _eventImport.AddBatchAsync(input ?? new SimpleImportBatchInput(), cancellationToken);

    /// <summary>号码导入批量追加。对齐 createmobile / savemobile（文本）。</summary>
    public Task<int> CreateMobileAsync(SimpleImportBatchInput input, CancellationToken cancellationToken = default) =>
        _mobileImport.AddBatchAsync(input ?? new SimpleImportBatchInput(), cancellationToken);

    /// <summary>保存注册导入行。对齐 edituser / saveuser（单行，不含 Excel URL）。</summary>
    public Task SaveUserImportAsync(UserImportSaveInput input, CancellationToken cancellationToken = default) =>
        _memberImport.SaveUserAsync(input ?? new UserImportSaveInput(), cancellationToken);

    /// <summary>保存证件导入。对齐 editidcard / saveidcard。</summary>
    public Task SaveIdCardImportAsync(IdCardImportSaveInput input, CancellationToken cancellationToken = default) =>
        _memberImport.SaveIdCardAsync(input ?? new IdCardImportSaveInput(), cancellationToken);

    /// <summary>保存银行导入。对齐 editbank / savebank。</summary>
    public Task SaveBankImportAsync(BankImportSaveInput input, CancellationToken cancellationToken = default) =>
        _memberImport.SaveBankAsync(input ?? new BankImportSaveInput(), cancellationToken);

    /// <summary>删除注册导入。对齐 deleteuser。</summary>
    public Task DeleteUserImportAsync(int id, CancellationToken cancellationToken = default) =>
        _memberImport.DeleteUserAsync(id, cancellationToken);

    /// <summary>删除证件导入。对齐 deleteidcard。</summary>
    public Task DeleteIdCardImportAsync(int id, CancellationToken cancellationToken = default) =>
        _memberImport.DeleteIdCardAsync(id, cancellationToken);

    /// <summary>删除银行导入。对齐 deletebank。</summary>
    public Task DeleteBankImportAsync(int id, CancellationToken cancellationToken = default) =>
        _memberImport.DeleteBankAsync(id, cancellationToken);

    /// <summary>删除活动导入。对齐 deleteevent。</summary>
    public Task DeleteEventImportAsync(int id, CancellationToken cancellationToken = default) =>
        _eventImport.DeleteAsync(id, cancellationToken);

    /// <summary>删除号码导入。对齐 deletemobile。</summary>
    public Task DeleteMobileImportAsync(int id, CancellationToken cancellationToken = default) =>
        _mobileImport.DeleteAsync(id, cancellationToken);

    /// <summary>批量删除注册导入。对齐 batchdeleteuser。</summary>
    public async Task BatchDeleteUserImportAsync(SiteAdminBatchIdsInput input, CancellationToken cancellationToken = default)
    {
        var ids = RequireIds(input);
        var siteId = _site.RequireSiteId();
        await _db.Deleteable<UserImport>()
            .Where(t => t.SiteID == siteId && ids.Contains(t.ID))
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>批量删除证件导入。对齐 batchdeleteidcard。</summary>
    public async Task BatchDeleteIdCardImportAsync(SiteAdminBatchIdsInput input, CancellationToken cancellationToken = default)
    {
        var ids = RequireIds(input);
        var siteId = _site.RequireSiteId();
        await _db.Deleteable<UserIdCardImport>()
            .Where(t => t.SiteID == siteId && ids.Contains(t.ID))
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>批量删除银行导入。对齐 batchdeletebank。</summary>
    public async Task BatchDeleteBankImportAsync(SiteAdminBatchIdsInput input, CancellationToken cancellationToken = default)
    {
        var ids = RequireIds(input);
        var siteId = _site.RequireSiteId();
        await _db.Deleteable<UserBankImport>()
            .Where(t => t.SiteID == siteId && ids.Contains(t.ID))
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>批量删除活动导入。对齐 batchdeleteevent。</summary>
    public async Task BatchDeleteEventImportAsync(SiteAdminBatchIdsInput input, CancellationToken cancellationToken = default)
    {
        var ids = RequireIds(input);
        var siteId = _site.RequireSiteId();
        await _db.Deleteable<EventImport>()
            .Where(t => t.SiteID == siteId && ids.Contains(t.ID))
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>批量删除号码导入。对齐 batchdeletemobile。</summary>
    public async Task BatchDeleteMobileImportAsync(SiteAdminBatchIdsInput input, CancellationToken cancellationToken = default)
    {
        var ids = RequireIds(input);
        var siteId = _site.RequireSiteId();
        await _db.Deleteable<MobileImport>()
            .Where(t => t.SiteID == siteId && ids.Contains(t.ID))
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>校验批量编号。</summary>
    private static List<int> RequireIds(SiteAdminBatchIdsInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var ids = (input.Ids ?? []).Where(i => i > 0).Distinct().ToList();
        if (ids.Count == 0)
            throw Oops.Oh("#参数错误#");
        return ids;
    }
}

/// <summary>管理员提示音保存。</summary>
public sealed class SiteAdminSoundSaveInput
{
    /// <summary>管理员编号；0 时取站内第一条。</summary>
    public int AdminID { get; set; }

    /// <summary>类型键。</summary>
    public string Type { get; set; }

    /// <summary>地址。</summary>
    public string Url { get; set; }
}

/// <summary>批量编号。</summary>
public sealed class SiteAdminBatchIdsInput
{
    /// <summary>编号列表。</summary>
    public List<int> Ids { get; set; }
}
