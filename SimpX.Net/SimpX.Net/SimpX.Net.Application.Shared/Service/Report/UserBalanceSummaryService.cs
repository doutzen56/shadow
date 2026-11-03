using SimpX.Net.Data.Entity.Games;
using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 余额汇总只读。主钱包按币种汇总；游戏钱包按游戏汇总。
/// </summary>
public sealed partial class UserBalanceSummaryService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;

    /// <summary>
    /// 注入依赖。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站</param>
    public UserBalanceSummaryService(IBizDbAccessor biz, ICurrentSiteAccessor site)
    {
        _db = biz.Db;
        _site = site;
    }

    /// <summary>
    /// 主钱包按币种合计。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行列表</returns>
    public async Task<List<WalletBalanceItemDto>> ListWalletAsync(CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<UserMoney>()
            .Where(m => m.SiteID == siteId)
            .GroupBy(m => m.Currency)
            .Select(m => new
            {
                Currency = m.Currency,
                TotalMoney = SqlFunc.AggregateSum(m.TotalMoney),
                LockMoney = SqlFunc.AggregateSum(m.LockMoney),
                Unbalanced = SqlFunc.AggregateSum(m.Unbalanced),
                UserCount = SqlFunc.AggregateCount(m.UserID)
            })
            .ToListAsync(cancellationToken);

        return rows.OrderBy(r => r.Currency).Select(r => new WalletBalanceItemDto
        {
            Currency = r.Currency,
            TotalMoney = r.TotalMoney,
            LockMoney = r.LockMoney,
            Unbalanced = r.Unbalanced,
            UserCount = r.UserCount
        }).ToList();
    }

    /// <summary>
    /// 游戏钱包按游戏合计。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行列表</returns>
    public async Task<List<GameBalanceItemDto>> ListGameAsync(CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<GameUser>()
            .Where(g => g.SiteID == siteId)
            .GroupBy(g => g.Game)
            .Select(g => new
            {
                Game = g.Game,
                Balance = SqlFunc.AggregateSum(g.Balance),
                UserCount = SqlFunc.AggregateCount(g.UserID)
            })
            .ToListAsync(cancellationToken);

        return rows.OrderByDescending(r => r.Balance).Select(r => new GameBalanceItemDto
        {
            Game = r.Game,
            Balance = r.Balance,
            UserCount = r.UserCount
        }).ToList();
    }

    // --- merged from UserBalanceSummaryService.WaveGaps.cs ---



    /// <summary>
    /// 对齐旧 balance。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> BalanceAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return BalanceCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 balanceinfo。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> BalanceinfoAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return BalanceinfoCoreAsync(input, cancellationToken);
    }


    /// <summary>核心：balance —— 钱包余额列表。</summary>
    private async Task<object?> BalanceCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        _ = input;
        return await ListWalletAsync(cancellationToken);
    }


    /// <summary>核心：balanceinfo —— 钱包+游戏余额。</summary>
    private async Task<object?> BalanceinfoCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        _ = input;
        var wallet = await ListWalletAsync(cancellationToken);
        var game = await ListGameAsync(cancellationToken);
        return new { Wallet = wallet, Game = game };
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

/// <summary>主钱包汇总行。</summary>
public sealed class WalletBalanceItemDto
{
    /// <summary>币种。</summary>
    public int Currency { get; set; }

    /// <summary>总余额。</summary>
    public decimal TotalMoney { get; set; }

    /// <summary>锁定。</summary>
    public decimal LockMoney { get; set; }

    /// <summary>未结算。</summary>
    public decimal Unbalanced { get; set; }

    /// <summary>人数。</summary>
    public int UserCount { get; set; }
}

/// <summary>游戏钱包汇总行。</summary>
public sealed class GameBalanceItemDto
{
    /// <summary>游戏编号。</summary>
    public int Game { get; set; }

    /// <summary>余额合计。</summary>
    public decimal Balance { get; set; }

    /// <summary>人数。</summary>
    public int UserCount { get; set; }
}
