using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 线下入款审核。对齐旧 checklist / checkagree / checkreject 主路径（入账走账本，不做完整手续费拆分）。
/// </summary>
public sealed class BankRechargeCheckService : ITransient
{
    /// <summary>待审。</summary>
    public const int StatusNone = 0;

    /// <summary>拒绝。</summary>
    public const int StatusFailed = 1;

    /// <summary>通过。</summary>
    public const int StatusSuccess = 2;

    /// <summary>处理中。</summary>
    public const int StatusProcessing = 3;

    /// <summary>充值账变类型（对齐旧 Recharge）。</summary>
    public const int MoneyTypeRecharge = 1;

    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;
    private readonly IMoneyLedger _money;

    /// <summary>
    /// 注入库、定站、账本。
    /// </summary>
    public BankRechargeCheckService(IBizDbAccessor biz, ICurrentSiteAccessor site, IMoneyLedger money)
    {
        _db = biz.Db;
        _site = site;
        _money = money;
    }

    /// <summary>
    /// 入款审核列表（最多 500，兼容旧 JWT 调用）。
    /// </summary>
    public async Task<List<BankRechargeCheckListItemDto>> ListAsync(
        BankRechargeCheckListInput input,
        CancellationToken cancellationToken = default)
    {
        var page = await PageAsync(input, pageIndex: 1, pageSize: 500, cancellationToken);
        return page.Items;
    }

    /// <summary>
    /// 入款审核分页。对齐旧 checklist + ShowResult 总条数。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <param name="pageIndex">页码</param>
    /// <param name="pageSize">页大小</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>总条数与当前页</returns>
    public async Task<BankRechargeCheckPageOutput> PageAsync(
        BankRechargeCheckListInput input,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        input ??= new BankRechargeCheckListInput();
        var siteId = _site.RequireSiteId();
        // 对齐旧 checklist：不过滤 IsDeleted（旧 BDC 查询未加软删条件）
        var query = _db.Queryable<BankRechargeOrder>()
            .Where(o => o.SiteID == siteId);

        if (OldApiMoneyDisplay.TryParseCheckStatus(input.Status, out var st))
            query = query.Where(o => o.Status == st);
        if (input.PayID > 0)
            query = query.Where(o => o.PayID == input.PayID);
        if (input.Currency is > 0)
            query = query.Where(o => o.Currency == input.Currency.Value);
        if (!string.IsNullOrWhiteSpace(input.OrderID))
        {
            var oid = input.OrderID.Trim();
            query = query.Where(o => o.OrderID != null && o.OrderID.Contains(oid));
        }
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
                    return new BankRechargeCheckPageOutput { Items = [], RecordCount = 0 };
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
                return new BankRechargeCheckPageOutput { Items = [], RecordCount = 0 };
            var memberIds = await _db.Queryable<User>()
                .Where(u => u.SiteID == siteId && agentIds.Contains(u.AgentID))
                .Select(u => u.ID)
                .Take(2000)
                .ToListAsync(cancellationToken);
            if (memberIds.Count == 0)
                return new BankRechargeCheckPageOutput { Items = [], RecordCount = 0 };
            query = query.Where(o => memberIds.Contains(o.UserID));
        }

        var index = pageIndex < 1 ? 1 : pageIndex;
        var size = pageSize < 1 ? 20 : Math.Min(pageSize, 5000);
        RefAsync<int> total = 0;
        var rows = await query
            .OrderBy(o => o.CreateAt, OrderByType.Desc)
            .ToPageListAsync(index, size, total, cancellationToken);

        var names = await LoadNamesAsync(siteId, rows.Select(r => r.UserID).Distinct().ToList(), cancellationToken);
        var payIds = rows.Select(r => r.PayID).Where(id => id > 0).Distinct().ToList();
        var payNames = new Dictionary<int, string>();
        if (payIds.Count > 0)
        {
            var pays = await _db.Queryable<SimpX.Net.Data.Entity.Sites.SitePayment>()
                .Where(p => p.SiteID == siteId && payIds.Contains(p.ID))
                .Select(p => new { p.ID, p.Name })
                .ToListAsync(cancellationToken);
            foreach (var p in pays)
                payNames[p.ID] = p.Name ?? string.Empty;
        }

        var adminIds = rows.Select(r => r.AdminID).Where(id => id > 0).Distinct().ToList();
        var adminNames = new Dictionary<int, string>();
        if (adminIds.Count > 0)
        {
            var admins = await _db.Queryable<SimpX.Net.Data.Entity.Sites.SiteAdmin>()
                .Where(a => a.SiteID == siteId && adminIds.Contains(a.ID))
                .Select(a => new { a.ID, a.AdminName })
                .ToListAsync(cancellationToken);
            foreach (var a in admins)
                adminNames[a.ID] = a.AdminName ?? string.Empty;
        }

        return new BankRechargeCheckPageOutput
        {
            RecordCount = total.Value,
            Items = rows.Select(o => new BankRechargeCheckListItemDto
            {
                ID = o.ID,
                OrderID = o.OrderID ?? string.Empty,
                PayID = o.PayID,
                PayName = payNames.GetValueOrDefault(o.PayID, string.Empty),
                UserID = o.UserID,
                UserName = names.GetValueOrDefault(o.UserID, string.Empty),
                Money = o.Money,
                Paid = o.Paid,
                Status = o.Status,
                Currency = o.Currency,
                CreateAt = o.CreateAt,
                CheckAt = o.CheckAt,
                Name = o.Name ?? string.Empty,
                Description = o.Description ?? string.Empty,
                AdminName = adminNames.GetValueOrDefault(o.AdminID, string.Empty)
            }).ToList()
        };
    }

    /// <summary>
    /// 入款单详情。对齐旧 checkinfo。
    /// </summary>
    public async Task<BankRechargeCheckListItemDto> InfoAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw Oops.Oh("#编号错误#");
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<BankRechargeOrder>()
            .Where(o => o.SiteID == siteId && o.ID == id)
            .Take(1)
            .ToListAsync(cancellationToken);
        var o = rows.FirstOrDefault() ?? throw Oops.Oh("#编号错误#");
        var names = await LoadNamesAsync(siteId, [o.UserID], cancellationToken);
        var payName = string.Empty;
        if (o.PayID > 0)
        {
            var pays = await _db.Queryable<SimpX.Net.Data.Entity.Sites.SitePayment>()
                .Where(p => p.SiteID == siteId && p.ID == o.PayID)
                .Select(p => p.Name)
                .Take(1)
                .ToListAsync(cancellationToken);
            payName = pays.FirstOrDefault() ?? string.Empty;
        }

        var adminName = string.Empty;
        if (o.AdminID > 0)
        {
            var admins = await _db.Queryable<SimpX.Net.Data.Entity.Sites.SiteAdmin>()
                .Where(a => a.SiteID == siteId && a.ID == o.AdminID)
                .Select(a => a.AdminName)
                .Take(1)
                .ToListAsync(cancellationToken);
            adminName = admins.FirstOrDefault() ?? string.Empty;
        }

        // 旧 checkinfo 附带提现次数；本刀按会员历史提现单计数
        var withdrawCount = await _db.Queryable<WithdrawOrder>()
            .Where(w => w.SiteID == siteId && w.UserID == o.UserID)
            .CountAsync(cancellationToken);

        return new BankRechargeCheckListItemDto
        {
            ID = o.ID,
            OrderID = o.OrderID ?? string.Empty,
            PayID = o.PayID,
            PayName = payName,
            UserID = o.UserID,
            UserName = names.GetValueOrDefault(o.UserID, string.Empty),
            Money = o.Money,
            Paid = o.Paid,
            Status = o.Status,
            Currency = o.Currency,
            CreateAt = o.CreateAt,
            CheckAt = o.CheckAt,
            Name = o.Name ?? string.Empty,
            Description = o.Description ?? string.Empty,
            AdminName = adminName,
            PayOrderID = o.PayOrderID ?? string.Empty,
            PayRemark = string.Empty,
            WithdrawCount = withdrawCount
        };
    }

    /// <summary>
    /// 标记处理中。对齐旧 checkprocessing。
    /// </summary>
    public async Task MarkProcessingAsync(
        int id,
        int adminId,
        string? description,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw Oops.Oh("#编号错误#");
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<BankRechargeOrder>()
            .Where(o => o.SiteID == siteId && o.ID == id)
            .Take(1)
            .ToListAsync(cancellationToken);
        var order = rows.FirstOrDefault() ?? throw Oops.Oh("#编号错误#");
        if (order.Status is not (StatusNone or StatusProcessing))
            throw Oops.Oh($"当前状态为{order.Status}");

        order.Status = StatusProcessing;
        order.CheckAt = DateTime.Now;
        order.AdminID = adminId;
        if (!string.IsNullOrWhiteSpace(description))
            order.Description = description.Trim();

        await _db.Updateable(order)
            .UpdateColumns(o => new { o.Status, o.Description, o.CheckAt, o.AdminID })
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 审核。通过时写充值账变（手续费本刀按 0）；拒绝只改状态。
    /// </summary>
    public async Task CheckAsync(
        BankRechargeCheckInput input,
        int adminId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.ID <= 0)
            throw Oops.Oh("#编号错误#");
        if (input.Status is not (StatusFailed or StatusSuccess))
            throw Oops.Oh("#状态错误#");

        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<BankRechargeOrder>()
            .Where(o => o.SiteID == siteId && o.ID == input.ID)
            .Take(1)
            .ToListAsync(cancellationToken);
        var order = rows.FirstOrDefault() ?? throw Oops.Oh("#编号错误#");
        if (order.Status is not (StatusNone or StatusProcessing))
            throw Oops.Oh($"当前状态为{order.Status}");

        var desc = $"{input.Description ?? string.Empty} | {input.DescriptionA ?? string.Empty}".Trim();
        order.Status = input.Status;
        order.Description = desc;
        order.CheckAt = DateTime.Now;
        order.AdminID = adminId;

        await _db.Ado.BeginTranAsync();
        try
        {
            if (input.Status == StatusSuccess)
            {
                var paid = input.Paid > 0 ? input.Paid : order.Money;
                order.Paid = paid;
                var source = string.IsNullOrWhiteSpace(order.OrderID) ? order.ID.ToString() : order.OrderID;
                await _money.AddMoneyLogAsync(
                    siteId,
                    order.UserID,
                    paid,
                    MoneyTypeRecharge,
                    source,
                    desc,
                    order.Currency,
                    cancellationToken);

                if (!string.IsNullOrWhiteSpace(order.OrderID))
                {
                    await _db.Updateable<RechargeOrder>()
                        .SetColumns(r => new RechargeOrder
                        {
                            IsPay = true,
                            Paid = paid,
                            PayAt = DateTime.Now,
                            SystemID = input.SystemID ?? string.Empty,
                            AdminID = adminId,
                            Remark = desc
                        })
                        .Where(r => r.SiteID == siteId && r.ID == order.OrderID && !r.IsPay)
                        .ExecuteCommandAsync(cancellationToken);
                }
            }
            else
            {
                order.Paid = 0;
            }

            await _db.Updateable(order)
                .UpdateColumns(o => new { o.Status, o.Description, o.CheckAt, o.Paid, o.AdminID })
                .ExecuteCommandAsync(cancellationToken);
            await _db.Ado.CommitTranAsync();
        }
        catch
        {
            await _db.Ado.RollbackTranAsync();
            throw;
        }
    }

    private async Task<Dictionary<int, string>> LoadNamesAsync(int siteId, List<int> ids, CancellationToken ct)
    {
        var map = new Dictionary<int, string>();
        if (ids.Count == 0) return map;
        var users = await _db.Queryable<User>()
            .Where(u => u.SiteID == siteId && ids.Contains(u.ID))
            .Select(u => new { u.ID, u.UserName })
            .ToListAsync(ct);
        foreach (var u in users)
            map[u.ID] = u.UserName ?? string.Empty;
        return map;
    }
}
