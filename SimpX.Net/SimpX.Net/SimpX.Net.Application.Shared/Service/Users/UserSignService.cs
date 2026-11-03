using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 租户后台签到记录（只读列表 + 手工补录标记，不发奖）。对齐旧 /report/user/signin。
/// </summary>
public sealed partial class UserSignService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;

    /// <summary>
    /// 注入依赖。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站</param>
    public UserSignService(IBizDbAccessor biz, ICurrentSiteAccessor site)
    {
        _db = biz.Db;
        _site = site;
    }

    /// <summary>
    /// 分页列表。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页</returns>
    public async Task<UserSignPageOutput> PageAsync(
        UserSignPageInput input,
        CancellationToken cancellationToken = default)
    {
        input ??= new UserSignPageInput();
        var siteId = _site.RequireSiteId();
        var page = input.Page <= 0 ? 1 : input.Page;
        var pageSize = input.PageSize <= 0 ? 20 : Math.Min(input.PageSize, 100);

        var query = _db.Queryable<UserSign, User>((s, u) => new JoinQueryInfos(
                JoinType.Left, s.UserID == u.ID && s.SiteID == u.SiteID))
            .Where((s, u) => s.SiteID == siteId);

        if (input.UserID > 0)
            query = query.Where((s, u) => s.UserID == input.UserID);
        if (!string.IsNullOrWhiteSpace(input.UserName))
            query = query.Where((s, u) => u.UserName.Contains(input.UserName.Trim()));
        if (input.StartAt.HasValue)
        {
            var start = input.StartAt.Value;
            query = query.Where((s, u) => s.CreateAt >= start);
        }
        if (input.EndAt.HasValue)
        {
            var end = input.EndAt.Value;
            query = query.Where((s, u) => s.CreateAt <= end);
        }

        RefAsync<int> total = 0;
        var rows = await query.OrderBy((s, u) => s.CreateAt, OrderByType.Desc)
            .Select((s, u) => new UserSignListItemDto
            {
                ID = s.ID,
                UserID = s.UserID,
                UserName = u.UserName,
                IsAdd = s.IsAdd,
                CreateAt = s.CreateAt
            })
            .ToPageListAsync(page, pageSize, total, cancellationToken);

        return new UserSignPageOutput { Items = rows, Total = total };
    }

    /// <summary>
    /// 手工补一条签到记录（仅记库，不发奖）。
    /// </summary>
    /// <param name="userId">会员编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>编号</returns>
    public async Task<int> AddManualAsync(int userId, CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
            throw Oops.Oh("#请选择会员#");
        var siteId = _site.RequireSiteId();
        var ok = await _db.Queryable<User>()
            .AnyAsync(u => u.SiteID == siteId && u.ID == userId, cancellationToken);
        if (!ok)
            throw Oops.Oh("#会员不存在#");

        return await _db.Insertable(new UserSign
        {
            SiteID = siteId,
            UserID = userId,
            IsAdd = true,
            CreateAt = DateTime.Now
        }).ExecuteReturnIdentityAsync(cancellationToken);
    }

    // --- merged from UserSignService.WaveGaps.cs ---



    /// <summary>
    /// 对齐旧 signinlog。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> SigninlogAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return SigninlogCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 signinlogreport。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> SigninlogreportAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return SigninlogreportCoreAsync(input, cancellationToken);
    }


    /// <summary>核心：signinlog</summary>
    private async Task<object?> SigninlogCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        return await PageAsync(new UserSignPageInput
        {
            Page = Math.Max(1, ReadInt(input, "PageIndex", "page", "Page")),
            PageSize = Math.Clamp(ReadInt(input, "PageSize", "pagesize") is var ps && ps > 0 ? ps : 20, 1, 100),
            UserID = ReadInt(input, "UserID", "UserId"),
            UserName = ReadString(input, "UserName", "User", "user")
        }, cancellationToken);
    }


    /// <summary>核心：signinlogreport</summary>
    private Task<object?> SigninlogreportCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken) =>
        SigninlogCoreAsync(input, cancellationToken);


    private static string ReadString(Dictionary<string, object?> input, params string[] keys)
    {
        foreach (var k in keys)
        {
            if (!input.TryGetValue(k, out var v) || v is null) continue;
            var s = v.ToString();
            if (!string.IsNullOrEmpty(s)) return s!;
        }

        return string.Empty;
    }


    private static int ReadInt(Dictionary<string, object?> input, params string[] keys)
    {
        foreach (var k in keys)
        {
            if (!input.TryGetValue(k, out var v) || v is null) continue;
            if (v is int i) return i;
            if (int.TryParse(v.ToString(), out var n)) return n;
        }
        return 0;
    }
}

/// <summary>签到分页入参。</summary>
public sealed class UserSignPageInput
{
    /// <summary>页码。</summary>
    public int Page { get; set; } = 1;

    /// <summary>页大小。</summary>
    public int PageSize { get; set; } = 20;

    /// <summary>会员编号；0=全部。</summary>
    public int UserID { get; set; }

    /// <summary>用户名模糊。</summary>
    public string UserName { get; set; }

    /// <summary>开始时间（含）。</summary>
    public DateTime? StartAt { get; set; }

    /// <summary>结束时间（含）。</summary>
    public DateTime? EndAt { get; set; }
}

/// <summary>签到分页出参。</summary>
public sealed class UserSignPageOutput
{
    /// <summary>行。</summary>
    public List<UserSignListItemDto> Items { get; set; }

    /// <summary>总数。</summary>
    public int Total { get; set; }
}

/// <summary>签到列表行。</summary>
public sealed class UserSignListItemDto
{
    /// <summary>编号。</summary>
    public int ID { get; set; }

    /// <summary>会员编号。</summary>
    public int UserID { get; set; }

    /// <summary>用户名。</summary>
    public string UserName { get; set; }

    /// <summary>是否手工补录。</summary>
    public bool IsAdd { get; set; }

    /// <summary>时间。</summary>
    public DateTime CreateAt { get; set; }
}
