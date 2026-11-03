using SimpX.Net.Data.Entity.Users;

using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Systems;
namespace SimpX.Net.Application.Shared;

/// <summary>
/// 租户后台提现订单只读。对齐旧 withdraw/order 列表主字段。
/// </summary>
public sealed partial class WithdrawOrderService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;

    /// <summary>
    /// 注入业务库与定站。
    /// </summary>
    public WithdrawOrderService(IBizDbAccessor biz, ICurrentSiteAccessor site)
    {
        _db = biz.Db;
        _site = site;
    }

    /// <summary>
    /// 分页列出提现订单。
    /// </summary>
    public async Task<WithdrawOrderPageOutput> PageAsync(
        WithdrawOrderPageInput input,
        CancellationToken cancellationToken = default)
    {
        input ??= new WithdrawOrderPageInput();
        var siteId = _site.RequireSiteId();
        var page = input.Page < 1 ? 1 : input.Page;
        var size = input.PageSize is < 1 or > 200 ? 20 : input.PageSize;

        var query = _db.Queryable<WithdrawOrder>().Where(o => o.SiteID == siteId);
        if (input.OrderID is > 0)
            query = query.Where(o => o.ID == input.OrderID.Value);
        if (!string.IsNullOrWhiteSpace(input.SystemID))
        {
            var sys = input.SystemID.Trim();
            query = query.Where(o => o.SystemID != null && o.SystemID.Contains(sys));
        }
        if (input.Status.HasValue)
            query = query.Where(o => o.Status == input.Status.Value);
        if (input.ChannelID is > 0)
            query = query.Where(o => o.ChannelID == input.ChannelID.Value);
        if (input.Currency.HasValue)
            query = query.Where(o => o.Currency == input.Currency.Value);
        if (input.FromCurrency.HasValue)
            query = query.Where(o => o.FromCurrency == input.FromCurrency.Value);
        if (input.MoneyFrom.HasValue)
            query = query.Where(o => o.Money >= input.MoneyFrom.Value);
        if (input.MoneyTo.HasValue)
            query = query.Where(o => o.Money <= input.MoneyTo.Value);
        var byPayAt = string.Equals(input.DateType, "PayAt", StringComparison.OrdinalIgnoreCase);
        if (input.StartAt.HasValue && input.StartAt.Value != default)
        {
            if (byPayAt)
                query = query.Where(o => o.PayAt >= input.StartAt.Value);
            else
                query = query.Where(o => o.CreateAt >= input.StartAt.Value);
        }
        if (input.EndAt.HasValue && input.EndAt.Value != default)
        {
            var endExclusive = input.EndAt.Value.Date.AddDays(1);
            if (byPayAt)
                query = query.Where(o => o.PayAt < endExclusive);
            else
                query = query.Where(o => o.CreateAt < endExclusive);
        }

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
                    return new WithdrawOrderPageOutput { Items = [], Total = 0 };
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
                return new WithdrawOrderPageOutput { Items = [], Total = 0 };
            var childIds = await _db.Queryable<User>()
                .Where(u => u.SiteID == siteId && agentIds.Contains(u.AgentID))
                .Select(u => u.ID)
                .ToListAsync(cancellationToken);
            if (childIds.Count == 0)
                return new WithdrawOrderPageOutput { Items = [], Total = 0 };
            query = query.Where(o => childIds.Contains(o.UserID));
        }

        // 审核时长：CreateAt → AuditAt（分钟）
        if (input.StartAudit is >= 0)
        {
            var minAudit = input.StartAudit.Value;
            query = query.Where(o => o.AuditAt.Year > 1900 && o.AuditAt >= o.CreateAt.AddMinutes(minAudit));
        }
        if (input.EndAudit is >= 0)
        {
            var maxAudit = input.EndAudit.Value;
            query = query.Where(o => o.AuditAt.Year > 1900 && o.AuditAt <= o.CreateAt.AddMinutes(maxAudit));
        }

        // 出款时长：AuditAt（无效则 CreateAt）→ PayAt（分钟）
        if (input.StartCash is >= 0)
        {
            var minCash = input.StartCash.Value;
            query = query.Where(o =>
                o.PayAt.Year > 1900 &&
                ((o.AuditAt.Year > 1900 && o.PayAt >= o.AuditAt.AddMinutes(minCash))
                 || (o.AuditAt.Year <= 1900 && o.PayAt >= o.CreateAt.AddMinutes(minCash))));
        }
        if (input.EndCash is >= 0)
        {
            var maxCash = input.EndCash.Value;
            query = query.Where(o =>
                o.PayAt.Year > 1900 &&
                ((o.AuditAt.Year > 1900 && o.PayAt <= o.AuditAt.AddMinutes(maxCash))
                 || (o.AuditAt.Year <= 1900 && o.PayAt <= o.CreateAt.AddMinutes(maxCash))));
        }

        RefAsync<int> total = 0;
        var rows = await query
            .OrderBy(o => o.CreateAt, OrderByType.Desc)
            .ToPageListAsync(page, size, total, cancellationToken);

        var items = await MapListItemsAsync(siteId, rows, cancellationToken);
        return new WithdrawOrderPageOutput
        {
            Total = total,
            Items = items
        };
    }

    /// <summary>
    /// 提现订单详情。对齐旧 orderinfo 主字段（不含风控统计）。
    /// </summary>
    /// <param name="id">订单号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>详情</returns>
    public async Task<WithdrawOrderInfoDto> InfoAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw Oops.Oh("#订单不存在#");
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<WithdrawOrder>()
            .Where(o => o.SiteID == siteId && o.ID == id)
            .Take(1)
            .ToListAsync(cancellationToken);
        var order = rows.FirstOrDefault() ?? throw Oops.Oh("#订单不存在#");
        var items = await MapListItemsAsync(siteId, [order], cancellationToken);
        var item = items.FirstOrDefault() ?? MapListItem(order, new Dictionary<int, string>());
        var sysId = 0;
        if (order.ChannelID > 0)
        {
            var channels = await _db.Queryable<SimpX.Net.Data.Entity.Sites.WithdrawChannel>()
                .Where(c => c.SiteID == siteId && c.ID == order.ChannelID)
                .Take(1)
                .ToListAsync(cancellationToken);
            sysId = channels.FirstOrDefault()?.SysID ?? 0;
        }

        return new WithdrawOrderInfoDto
        {
            OrderID = item.OrderID,
            UserID = item.UserID,
            UserName = item.UserName,
            Money = item.Money,
            Paid = item.Paid,
            Fee = item.Fee,
            Cost = item.Cost,
            Status = item.Status,
            StatusValue = item.StatusValue,
            ChannelID = item.ChannelID,
            Channel = item.Channel,
            CreateAt = item.CreateAt,
            PayAt = item.PayAt,
            Currency = item.Currency,
            CurrencyCode = item.CurrencyCode,
            AccountName = item.AccountName,
            AccountNumber = item.AccountNumber,
            BankInfo = item.BankInfo,
            AgentName = item.AgentName,
            Group = item.Group,
            AdminID = item.AdminID,
            Admin = item.Admin,
            CashierID = item.CashierID,
            Cashier = item.Cashier,
            SystemID = item.SystemID,
            AuditAt = item.AuditAt,
            AuditTime = item.AuditTime,
            DealTime = item.DealTime,
            Description = item.Description,
            DescriptionA = item.DescriptionA,
            RealPaid = item.RealPaid,
            Mark = item.Mark,
            SysID = sysId,
            Bank = order.Bank
        };
    }

    /// <summary>
    /// 提现订单操作日志。对齐旧 orderlog。
    /// </summary>
    /// <param name="orderId">订单号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>日志行</returns>
    public async Task<List<WithdrawOrderLogItemDto>> ListLogsAsync(
        int orderId,
        CancellationToken cancellationToken = default)
    {
        if (orderId <= 0)
            throw Oops.Oh("#订单不存在#");
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<WithdrawOrderLog>()
            .Where(l => l.SiteID == siteId && l.OrderID == orderId)
            .OrderBy(l => l.CreateAt, OrderByType.Desc)
            .ToListAsync(cancellationToken);
        return rows.Select(l => new WithdrawOrderLogItemDto
        {
            ID = l.ID,
            OrderID = l.OrderID,
            Status = l.Status,
            CreateAt = l.CreateAt,
            AdminID = l.AdminID
        }).ToList();
    }

    /// <summary>批量充实列表行（上级/分组/渠道/管理员名/时长）。</summary>
    private async Task<List<WithdrawOrderListItemDto>> MapListItemsAsync(
        int siteId,
        List<WithdrawOrder> rows,
        CancellationToken cancellationToken)
    {
        if (rows.Count == 0) return [];

        var userIds = rows.Select(r => r.UserID).Distinct().ToList();
        var users = await _db.Queryable<User>()
            .Where(u => u.SiteID == siteId && userIds.Contains(u.ID))
            .Select(u => new { u.ID, u.UserName, u.AgentID, u.GroupID })
            .ToListAsync(cancellationToken);
        var nameMap = users.ToDictionary(u => u.ID, u => u.UserName ?? string.Empty);
        var agentIdMap = users.ToDictionary(u => u.ID, u => u.AgentID);
        var groupIdMap = users.ToDictionary(u => u.ID, u => u.GroupID);

        var parentIds = agentIdMap.Values.Where(id => id > 0).Distinct().ToList();
        var parentNameMap = new Dictionary<int, string>();
        if (parentIds.Count > 0)
        {
            var parents = await _db.Queryable<User>()
                .Where(u => u.SiteID == siteId && parentIds.Contains(u.ID))
                .Select(u => new { u.ID, u.UserName })
                .ToListAsync(cancellationToken);
            foreach (var p in parents)
                parentNameMap[p.ID] = p.UserName ?? string.Empty;
        }

        var groupIds = groupIdMap.Values.Where(id => id > 0).Distinct().ToList();
        var groupNameMap = new Dictionary<int, string>();
        if (groupIds.Count > 0)
        {
            var groups = await _db.Queryable<UserGroup>()
                .Where(g => g.SiteID == siteId && groupIds.Contains(g.ID))
                .Select(g => new { g.ID, g.Name })
                .ToListAsync(cancellationToken);
            foreach (var g in groups)
                groupNameMap[g.ID] = g.Name ?? string.Empty;
        }

        var channelIds = rows.Select(r => r.ChannelID).Where(id => id > 0).Distinct().ToList();
        var channelNameMap = new Dictionary<int, string>();
        if (channelIds.Count > 0)
        {
            var channels = await _db.Queryable<SimpX.Net.Data.Entity.Sites.WithdrawChannel>()
                .Where(c => c.SiteID == siteId && channelIds.Contains(c.ID))
                .Select(c => new { c.ID, c.Name })
                .ToListAsync(cancellationToken);
            foreach (var c in channels)
                channelNameMap[c.ID] = c.Name ?? string.Empty;
        }

        var adminIds = rows.SelectMany(r => new[] { r.AdminID, r.CashierID }).Where(id => id > 0).Distinct().ToList();
        var adminNameMap = new Dictionary<int, string>();
        if (adminIds.Count > 0)
        {
            var admins = await _db.Queryable<SimpX.Net.Data.Entity.Sites.SiteAdmin>()
                .Where(a => a.SiteID == siteId && adminIds.Contains(a.ID))
                .Select(a => new { a.ID, a.AdminName })
                .ToListAsync(cancellationToken);
            foreach (var a in admins)
                adminNameMap[a.ID] = a.AdminName ?? string.Empty;
        }

        return rows.Select(o =>
        {
            var item = MapListItem(o, nameMap);
            if (agentIdMap.TryGetValue(o.UserID, out var aid) && parentNameMap.TryGetValue(aid, out var an))
                item.AgentName = an;
            if (groupIdMap.TryGetValue(o.UserID, out var gid) && groupNameMap.TryGetValue(gid, out var gn))
                item.Group = gn;
            item.Channel = channelNameMap.GetValueOrDefault(o.ChannelID, string.Empty);
            item.Admin = adminNameMap.GetValueOrDefault(o.AdminID, o.AdminID > 0 ? o.AdminID.ToString() : string.Empty);
            item.Cashier = adminNameMap.GetValueOrDefault(o.CashierID, o.CashierID > 0 ? o.CashierID.ToString() : string.Empty);
            return item;
        }).ToList();
    }

    /// <summary>列表行映射。</summary>
    private static WithdrawOrderListItemDto MapListItem(
        WithdrawOrder o,
        Dictionary<int, string> nameMap)
    {
        var desc = o.Description ?? string.Empty;
        var parts = desc.Split('|');
        var currencyCode = OldApiMoneyDisplay.FormatCurrency(o.Currency);
        return new()
        {
            OrderID = o.ID,
            UserID = o.UserID,
            UserName = nameMap.GetValueOrDefault(o.UserID, string.Empty),
            Money = o.Money,
            Paid = o.Paid,
            Fee = o.Fee,
            Cost = o.Cost,
            Status = o.Status,
            StatusValue = FormatWithdrawStatus(o.Status),
            ChannelID = o.ChannelID,
            CreateAt = o.CreateAt,
            PayAt = o.PayAt,
            Currency = o.Currency,
            CurrencyCode = currencyCode,
            AccountName = o.AccountName ?? string.Empty,
            AccountNumber = MaskAccount(o.AccountNumber),
            BankInfo = $"{o.AccountName ?? string.Empty} {MaskAccount(o.AccountNumber)}".Trim(),
            AdminID = o.AdminID,
            CashierID = o.CashierID,
            SystemID = o.SystemID ?? string.Empty,
            AuditAt = o.AuditAt,
            AuditTime = FormatSpan(o.CreateAt, o.AuditAt),
            DealTime = FormatSpan(o.AuditAt != default && o.AuditAt.Year > 1900 ? o.AuditAt : o.CreateAt, o.PayAt),
            Description = parts.Length > 0 ? parts[0].Trim() : string.Empty,
            DescriptionA = parts.Length > 1 ? parts[1].Trim() : string.Empty,
            RealPaid = OldApiMoneyDisplay.FormatMoneyCurrency(o.Paid, o.Currency),
            Mark = o.Mark ?? string.Empty
        };
    }

    private static string FormatWithdrawStatus(int status) => OldApiEnumDisplay.WithdrawStatus(status);

    private static string FormatSpan(DateTime from, DateTime to)
    {
        if (from == default || to == default || to.Year <= 1900 || from.Year <= 1900) return string.Empty;
        var span = to - from;
        if (span < TimeSpan.Zero) return string.Empty;
        return $"{(int)span.TotalHours}:{span.Minutes:D2}:{span.Seconds:D2}";
    }

    private static string MaskAccount(string account)
    {
        if (string.IsNullOrEmpty(account) || account.Length < 8)
            return account ?? string.Empty;
        return account[..4] + "****" + account[^4..];
    }

    private async Task<Dictionary<int, string>> LoadUserNamesAsync(
        int siteId,
        List<int> userIds,
        CancellationToken cancellationToken)
    {
        var map = new Dictionary<int, string>();
        if (userIds.Count == 0) return map;
        var users = await _db.Queryable<User>()
            .Where(u => u.SiteID == siteId && userIds.Contains(u.ID))
            .Select(u => new { u.ID, u.UserName })
            .ToListAsync(cancellationToken);
        foreach (var u in users)
            map[u.ID] = u.UserName ?? string.Empty;
        return map;
    }

    // --- merged from WithdrawOrderService.WaveGaps.cs ---


    /// <summary>
    /// 对齐旧 channelinfo。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> ChannelinfoAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return ChannelinfoCoreAsync(input, cancellationToken);
    }

    /// <summary>
    /// 对齐旧 channelist。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> ChannelistAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return ChannelistCoreAsync(input, cancellationToken);
    }

    /// <summary>
    /// 对齐旧 deletechannel（软删，实体有 IsDeleted）。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> DeletechannelAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return DeletechannelCoreAsync(input, cancellationToken);
    }

    /// <summary>
    /// 对齐旧 exportwithdraworder：返回与列表同结构，由前端落文件。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> ExportwithdraworderAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return ExportwithdraworderCoreAsync(input, cancellationToken);
    }

    /// <summary>
    /// 对齐旧 openchannefirst。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> OpenchannefirstAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return OpenchannefirstCoreAsync(input, cancellationToken);
    }

    /// <summary>
    /// 对齐旧 openchannelist。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> OpenchannelistAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return OpenchannelistCoreAsync(input, cancellationToken);
    }

    /// <summary>
    /// 对齐旧 openchannessystem。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> OpenchannessystemAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return OpenchannessystemCoreAsync(input, cancellationToken);
    }

    /// <summary>
    /// 对齐旧 savechannel：缺口入口明确提示走渠道服务。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> SavechannelAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return SavechannelCoreAsync(input, cancellationToken);
    }

    /// <summary>
    /// 对齐旧 washflowinfo：读会员未完成打码与钱包流水要求。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> WashflowinfoAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return WashflowinfoCoreAsync(input, cancellationToken);
    }

    /// <summary>渠道详情。</summary>
    private async Task<object?> ChannelinfoCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var id = ReadInt(input, "ID", "Id", "ChannelID");
        if (id <= 0)
            throw Oops.Oh("#参数错误#");
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<WithdrawChannel>()
            .Where(c => c.SiteID == siteId && c.ID == id && !c.IsDeleted)
            .Take(1)
            .ToListAsync(cancellationToken);
        var c = rows.FirstOrDefault() ?? throw Oops.Oh("#渠道编号错误#");
        string sysName = string.Empty;
        if (c.SysID > 0)
        {
            var sys = await _db.Queryable<SystemWithdraw>()
                .Where(s => s.ID == c.SysID)
                .Take(1)
                .ToListAsync(cancellationToken);
            sysName = sys.FirstOrDefault()?.Name ?? string.Empty;
        }

        return new
        {
            c.ID,
            c.Name,
            c.SysID,
            SysName = sysName,
            c.IsOpen,
            c.Cost,
            c.CreateAt,
            c.Currency,
            c.Sort
        };
    }

    /// <summary>本站渠道列表。</summary>
    private async Task<object?> ChannelistCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        _ = input;
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<WithdrawChannel>()
            .Where(c => c.SiteID == siteId && !c.IsDeleted)
            .OrderBy(c => c.Sort, OrderByType.Desc)
            .ToListAsync(cancellationToken);
        return rows.Select(c => new
        {
            c.ID,
            c.Name,
            c.SysID,
            c.IsOpen,
            c.Cost,
            c.CreateAt,
            c.Currency,
            c.Sort
        }).ToList();
    }

    /// <summary>软删渠道。</summary>
    private async Task<object?> DeletechannelCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var id = ReadInt(input, "ID", "Id");
        if (id <= 0)
            throw Oops.Oh("#参数错误#");
        var siteId = _site.RequireSiteId();
        var n = await _db.Updateable<WithdrawChannel>()
            .SetColumns(c => c.IsDeleted == true)
            .Where(c => c.SiteID == siteId && c.ID == id)
            .ExecuteCommandAsync(cancellationToken);
        if (n != 1)
            throw Oops.Oh("#渠道编号错误#");
        return new { ok = true, id };
    }

    /// <summary>导出：分页同结构，页大小放大。</summary>
    private async Task<object?> ExportwithdraworderCoreAsync(
        Dictionary<string, object?> input,
        CancellationToken cancellationToken)
    {
        var pageInput = new WithdrawOrderPageInput
        {
            Page = 1,
            PageSize = 200,
            OrderID = ReadInt(input, "OrderID", "ID") is var oid && oid > 0 ? oid : null,
            Status = ReadNullableInt(input, "Status"),
            ChannelID = ReadNullableInt(input, "ChannelID"),
            Currency = ReadNullableInt(input, "Currency"),
            User = ReadString(input, "User")
        };
        var page = await PageAsync(pageInput, cancellationToken);
        return page;
    }

    /// <summary>排序最高的开放渠道。</summary>
    private async Task<object?> OpenchannefirstCoreAsync(
        Dictionary<string, object?> input,
        CancellationToken cancellationToken)
    {
        var list = await OpenchannelistCoreAsync(input, cancellationToken);
        if (list is System.Collections.IEnumerable rows)
        {
            foreach (var row in rows)
                return row;
        }

        return null;
    }

    /// <summary>开放渠道。</summary>
    private async Task<object?> OpenchannelistCoreAsync(
        Dictionary<string, object?> input,
        CancellationToken cancellationToken)
    {
        _ = input;
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<WithdrawChannel>()
            .Where(c => c.SiteID == siteId && c.IsOpen && !c.IsDeleted)
            .OrderBy(c => c.Sort, OrderByType.Desc)
            .ToListAsync(cancellationToken);
        return rows.Select(c => new { c.ID, c.Name, c.Currency }).ToList();
    }

    /// <summary>开放且挂系统代付的渠道。</summary>
    private async Task<object?> OpenchannessystemCoreAsync(
        Dictionary<string, object?> input,
        CancellationToken cancellationToken)
    {
        _ = input;
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<WithdrawChannel>()
            .Where(c => c.SiteID == siteId && c.IsOpen && !c.IsDeleted && c.SysID != 0)
            .OrderBy(c => c.Sort, OrderByType.Desc)
            .ToListAsync(cancellationToken);
        return rows.Select(c => new { c.ID, c.Name, c.Currency, c.SysID }).ToList();
    }

    /// <summary>保存请走 withdrawChannel 服务，避免订单壳重复写。</summary>
    private Task<object?> SavechannelCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        _ = input;
        _ = cancellationToken;
        throw Oops.Oh("#请走提现渠道保存接口#");
    }

    /// <summary>提现审核页流水摘要。</summary>
    private async Task<object?> WashflowinfoCoreAsync(
        Dictionary<string, object?> input,
        CancellationToken cancellationToken)
    {
        var userId = ReadInt(input, "UserID", "UserId");
        var orderId = ReadInt(input, "OrderID", "ID", "Id");
        var currency = ReadInt(input, "Currency");
        var siteId = _site.RequireSiteId();
        var createAt = DateTime.Now;
        if (orderId > 0)
        {
            var orders = await _db.Queryable<WithdrawOrder>()
                .Where(o => o.SiteID == siteId && o.ID == orderId)
                .Take(1)
                .ToListAsync(cancellationToken);
            var order = orders.FirstOrDefault() ?? throw Oops.Oh("#订单不存在#");
            if (userId > 0 && order.UserID != userId)
                throw Oops.Oh("#订单不存在#");
            userId = order.UserID;
            if (currency <= 0)
                currency = order.Currency;
            createAt = order.CreateAt;
        }

        if (userId <= 0)
            throw Oops.Oh("#参数错误#");
        if (currency <= 0)
            currency = 1;

        // 待审页看的是「上次成功出款之后、本单申请之前」的打码，不是 OrderID=0 的空窗口。
        var finishedQuery = _db.Queryable<WithdrawOrder>()
            .Where(o => o.SiteID == siteId && o.UserID == userId && o.Status == WithdrawOrderOperateService.StatusFinish);
        if (orderId > 0)
            finishedQuery = finishedQuery.Where(o => o.ID < orderId);
        var lastFinish = await finishedQuery
            .OrderBy(o => o.CreateAt, OrderByType.Desc)
            .Take(1)
            .ToListAsync(cancellationToken);
        var lastAt = lastFinish.FirstOrDefault()?.CreateAt;

        var wallet = await _db.Queryable<UserMoney>()
            .Where(m => m.SiteID == siteId && m.UserID == userId && m.Currency == currency)
            .Take(1)
            .ToListAsync(cancellationToken);
        var washQuery = _db.Queryable<UserWashFlow>()
            .Where(w => w.SiteID == siteId && w.UserID == userId && w.CreateAt < createAt);
        if (lastAt.HasValue)
            washQuery = washQuery.Where(w => w.CreateAt > lastAt.Value);
        var washRows = await washQuery
            .OrderBy(w => w.CreateAt, OrderByType.Desc)
            .Take(200)
            .ToListAsync(cancellationToken);

        var bet = washRows.Sum(w => w.BetMoney);
        var flow = washRows.Sum(w => w.Flow);
        return new
        {
            UserID = userId,
            OrderID = orderId,
            Currency = currency,
            WashFlow = wallet.FirstOrDefault()?.WashFlow ?? 0m,
            BetMoney = bet,
            Flow = flow,
            Money = washRows.Sum(w => w.Money),
            IsBet = bet >= flow,
            Distance = Math.Max(0m, flow - bet),
            Items = washRows.Select(w => new
            {
                w.ID,
                w.CreateAt,
                w.EndAt,
                w.Money,
                w.BetMoney,
                w.Flow,
                w.Type,
                w.Description,
                w.EventID,
                w.ReviewID,
                w.OrderID,
                IsBet = w.BetMoney >= w.Flow,
                Distance = Math.Max(0m, w.Flow - w.BetMoney)
            }).ToList()
        };
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

    private static int? ReadNullableInt(Dictionary<string, object?> input, params string[] keys)
    {
        var n = ReadInt(input, keys);
        return n == 0 ? null : n;
    }

    private static string ReadString(Dictionary<string, object?> input, params string[] keys)
    {
        foreach (var k in keys)
        {
            if (!input.TryGetValue(k, out var v) || v is null) continue;
            var s = v.ToString();
            if (!string.IsNullOrWhiteSpace(s))
                return s.Trim();
        }
        return string.Empty;
    }
}
