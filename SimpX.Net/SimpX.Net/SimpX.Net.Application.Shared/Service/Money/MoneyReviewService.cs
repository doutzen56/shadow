using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 租户后台加减款审核。对齐旧 MoneyReviewAgent.CheckMoneyReview + moneyreview/list。
/// </summary>
public sealed class MoneyReviewService : ITransient
{
    /// <summary>待审。</summary>
    public const int StatusNone = 0;

    /// <summary>失败。</summary>
    public const int StatusFailed = 1;

    /// <summary>通过。</summary>
    public const int StatusSuccess = 2;

    /// <summary>加款。</summary>
    public const int ActionIn = 1;

    /// <summary>扣款。</summary>
    public const int ActionOut = 2;

    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;
    private readonly IMoneyLedger _money;
    private readonly IMoneyLedgerCallbacks _callbacks;

    /// <summary>
    /// 注入库、定站与账本。单测不传缓存回调。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站</param>
    /// <param name="money">账本</param>
    public MoneyReviewService(IBizDbAccessor biz, ICurrentSiteAccessor site, IMoneyLedger money)
        : this(biz, site, money, null)
    {
    }

    /// <summary>
    /// 注入库、定站、账本和钱包缓存刷新。审核提交后再读一次钱包，避免事务里读到旧余额。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站</param>
    /// <param name="money">账本</param>
    /// <param name="callbacks">账变后的缓存刷新；可空</param>
    public MoneyReviewService(
        IBizDbAccessor biz,
        ICurrentSiteAccessor site,
        IMoneyLedger money,
        IMoneyLedgerCallbacks callbacks)
    {
        _db = biz.Db;
        _site = site;
        _money = money;
        _callbacks = callbacks;
    }

    /// <summary>
    /// 审核单列表。对齐旧 list 主筛选（不含代理树/总账换算）。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行列表</returns>
    public async Task<List<MoneyReviewListItemDto>> ListAsync(
        MoneyReviewListInput input,
        CancellationToken cancellationToken = default)
    {
        input ??= new MoneyReviewListInput();
        var siteId = _site.RequireSiteId();
        var query = _db.Queryable<MoneyReview>().Where(r => r.SiteID == siteId);

        if (!string.IsNullOrWhiteSpace(input.Status) && OldApiMoneyDisplay.TryParseCheckStatus(input.Status, out var st))
            query = query.Where(r => r.Status == st);
        if (input.Type.HasValue)
            query = query.Where(r => r.Type == input.Type.Value);
        if (input.Currency.HasValue)
            query = query.Where(r => r.Currency == input.Currency.Value);
        if (input.StartAt.HasValue && input.StartAt.Value != default)
            query = query.Where(r => r.CreateAt >= input.StartAt.Value);
        if (input.EndAt.HasValue && input.EndAt.Value != default)
            query = query.Where(r => r.CreateAt < input.EndAt.Value.Date.AddDays(1));

        if (!string.IsNullOrWhiteSpace(input.User))
        {
            var name = input.User.Trim();
            var belong = input.BelongUser is "1" or "true" or "True";
            if (int.TryParse(name, out var uidExact))
            {
                if (belong)
                {
                    var childIds = await _db.Queryable<User>()
                        .Where(u => u.SiteID == siteId && u.AgentID == uidExact)
                        .Select(u => u.ID)
                        .ToListAsync(cancellationToken);
                    childIds.Add(uidExact);
                    query = query.Where(r => childIds.Contains(r.UserID));
                }
                else
                    query = query.Where(r => r.UserID == uidExact);
            }
            else
            {
                var uids = await _db.Queryable<User>()
                    .Where(u => u.SiteID == siteId && u.UserName.Contains(name))
                    .Select(u => u.ID)
                    .Take(500)
                    .ToListAsync(cancellationToken);
                if (uids.Count == 0)
                    return new List<MoneyReviewListItemDto>();
                if (belong)
                {
                    var rootId = uids[0];
                    var childIds = await _db.Queryable<User>()
                        .Where(u => u.SiteID == siteId && u.AgentID == rootId)
                        .Select(u => u.ID)
                        .ToListAsync(cancellationToken);
                    childIds.Add(rootId);
                    query = query.Where(r => childIds.Contains(r.UserID));
                }
                else
                    query = query.Where(r => uids.Contains(r.UserID));
            }
        }
        if (!string.IsNullOrWhiteSpace(input.SourceID))
            query = query.Where(r => r.SourceID != null && r.SourceID.Contains(input.SourceID.Trim()));
        if (!string.IsNullOrWhiteSpace(input.Agent))
        {
            var agentName = input.Agent.Trim();
            var agentIds = await _db.Queryable<User>()
                .Where(u => u.SiteID == siteId && u.UserName.Contains(agentName))
                .Select(u => u.ID)
                .Take(500)
                .ToListAsync(cancellationToken);
            if (agentIds.Count == 0)
                return new List<MoneyReviewListItemDto>();
            var memberIds = await _db.Queryable<User>()
                .Where(u => u.SiteID == siteId && agentIds.Contains(u.AgentID))
                .Select(u => u.ID)
                .Take(2000)
                .ToListAsync(cancellationToken);
            if (memberIds.Count == 0)
                return new List<MoneyReviewListItemDto>();
            query = query.Where(r => memberIds.Contains(r.UserID));
        }
        if (!string.IsNullOrWhiteSpace(input.CreatorAdminName))
        {
            var creatorName = input.CreatorAdminName.Trim();
            var creatorIds = await _db.Queryable<SiteAdmin>()
                .Where(a => a.SiteID == siteId && a.AdminName.Contains(creatorName))
                .Select(a => a.ID)
                .Take(200)
                .ToListAsync(cancellationToken);
            if (creatorIds.Count == 0)
                return new List<MoneyReviewListItemDto>();
            query = query.Where(r => creatorIds.Contains(r.CreatorID));
        }
        if (!string.IsNullOrWhiteSpace(input.ReviewerAdminName))
        {
            var reviewerName = input.ReviewerAdminName.Trim();
            var reviewerIds = await _db.Queryable<SiteAdmin>()
                .Where(a => a.SiteID == siteId && a.AdminName.Contains(reviewerName))
                .Select(a => a.ID)
                .Take(200)
                .ToListAsync(cancellationToken);
            if (reviewerIds.Count == 0)
                return new List<MoneyReviewListItemDto>();
            query = query.Where(r => reviewerIds.Contains(r.ReviewerID));
        }
        if (input.Action is ActionIn or ActionOut)
            query = query.Where(r => r.Action == input.Action.Value);

        var rows = await query
            .OrderBy(r => r.CreateAt, OrderByType.Desc)
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
                nameMap[u.ID] = u.UserName ?? string.Empty;
        }

        var adminIds = rows
            .SelectMany(r => new[] { r.CreatorID, r.ReviewerID })
            .Where(id => id > 0)
            .Distinct()
            .ToList();
        var adminNames = new Dictionary<int, string>();
        if (adminIds.Count > 0)
        {
            var admins = await _db.Queryable<SiteAdmin>()
                .Where(a => a.SiteID == siteId && adminIds.Contains(a.ID))
                .Select(a => new { a.ID, a.AdminName })
                .ToListAsync(cancellationToken);
            foreach (var a in admins)
                adminNames[a.ID] = a.AdminName ?? string.Empty;
        }

        var payIds = rows.Select(r => r.PayID).Where(id => id > 0).Distinct().ToList();
        var payNames = payIds.Count == 0
            ? new Dictionary<int, string>()
            : (await _db.Queryable<SitePayment>()
                .Where(p => p.SiteID == siteId && payIds.Contains(p.ID))
                .Select(p => new { p.ID, p.Name })
                .ToListAsync(cancellationToken))
            .ToDictionary(x => x.ID, x => x.Name ?? string.Empty);

        return rows.Select(r =>
        {
            var parts = (r.Description ?? string.Empty).Split('|');
            return new MoneyReviewListItemDto
            {
                ID = r.ID,
                UserID = r.UserID,
                UserName = nameMap.TryGetValue(r.UserID, out var n) ? n : string.Empty,
                CreateAt = r.CreateAt,
                Money = r.Money,
                Type = r.Type,
                Action = r.Action,
                Status = r.Status,
                Currency = r.Currency,
                SourceID = r.SourceID ?? string.Empty,
                Description = parts.Length > 0 ? parts[0].Trim() : string.Empty,
                DescriptionA = parts.Length > 1 ? parts[1].Trim() : string.Empty,
                Flow = r.Flow,
                MinFlow = r.MinFlow,
                CreatorID = r.CreatorID,
                CreatorName = adminNames.GetValueOrDefault(r.CreatorID, string.Empty),
                ReviewerID = r.ReviewerID,
                ReviewerName = adminNames.GetValueOrDefault(r.ReviewerID, string.Empty),
                ReviewAt = r.ReviewAt,
                PayName = r.PayID > 0 && payNames.TryGetValue(r.PayID, out var pn) ? pn : string.Empty
            };
        }).ToList();
    }

    /// <summary>
    /// 审核一笔。对齐旧 CheckMoneyReview：扣款先尝试解锁；通过则走账本；失败不改余额（本刀不写 0 元流水）。
    /// </summary>
    /// <param name="input">审核入参</param>
    /// <param name="reviewerId">审核人站点管理员编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task CheckAsync(
        MoneyReviewCheckInput input,
        int reviewerId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.ID <= 0)
            throw Oops.Oh("#编号不存在#");
        if (input.Status is not (StatusFailed or StatusSuccess))
            throw Oops.Oh("#未选择审核类型#");

        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<MoneyReview>()
            .Where(r => r.SiteID == siteId && r.ID == input.ID)
            .Take(1)
            .ToListAsync(cancellationToken);
        var review = rows.FirstOrDefault() ?? throw Oops.Oh("#编号不存在#");
        if (review.Status != StatusNone)
            throw Oops.Oh($"已被审核,当前状态: {review.Status}");

        var desc = $"{input.Description ?? string.Empty} | {input.DescriptionA ?? string.Empty}".Trim();
        if (!string.IsNullOrWhiteSpace(desc) && desc != "|")
            review.Description = desc;

        review.ReviewerID = reviewerId;
        review.Status = input.Status;
        review.ReviewAt = DateTime.Now;

        var signedMoney = review.Money;
        if (review.Action == ActionOut)
            signedMoney = -Math.Abs(review.Money);

        await _db.Ado.BeginTranAsync();
        try
        {
            if (review.Action == ActionOut)
                await TryUnlockReviewAsync(siteId, review.UserID, review.ID, review.Description ?? string.Empty, review.Currency, cancellationToken);

            if (input.Status == StatusSuccess)
            {
                await _money.AddMoneyLogAsync(
                    siteId,
                    review.UserID,
                    signedMoney,
                    review.Type,
                    review.ID.ToString(),
                    review.Description ?? string.Empty,
                    review.Currency,
                    cancellationToken,
                    flowRate: review.Action == ActionIn ? review.Flow : 0,
                    minFlow: review.Action == ActionIn ? review.MinFlow : 0,
                    eventId: review.EventIDNoTrans,
                    reviewId: review.ID);
            }

            var updated = await _db.Updateable(review)
                .UpdateColumns(r => new { r.ReviewerID, r.Status, r.Description, r.ReviewAt })
                .ExecuteCommandAsync(cancellationToken);
            if (updated != 1)
                throw Oops.Oh("#更新失败#");

            await _db.Ado.CommitTranAsync();
        }
        catch
        {
            await _db.Ado.RollbackTranAsync();
            throw;
        }

        // 入账发生在提交前。提交后再读钱包写缓存，前台才看得到审核后的余额和锁定。
        if (_callbacks != null)
            await _callbacks.AfterUnlockReviewAsync(siteId, review.UserID, review.Currency, cancellationToken);
    }

    /// <summary>
    /// 批量审核。对齐旧 batchsuccess|batchfail；任一条失败立刻停并抛出。
    /// </summary>
    /// <param name="ids">逗号分隔审核单号</param>
    /// <param name="status">目标状态：1 失败 / 2 通过</param>
    /// <param name="reviewerId">审核人</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task BatchCheckAsync(
        string ids,
        int status,
        int reviewerId,
        CancellationToken cancellationToken = default)
    {
        if (status is not (StatusFailed or StatusSuccess))
            throw Oops.Oh("#未选择审核类型#");
        var idList = ParseIds(ids);
        if (idList.Count == 0)
            throw Oops.Oh("数据错误");

        foreach (var id in idList)
        {
            try
            {
                await CheckAsync(
                    new MoneyReviewCheckInput { ID = id, Status = status },
                    reviewerId,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                if (msg.StartsWith("Oops:", StringComparison.OrdinalIgnoreCase))
                    msg = msg["Oops:".Length..].Trim();
                throw Oops.Oh($"Id:{id}Fail {msg}");
            }
        }
    }

    /// <summary>解析逗号/分号分隔的编号列表。</summary>
    private static List<int> ParseIds(string ids)
    {
        var list = new List<int>();
        if (string.IsNullOrWhiteSpace(ids))
            return list;
        foreach (var part in ids.Split([',', ';', '|', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (int.TryParse(part, out var id) && id > 0)
                list.Add(id);
        }

        return list;
    }

    /// <summary>
    /// 扣款审核解锁：有锁才解；Y2 待审未写锁时跳过（避免卡死审核）。
    /// </summary>
    private async Task TryUnlockReviewAsync(
        int siteId,
        int userId,
        int reviewId,
        string description,
        int currency,
        CancellationToken cancellationToken)
    {
        var locks = await _db.Queryable<MoneyLock>()
            .Where(l => l.UserID == userId && l.Type == MoneyLedgerService.LockTypeReview && l.SourceID == reviewId)
            .Take(1)
            .ToListAsync(cancellationToken);
        if (locks.FirstOrDefault() == null)
            return;
        await _money.UnlockReviewAsync(siteId, userId, reviewId, description, currency, cancellationToken);
    }
}
