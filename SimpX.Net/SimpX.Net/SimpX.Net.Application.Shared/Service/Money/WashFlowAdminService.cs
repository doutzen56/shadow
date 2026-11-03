using System.Data;
using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 打码流水管理。对齐旧 /admin/money/washflow/*：清单、日志、手动增减。
/// </summary>
public sealed class WashFlowAdminService : ITransient
{
    /// <summary>旧 MoneyType.AdminIn（管理员调整流水用）。</summary>
    public const int MoneyTypeAdminIn = 250;

    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;

    /// <summary>
    /// 注入库与定站。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站</param>
    public WashFlowAdminService(IBizDbAccessor biz, ICurrentSiteAccessor site)
    {
        _db = biz.Db;
        _site = site;
    }

    /// <summary>
    /// 当前打码量清单。对齐旧 list。
    /// </summary>
    /// <param name="userId">会员</param>
    /// <param name="currency">币种（必填）</param>
    /// <param name="orderId">提现订单；&gt;0 时筛 OrderID</param>
    /// <param name="showOpenOnly">true 时只看未挂提现单的流水（OrderID=0）</param>
    /// <param name="startAt">开始</param>
    /// <param name="endAt">结束</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行与合计</returns>
    public async Task<WashFlowListResultDto> ListAsync(
        int userId,
        int currency,
        int orderId = 0,
        bool showOpenOnly = false,
        DateTime? startAt = null,
        DateTime? endAt = null,
        CancellationToken cancellationToken = default)
    {
        if (currency <= 0)
            throw Oops.Oh("#货币不能为空#");
        var siteId = _site.RequireSiteId();
        var query = _db.Queryable<UserWashFlow>()
            .Where(t => t.SiteID == siteId && t.UserID == userId);
        if (startAt.HasValue && startAt.Value != default)
            query = query.Where(t => t.CreateAt >= startAt.Value);
        if (endAt.HasValue && endAt.Value != default)
            query = query.Where(t => t.CreateAt <= endAt.Value);
        if (showOpenOnly)
            query = query.Where(t => t.OrderID == 0);
        if (orderId > 0)
            query = query.Where(t => t.OrderID == orderId);

        var rows = await query.OrderBy(t => t.CreateAt, OrderByType.Desc).ToListAsync(cancellationToken);
        // 汇率本刀按 1；币种展示用入参
        var total = new
        {
            BetMoney = rows.Sum(p => p.BetMoney),
            Money = rows.Sum(p => p.Money),
            Flow = rows.Sum(p => p.Flow)
        };
        return new WashFlowListResultDto
        {
            Total = total,
            Items = rows.Select(t => new WashFlowListItemDto
            {
                ID = t.ID,
                UserID = t.UserID,
                CreateAt = t.CreateAt,
                EndAt = t.EndAt,
                Money = t.Money,
                Type = t.Type,
                Currency = t.Currency,
                BetMoney = t.BetMoney,
                Flow = t.Flow,
                CurMoney = currency,
                Description = t.Description ?? string.Empty,
                OrderID = t.OrderID,
                EventID = t.EventID,
                ReviewID = t.ReviewID
            }).ToList()
        };
    }

    /// <summary>
    /// 流水日志详情。对齐旧 log。
    /// </summary>
    /// <param name="userId">会员</param>
    /// <param name="washId">流水编号</param>
    /// <param name="currency">展示币种</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>日志行</returns>
    public async Task<List<WashFlowLogItemDto>> LogAsync(
        int userId,
        int washId,
        int currency,
        CancellationToken cancellationToken = default)
    {
        if (currency <= 0)
            throw Oops.Oh("#货币不能为空#");
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<WashFlowLog>()
            .Where(t => t.SiteID == siteId && t.UserID == userId && t.WashID == washId)
            .OrderBy(t => t.CreateAt, OrderByType.Desc)
            .ToListAsync(cancellationToken);
        return rows.Select(t =>
        {
            var esid = t.EsID ?? string.Empty;
            if (esid.Contains('_', StringComparison.Ordinal))
                esid = esid[(esid.LastIndexOf('_') + 1)..];
            return new WashFlowLogItemDto
            {
                ID = t.ID,
                BetMoney = t.BetMoney,
                WashFlow = t.WashFlow,
                Currency = currency,
                CreateAt = t.CreateAt,
                Description = t.Description ?? string.Empty,
                EsID = esid
            };
        }).ToList();
    }

    /// <summary>
    /// 手动增减打码量。对齐旧 updatewashflow / updatewashflowsingle。
    /// Bet/BetFull：赠送有效流水（正 BetMoney）；Flow：增加要求（负 BetMoney）；走 usr_AddWashFlowLog。
    /// </summary>
    /// <param name="userId">会员</param>
    /// <param name="money">金额（正数）</param>
    /// <param name="type">Bet / BetFull / Flow / BetID</param>
    /// <param name="description">备注</param>
    /// <param name="currency">币种</param>
    /// <param name="washId">单条流水编号（BetID 用）</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task UpdateAsync(
        int userId,
        decimal money,
        string type,
        string? description,
        int currency,
        int washId = 0,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0 || currency <= 0)
            throw Oops.Oh("#系统错误#");
        money = Math.Abs(money);
        if (money <= 0 && !string.Equals(type, "BetFull", StringComparison.OrdinalIgnoreCase))
            throw Oops.Oh("#金额错误#");

        var siteId = _site.RequireSiteId();
        var wallets = await _db.Queryable<UserMoney>()
            .Where(m => m.SiteID == siteId && m.UserID == userId && m.Currency == currency)
            .Take(1)
            .ToListAsync(cancellationToken);
        var wallet = wallets.FirstOrDefault();
        var action = (type ?? string.Empty).Trim();

        if (action is "Bet" or "BetFull")
        {
            var wash = wallet?.WashFlow ?? 0m;
            if (wash >= 0)
            {
                throw Oops.Oh(action == "BetFull"
                    ? "设置流水已经打满"
                    : "流水已经打满,不能赠送");
            }

            if (action == "BetFull")
                money = Math.Abs(wash);
        }

        var desc = description ?? string.Empty;
        decimal betMoney;
        switch (action)
        {
            case "Bet":
            case "BetID":
            case "BetFull":
                betMoney = money;
                break;
            case "Flow":
                betMoney = -money;
                break;
            default:
                throw Oops.Oh("#未指定有效的动作#");
        }

        await CallAddWashFlowLogAsync(siteId, userId, betMoney, desc, currency, cancellationToken);

        if (action == "BetFull")
        {
            // 打满：钱包流水要求清零
            if (wallet != null)
            {
                wallet.WashFlow = 0;
                await _db.Updateable(wallet)
                    .UpdateColumns(m => new { m.WashFlow })
                    .ExecuteCommandAsync(cancellationToken);
            }
        }

        _ = washId;
    }

    /// <summary>调 usr_AddWashFlowLog；失败抛业务错。</summary>
    private async Task CallAddWashFlowLogAsync(
        int siteId,
        int userId,
        decimal betMoney,
        string description,
        int currency,
        CancellationToken cancellationToken)
    {
        try
        {
            var washOut = new SugarParameter("@WashFlow", DBNull.Value)
            {
                Direction = ParameterDirection.Output,
                DbType = System.Data.DbType.Decimal
            };
            await _db.Ado.UseStoredProcedure().ExecuteCommandAsync(
                "usr_AddWashFlowLog",
                new SugarParameter("@SiteID", siteId),
                new SugarParameter("@UserID", userId),
                new SugarParameter("@BetMoney", betMoney),
                new SugarParameter("@Description", description),
                washOut,
                new SugarParameter("@Money", 0m),
                new SugarParameter("@Type", MoneyTypeAdminIn),
                new SugarParameter("@Currency", currency),
                new SugarParameter("@EventID", 0),
                new SugarParameter("@ReviewID", 0));
            _ = cancellationToken;
        }
        catch (Exception ex)
        {
            throw Oops.Oh($"#流水更新失败#: {ex.Message}");
        }
    }
}

/// <summary>打码清单结果。</summary>
public sealed class WashFlowListResultDto
{
    /// <summary>合计。</summary>
    public object Total { get; set; }

    /// <summary>行。</summary>
    public List<WashFlowListItemDto> Items { get; set; } = [];
}

/// <summary>打码清单行。</summary>
public sealed class WashFlowListItemDto
{
    /// <summary>编号。</summary>
    public int ID { get; set; }

    /// <summary>会员。</summary>
    public int UserID { get; set; }

    /// <summary>开始。</summary>
    public DateTime CreateAt { get; set; }

    /// <summary>结束。</summary>
    public DateTime EndAt { get; set; }

    /// <summary>入款金额。</summary>
    public decimal Money { get; set; }

    /// <summary>入款类型。</summary>
    public int Type { get; set; }

    /// <summary>币种。</summary>
    public int Currency { get; set; }

    /// <summary>已打码。</summary>
    public decimal BetMoney { get; set; }

    /// <summary>要求流水。</summary>
    public decimal Flow { get; set; }

    /// <summary>查询币种（展示）。</summary>
    public int CurMoney { get; set; }

    /// <summary>备注。</summary>
    public string Description { get; set; }

    /// <summary>提现单号。</summary>
    public int OrderID { get; set; }

    /// <summary>活动。</summary>
    public int EventID { get; set; }

    /// <summary>加款审核号。</summary>
    public int ReviewID { get; set; }
}

/// <summary>打码日志行。</summary>
public sealed class WashFlowLogItemDto
{
    /// <summary>编号。</summary>
    public int ID { get; set; }

    /// <summary>打码。</summary>
    public decimal BetMoney { get; set; }

    /// <summary>剩余要求。</summary>
    public decimal WashFlow { get; set; }

    /// <summary>币种。</summary>
    public int Currency { get; set; }

    /// <summary>时间。</summary>
    public DateTime CreateAt { get; set; }

    /// <summary>备注。</summary>
    public string Description { get; set; }

    /// <summary>ES 单号尾段。</summary>
    public string EsID { get; set; }
}
