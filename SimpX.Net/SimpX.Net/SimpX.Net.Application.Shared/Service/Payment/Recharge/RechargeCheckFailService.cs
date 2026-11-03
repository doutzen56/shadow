using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 比对失败充值订单只读。对齐旧 recharge 列表 IsCheckFail=true。
/// </summary>
public sealed class RechargeCheckFailService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;

    /// <summary>注入。</summary>
    public RechargeCheckFailService(IBizDbAccessor biz, ICurrentSiteAccessor site)
    {
        _db = biz.Db;
        _site = site;
    }

    /// <summary>分页列出比对失败单。</summary>
    public async Task<RechargeCheckFailPageOutput> PageAsync(
        RechargeCheckFailPageInput input,
        CancellationToken cancellationToken = default)
    {
        input ??= new RechargeCheckFailPageInput();
        var siteId = _site.RequireSiteId();
        var page = input.Page < 1 ? 1 : input.Page;
        var size = input.PageSize is < 1 or > 200 ? 20 : input.PageSize;

        var query = _db.Queryable<RechargeOrder>()
            .Where(o => o.SiteID == siteId && o.IsCheckFail && !o.IsDeleted);
        if (!string.IsNullOrWhiteSpace(input.OrderID))
        {
            var oid = input.OrderID.Trim();
            query = query.Where(o => o.ID != null && o.ID.Contains(oid));
        }
        if (input.Currency is > 0)
            query = query.Where(o => o.Currency == input.Currency.Value);
        if (input.IsPay.HasValue)
            query = query.Where(o => o.IsPay == input.IsPay.Value);
        if (input.StartAt.HasValue && input.StartAt.Value != default)
            query = query.Where(o => o.CreateAt >= input.StartAt.Value);
        if (input.EndAt.HasValue && input.EndAt.Value != default)
            query = query.Where(o => o.CreateAt < input.EndAt.Value.Date.AddDays(1));
        if (!string.IsNullOrWhiteSpace(input.User))
        {
            var name = input.User.Trim();
            if (int.TryParse(name, out var uidExact))
                query = query.Where(o => o.UserID == uidExact);
            else
            {
                var uids = await _db.Queryable<User>()
                    .Where(u => u.SiteID == siteId && u.UserName.Contains(name))
                    .Select(u => u.ID)
                    .Take(500)
                    .ToListAsync(cancellationToken);
                if (uids.Count == 0)
                    return new RechargeCheckFailPageOutput { Items = [], Total = 0 };
                query = query.Where(o => uids.Contains(o.UserID));
            }
        }
        if (!string.IsNullOrWhiteSpace(input.Agent))
        {
            var agentName = input.Agent.Trim();
            var agentIds = await _db.Queryable<User>()
                .Where(u => u.SiteID == siteId && u.UserName.Contains(agentName))
                .Select(u => u.ID)
                .Take(500)
                .ToListAsync(cancellationToken);
            if (agentIds.Count == 0)
                return new RechargeCheckFailPageOutput { Items = [], Total = 0 };
            var memberIds = await _db.Queryable<User>()
                .Where(u => u.SiteID == siteId && agentIds.Contains(u.AgentID))
                .Select(u => u.ID)
                .Take(2000)
                .ToListAsync(cancellationToken);
            if (memberIds.Count == 0)
                return new RechargeCheckFailPageOutput { Items = [], Total = 0 };
            query = query.Where(o => memberIds.Contains(o.UserID));
        }

        RefAsync<int> total = 0;
        var rows = await query
            .OrderBy(o => o.CreateAt, OrderByType.Desc)
            .ToPageListAsync(page, size, total, cancellationToken);

        var names = new Dictionary<int, string>();
        var userAgentMap = new Dictionary<int, int>();
        var ids = rows.Select(r => r.UserID).Distinct().ToList();
        if (ids.Count > 0)
        {
            var users = await _db.Queryable<User>()
                .Where(u => u.SiteID == siteId && ids.Contains(u.ID))
                .Select(u => new { u.ID, u.UserName, u.AgentID })
                .ToListAsync(cancellationToken);
            foreach (var u in users)
            {
                names[u.ID] = u.UserName ?? string.Empty;
                userAgentMap[u.ID] = u.AgentID;
            }
        }

        var parentIds = userAgentMap.Values.Where(id => id > 0).Distinct().ToList();
        var agentNames = parentIds.Count == 0
            ? new Dictionary<int, string>()
            : (await _db.Queryable<User>()
                .Where(u => u.SiteID == siteId && parentIds.Contains(u.ID))
                .Select(u => new { u.ID, u.UserName })
                .ToListAsync(cancellationToken))
            .ToDictionary(x => x.ID, x => x.UserName ?? string.Empty);

        var payIds = rows.Select(r => r.PayID).Where(id => id > 0).Distinct().ToList();
        var payNames = payIds.Count == 0
            ? new Dictionary<int, string>()
            : (await _db.Queryable<SitePayment>()
                .Where(p => p.SiteID == siteId && payIds.Contains(p.ID))
                .Select(p => new { p.ID, p.Name })
                .ToListAsync(cancellationToken))
            .ToDictionary(x => x.ID, x => x.Name ?? string.Empty);

        return new RechargeCheckFailPageOutput
        {
            Total = total,
            Items = rows.Select(o =>
            {
                var aid = userAgentMap.GetValueOrDefault(o.UserID, 0);
                return new RechargeCheckFailListItemDto
                {
                    OrderID = o.ID,
                    UserID = o.UserID,
                    UserName = names.GetValueOrDefault(o.UserID, string.Empty),
                    AgentID = aid,
                    AgentName = aid > 0 ? agentNames.GetValueOrDefault(aid, string.Empty) : string.Empty,
                    Money = o.Money,
                    Paid = o.Paid,
                    IsPay = o.IsPay,
                    Count = o.Count,
                    PayAt = o.PayAt,
                    PayID = o.PayID,
                    PayName = payNames.GetValueOrDefault(o.PayID, string.Empty),
                    Currency = o.Currency,
                    CreateAt = o.CreateAt,
                    Remark = o.Remark ?? string.Empty
                };
            }).ToList()
        };
    }
}

/// <summary>比对失败分页入参。</summary>
public sealed class RechargeCheckFailPageInput
{
    /// <summary>页码。</summary>
    public int Page { get; set; } = 1;

    /// <summary>每页条数。</summary>
    public int PageSize { get; set; } = 20;

    /// <summary>订单号。</summary>
    public string OrderID { get; set; }

    /// <summary>会员名。</summary>
    public string User { get; set; }

    /// <summary>上级代理用户名。</summary>
    public string Agent { get; set; }

    /// <summary>币种；空=全部。</summary>
    public int? Currency { get; set; }

    /// <summary>是否到帐；空=全部。</summary>
    public bool? IsPay { get; set; }

    /// <summary>创建起。</summary>
    public DateTime? StartAt { get; set; }

    /// <summary>创建止。</summary>
    public DateTime? EndAt { get; set; }
}

/// <summary>比对失败分页出参。</summary>
public sealed class RechargeCheckFailPageOutput
{
    /// <summary>总数。</summary>
    public int Total { get; set; }

    /// <summary>行。</summary>
    public List<RechargeCheckFailListItemDto> Items { get; set; } = [];
}

/// <summary>比对失败行。</summary>
public sealed class RechargeCheckFailListItemDto
{
    /// <summary>订单号。</summary>
    public string OrderID { get; set; }

    /// <summary>会员编号。</summary>
    public int UserID { get; set; }

    /// <summary>会员名。</summary>
    public string UserName { get; set; }

    /// <summary>上级代理编号。</summary>
    public int AgentID { get; set; }

    /// <summary>上级代理名。</summary>
    public string AgentName { get; set; }

    /// <summary>金额。</summary>
    public decimal Money { get; set; }

    /// <summary>实付。</summary>
    public decimal Paid { get; set; }

    /// <summary>是否已付。</summary>
    public bool IsPay { get; set; }

    /// <summary>第几次到帐。</summary>
    public int Count { get; set; }

    /// <summary>到帐时间。</summary>
    public DateTime PayAt { get; set; }

    /// <summary>渠道。</summary>
    public int PayID { get; set; }

    /// <summary>渠道名。</summary>
    public string PayName { get; set; }

    /// <summary>币种。</summary>
    public int Currency { get; set; }

    /// <summary>创建时间。</summary>
    public DateTime CreateAt { get; set; }

    /// <summary>备注。</summary>
    public string Remark { get; set; }
}
