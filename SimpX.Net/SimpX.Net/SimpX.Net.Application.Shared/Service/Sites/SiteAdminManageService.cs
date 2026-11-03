using SimpX.Net.Data.Entity.Sites;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 租户后台管理员 / 权限分组 / 操作日志 / 页面配置。
/// </summary>
public sealed partial class SiteAdminManageService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;
    private readonly MemberImportService _memberImport;
    private readonly EventImportAdminService _eventImport;
    private readonly MobileImportAdminService _mobileImport;

    /// <summary>
    /// 注入依赖。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站</param>
    /// <param name="memberImport">会员导入</param>
    /// <param name="eventImport">活动导入</param>
    /// <param name="mobileImport">号码导入</param>
    public SiteAdminManageService(
        IBizDbAccessor biz,
        ICurrentSiteAccessor site,
        MemberImportService memberImport,
        EventImportAdminService eventImport,
        MobileImportAdminService mobileImport)
    {
        _db = biz.Db;
        _site = site;
        _memberImport = memberImport;
        _eventImport = eventImport;
        _mobileImport = mobileImport;
    }

    /// <summary>管理员分页（不含密码）。</summary>
    /// <param name="input">筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页</returns>
    public async Task<SiteAdminPageOutput> PageAdminsAsync(
        SiteAdminPageInput input,
        CancellationToken cancellationToken = default)
    {
        input ??= new SiteAdminPageInput();
        var siteId = _site.RequireSiteId();
        var pageSize = Math.Clamp(input.PageSize <= 0 ? 20 : input.PageSize, 1, 100);
        var page = Math.Max(1, input.Page <= 0 ? 1 : input.Page);
        var query = _db.Queryable<SiteAdmin>().Where(a => a.SiteID == siteId);
        if (!string.IsNullOrWhiteSpace(input.AdminName))
            query = query.Where(a => a.AdminName.Contains(input.AdminName.Trim()));
        if (input.GroupID > 0)
            query = query.Where(a => a.GroupID == input.GroupID);
        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderBy(a => a.ID, OrderByType.Desc)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new SiteAdminPageOutput
        {
            Total = total,
            Items = rows.Select(a => new SiteAdminItemDto
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
            }).ToList()
        };
    }

    /// <summary>保存管理员（新建要密码）。</summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>编号</returns>
    public async Task<int> SaveAdminAsync(SiteAdminSaveInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (string.IsNullOrWhiteSpace(input.AdminName))
            throw Oops.Oh("#用户名错误#");
        var siteId = _site.RequireSiteId();
        if (input.ID > 0)
        {
            var rows = await _db.Queryable<SiteAdmin>()
                .Where(a => a.SiteID == siteId && a.ID == input.ID).Take(1).ToListAsync(cancellationToken);
            var row = rows.FirstOrDefault() ?? throw Oops.Oh("#记录不存在#");
            row.NickName = input.NickName ?? string.Empty;
            row.Status = input.Status;
            row.GroupID = input.GroupID;
            row.AuditUpperLimit = input.AuditUpperLimit;
            if (!string.IsNullOrWhiteSpace(input.Password))
                row.Password = Md5PasswordHasher.Hash(input.Password.Trim());
            await _db.Updateable(row).ExecuteCommandAsync(cancellationToken);
            return row.ID;
        }

        if (string.IsNullOrWhiteSpace(input.Password))
            throw Oops.Oh("#密码错误#");
        return await _db.Insertable(new SiteAdmin
        {
            SiteID = siteId,
            AdminName = input.AdminName.Trim(),
            NickName = input.NickName ?? string.Empty,
            Password = Md5PasswordHasher.Hash(input.Password.Trim()),
            Status = input.Status,
            GroupID = input.GroupID,
            AuditUpperLimit = input.AuditUpperLimit,
            CreateAt = DateTime.Now,
            LoginAt = DateTime.MinValue,
            LoginIP = string.Empty,
            Face = string.Empty,
            Sign = string.Empty,
            WarningSoundString = string.Empty,
            SecretKey = Guid.Empty,
            Function = 0
        }).ExecuteReturnIdentityAsync(cancellationToken);
    }

    /// <summary>重置密码。</summary>
    /// <param name="input">编号与新密码</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task ResetPasswordAsync(SiteAdminResetPwdInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.ID <= 0 || string.IsNullOrWhiteSpace(input.Password))
            throw Oops.Oh("#参数错误#");
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<SiteAdmin>()
            .Where(a => a.SiteID == siteId && a.ID == input.ID).Take(1).ToListAsync(cancellationToken);
        var row = rows.FirstOrDefault() ?? throw Oops.Oh("#记录不存在#");
        row.Password = Md5PasswordHasher.Hash(input.Password.Trim());
        await _db.Updateable(row).UpdateColumns(a => a.Password).ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>清除谷歌验证。</summary>
    /// <param name="id">管理员编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task ClearTotpAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw Oops.Oh("#参数错误#");
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<SiteAdmin>()
            .Where(a => a.SiteID == siteId && a.ID == id).Take(1).ToListAsync(cancellationToken);
        var row = rows.FirstOrDefault() ?? throw Oops.Oh("#记录不存在#");
        row.SecretKey = Guid.Empty;
        await _db.Updateable(row).UpdateColumns(a => a.SecretKey).ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>权限分组列表。</summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行列表</returns>
    public async Task<List<SiteAdminGroupItemDto>> ListGroupsAsync(CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<SiteAdminGroup>()
            .Where(g => g.SiteID == siteId).OrderBy(g => g.ID).ToListAsync(cancellationToken);
        return rows.Select(g => new SiteAdminGroupItemDto
        {
            ID = g.ID,
            Name = g.Name,
            PermissionString = g.PermissionString,
            Member = g.Member,
            CreateAt = g.CreateAt
        }).ToList();
    }

    /// <summary>保存分组。</summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>编号</returns>
    public async Task<int> SaveGroupAsync(SiteAdminGroupSaveInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (string.IsNullOrWhiteSpace(input.Name))
            throw Oops.Oh("#名称错误#");
        var siteId = _site.RequireSiteId();
        if (input.ID > 0)
        {
            var rows = await _db.Queryable<SiteAdminGroup>()
                .Where(g => g.SiteID == siteId && g.ID == input.ID).Take(1).ToListAsync(cancellationToken);
            var row = rows.FirstOrDefault() ?? throw Oops.Oh("#记录不存在#");
            row.Name = input.Name.Trim();
            row.PermissionString = input.PermissionString ?? string.Empty;
            await _db.Updateable(row).ExecuteCommandAsync(cancellationToken);
            return row.ID;
        }

        return await _db.Insertable(new SiteAdminGroup
        {
            SiteID = siteId,
            Name = input.Name.Trim(),
            PermissionString = input.PermissionString ?? string.Empty,
            Member = 0,
            CreateAt = DateTime.Now
        }).ExecuteReturnIdentityAsync(cancellationToken);
    }

    /// <summary>删除分组。</summary>
    /// <param name="id">编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task DeleteGroupAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw Oops.Oh("#参数错误#");
        var siteId = _site.RequireSiteId();
        var used = await _db.Queryable<SiteAdmin>()
            .Where(a => a.SiteID == siteId && a.GroupID == id).AnyAsync(cancellationToken);
        if (used)
            throw Oops.Oh("#分组下还有管理员#");
        await _db.Deleteable<SiteAdminGroup>()
            .Where(g => g.SiteID == siteId && g.ID == id).ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>操作日志分页。</summary>
    /// <param name="input">筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页</returns>
    public async Task<SiteAdminLogPageOutput> PageLogsAsync(
        SiteAdminLogPageInput input,
        CancellationToken cancellationToken = default)
    {
        input ??= new SiteAdminLogPageInput();
        var siteId = _site.RequireSiteId();
        var pageSize = Math.Clamp(input.PageSize <= 0 ? 20 : input.PageSize, 1, 100);
        var page = Math.Max(1, input.Page <= 0 ? 1 : input.Page);
        var query = _db.Queryable<SiteAdminLog>().Where(l => l.SiteID == siteId);
        if (input.AdminID > 0)
            query = query.Where(l => l.AdminID == input.AdminID);
        if (input.Type > 0)
            query = query.Where(l => l.Type == input.Type);
        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderBy(l => l.ID, OrderByType.Desc)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new SiteAdminLogPageOutput
        {
            Total = total,
            Items = rows.Select(l => new SiteAdminLogItemDto
            {
                ID = l.ID,
                AdminID = l.AdminID,
                Type = l.Type,
                Content = l.Content,
                IP = l.IP,
                CreateAt = l.CreateAt
            }).ToList()
        };
    }

    /// <summary>页面配置列表。</summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行列表</returns>
    public async Task<List<SitePageSettingItemDto>> ListPageSettingsAsync(CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<SitePageSetting>()
            .Where(p => p.SiteID == siteId).OrderBy(p => p.Sort).OrderBy(p => p.ID)
            .Take(500).ToListAsync(cancellationToken);
        return rows.Select(p => new SitePageSettingItemDto
        {
            ID = p.ID,
            ControlID = p.ControlID,
            ItemID = p.ItemID,
            Sort = p.Sort,
            IsHide = p.IsHide
        }).ToList();
    }

    /// <summary>保存页面配置显隐与排序。</summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SavePageSettingAsync(SitePageSettingSaveInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.ID <= 0)
            throw Oops.Oh("#参数错误#");
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<SitePageSetting>()
            .Where(p => p.SiteID == siteId && p.ID == input.ID).Take(1).ToListAsync(cancellationToken);
        var row = rows.FirstOrDefault() ?? throw Oops.Oh("#记录不存在#");
        row.Sort = input.Sort;
        row.IsHide = input.IsHide;
        await _db.Updateable(row).UpdateColumns(p => new { p.Sort, p.IsHide }).ExecuteCommandAsync(cancellationToken);
    }
}

/// <summary>管理员筛选。</summary>
public sealed class SiteAdminPageInput
{
    /// <summary>页码。</summary>
    public int Page { get; set; } = 1;

    /// <summary>每页。</summary>
    public int PageSize { get; set; } = 20;

    /// <summary>用户名。</summary>
    public string AdminName { get; set; }

    /// <summary>分组；0=全部。</summary>
    public int GroupID { get; set; }
}

/// <summary>管理员分页。</summary>
public sealed class SiteAdminPageOutput
{
    /// <summary>总数。</summary>
    public int Total { get; set; }

    /// <summary>行。</summary>
    public List<SiteAdminItemDto> Items { get; set; }
}

/// <summary>管理员行。</summary>
public sealed class SiteAdminItemDto
{
    /// <summary>编号。</summary>
    public int ID { get; set; }

    /// <summary>用户名。</summary>
    public string AdminName { get; set; }

    /// <summary>昵称。</summary>
    public string NickName { get; set; }

    /// <summary>状态。</summary>
    public int Status { get; set; }

    /// <summary>分组。</summary>
    public int GroupID { get; set; }

    /// <summary>上次登录。</summary>
    public DateTime LoginAt { get; set; }

    /// <summary>登录 IP。</summary>
    public string LoginIP { get; set; }

    /// <summary>创建。</summary>
    public DateTime CreateAt { get; set; }

    /// <summary>是否已绑谷歌。</summary>
    public bool HasTotp { get; set; }
}

/// <summary>保存管理员。</summary>
public sealed class SiteAdminSaveInput
{
    /// <summary>编号；0=新建。</summary>
    public int ID { get; set; }

    /// <summary>用户名。</summary>
    public string AdminName { get; set; }

    /// <summary>昵称。</summary>
    public string NickName { get; set; }

    /// <summary>密码；编辑时可空。</summary>
    public string Password { get; set; }

    /// <summary>状态。</summary>
    public int Status { get; set; }

    /// <summary>分组。</summary>
    public int GroupID { get; set; }

    /// <summary>加款上限。</summary>
    public decimal AuditUpperLimit { get; set; }
}

/// <summary>重置密码。</summary>
public sealed class SiteAdminResetPwdInput
{
    /// <summary>管理员编号。</summary>
    public int ID { get; set; }

    /// <summary>新密码。</summary>
    public string Password { get; set; }
}

/// <summary>分组行。</summary>
public sealed class SiteAdminGroupItemDto
{
    /// <summary>编号。</summary>
    public int ID { get; set; }

    /// <summary>名称。</summary>
    public string Name { get; set; }

    /// <summary>权限原文。</summary>
    public string PermissionString { get; set; }

    /// <summary>人数。</summary>
    public int Member { get; set; }

    /// <summary>创建。</summary>
    public DateTime CreateAt { get; set; }
}

/// <summary>保存分组。</summary>
public sealed class SiteAdminGroupSaveInput
{
    /// <summary>编号；0=新建。</summary>
    public int ID { get; set; }

    /// <summary>名称。</summary>
    public string Name { get; set; }

    /// <summary>权限原文。</summary>
    public string PermissionString { get; set; }
}

/// <summary>操作日志筛选。</summary>
public sealed class SiteAdminLogPageInput
{
    /// <summary>页码。</summary>
    public int Page { get; set; } = 1;

    /// <summary>每页。</summary>
    public int PageSize { get; set; } = 20;

    /// <summary>管理员。</summary>
    public int AdminID { get; set; }

    /// <summary>类型；0=全部。</summary>
    public int Type { get; set; }
}

/// <summary>操作日志分页。</summary>
public sealed class SiteAdminLogPageOutput
{
    /// <summary>总数。</summary>
    public int Total { get; set; }

    /// <summary>行。</summary>
    public List<SiteAdminLogItemDto> Items { get; set; }
}

/// <summary>操作日志行。</summary>
public sealed class SiteAdminLogItemDto
{
    /// <summary>编号。</summary>
    public int ID { get; set; }

    /// <summary>管理员。</summary>
    public int AdminID { get; set; }

    /// <summary>类型。</summary>
    public int Type { get; set; }

    /// <summary>内容。</summary>
    public string Content { get; set; }

    /// <summary>IP。</summary>
    public string IP { get; set; }

    /// <summary>时间。</summary>
    public DateTime CreateAt { get; set; }
}

/// <summary>页面配置行。</summary>
public sealed class SitePageSettingItemDto
{
    /// <summary>编号。</summary>
    public int ID { get; set; }

    /// <summary>控件。</summary>
    public int ControlID { get; set; }

    /// <summary>菜单项。</summary>
    public int ItemID { get; set; }

    /// <summary>排序。</summary>
    public short Sort { get; set; }

    /// <summary>是否隐藏。</summary>
    public bool IsHide { get; set; }
}

/// <summary>保存页面配置。</summary>
public sealed class SitePageSettingSaveInput
{
    /// <summary>编号。</summary>
    public int ID { get; set; }

    /// <summary>排序。</summary>
    public short Sort { get; set; }

    /// <summary>是否隐藏。</summary>
    public bool IsHide { get; set; }
}
