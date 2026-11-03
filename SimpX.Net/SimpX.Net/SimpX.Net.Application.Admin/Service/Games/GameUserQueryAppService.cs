using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 租户后台游戏会员与额度记录（JWT）。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "gameUserQuery", Order = 544, Description = "游戏会员额度")]
public partial class GameUserQueryAppService : IDynamicApiController, ITransient
{
    private readonly GameUserQueryService _svc;
    private readonly GameTransferService _transfer;
    private readonly ICurrentSiteAccessor _site;

    /// <summary>
    /// 注入。
    /// </summary>
    /// <param name="svc">查询</param>
    /// <param name="transfer">转账</param>
    /// <param name="site">当前站</param>
    public GameUserQueryAppService(
        GameUserQueryService svc,
        GameTransferService transfer,
        ICurrentSiteAccessor site)
    {
        _svc = svc;
        _transfer = transfer;
        _site = site;
    }

    /// <summary>游戏会员分页。</summary>
    /// <param name="input">筛选</param>
    /// <returns>分页</returns>
    [ApiDescriptionSettings(Name = "PageUsers"), HttpPost]
    [DisplayName("游戏会员分页")]
    public Task<GameUserPageOutput> PageUsers(GameUserPageInput input) =>
        _svc.PageUsersAsync(input ?? new GameUserPageInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>额度记录分页。</summary>
    /// <param name="input">筛选</param>
    /// <returns>分页</returns>
    [ApiDescriptionSettings(Name = "PageTransfers"), HttpPost]
    [DisplayName("额度记录分页")]
    public Task<GameTransferPageOutput> PageTransfers(GameTransferPageInput input) =>
        _svc.PageTransfersAsync(input ?? new GameTransferPageInput(), App.HttpContext?.RequestAborted ?? default);
}
