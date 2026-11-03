using SimpX.Net.Data.Entity.Games;
using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 游戏会员与额度记录只读。
/// </summary>
public sealed partial class GameUserQueryService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;

    /// <summary>
    /// 注入依赖。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站</param>
    public GameUserQueryService(IBizDbAccessor biz, ICurrentSiteAccessor site)
    {
        _db = biz.Db;
        _site = site;
    }

    /// <summary>
    /// 游戏会员分页。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页</returns>
    public async Task<GameUserPageOutput> PageUsersAsync(
        GameUserPageInput input,
        CancellationToken cancellationToken = default)
    {
        input ??= new GameUserPageInput();
        var siteId = _site.RequireSiteId();
        var pageSize = Math.Clamp(input.PageSize <= 0 ? 20 : input.PageSize, 1, 100);
        var page = Math.Max(1, input.Page <= 0 ? 1 : input.Page);
        var query = _db.Queryable<GameUser>().Where(g => g.SiteID == siteId);
        if (input.UserID > 0)
            query = query.Where(g => g.UserID == input.UserID);
        if (input.Game > 0)
            query = query.Where(g => g.Game == input.Game);
        if (!string.IsNullOrWhiteSpace(input.PlayerName))
            query = query.Where(g => g.PlayerName.Contains(input.PlayerName.Trim()));

        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderBy(g => g.UpdateAt, OrderByType.Desc)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        var ids = rows.Select(r => r.UserID).Distinct().ToList();
        var nameMap = new Dictionary<int, string>();
        if (ids.Count > 0)
        {
            var names = await _db.Queryable<User>()
                .Where(u => u.SiteID == siteId && ids.Contains(u.ID))
                .Select(u => new { u.ID, u.UserName }).ToListAsync(cancellationToken);
            foreach (var n in names)
                nameMap[n.ID] = n.UserName;
        }

        return new GameUserPageOutput
        {
            Total = total,
            Items = rows.Select(g => new GameUserItemDto
            {
                UserID = g.UserID,
                UserName = nameMap.TryGetValue(g.UserID, out var un) ? un : string.Empty,
                Game = g.Game,
                Currency = g.Currency,
                PlayerName = g.PlayerName,
                Balance = g.Balance,
                Withdraw = g.Withdraw,
                UpdateAt = g.UpdateAt,
                IsTest = g.IsTest
            }).ToList()
        };
    }

    /// <summary>
    /// 额度转账记录分页。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页</returns>
    public async Task<GameTransferPageOutput> PageTransfersAsync(
        GameTransferPageInput input,
        CancellationToken cancellationToken = default)
    {
        input ??= new GameTransferPageInput();
        var siteId = _site.RequireSiteId();
        var pageSize = Math.Clamp(input.PageSize <= 0 ? 20 : input.PageSize, 1, 100);
        var page = Math.Max(1, input.Page <= 0 ? 1 : input.Page);
        var query = _db.Queryable<GameTransferOrder>().Where(o => o.SiteID == siteId);
        if (input.OrderID > 0)
            query = query.Where(o => o.ID == input.OrderID);
        if (input.UserID > 0)
            query = query.Where(o => o.UserID == input.UserID);
        if (!string.IsNullOrWhiteSpace(input.User))
        {
            var name = input.User.Trim();
            var userIds = await _db.Queryable<User>()
                .Where(u => u.SiteID == siteId && u.UserName == name)
                .Select(u => u.ID)
                .Take(1)
                .ToListAsync(cancellationToken);
            query = query.Where(o => o.UserID == userIds.FirstOrDefault());
        }
        if (input.Type > 0)
            query = query.Where(o => o.Type == input.Type);
        if (input.Currency >= 0)
            query = query.Where(o => o.Currency == input.Currency);
        if (input.Status >= 0)
            query = query.Where(o => o.Status == input.Status);
        if (input.Action > 0)
            query = query.Where(o => o.Action == input.Action);
        if (input.StartAt.HasValue)
            query = query.Where(o => o.CreateAt >= input.StartAt.Value);
        if (input.EndAt.HasValue)
            query = query.Where(o => o.CreateAt < input.EndAt.Value.Date.AddDays(1));

        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderBy(o => o.ID, OrderByType.Desc)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        var ids = rows.Select(r => r.UserID).Distinct().ToList();
        var nameMap = new Dictionary<int, string>();
        if (ids.Count > 0)
        {
            var names = await _db.Queryable<User>()
                .Where(u => u.SiteID == siteId && ids.Contains(u.ID))
                .Select(u => new { u.ID, u.UserName }).ToListAsync(cancellationToken);
            foreach (var n in names)
                nameMap[n.ID] = n.UserName;
        }

        return new GameTransferPageOutput
        {
            Total = total,
            Items = rows.Select(o => new GameTransferItemDto
            {
                ID = o.ID,
                UserID = o.UserID,
                UserName = nameMap.TryGetValue(o.UserID, out var un) ? un : string.Empty,
                Type = o.Type,
                Money = o.Money,
                Status = o.Status,
                Action = o.Action,
                Currency = o.Currency,
                Description = o.Description,
                ResultID = o.ResultID,
                CreateAt = o.CreateAt,
                CheckAt = o.CheckAt
            }).ToList()
        };
    }
}

/// <summary>游戏会员筛选。</summary>
public sealed class GameUserPageInput
{
    /// <summary>页码。</summary>
    public int Page { get; set; } = 1;

    /// <summary>每页。</summary>
    public int PageSize { get; set; } = 20;

    /// <summary>会员。</summary>
    public int UserID { get; set; }

    /// <summary>游戏。</summary>
    public int Game { get; set; }

    /// <summary>第三方帐号。</summary>
    public string PlayerName { get; set; }
}

/// <summary>游戏会员分页。</summary>
public sealed class GameUserPageOutput
{
    /// <summary>总数。</summary>
    public int Total { get; set; }

    /// <summary>行。</summary>
    public List<GameUserItemDto> Items { get; set; }
}

/// <summary>游戏会员行。</summary>
public sealed class GameUserItemDto
{
    /// <summary>会员。</summary>
    public int UserID { get; set; }

    /// <summary>用户名。</summary>
    public string UserName { get; set; }

    /// <summary>游戏。</summary>
    public int Game { get; set; }

    /// <summary>币种。</summary>
    public int Currency { get; set; }

    /// <summary>第三方帐号。</summary>
    public string PlayerName { get; set; }

    /// <summary>第三方密码。</summary>
    public string PlayerPass { get; set; }

    /// <summary>余额。</summary>
    public decimal Balance { get; set; }

    /// <summary>可转出。</summary>
    public decimal Withdraw { get; set; }

    /// <summary>创建时间。</summary>
    public DateTime CreateAt { get; set; }

    /// <summary>更新时间。</summary>
    public DateTime UpdateAt { get; set; }

    /// <summary>锁定位。</summary>
    public int Lock { get; set; }

    /// <summary>限红串。</summary>
    public string Limits { get; set; }

    /// <summary>限红类型。</summary>
    public int LimitType { get; set; }

    /// <summary>测试号。</summary>
    public bool IsTest { get; set; }
}

/// <summary>额度记录筛选。</summary>
public sealed class GameTransferPageInput
{
    /// <summary>页码。</summary>
    public int Page { get; set; } = 1;

    /// <summary>每页。</summary>
    public int PageSize { get; set; } = 20;

    /// <summary>订单号。</summary>
    public int OrderID { get; set; }

    /// <summary>会员。</summary>
    public int UserID { get; set; }

    /// <summary>用户名。</summary>
    public string User { get; set; }

    /// <summary>游戏类型。</summary>
    public int Type { get; set; }

    /// <summary>币种；-1=全部。</summary>
    public int Currency { get; set; } = -1;

    /// <summary>状态；-1=全部。</summary>
    public int Status { get; set; } = -1;

    /// <summary>方向；0=全部。</summary>
    public int Action { get; set; }

    /// <summary>开始。</summary>
    public DateTime? StartAt { get; set; }

    /// <summary>结束。</summary>
    public DateTime? EndAt { get; set; }
}

/// <summary>额度记录分页。</summary>
public sealed class GameTransferPageOutput
{
    /// <summary>总数。</summary>
    public int Total { get; set; }

    /// <summary>行。</summary>
    public List<GameTransferItemDto> Items { get; set; }
}

/// <summary>额度记录行。</summary>
public sealed class GameTransferItemDto
{
    /// <summary>订单号。</summary>
    public int ID { get; set; }

    /// <summary>会员。</summary>
    public int UserID { get; set; }

    /// <summary>用户名。</summary>
    public string UserName { get; set; }

    /// <summary>游戏类型。</summary>
    public int Type { get; set; }

    /// <summary>金额。</summary>
    public decimal Money { get; set; }

    /// <summary>状态。</summary>
    public int Status { get; set; }

    /// <summary>方向。</summary>
    public int Action { get; set; }

    /// <summary>币种。</summary>
    public int Currency { get; set; }

    /// <summary>备注。</summary>
    public string Description { get; set; }

    /// <summary>远程单号。</summary>
    public string ResultID { get; set; }

    /// <summary>创建。</summary>
    public DateTime CreateAt { get; set; }

    /// <summary>完成。</summary>
    public DateTime CheckAt { get; set; }
}
