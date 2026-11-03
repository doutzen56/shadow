namespace SimpX.Net.Application.Shared;

/// <summary>
/// 游戏注单明细查询。共享层只认这个接口，不引用检索客户端。
/// 租户后台有实现就注入；其它宿主可以不注册，调用方要容忍空。
/// </summary>
public interface IGameOrderEsQuery
{
    /// <summary>
    /// 按站点和分类分页查注单。分类决定索引；-1 表示全部，走综合索引。
    /// </summary>
    /// <param name="siteId">站点</param>
    /// <param name="category">分类；-1 为全部</param>
    /// <param name="page">页码，从 1 开始</param>
    /// <param name="size">每页条数</param>
    /// <param name="userId">会员；小于等于 0 表示不限</param>
    /// <param name="start">开始时间，含这一刻；空表示不限</param>
    /// <param name="end">结束时间，不含这一刻；空表示不限</param>
    /// <param name="sourceId">第三方单号；空表示不限</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页。没配检索或查询失败时看 <see cref="GameOrderEsPage.QueryFailed"/> 和说明</returns>
    Task<GameOrderEsPage> SearchAsync(
        int siteId,
        int category,
        int page,
        int size,
        int userId,
        DateTime? start,
        DateTime? end,
        string sourceId,
        CancellationToken cancellationToken = default,
        string currencyCode = null,
        string typeName = null,
        string playerName = null);

    /// <summary>
    /// 按第三方单号取一条注单。
    /// </summary>
    /// <param name="siteId">站点</param>
    /// <param name="sourceId">第三方单号</param>
    /// <param name="categoryHint">分类；空或 -1 走综合索引，有分类先查对应索引</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>命中或说明。找不到时 <see cref="GameOrderEsHit.Found"/> 为 false</returns>
    Task<GameOrderEsHit> GetBySourceIdAsync(
        int siteId,
        string sourceId,
        int? categoryHint,
        CancellationToken cancellationToken = default);

    /// <summary>打赏索引 rewardlog* 分页（旧 Admin reward）。</summary>
    Task<GameOrderEsPage> SearchRewardAsync(
        int siteId,
        int page,
        int size,
        int userId,
        DateTime? start,
        DateTime? end,
        string sourceId,
        string currencyCode = null,
        string typeName = null,
        string playerName = null,
        CancellationToken cancellationToken = default);
}

/// <summary>注单明细分页。</summary>
public sealed class GameOrderEsPage
{
    /// <summary>命中总数。查询失败时为 0。</summary>
    public int Total { get; set; }

    /// <summary>本页注单。</summary>
    public List<GameOrderEsHit> Items { get; set; } = new();

    /// <summary>给调用方看的说明。成功时为空。</summary>
    public string Message { get; set; }

    /// <summary>检索没配上，不是集群报错。</summary>
    public bool NotConfigured { get; set; }

    /// <summary>查询过程失败。为 true 时不要当成“没有数据”。</summary>
    public bool QueryFailed { get; set; }
}

/// <summary>一条注单明细。</summary>
public sealed class GameOrderEsHit
{
    /// <summary>是否查到这笔单。</summary>
    public bool Found { get; set; }

    /// <summary>检索没配上。</summary>
    public bool NotConfigured { get; set; }

    /// <summary>查询过程失败。</summary>
    public bool QueryFailed { get; set; }

    /// <summary>说明。</summary>
    public string Message { get; set; }

    /// <summary>第三方单号。</summary>
    public string SourceID { get; set; }

    /// <summary>索引里的文档号。</summary>
    public string EsID { get; set; }

    /// <summary>会员。</summary>
    public int UserID { get; set; }

    /// <summary>用户名。</summary>
    public string UserName { get; set; }

    /// <summary>玩家名。</summary>
    public string PlayerName { get; set; }

    /// <summary>投注。</summary>
    public decimal BetMoney { get; set; }

    /// <summary>有效投注。</summary>
    public decimal BetAmount { get; set; }

    /// <summary>盈亏。</summary>
    public decimal Money { get; set; }

    /// <summary>游戏类型数字。集群里常见的是字母代码，对不上时为 0，看 <see cref="TypeName"/>。</summary>
    public int Type { get; set; }

    /// <summary>游戏类型原文，例如厂商代码。</summary>
    public string TypeName { get; set; }

    /// <summary>状态原文。</summary>
    public string Status { get; set; }

    /// <summary>币种数字。集群里常见的是字母代码，对不上时为 0，看 <see cref="CurrencyCode"/>。</summary>
    public int Currency { get; set; }

    /// <summary>币种原文。</summary>
    public string CurrencyCode { get; set; }

    /// <summary>游戏名。</summary>
    public string Game { get; set; }

    /// <summary>下注时间。</summary>
    public DateTime? CreateAt { get; set; }

    /// <summary>结算时间。没有或是空日期时为 null。</summary>
    public DateTime? SettlementAt { get; set; }

    /// <summary>分类。对不上已知名字时用请求里的分类。</summary>
    public int Category { get; set; }
}
