namespace SimpX.Net.Application.Shared;

/// <summary>
/// 注单分类入口：对齐旧 /admin/game/log/*。默认日汇总 + 打赏表。
/// 带单号或 UseEsDetail 时走注单明细；详情在打赏未命中后按分类查索引。
/// </summary>
public sealed partial class GameOrderSummaryService
{
    /// <summary>旧 GameCategory.Lottery。</summary>
    public const int CategoryLottery = 0;

    /// <summary>旧 GameCategory.ESport。</summary>
    public const int CategoryESport = 1;

    /// <summary>旧 GameCategory.Slot。</summary>
    public const int CategorySlot = 2;

    /// <summary>旧 GameCategory.Live。</summary>
    public const int CategoryLive = 3;

    /// <summary>旧 GameCategory.Chess。</summary>
    public const int CategoryChess = 4;

    /// <summary>旧 GameCategory.Sport。</summary>
    public const int CategorySport = 5;

    /// <summary>旧 GameCategory.Fish。</summary>
    public const int CategoryFish = 6;

    /// <summary>旧 GameCategory.Animal。</summary>
    public const int CategoryAnimal = 7;

    /// <summary>旧 GameCategory.MiniGame。</summary>
    public const int CategoryMini = 8;

    /// <summary>旧 GameCategory.Battle（幻想/对战近似）。</summary>
    public const int CategoryFantasy = 9;

    /// <summary>
    /// 分类对应的注单索引。幻想同时查对战和幻想；未知或全部用综合索引。
    /// </summary>
    /// <param name="category">分类；-1 或未列出的值走综合索引</param>
    /// <returns>索引通配</returns>
    /// <summary>
    /// 旧 Admin /admin/game/log/* 走 GameLogV2 月索引（别名根 gamelogv2），再按 Category 过滤。
    /// 分类物理索引（livelog*/chesslog*）仅作兼容回落，主查询用 <see cref="EsAdminLogIndexPattern"/>。
    /// </summary>
    public static string EsIndexPattern(int category) => EsAdminLogIndexPattern();

    /// <summary>旧站 GameLogV2 月滚动索引通配。</summary>
    public static string EsAdminLogIndexPattern() => "gamelogv2*";

    /// <summary>
    /// 按分类分页。对齐旧 animal/chess/slot 等列表入口。
    /// 筛选里带单号或 UseEsDetail 时改查注单明细。
    /// </summary>
    /// <param name="category">分类；-1=全部</param>
    /// <param name="input">筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页</returns>
    public Task<GameOrderSummaryPageOutput> PageByCategoryAsync(
        int category,
        GameOrderSummaryPageInput input,
        CancellationToken cancellationToken = default)
    {
        input ??= new GameOrderSummaryPageInput();
        input.Category = category;
        return PageAsync(input, cancellationToken);
    }

    /// <summary>
    /// 导出：无异步导出管线时返回已有报表文件列表，避免假成功空包。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>已有报表文件</returns>
    public Task<List<SiteFileReportItemDto>> ExportStubAsync(CancellationToken cancellationToken = default) =>
        ListFilesAsync(cancellationToken);

    /// <summary>
    /// 同步详情入口（兼容旧 App 壳）；内部不查库，请改调异步 Info。
    /// </summary>
    /// <param name="sourceId">第三方单号</param>
    /// <param name="type">游戏类型</param>
    /// <returns>说明</returns>
    public GameOrderInfoStubDto InfoStub(string? sourceId, int type) =>
        new()
        {
            SourceID = sourceId ?? string.Empty,
            Type = type,
            Message = "#请改调异步 Info；日汇总无明细，打赏查 game_RewardLog#"
        };
}

/// <summary>注单详情（打赏命中或说明）。</summary>
public sealed class GameOrderInfoStubDto
{
    /// <summary>第三方单号。</summary>
    public string SourceID { get; set; }

    /// <summary>游戏类型。</summary>
    public int Type { get; set; }

    /// <summary>说明；命中时可空。</summary>
    public string Message { get; set; }

    /// <summary>是否命中明细。</summary>
    public bool Found { get; set; }

    /// <summary>会员。</summary>
    public int UserID { get; set; }

    /// <summary>玩家名。</summary>
    public string PlayerName { get; set; }

    /// <summary>交易额。</summary>
    public decimal TradeAmount { get; set; }

    /// <summary>打赏时间。</summary>
    public DateTime? RewardAt { get; set; }

    /// <summary>币种。</summary>
    public int Currency { get; set; }

    /// <summary>会员用户名。注单明细才有。</summary>
    public string UserName { get; set; }

    /// <summary>投注。打赏命中时与交易额相同。</summary>
    public decimal BetMoney { get; set; }

    /// <summary>有效投注。</summary>
    public decimal BetAmount { get; set; }

    /// <summary>盈亏。</summary>
    public decimal Money { get; set; }

    /// <summary>游戏类型原文。</summary>
    public string TypeName { get; set; }

    /// <summary>币种原文。</summary>
    public string CurrencyCode { get; set; }

    /// <summary>游戏名。</summary>
    public string Game { get; set; }

    /// <summary>状态原文。</summary>
    public string Status { get; set; }

    /// <summary>下注时间。</summary>
    public DateTime? CreateAt { get; set; }

    /// <summary>结算时间。</summary>
    public DateTime? SettlementAt { get; set; }

    /// <summary>索引文档号。</summary>
    public string EsID { get; set; }

    /// <summary>分类。</summary>
    public int Category { get; set; }
}
