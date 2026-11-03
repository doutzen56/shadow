using Furion.UnifyResult;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.WebApi;

/// <summary>
/// 会员兼容转盘：/request/events/delottery/*。对齐旧 events/delottery getcount、bet。
/// </summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "delottery", Order = 35)]
public class DelotteryAppService : IDynamicApiController, ITransient
{
    private readonly MemberAuthService _memberAuth;
    private readonly ICurrentSiteAccessor _site;
    private readonly IDelotteryDrawService _draw;
    private readonly IUserActivityLockService _activityLock;

    /// <summary>
    /// 注入会员会话、定站、抽奖与活动锁。
    /// </summary>
    /// <param name="memberAuth">会员会话</param>
    /// <param name="site">定站</param>
    /// <param name="draw">转盘抽奖</param>
    /// <param name="activityLock">活动锁</param>
    public DelotteryAppService(
        MemberAuthService memberAuth,
        ICurrentSiteAccessor site,
        IDelotteryDrawService draw,
        IUserActivityLockService activityLock)
    {
        _memberAuth = memberAuth;
        _site = site;
        _draw = draw;
        _activityLock = activityLock;
    }

    /// <summary>
    /// 剩余抽奖次数。对齐旧 POST /request/events/delottery/getcount。
    /// </summary>
    /// <returns>旧版返回；info.Count 为剩余次数</returns>
    [AllowAnonymous]
    [HttpPost("/request/events/delottery/getcount")]
    [NonUnify]
    [DisplayName("转盘剩余次数")]
    public async Task<OldApiResult> GetCount()
    {
        var http = App.HttpContext;
        var ct = http?.RequestAborted ?? default;
        var siteId = _site.RequireSiteId();
        var userId = await _memberAuth.TryResolveUserIdFromCookieAsync(siteId, ct);
        if (userId == null)
            return OldApiResult.NeedLogin();

        if (!int.TryParse(OldApiRequestParam.Read(http, "ID"), out var eventId) || eventId <= 0)
            return OldApiResult.Fail("#未指定活动ID#");

        var count = await _draw.GetBalanceAsync(siteId, userId.Value, ct);
        return OldApiResult.Ok(string.Empty, new { Count = (int)Math.Floor(count) });
    }

    /// <summary>
    /// 抽一次。对齐旧 POST /request/events/delottery/bet。
    /// </summary>
    /// <returns>旧版返回；成功 info 含 Product/Index/msg</returns>
    [AllowAnonymous]
    [HttpPost("/request/events/delottery/bet")]
    [NonUnify]
    [DisplayName("转盘抽奖")]
    public async Task<OldApiResult> Bet()
    {
        var http = App.HttpContext;
        var ct = http?.RequestAborted ?? default;
        var siteId = _site.RequireSiteId();
        var userId = await _memberAuth.TryResolveUserIdFromCookieAsync(siteId, ct);
        if (userId == null)
            return OldApiResult.NeedLogin();

        if (await _activityLock.IsBlockedForActivityAsync(siteId, userId.Value, cancellationToken: ct))
            return OldApiResult.Fail("#无奖品可领#");

        if (!int.TryParse(OldApiRequestParam.Read(http, "ID"), out var eventId) || eventId <= 0)
            return OldApiResult.Fail("#未指定活动ID#");

        var result = await _draw.DrawAsync(siteId, userId.Value, eventId, ct);
        if (!result.Success)
            return OldApiResult.Fail(string.IsNullOrWhiteSpace(result.ErrorMessage)
                ? "#抽奖失败#"
                : result.ErrorMessage);

        // 旧版 Index 取 SourceID 第三段（奖品 ID）；再来一次无 SourceID 时用 ItemID
        var index = result.ItemID.ToString();
        if (!string.IsNullOrWhiteSpace(result.SourceID))
        {
            var parts = result.SourceID.Split('-');
            if (parts.Length >= 3)
                index = parts[^1];
        }

        var msg = result.ItemType == DelotteryDrawService.PrizeProduct
            ? "待审核"
            : "#领取成功#";
        return OldApiResult.Ok(string.Empty, new
        {
            Product = result.ItemName ?? string.Empty,
            Index = index,
            msg
        });
    }
}
