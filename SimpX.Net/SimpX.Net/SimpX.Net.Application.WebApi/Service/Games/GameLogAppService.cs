using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.WebApi;

/// <summary>
/// 会员游戏注单旧路径：/request/game/log/*（厂商明细未迁全量时挂空列表）。
/// </summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "game-log", Order = 32)]
public class GameLogAppService : IDynamicApiController, ITransient
{
    private readonly MemberAuthService _auth;
    private readonly ICurrentSiteAccessor _site;

    public GameLogAppService(MemberAuthService auth, ICurrentSiteAccessor site)
    {
        _auth = auth;
        _site = site;
    }

    [AllowAnonymous]
    [HttpPost("/request/game/log/list"), HttpGet("/request/game/log/list")]
    [HttpPost("/request/game/log/listv1"), HttpGet("/request/game/log/listv1")]
    [HttpPost("/request/game/log/ownlist"), HttpGet("/request/game/log/ownlist")]
    [HttpPost("/request/game/log/ownlistv1"), HttpGet("/request/game/log/ownlistv1")]
    [HttpPost("/request/game/log/slot"), HttpGet("/request/game/log/slot")]
    [HttpPost("/request/game/log/slotv1"), HttpGet("/request/game/log/slotv1")]
    [HttpPost("/request/game/log/slotinfo"), HttpGet("/request/game/log/slotinfo")]
    [HttpPost("/request/game/log/slotinfov1"), HttpGet("/request/game/log/slotinfov1")]
    [HttpPost("/request/game/log/live"), HttpGet("/request/game/log/live")]
    [HttpPost("/request/game/log/livev1"), HttpGet("/request/game/log/livev1")]
    [HttpPost("/request/game/log/liveinfo"), HttpGet("/request/game/log/liveinfo")]
    [HttpPost("/request/game/log/liveinfov1"), HttpGet("/request/game/log/liveinfov1")]
    [HttpPost("/request/game/log/fish"), HttpGet("/request/game/log/fish")]
    [HttpPost("/request/game/log/fishv1"), HttpGet("/request/game/log/fishv1")]
    [HttpPost("/request/game/log/fishinfo"), HttpGet("/request/game/log/fishinfo")]
    [HttpPost("/request/game/log/fishinfov1"), HttpGet("/request/game/log/fishinfov1")]
    [HttpPost("/request/game/log/chesslog"), HttpGet("/request/game/log/chesslog")]
    [HttpPost("/request/game/log/chesslogv1"), HttpGet("/request/game/log/chesslogv1")]
    [HttpPost("/request/game/log/chessinfo"), HttpGet("/request/game/log/chessinfo")]
    [HttpPost("/request/game/log/chessinfov1"), HttpGet("/request/game/log/chessinfov1")]
    [HttpPost("/request/game/log/sportinfo"), HttpGet("/request/game/log/sportinfo")]
    [HttpPost("/request/game/log/sportinfov1"), HttpGet("/request/game/log/sportinfov1")]
    [HttpPost("/request/game/log/lottery"), HttpGet("/request/game/log/lottery")]
    [HttpPost("/request/game/log/lotteryv1"), HttpGet("/request/game/log/lotteryv1")]
    [HttpPost("/request/game/log/lotteryinfo"), HttpGet("/request/game/log/lotteryinfo")]
    [HttpPost("/request/game/log/lotteryinfov1"), HttpGet("/request/game/log/lotteryinfov1")]
    [HttpPost("/request/game/log/esportinfo"), HttpGet("/request/game/log/esportinfo")]
    [HttpPost("/request/game/log/esportinfov1"), HttpGet("/request/game/log/esportinfov1")]
    [HttpPost("/request/game/log/esportdetail"), HttpGet("/request/game/log/esportdetail")]
    [HttpPost("/request/game/log/animal"), HttpGet("/request/game/log/animal")]
    [HttpPost("/request/game/log/animalv1"), HttpGet("/request/game/log/animalv1")]
    [HttpPost("/request/game/log/animalinfo"), HttpGet("/request/game/log/animalinfo")]
    [HttpPost("/request/game/log/animalinfov1"), HttpGet("/request/game/log/animalinfov1")]
    [HttpPost("/request/game/log/mini"), HttpGet("/request/game/log/mini")]
    [HttpPost("/request/game/log/miniv1"), HttpGet("/request/game/log/miniv1")]
    [HttpPost("/request/game/log/minigameinfo"), HttpGet("/request/game/log/minigameinfo")]
    [HttpPost("/request/game/log/minigameinfov1"), HttpGet("/request/game/log/minigameinfov1")]
    [HttpPost("/request/game/log/battle"), HttpGet("/request/game/log/battle")]
    [HttpPost("/request/game/log/battleinfo"), HttpGet("/request/game/log/battleinfo")]
    [HttpPost("/request/game/log/reward"), HttpGet("/request/game/log/reward")]
    [HttpPost("/request/game/log/rewardinfo"), HttpGet("/request/game/log/rewardinfo")]
    [HttpPost("/request/game/log/logroll"), HttpGet("/request/game/log/logroll")]
    [NonUnify, DisplayName("game-log-stub")]
    public Task<OldApiResult> Stub() =>
        Run(async ct =>
        {
            await RequireLoginAsync(ct);
            return (object)new { Items = Array.Empty<object>(), Total = 0 };
        });

    private async Task RequireLoginAsync(CancellationToken ct)
    {
        var siteId = _site.RequireSiteId();
        var userId = await _auth.TryResolveUserIdFromCookieAsync(siteId, ct);
        if (userId is null or <= 0)
            throw Oops.Oh("#请先登录#");
    }

    private static async Task<OldApiResult> Run<T>(Func<CancellationToken, Task<T>> action) => await OldApiFormReader.RunAsync(action);
}
