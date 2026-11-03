using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 活动参与日志查询（租户后台列表）。
/// </summary>
public sealed partial class EventLogService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;

    /// <summary>
    /// 注入库与定站。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">定站</param>
    public EventLogService(IBizDbAccessor biz, ICurrentSiteAccessor site)
    {
        _db = biz.Db;
        _site = site;
    }

    /// <summary>
    /// 列表（最多 500 条）。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行列表</returns>
    public async Task<List<EventLogListItemDto>> ListAsync(
        EventLogListInput input,
        CancellationToken cancellationToken = default)
    {
        input ??= new EventLogListInput();
        var siteId = _site.RequireSiteId();
        var query = _db.Queryable<EventLog>().Where(l => l.SiteID == siteId);

        if (!string.IsNullOrWhiteSpace(input.Status) && int.TryParse(input.Status.Trim(), out var st))
            query = query.Where(l => l.Status == st);
        if (input.Type.HasValue)
            query = query.Where(l => l.Type == input.Type.Value);
        if (!string.IsNullOrWhiteSpace(input.SourceID))
        {
            var src = input.SourceID.Trim();
            query = query.Where(l => l.SourceID == src);
        }

        if (input.StartAt.HasValue && input.StartAt.Value != default)
            query = query.Where(l => l.CreateAt >= input.StartAt.Value);
        if (input.EndAt.HasValue && input.EndAt.Value != default)
            query = query.Where(l => l.CreateAt < input.EndAt.Value.Date.AddDays(1));

        if (!string.IsNullOrWhiteSpace(input.User))
        {
            var name = input.User.Trim();
            var users = await _db.Queryable<User>()
                .Where(u => u.SiteID == siteId && u.UserName == name)
                .Take(1)
                .ToListAsync(cancellationToken);
            var uid = users.FirstOrDefault()?.ID ?? -1;
            query = query.Where(l => l.UserID == uid);
        }

        var rows = await query
            .OrderBy(l => l.CreateAt, OrderByType.Desc)
            .Take(500)
            .ToListAsync(cancellationToken);

        var userIds = rows.Select(r => r.UserID).Distinct().ToList();
        var nameMap = new Dictionary<int, string>();
        if (userIds.Count > 0)
        {
            var users = await _db.Queryable<User>()
                .Where(u => u.SiteID == siteId && userIds.Contains(u.ID))
                .Select(u => new { u.ID, u.UserName })
                .ToListAsync(cancellationToken);
            foreach (var u in users)
                nameMap[u.ID] = u.UserName;
        }

        var eventIds = rows.Select(r => r.EventID).Distinct().ToList();
        var titleMap = new Dictionary<int, string>();
        if (eventIds.Count > 0)
        {
            var events = await _db.Queryable<SiteEvent>()
                .Where(e => e.SiteID == siteId && eventIds.Contains(e.ID))
                .Select(e => new { e.ID, e.Title })
                .ToListAsync(cancellationToken);
            foreach (var e in events)
                titleMap[e.ID] = e.Title;
        }

        return rows.Select(r => new EventLogListItemDto
        {
            ID = r.ID,
            EventID = r.EventID,
            Type = r.Type,
            UserID = r.UserID,
            UserName = nameMap.TryGetValue(r.UserID, out var un) ? un : string.Empty,
            Reward = r.Reward,
            Flow = r.Flow,
            Status = r.Status,
            Put = r.Put,
            IsAuto = r.IsAuto,
            Currency = r.Currency,
            SourceID = r.SourceID,
            CreateAt = r.CreateAt,
            CheckMessage = r.CheckMessage,
            EventTitle = titleMap.TryGetValue(r.EventID, out var t) ? t : string.Empty
        }).ToList();
    }

    // --- merged from EventLogService.WaveGaps.cs ---



    /// <summary>
    /// 对齐旧 info。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> InfoAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return InfoCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 manualsigninrewart。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> ManualsigninrewartAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return ManualsigninrewartCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 save。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> SaveAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return SaveCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 savebet。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> SavebetAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return SavebetCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 savelotterycount。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> SavelotterycountAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return SavelotterycountCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 saveredenvelopecount。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> SaveredenvelopecountAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return SaveredenvelopecountCoreAsync(input, cancellationToken);
    }


    /// <summary>核心：info —— 单条参与日志。</summary>
    private async Task<object?> InfoCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var id = ReadInt(input, "ID", "Id", "LogID");
        if (id <= 0)
            throw Oops.Oh("#ID不存在#");
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<EventLog>()
            .Where(l => l.SiteID == siteId && l.ID == id)
            .Take(1)
            .ToListAsync(cancellationToken);
        var log = rows.FirstOrDefault() ?? throw Oops.Oh("#ID不存在#");
        var events = await _db.Queryable<SiteEvent>()
            .Where(e => e.SiteID == siteId && e.ID == log.EventID)
            .Take(1)
            .ToListAsync(cancellationToken);
        var ev = events.FirstOrDefault() ?? throw Oops.Oh("#活动已被删除#");
        var users = await _db.Queryable<User>()
            .Where(u => u.SiteID == siteId && u.ID == log.UserID)
            .Take(1)
            .ToListAsync(cancellationToken);
        return new
        {
            log.ID,
            log.UserID,
            log.EventID,
            Event = ev.Title ?? string.Empty,
            UserName = users.FirstOrDefault()?.UserName ?? string.Empty,
            Type = log.Type,
            StatusVlaue = log.Status,
            Status = log.Status,
            Put = log.Put,
            IP = log.IP ?? string.Empty,
            IPAddress = string.Empty,
            ErrorMsg = log.CheckMessage ?? string.Empty,
            log.CreateAt,
            ExpectedAt = log.ExpectedAt,
            ReleaseAt = log.ReleaseAt,
            log.Reward,
            log.Flow
        };
    }


    /// <summary>核心：manualsigninrewart</summary>
    private Task<object?> ManualsigninrewartCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        throw Oops.Oh("#功能尚未接通#");
    }


    /// <summary>核心：save</summary>
    private Task<object?> SaveCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        throw Oops.Oh("#功能尚未接通#");
    }


    /// <summary>核心：savebet</summary>
    private Task<object?> SavebetCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        throw Oops.Oh("#功能尚未接通#");
    }


    /// <summary>核心：savelotterycount</summary>
    private Task<object?> SavelotterycountCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        throw Oops.Oh("#功能尚未接通#");
    }


    /// <summary>核心：saveredenvelopecount</summary>
    private Task<object?> SaveredenvelopecountCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        throw Oops.Oh("#功能尚未接通#");
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
