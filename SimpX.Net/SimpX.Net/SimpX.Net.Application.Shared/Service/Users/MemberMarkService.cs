using SimpX.Net.Data;
using SimpX.Net.Data.Entity.Users;
using SqlSugar;
using System.Text.Json.Serialization;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 站点后台会员备注：list / add / delete；只操作当前站点。
/// </summary>
public sealed class MemberMarkService : ITransient
{
    /// <summary>会员列表权限（与 OldApi 备注接口一致）。</summary>
    public const string MemberListPermissionId = "1e7cc96020b843b988ed6df39024867e";

    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;
    private readonly SiteAdminAuthService _adminAuth;
    private readonly IPermissionChecker _permissionChecker;
    private readonly UserManager _userManager;

    /// <summary>
    /// 注入依赖。备注表在业务库。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站点</param>
    /// <param name="adminAuth">后台会话（OldApi）</param>
    /// <param name="permissionChecker">JWT 按钮权限</param>
    /// <param name="userManager">JWT 当前账号</param>
    public MemberMarkService(
        IBizDbAccessor biz,
        ICurrentSiteAccessor site,
        SiteAdminAuthService adminAuth,
        IPermissionChecker permissionChecker,
        UserManager userManager)
    {
        _db = biz.Db;
        _site = site;
        _adminAuth = adminAuth;
        _permissionChecker = permissionChecker;
        _userManager = userManager;
    }

    /// <summary>
    /// 备注列表。对应旧版 /admin/user/member/info/marklist：usr_Mark 合并 Content 含「#删除#」的 UserLog。
    /// </summary>
    /// <param name="userId">会员编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>旧版返回格式</returns>
    public async Task<OldApiResult> ListAsync(int userId, CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
            return OldApiResult.Fail("#用户不存在#");

        var siteId = _site.RequireSiteId();
        var exists = await _db.Queryable<User>()
            .Where(u => u.ID == userId && u.SiteID == siteId)
            .AnyAsync(cancellationToken);
        if (!exists)
            return OldApiResult.Fail("#用户不存在#");

        var marks = await _db.Queryable<UserMark>()
            .Where(m => m.SiteID == siteId && m.UserID == userId)
            .OrderBy(m => m.CreateAt, OrderByType.Desc)
            .Take(200)
            .ToListAsync(cancellationToken);

        var deleteLogs = await _db.Queryable<UserLog>()
            .Where(l => l.SiteID == siteId && l.UserID == userId && l.Content != null && l.Content.Contains("#删除#"))
            .OrderBy(l => l.CreateAt, OrderByType.Desc)
            .Take(200)
            .ToListAsync(cancellationToken);

        var merged = marks
            .Select(m => new MemberMarkItemDto
            {
                ID = m.ID,
                Content = AppendOperator(m.Content, m.AdminName),
                SiteID = m.SiteID,
                UserID = m.UserID,
                CreateAt = m.CreateAt
            })
            .Concat(deleteLogs.Select(l => new MemberMarkItemDto
            {
                ID = l.ID,
                Content = l.Content ?? string.Empty,
                SiteID = l.SiteID,
                UserID = l.UserID,
                CreateAt = l.CreateAt
            }))
            .OrderByDescending(x => x.CreateAt)
            .Take(200)
            .ToList();

        return OldApiResult.Ok(string.Empty, merged);
    }

    /// <summary>
    /// 添加备注。对应旧版 /admin/user/member/info/addmark。
    /// </summary>
    /// <param name="userId">会员编号</param>
    /// <param name="content">备注内容</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>旧版返回格式</returns>
    public async Task<OldApiResult> AddAsync(int userId, string content, CancellationToken cancellationToken = default)
    {
        content = content?.Trim();
        if (string.IsNullOrEmpty(content))
            return OldApiResult.Fail("#备注内容不能为空#");
        if (userId <= 0)
            return OldApiResult.Fail("#用户不存在#");

        var siteId = _site.RequireSiteId();
        var denied = await EnsureCanManageAsync(cancellationToken);
        if (denied != null)
            return denied;

        var adminName = await ResolveAdminDisplayNameAsync(cancellationToken);
        var exists = await _db.Queryable<User>()
            .Where(u => u.ID == userId && u.SiteID == siteId)
            .AnyAsync(cancellationToken);
        if (!exists)
            return OldApiResult.Fail("#该用户不存在#");

        var mark = new UserMark
        {
            SiteID = siteId,
            UserID = userId,
            Content = content,
            CreateAt = DateTime.Now,
            AdminName = adminName
        };
        var id = await _db.Insertable(mark).ExecuteReturnIdentityAsync(cancellationToken);
        if (id <= 0)
            return OldApiResult.Fail("#操作失败#");

        return OldApiResult.Ok("#操作成功#");
    }

    /// <summary>
    /// 删除备注。对应旧版 /admin/user/member/info/deletemark。
    /// </summary>
    /// <param name="markId">备注编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>旧版返回格式</returns>
    public async Task<OldApiResult> DeleteAsync(int markId, CancellationToken cancellationToken = default)
    {
        if (markId <= 0)
            return OldApiResult.Fail("#操作失败#");

        var siteId = _site.RequireSiteId();
        var denied = await EnsureCanManageAsync(cancellationToken);
        if (denied != null)
            return denied;

        var rows = await _db.Queryable<UserMark>()
            .Where(m => m.ID == markId && m.SiteID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        if (rows.Count == 0)
            return OldApiResult.Fail("#操作失败#");

        var mark = rows[0];
        var adminName = await ResolveAdminDisplayNameAsync(cancellationToken);
        await _db.Insertable(new UserLog
        {
            SiteID = siteId,
            UserID = mark.UserID,
            Content = $"#删除# {(mark.Content ?? string.Empty)} 操作员:{adminName}",
            CreateAt = DateTime.Now
        }).ExecuteCommandAsync(cancellationToken);

        var n = await _db.Deleteable<UserMark>()
            .Where(m => m.ID == markId && m.SiteID == siteId)
            .ExecuteCommandAsync(cancellationToken);
        return n > 0 ? OldApiResult.Ok("#操作成功#") : OldApiResult.Fail("#操作失败#");
    }

    /// <summary>
    /// OldApi 站点管理员或 JWT 具备会员列表权限才可写备注。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>拒绝结果；允许则为 null</returns>
    private async Task<OldApiResult> EnsureCanManageAsync(CancellationToken cancellationToken)
    {
        var admin = await _adminAuth.TryGetCurrentAdminAsync(cancellationToken);
        if (admin != null)
            return null;

        if (_userManager.UserId <= 0)
            return OldApiResult.NeedLogin();

        if (await _permissionChecker.IsGrantedAsync(MemberListPermissionId))
            return null;

        return OldApiResult.NeedPermission(
            OldApiPermissionEvaluator.BuildPermissionDeniedMessage(MemberListPermissionId));
    }

    /// <summary>
    /// 取操作员展示名：优先 OldApi 管理员名，否则 JWT 账号。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>展示名</returns>
    private async Task<string> ResolveAdminDisplayNameAsync(CancellationToken cancellationToken)
    {
        var admin = await _adminAuth.TryGetCurrentAdminAsync(cancellationToken);
        if (admin != null)
            return admin.AdminName ?? string.Empty;
        return _userManager.Account ?? _userManager.RealName ?? string.Empty;
    }

    /// <summary>
    /// 备注内容后附操作员名（展示方式跟旧版一样）。
    /// </summary>
    private static string AppendOperator(string content, string adminName)
    {
        if (string.IsNullOrEmpty(adminName))
            return content ?? string.Empty;
        return (content ?? string.Empty) + "<br>操作员:" + adminName;
    }
}

/// <summary>会员备注列表行。</summary>
public sealed class MemberMarkItemDto
{
    /// <summary>备注编号。</summary>
    [JsonPropertyName("ID")]
    public int ID { get; set; }

    /// <summary>内容（可含操作员后缀）。</summary>
    [JsonPropertyName("Content")]
    public string Content { get; set; }

    /// <summary>站点编号。</summary>
    [JsonPropertyName("SiteID")]
    public int SiteID { get; set; }

    /// <summary>会员编号。</summary>
    [JsonPropertyName("UserID")]
    public int UserID { get; set; }

    /// <summary>创建时间。</summary>
    [JsonPropertyName("CreateAt")]
    public DateTime CreateAt { get; set; }
}
