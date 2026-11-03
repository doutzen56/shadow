using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 注单分类入口（与 GameOrderSummaryAppService 同服务名）。
/// </summary>
public partial class GameOrderSummaryAppService
{
    /// <summary>全部。对齐旧 list。</summary>
    /// <param name="input">筛选</param>
    /// <returns>分页</returns>
    [ApiDescriptionSettings(Name = "List"), HttpPost]
    [DisplayName("注单列表")]
    public Task<GameOrderSummaryPageOutput> List(GameOrderSummaryPageInput input) =>
        _svc.PageAsync(input ?? new GameOrderSummaryPageInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>棋牌。</summary>
    /// <param name="input">筛选</param>
    /// <returns>分页</returns>
    [ApiDescriptionSettings(Name = "Chess"), HttpPost]
    [DisplayName("棋牌注单")]
    public Task<GameOrderSummaryPageOutput> Chess(GameOrderSummaryPageInput input) =>
        _svc.PageByCategoryAsync(GameOrderSummaryService.CategoryChess, input, App.HttpContext?.RequestAborted ?? default);

    /// <summary>电竞。</summary>
    /// <param name="input">筛选</param>
    /// <returns>分页</returns>
    [ApiDescriptionSettings(Name = "Esport"), HttpPost]
    [DisplayName("电竞注单")]
    public Task<GameOrderSummaryPageOutput> Esport(GameOrderSummaryPageInput input) =>
        _svc.PageByCategoryAsync(GameOrderSummaryService.CategoryESport, input, App.HttpContext?.RequestAborted ?? default);

    /// <summary>体育。</summary>
    /// <param name="input">筛选</param>
    /// <returns>分页</returns>
    [ApiDescriptionSettings(Name = "Sport"), HttpPost]
    [DisplayName("体育注单")]
    public Task<GameOrderSummaryPageOutput> Sport(GameOrderSummaryPageInput input) =>
        _svc.PageByCategoryAsync(GameOrderSummaryService.CategorySport, input, App.HttpContext?.RequestAborted ?? default);

    /// <summary>彩票。</summary>
    /// <param name="input">筛选</param>
    /// <returns>分页</returns>
    [ApiDescriptionSettings(Name = "Lottery"), HttpPost]
    [DisplayName("彩票注单")]
    public Task<GameOrderSummaryPageOutput> Lottery(GameOrderSummaryPageInput input) =>
        _svc.PageByCategoryAsync(GameOrderSummaryService.CategoryLottery, input, App.HttpContext?.RequestAborted ?? default);

    /// <summary>真人。</summary>
    /// <param name="input">筛选</param>
    /// <returns>分页</returns>
    [ApiDescriptionSettings(Name = "Live"), HttpPost]
    [DisplayName("真人注单")]
    public Task<GameOrderSummaryPageOutput> Live(GameOrderSummaryPageInput input) =>
        _svc.PageByCategoryAsync(GameOrderSummaryService.CategoryLive, input, App.HttpContext?.RequestAborted ?? default);

    /// <summary>电子。</summary>
    /// <param name="input">筛选</param>
    /// <returns>分页</returns>
    [ApiDescriptionSettings(Name = "Slot"), HttpPost]
    [DisplayName("电子注单")]
    public Task<GameOrderSummaryPageOutput> Slot(GameOrderSummaryPageInput input) =>
        _svc.PageByCategoryAsync(GameOrderSummaryService.CategorySlot, input, App.HttpContext?.RequestAborted ?? default);

    /// <summary>捕鱼。</summary>
    /// <param name="input">筛选</param>
    /// <returns>分页</returns>
    [ApiDescriptionSettings(Name = "Fish"), HttpPost]
    [DisplayName("捕鱼注单")]
    public Task<GameOrderSummaryPageOutput> Fish(GameOrderSummaryPageInput input) =>
        _svc.PageByCategoryAsync(GameOrderSummaryService.CategoryFish, input, App.HttpContext?.RequestAborted ?? default);

    /// <summary>动物。</summary>
    /// <param name="input">筛选</param>
    /// <returns>分页</returns>
    [ApiDescriptionSettings(Name = "Animal"), HttpPost]
    [DisplayName("动物注单")]
    public Task<GameOrderSummaryPageOutput> Animal(GameOrderSummaryPageInput input) =>
        _svc.PageByCategoryAsync(GameOrderSummaryService.CategoryAnimal, input, App.HttpContext?.RequestAborted ?? default);

    /// <summary>小游戏。</summary>
    /// <param name="input">筛选</param>
    /// <returns>分页</returns>
    [ApiDescriptionSettings(Name = "Mini"), HttpPost]
    [DisplayName("小游戏注单")]
    public Task<GameOrderSummaryPageOutput> Mini(GameOrderSummaryPageInput input) =>
        _svc.PageByCategoryAsync(GameOrderSummaryService.CategoryMini, input, App.HttpContext?.RequestAborted ?? default);

    /// <summary>幻想/对战。</summary>
    /// <param name="input">筛选</param>
    /// <returns>分页</returns>
    [ApiDescriptionSettings(Name = "Fantasy"), HttpPost]
    [DisplayName("幻想注单")]
    public Task<GameOrderSummaryPageOutput> Fantasy(GameOrderSummaryPageInput input) =>
        _svc.PageByCategoryAsync(GameOrderSummaryService.CategoryFantasy, input, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 注单明细分页。不走日汇总。分类放在筛选的 Category 里，-1 为综合索引。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <returns>明细分页</returns>
    [ApiDescriptionSettings(Name = "PageDetail"), HttpPost]
    [DisplayName("注单明细")]
    public Task<GameOrderSummaryPageOutput> PageDetail(GameOrderSummaryPageInput input)
    {
        input ??= new GameOrderSummaryPageInput();
        input.UseEsDetail = true;
        return _svc.PageDetailFromEsAsync(input.Category, input, App.HttpContext?.RequestAborted ?? default);
    }

    /// <summary>打赏：读 game_RewardLog。</summary>
    /// <param name="input">筛选</param>
    /// <returns>分页</returns>
    [ApiDescriptionSettings(Name = "Reward"), HttpPost]
    [DisplayName("打赏记录")]
    public Task<GameOrderSummaryPageOutput> Reward(GameOrderSummaryPageInput input) =>
        _svc.PageRewardAsync(input ?? new GameOrderSummaryPageInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>Excel 导出占位。</summary>
    /// <returns>文件列表</returns>
    [ApiDescriptionSettings(Name = "Excel"), HttpPost]
    [DisplayName("注单导出占位")]
    public Task<List<SiteFileReportItemDto>> Excel() =>
        _svc.ExportStubAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>导出动物。</summary>
    /// <returns>文件</returns>
    [ApiDescriptionSettings(Name = "ExportAnimalLog"), HttpPost]
    [DisplayName("导出动物注单")]
    public Task<List<SiteFileReportItemDto>> ExportAnimalLog() =>
        _svc.ExportStubAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>导出真人。</summary>
    /// <returns>文件</returns>
    [ApiDescriptionSettings(Name = "ExportLiveLog"), HttpPost]
    [DisplayName("导出真人注单")]
    public Task<List<SiteFileReportItemDto>> ExportLiveLog() =>
        _svc.ExportStubAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>导出打赏。</summary>
    /// <returns>文件</returns>
    [ApiDescriptionSettings(Name = "ExportRewardLog"), HttpPost]
    [DisplayName("导出打赏")]
    public Task<List<SiteFileReportItemDto>> ExportRewardLog() =>
        _svc.ExportStubAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>棋牌详情。打赏没有时查棋牌注单索引。</summary>
    /// <param name="sourceId">第三方单号</param>
    /// <param name="type">游戏类型</param>
    /// <returns>详情或说明</returns>
    [ApiDescriptionSettings(Name = "ChessInfo"), HttpPost]
    [DisplayName("棋牌详情")]
    public Task<GameOrderInfoStubDto> ChessInfo([FromQuery] string? sourceId, [FromQuery] int type) =>
        _svc.InfoAsync(sourceId, type, GameOrderSummaryService.CategoryChess, App.HttpContext?.RequestAborted ?? default);

    /// <summary>电竞详情。打赏没有时查电竞注单索引。</summary>
    /// <param name="sourceId">第三方单号</param>
    /// <param name="type">游戏类型</param>
    /// <returns>详情或说明</returns>
    [ApiDescriptionSettings(Name = "EsportInfo"), HttpPost]
    [DisplayName("电竞详情")]
    public Task<GameOrderInfoStubDto> EsportInfo([FromQuery] string? sourceId, [FromQuery] int type) =>
        _svc.InfoAsync(sourceId, type, GameOrderSummaryService.CategoryESport, App.HttpContext?.RequestAborted ?? default);

    /// <summary>体育详情。打赏没有时查体育注单索引。</summary>
    /// <param name="sourceId">第三方单号</param>
    /// <param name="type">游戏类型</param>
    /// <returns>详情或说明</returns>
    [ApiDescriptionSettings(Name = "SportInfo"), HttpPost]
    [DisplayName("体育详情")]
    public Task<GameOrderInfoStubDto> SportInfo([FromQuery] string? sourceId, [FromQuery] int type) =>
        _svc.InfoAsync(sourceId, type, GameOrderSummaryService.CategorySport, App.HttpContext?.RequestAborted ?? default);

    /// <summary>彩票详情。打赏没有时查彩票注单索引。</summary>
    /// <param name="sourceId">第三方单号</param>
    /// <param name="type">游戏类型</param>
    /// <returns>详情或说明</returns>
    [ApiDescriptionSettings(Name = "LotteryInfo"), HttpPost]
    [DisplayName("彩票详情")]
    public Task<GameOrderInfoStubDto> LotteryInfo([FromQuery] string? sourceId, [FromQuery] int type) =>
        _svc.InfoAsync(sourceId, type, GameOrderSummaryService.CategoryLottery, App.HttpContext?.RequestAborted ?? default);

    /// <summary>真人详情。打赏没有时查真人注单索引。</summary>
    /// <param name="sourceId">第三方单号</param>
    /// <param name="type">游戏类型</param>
    /// <returns>详情或说明</returns>
    [ApiDescriptionSettings(Name = "LiveInfo"), HttpPost]
    [DisplayName("真人详情")]
    public Task<GameOrderInfoStubDto> LiveInfo([FromQuery] string? sourceId, [FromQuery] int type) =>
        _svc.InfoAsync(sourceId, type, GameOrderSummaryService.CategoryLive, App.HttpContext?.RequestAborted ?? default);

    /// <summary>电子详情。打赏没有时查电子注单索引。</summary>
    /// <param name="sourceId">第三方单号</param>
    /// <param name="type">游戏类型</param>
    /// <returns>详情或说明</returns>
    [ApiDescriptionSettings(Name = "SlotInfo"), HttpPost]
    [DisplayName("电子详情")]
    public Task<GameOrderInfoStubDto> SlotInfo([FromQuery] string? sourceId, [FromQuery] int type) =>
        _svc.InfoAsync(sourceId, type, GameOrderSummaryService.CategorySlot, App.HttpContext?.RequestAborted ?? default);

    /// <summary>捕鱼详情。打赏没有时查捕鱼注单索引。</summary>
    /// <param name="sourceId">第三方单号</param>
    /// <param name="type">游戏类型</param>
    /// <returns>详情或说明</returns>
    [ApiDescriptionSettings(Name = "FishInfo"), HttpPost]
    [DisplayName("捕鱼详情")]
    public Task<GameOrderInfoStubDto> FishInfo([FromQuery] string? sourceId, [FromQuery] int type) =>
        _svc.InfoAsync(sourceId, type, GameOrderSummaryService.CategoryFish, App.HttpContext?.RequestAborted ?? default);

    /// <summary>动物详情。打赏没有时查动物注单索引。</summary>
    /// <param name="sourceId">第三方单号</param>
    /// <param name="type">游戏类型</param>
    /// <returns>详情或说明</returns>
    [ApiDescriptionSettings(Name = "AnimalInfo"), HttpPost]
    [DisplayName("动物详情")]
    public Task<GameOrderInfoStubDto> AnimalInfo([FromQuery] string? sourceId, [FromQuery] int type) =>
        _svc.InfoAsync(sourceId, type, GameOrderSummaryService.CategoryAnimal, App.HttpContext?.RequestAborted ?? default);

    /// <summary>小游戏详情。打赏没有时查小游戏注单索引。</summary>
    /// <param name="sourceId">第三方单号</param>
    /// <param name="type">游戏类型</param>
    /// <returns>详情或说明</returns>
    [ApiDescriptionSettings(Name = "MiniInfo"), HttpPost]
    [DisplayName("小游戏详情")]
    public Task<GameOrderInfoStubDto> MiniInfo([FromQuery] string? sourceId, [FromQuery] int type) =>
        _svc.InfoAsync(sourceId, type, GameOrderSummaryService.CategoryMini, App.HttpContext?.RequestAborted ?? default);

    /// <summary>幻想详情。打赏没有时查对战和幻想索引。</summary>
    /// <param name="sourceId">第三方单号</param>
    /// <param name="type">游戏类型</param>
    /// <returns>详情或说明</returns>
    [ApiDescriptionSettings(Name = "FantasyInfo"), HttpPost]
    [DisplayName("幻想详情")]
    public Task<GameOrderInfoStubDto> FantasyInfo([FromQuery] string? sourceId, [FromQuery] int type) =>
        _svc.InfoAsync(sourceId, type, GameOrderSummaryService.CategoryFantasy, App.HttpContext?.RequestAborted ?? default);

    /// <summary>打赏详情。打赏表没有时再查综合注单索引。</summary>
    /// <param name="sourceId">第三方单号</param>
    /// <param name="type">游戏类型</param>
    /// <returns>详情或说明</returns>
    [ApiDescriptionSettings(Name = "RewardInfo"), HttpPost]
    [DisplayName("打赏详情")]
    public Task<GameOrderInfoStubDto> RewardInfo([FromQuery] string? sourceId, [FromQuery] int type) =>
        _svc.InfoAsync(sourceId, type, App.HttpContext?.RequestAborted ?? default);

    /// <summary>拉单测试占位。对齐旧 save。</summary>
    /// <param name="type">类型</param>
    /// <returns>说明</returns>
    [ApiDescriptionSettings(Name = "Save"), HttpPost]
    [DisplayName("拉单测试占位")]
    public object Save([FromQuery] int type) => new { count = 0, type, message = "#拉单作业请走调度宿主#" };

    /// <summary>更新状态占位。</summary>
    /// <returns>说明</returns>
    [ApiDescriptionSettings(Name = "UpdateStatus"), HttpPost]
    [DisplayName("注单状态占位")]
    public object UpdateStatus() => new { message = "#日汇总无状态字段#" };

    /// <summary>原始数据占位。</summary>
    /// <returns>说明</returns>
    [ApiDescriptionSettings(Name = "RawData"), HttpPost]
    [DisplayName("原始数据占位")]
    public object RawData() => new { message = "#无原始报文#" };
}
