namespace SimpX.Net.Application.Shared;

/// <summary>
/// ES 聚合用户日结游戏报表。对齐旧 GameReportAgent.SaveUserDateGameFromES / Battle。
/// Shared 不引用检索客户端；Schedule/Admin 有实现就注入。
/// </summary>
public interface IUserGameDateEsAggregate
{
    /// <summary>
    /// 按 SettlementAt ∈ [start, end) 复合聚合 UserID/SiteID/Category/Type/Currency。
    /// </summary>
    /// <param name="startBj">起（含），北京时间</param>
    /// <param name="endBj">止（不含），北京时间</param>
    /// <param name="typeNames">可选：限制 Type terms（EST/UTC 来自 game_UETimeZone.GameName）</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task<UserGameDateEsAggregateResult> AggregateAsync(
        DateTime startBj,
        DateTime endBj,
        IReadOnlyCollection<string> typeNames = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 对战索引 fantasylog 复合聚合（无 Category 源；Category 固定 Battle=9）。
    /// </summary>
    Task<UserGameDateEsAggregateResult> AggregateBattleAsync(
        DateTime startBj,
        DateTime endBj,
        CancellationToken cancellationToken = default);
}

/// <summary>ES 聚合结果。</summary>
public sealed class UserGameDateEsAggregateResult
{
    /// <summary>桶。</summary>
    public List<UserGameDateEsBucket> Buckets { get; set; } = new();

    /// <summary>业务检索未配置。</summary>
    public bool NotConfigured { get; set; }

    /// <summary>查询失败（非「无数据」）。</summary>
    public bool QueryFailed { get; set; }

    /// <summary>说明。</summary>
    public string Message { get; set; }
}

/// <summary>一条聚合桶 → 对应 rpt_UserDateGame* 一行键。</summary>
public sealed class UserGameDateEsBucket
{
    public int SiteID { get; set; }
    public int UserID { get; set; }
    public int Category { get; set; }
    public int Type { get; set; }
    public int Currency { get; set; }
    public decimal BetMoney { get; set; }
    public decimal BetAmount { get; set; }
    public decimal Money { get; set; }
    public int OrderCount { get; set; }
    public decimal JackPot { get; set; }
    public decimal JackPotCount { get; set; }
}
