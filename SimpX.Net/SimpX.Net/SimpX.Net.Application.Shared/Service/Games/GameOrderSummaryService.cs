using SimpX.Net.Data.Entity.Games;
using SimpX.Net.Data.Entity.Reports;
using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 游戏注单汇总（日视图）+ 报表文件列表。
/// 优先 rpt_UserDateGameView；视图空时回落 rpt_UserDateGame；打赏走 game_RewardLog。
/// 带单号或 UseEsDetail 时改查注单明细；没注入查询实现时仍走日汇总和打赏表。
/// </summary>
public sealed partial class GameOrderSummaryService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;
    private readonly IGameOrderEsQuery _es;

    /// <summary>
    /// 注入依赖。注单明细查询可以不注册，不注册时详情只查打赏表。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站</param>
    /// <param name="gameOrderEs">注单明细查询；其它宿主可以不注册</param>
    public GameOrderSummaryService(IBizDbAccessor biz, ICurrentSiteAccessor site, IGameOrderEsQuery gameOrderEs = null)
    {
        _db = biz.Db;
        _site = site;
        _es = gameOrderEs;
    }

    /// <summary>
    /// 注单日汇总分页。Category&gt;=0 时按分类筛（彩票是 0；-1=全部）。
    /// 带第三方单号或 UseEsDetail 时改查注单明细。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页</returns>
    public async Task<GameOrderSummaryPageOutput> PageAsync(
        GameOrderSummaryPageInput input,
        CancellationToken cancellationToken = default)
    {
        input ??= new GameOrderSummaryPageInput();
        if (WantsEsDetail(input))
            return await PageDetailFromEsAsync(input.Category, input, cancellationToken);

        var siteId = _site.RequireSiteId();
        var pageSize = Math.Clamp(input.PageSize <= 0 ? 20 : input.PageSize, 1, 100);
        var page = Math.Max(1, input.Page <= 0 ? 1 : input.Page);
        var start = input.StartAt?.Date ?? DateTime.Today.AddDays(-7);
        var end = (input.EndAt?.Date ?? DateTime.Today).AddDays(1);

        try
        {
            var query = _db.Queryable<UserDateGameView>()
                .Where(v => v.SiteID == siteId && v.Date >= start && v.Date < end);
            if (input.Category >= 0)
                query = query.Where(v => v.Category == input.Category);
            if (input.Type > 0)
                query = query.Where(v => v.Type == input.Type);
            if (input.UserID > 0)
                query = query.Where(v => v.UserID == input.UserID);
            if (input.Currency >= 0)
                query = query.Where(v => v.Currency == input.Currency);

            var total = await query.CountAsync(cancellationToken);
            if (total > 0)
            {
                var rows = await query.OrderBy(v => v.Date, OrderByType.Desc)
                    .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
                var nameMap = await LoadNamesAsync(siteId, rows.Select(r => r.UserID).Distinct().ToList(), cancellationToken);
                return new GameOrderSummaryPageOutput
                {
                    Total = total,
                    Items = rows.Select(v => new GameOrderSummaryItemDto
                    {
                        Date = v.Date,
                        UserID = v.UserID,
                        UserName = nameMap.TryGetValue(v.UserID, out var un) ? un : string.Empty,
                        Type = v.Type,
                        Category = v.Category,
                        Currency = v.Currency,
                        BetMoney = v.BetMoney,
                        BetAmount = v.BetAmount,
                        Money = v.Money,
                        OrderCount = v.OrderCount
                    }).ToList()
                };
            }
        }
        catch
        {
            // 视图不存在或查询失败时回落日汇总表
        }

        return await PageFromUserDateGameAsync(siteId, input, start, end, page, pageSize, cancellationToken);
    }

    /// <summary>
    /// 打赏记录分页（game_RewardLog；无 ES 明细时的 SQL 只读）。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页</returns>
    public async Task<GameOrderSummaryPageOutput> PageRewardAsync(
        GameOrderSummaryPageInput input,
        CancellationToken cancellationToken = default)
    {
        input ??= new GameOrderSummaryPageInput();
        var siteId = _site.RequireSiteId();
        var pageSize = Math.Clamp(input.PageSize <= 0 ? 20 : input.PageSize, 1, 100);
        var page = Math.Max(1, input.Page <= 0 ? 1 : input.Page);
        var start = input.StartAt ?? DateTime.Today.AddDays(-90);
        var end = (input.EndAt ?? DateTime.Now).AddSeconds(1);

        // 旧站 reward 走 ES rewardlog*
        if (_es != null)
        {
            var userId = input.UserID;
            if (userId <= 0 && !string.IsNullOrWhiteSpace(input.UserName))
            {
                var name = input.UserName.Trim();
                var ids = await _db.Queryable<User>()
                    .Where(u => u.SiteID == siteId && u.UserName == name)
                    .Select(u => u.ID).Take(1).ToListAsync(cancellationToken);
                userId = ids.FirstOrDefault();
            }
            var typeName = input.TypeName;
            if (string.IsNullOrWhiteSpace(typeName) && input.Type > 0)
                typeName = GameTypeMeta.Format(input.Type);
            var es = await _es.SearchRewardAsync(
                siteId, page, pageSize, userId, start, end, input.SourceID,
                input.CurrencyCode, typeName, input.PlayerName, cancellationToken);
            if (es != null && !es.NotConfigured && !es.QueryFailed)
            {
                return new GameOrderSummaryPageOutput
                {
                    Total = es.Total,
                    Items = (es.Items ?? new List<GameOrderEsHit>()).Select(hit => new GameOrderSummaryItemDto
                    {
                        Date = hit.CreateAt?.Date ?? hit.SettlementAt?.Date ?? default,
                        UserID = hit.UserID,
                        UserName = hit.UserName ?? string.Empty,
                        Type = hit.Type > 0 ? hit.Type : input.Type,
                        TypeName = hit.TypeName ?? string.Empty,
                        Currency = hit.Currency,
                        CurrencyCode = hit.CurrencyCode ?? string.Empty,
                        BetMoney = hit.BetMoney,
                        BetAmount = hit.BetAmount,
                        Money = hit.Money,
                        OrderCount = 1,
                        SourceID = hit.SourceID ?? string.Empty,
                        PlayerName = hit.PlayerName ?? string.Empty,
                        Game = hit.Game ?? string.Empty,
                        Status = hit.Status ?? string.Empty,
                        SettlementAt = hit.SettlementAt,
                        EsID = hit.EsID ?? string.Empty
                    }).ToList()
                };
            }
        }

        var query = _db.Queryable<RewardLog>()
            .Where(r => r.SiteID == siteId && r.RewardAt >= start && r.RewardAt < end);
        if (input.UserID > 0)
            query = query.Where(r => r.UserID == input.UserID);
        if (input.Type > 0)
            query = query.Where(r => r.Type == input.Type);
        if (input.Currency >= 0)
            query = query.Where(r => r.Currency == input.Currency);

        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderBy(r => r.RewardAt, OrderByType.Desc)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        var nameMap = await LoadNamesAsync(siteId, rows.Select(r => r.UserID).Distinct().ToList(), cancellationToken);
        return new GameOrderSummaryPageOutput
        {
            Total = total,
            Items = rows.Select(r => new GameOrderSummaryItemDto
            {
                Date = r.RewardAt.Date,
                UserID = r.UserID,
                UserName = nameMap.TryGetValue(r.UserID, out var un) ? un : string.Empty,
                Type = r.Type,
                Category = -1,
                Currency = r.Currency,
                BetMoney = r.TradeAmount,
                BetAmount = r.TradeAmount,
                Money = r.TradeAmount,
                OrderCount = 1,
                SourceID = r.SourceID ?? string.Empty
            }).ToList()
        };
    }

    /// <summary>
    /// 按第三方单号查详情。先查打赏表，没有再按分类查注单明细。
    /// </summary>
    /// <param name="sourceId">第三方单号</param>
    /// <param name="type">游戏类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>详情或说明</returns>
    public Task<GameOrderInfoStubDto> InfoAsync(
        string? sourceId,
        int type,
        CancellationToken cancellationToken = default) =>
        InfoAsync(sourceId, type, -1, cancellationToken);

    /// <summary>
    /// 按第三方单号查详情。先查打赏表，没有再按分类查注单明细。
    /// </summary>
    /// <param name="sourceId">第三方单号</param>
    /// <param name="type">游戏类型</param>
    /// <param name="category">分类；-1 表示不指定，走综合索引</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>详情或说明</returns>
    public async Task<GameOrderInfoStubDto> InfoAsync(
        string? sourceId,
        int type,
        int category,
        CancellationToken cancellationToken = default)
    {
        var sid = (sourceId ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(sid))
        {
            return new GameOrderInfoStubDto
            {
                SourceID = string.Empty,
                Type = type,
                Message = "#请提供注单单号#"
            };
        }

        var siteId = _site.RequireSiteId();
        var rewardQuery = _db.Queryable<RewardLog>()
            .Where(r => r.SiteID == siteId && r.SourceID == sid);
        if (type > 0)
            rewardQuery = rewardQuery.Where(r => r.Type == type);
        var rewards = await rewardQuery.Take(1).ToListAsync(cancellationToken);
        var hit = rewards.FirstOrDefault();
        if (hit != null)
        {
            return new GameOrderInfoStubDto
            {
                SourceID = hit.SourceID ?? sid,
                Type = hit.Type,
                Message = string.Empty,
                Found = true,
                UserID = hit.UserID,
                PlayerName = hit.PlayerName ?? string.Empty,
                TradeAmount = hit.TradeAmount,
                RewardAt = hit.RewardAt,
                Currency = hit.Currency
            };
        }

        if (_es != null)
        {
            var esHit = await _es.GetBySourceIdAsync(
                siteId,
                sid,
                category >= 0 ? category : null,
                cancellationToken);
            if (esHit != null && esHit.Found)
                return FromEsHit(esHit, sid, type);
            if (esHit != null && (esHit.QueryFailed || esHit.NotConfigured))
            {
                return new GameOrderInfoStubDto
                {
                    SourceID = sid,
                    Type = type,
                    Found = false,
                    Message = string.IsNullOrEmpty(esHit.Message) ? "#注单明细查询失败#" : esHit.Message
                };
            }
        }

        return new GameOrderInfoStubDto
        {
            SourceID = sid,
            Type = type,
            Message = _es == null
                ? "#没有注单明细查询；打赏已查 game_RewardLog 未命中，其它分类请看日汇总#"
                : "#打赏和注单明细都没有这笔单#"
        };
    }

    /// <summary>
    /// 按分类查注单明细。给列表在带单号或 UseEsDetail 时用，也给单独的明细入口用。
    /// </summary>
    /// <param name="category">分类；-1 为全部</param>
    /// <param name="input">筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>明细分页。没配上或查询失败时总数为 0，并带说明</returns>
    public async Task<GameOrderSummaryPageOutput> PageDetailFromEsAsync(
        int category,
        GameOrderSummaryPageInput input,
        CancellationToken cancellationToken = default)
    {
        input ??= new GameOrderSummaryPageInput();
        var siteId = _site.RequireSiteId();
        var pageSize = Math.Clamp(input.PageSize <= 0 ? 20 : input.PageSize, 1, 100);
        var page = Math.Max(1, input.Page <= 0 ? 1 : input.Page);
        // 旧站无时间时默认 CreateAt >= 今天-90
        var start = input.StartAt ?? DateTime.Today.AddDays(-90);
        var end = (input.EndAt ?? DateTime.Now).AddSeconds(1);
        if (_es == null)
        {
            return new GameOrderSummaryPageOutput
            {
                Items = new List<GameOrderSummaryItemDto>(),
                Message = "#没有配置注单明细检索#"
            };
        }

        var userId = input.UserID;
        if (userId <= 0 && !string.IsNullOrWhiteSpace(input.UserName))
        {
            var name = input.UserName.Trim();
            var ids = await _db.Queryable<User>()
                .Where(u => u.SiteID == siteId && u.UserName == name)
                .Select(u => u.ID)
                .Take(1)
                .ToListAsync(cancellationToken);
            userId = ids.FirstOrDefault();
            if (userId <= 0)
            {
                return new GameOrderSummaryPageOutput
                {
                    Items = new List<GameOrderSummaryItemDto>(),
                    Total = 0
                };
            }
        }

        var typeName = input.TypeName;
        if (string.IsNullOrWhiteSpace(typeName) && input.Type > 0)
            typeName = GameTypeMeta.Format(input.Type);

        var result = await _es.SearchAsync(
            siteId,
            category,
            page,
            pageSize,
            userId,
            start,
            end,
            input.SourceID,
            cancellationToken,
            input.CurrencyCode,
            typeName,
            input.PlayerName);
        if (result == null || result.QueryFailed || result.NotConfigured)
        {
            return new GameOrderSummaryPageOutput
            {
                Items = new List<GameOrderSummaryItemDto>(),
                Message = result?.Message ?? "#注单明细查询失败#"
            };
        }

        var items = (result.Items ?? new List<GameOrderEsHit>()).Select(hit => new GameOrderSummaryItemDto
        {
            Date = hit.CreateAt?.Date ?? default,
            UserID = hit.UserID,
            UserName = hit.UserName ?? string.Empty,
            Type = hit.Type > 0 ? hit.Type : input.Type,
            TypeName = hit.TypeName ?? string.Empty,
            Category = hit.Category,
            Currency = hit.Currency,
            CurrencyCode = hit.CurrencyCode ?? string.Empty,
            BetMoney = hit.BetMoney,
            BetAmount = hit.BetAmount,
            Money = hit.Money,
            OrderCount = 1,
            SourceID = hit.SourceID ?? string.Empty,
            PlayerName = hit.PlayerName ?? string.Empty,
            Game = hit.Game ?? string.Empty,
            Status = hit.Status ?? string.Empty,
            SettlementAt = hit.SettlementAt,
            EsID = hit.EsID ?? string.Empty
        }).ToList();

        var missingIds = items.Where(i => string.IsNullOrEmpty(i.UserName) && i.UserID > 0)
            .Select(i => i.UserID).Distinct().ToList();
        if (missingIds.Count > 0)
        {
            var nameMap = await LoadNamesAsync(siteId, missingIds, cancellationToken);
            foreach (var item in items)
            {
                if (string.IsNullOrEmpty(item.UserName) && nameMap.TryGetValue(item.UserID, out var name))
                    item.UserName = name;
            }
        }

        return new GameOrderSummaryPageOutput
        {
            Total = result.Total,
            Items = items
        };
    }

    /// <summary>有单号，或调用方明确要明细时，不走日汇总。</summary>
    private static bool WantsEsDetail(GameOrderSummaryPageInput input) =>
        input.UseEsDetail || !string.IsNullOrWhiteSpace(input.SourceID);

    /// <summary>把检索命中收成详情。</summary>
    private static GameOrderInfoStubDto FromEsHit(GameOrderEsHit hit, string sourceId, int type) =>
        new()
        {
            SourceID = string.IsNullOrEmpty(hit.SourceID) ? sourceId : hit.SourceID,
            Type = hit.Type > 0 ? hit.Type : type,
            TypeName = hit.TypeName ?? string.Empty,
            Message = string.Empty,
            Found = true,
            UserID = hit.UserID,
            UserName = hit.UserName ?? string.Empty,
            PlayerName = hit.PlayerName ?? string.Empty,
            TradeAmount = hit.BetMoney,
            BetMoney = hit.BetMoney,
            BetAmount = hit.BetAmount,
            Money = hit.Money,
            CreateAt = hit.CreateAt,
            SettlementAt = hit.SettlementAt,
            Currency = hit.Currency,
            CurrencyCode = hit.CurrencyCode ?? string.Empty,
            Game = hit.Game ?? string.Empty,
            Status = hit.Status ?? string.Empty,
            EsID = hit.EsID ?? string.Empty,
            Category = hit.Category
        };

    /// <summary>视图空或失败时，用 rpt_UserDateGame 聚合分页。</summary>
    private async Task<GameOrderSummaryPageOutput> PageFromUserDateGameAsync(
        int siteId,
        GameOrderSummaryPageInput input,
        DateTime start,
        DateTime end,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _db.Queryable<UserDateGame>()
            .Where(v => v.SiteID == siteId && v.Date >= start && v.Date < end);
        if (input.Category >= 0)
            query = query.Where(v => v.Category == input.Category);
        if (input.Type > 0)
            query = query.Where(v => v.Type == input.Type);
        if (input.UserID > 0)
            query = query.Where(v => v.UserID == input.UserID);
        if (input.Currency >= 0)
            query = query.Where(v => v.Currency == input.Currency);

        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderBy(v => v.Date, OrderByType.Desc)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        var nameMap = await LoadNamesAsync(siteId, rows.Select(r => r.UserID).Distinct().ToList(), cancellationToken);
        return new GameOrderSummaryPageOutput
        {
            Total = total,
            Items = rows.Select(v => new GameOrderSummaryItemDto
            {
                Date = v.Date,
                UserID = v.UserID,
                UserName = nameMap.TryGetValue(v.UserID, out var un) ? un : string.Empty,
                Type = v.Type,
                Category = v.Category,
                Currency = v.Currency,
                BetMoney = v.BetMoney,
                BetAmount = v.BetMoney,
                Money = v.Money,
                OrderCount = 0
            }).ToList()
        };
    }

    /// <summary>取用户名。</summary>
    private async Task<Dictionary<int, string>> LoadNamesAsync(
        int siteId,
        List<int> ids,
        CancellationToken cancellationToken)
    {
        var nameMap = new Dictionary<int, string>();
        if (ids.Count == 0)
            return nameMap;
        var names = await _db.Queryable<User>()
            .Where(u => u.SiteID == siteId && ids.Contains(u.ID))
            .Select(u => new { u.ID, u.UserName }).ToListAsync(cancellationToken);
        foreach (var n in names)
            nameMap[n.ID] = n.UserName ?? string.Empty;
        return nameMap;
    }

    /// <summary>报表文件列表。</summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行列表</returns>
    public async Task<List<SiteFileReportItemDto>> ListFilesAsync(CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<SiteFileDownload>()
            .Where(f => f.SiteID == siteId).OrderBy(f => f.ID, OrderByType.Desc)
            .Take(200).ToListAsync(cancellationToken);
        return rows.Select(f => new SiteFileReportItemDto
        {
            ID = f.ID,
            FileName = f.FileName,
            FilePath = f.FilePath,
            FileSize = f.FileSize,
            Frequency = f.Frequency,
            CreateAt = f.CreateAt
        }).ToList();
    }
}

/// <summary>注单汇总筛选。</summary>
public sealed class GameOrderSummaryPageInput
{
    /// <summary>页码。</summary>
    public int Page { get; set; } = 1;

    /// <summary>每页。</summary>
    public int PageSize { get; set; } = 20;

    /// <summary>开始。</summary>
    public DateTime? StartAt { get; set; }

    /// <summary>结束。</summary>
    public DateTime? EndAt { get; set; }

    /// <summary>分类；-1=全部（彩票是 0）。</summary>
    public int Category { get; set; } = -1;

    /// <summary>游戏类型；0=全部。</summary>
    public int Type { get; set; }

    /// <summary>会员。</summary>
    public int UserID { get; set; }

    /// <summary>币种；-1=全部。</summary>
    public int Currency { get; set; } = -1;

    /// <summary>第三方单号。有值时改查注单明细，不再走日汇总。</summary>
    public string SourceID { get; set; }

    /// <summary>为 true 时按分类查注单明细。默认 false，仍走日汇总。</summary>
    public bool UseEsDetail { get; set; }

    /// <summary>币种字母代码（ES）。</summary>
    public string CurrencyCode { get; set; }

    /// <summary>游戏类型枚举名（ES Type）。</summary>
    public string TypeName { get; set; }

    /// <summary>玩家名。</summary>
    public string PlayerName { get; set; }

    /// <summary>用户名（解析成 UserID）。</summary>
    public string UserName { get; set; }
}

/// <summary>注单汇总分页。</summary>
public sealed class GameOrderSummaryPageOutput
{
    /// <summary>总数。</summary>
    public int Total { get; set; }

    /// <summary>行。</summary>
    public List<GameOrderSummaryItemDto> Items { get; set; }

    /// <summary>说明。日汇总成功时为空；明细没查到或失败时带原因。</summary>
    public string Message { get; set; }
}

/// <summary>注单汇总行。</summary>
public sealed class GameOrderSummaryItemDto
{
    /// <summary>日期。</summary>
    public DateTime Date { get; set; }

    /// <summary>会员。</summary>
    public int UserID { get; set; }

    /// <summary>用户名。</summary>
    public string UserName { get; set; }

    /// <summary>类型。</summary>
    public int Type { get; set; }

    /// <summary>分类。</summary>
    public int Category { get; set; }

    /// <summary>币种。</summary>
    public int Currency { get; set; }

    /// <summary>投注。</summary>
    public decimal BetMoney { get; set; }

    /// <summary>有效投注。</summary>
    public decimal BetAmount { get; set; }

    /// <summary>盈亏。</summary>
    public decimal Money { get; set; }

    /// <summary>单量。</summary>
    public int OrderCount { get; set; }

    /// <summary>第三方单号（打赏等明细有）。</summary>
    public string SourceID { get; set; }

    /// <summary>玩家名。注单明细才有。</summary>
    public string PlayerName { get; set; }

    /// <summary>游戏名。注单明细才有。</summary>
    public string Game { get; set; }

    /// <summary>状态原文。注单明细才有。</summary>
    public string Status { get; set; }

    /// <summary>游戏类型原文。集群里常是厂商代码，数字类型对不上时看这个。</summary>
    public string TypeName { get; set; }

    /// <summary>币种原文。集群里常是字母代码。</summary>
    public string CurrencyCode { get; set; }

    /// <summary>结算时间。注单明细才有。</summary>
    public DateTime? SettlementAt { get; set; }

    /// <summary>索引文档号。注单明细才有。</summary>
    public string EsID { get; set; }
}

/// <summary>报表文件行。</summary>
public sealed class SiteFileReportItemDto
{
    /// <summary>编号。</summary>
    public int ID { get; set; }

    /// <summary>文件名。</summary>
    public string FileName { get; set; }

    /// <summary>路径。</summary>
    public string FilePath { get; set; }

    /// <summary>大小。</summary>
    public int FileSize { get; set; }

    /// <summary>下载次数。</summary>
    public int Frequency { get; set; }

    /// <summary>创建。</summary>
    public DateTime CreateAt { get; set; }
}
