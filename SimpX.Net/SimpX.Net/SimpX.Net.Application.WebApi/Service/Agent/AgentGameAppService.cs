using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;
using SimpX.Net.Data.Entity.Reports;
using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.WebApi;

/// <summary>
/// 代理侧游戏日报：/request/agent/game/list（rpt_UserDateGame + usr_Depth）。
/// </summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "agent-game", Order = 37)]
public class AgentGameAppService : IDynamicApiController, ITransient
{
    private readonly IBizDbAccessor _biz;
    private readonly ICurrentSiteAccessor _site;
    private readonly MemberAuthService _auth;

    public AgentGameAppService(
        IBizDbAccessor biz,
        ICurrentSiteAccessor site,
        MemberAuthService auth)
    {
        _biz = biz;
        _site = site;
        _auth = auth;
    }

    [AllowAnonymous]
    [HttpPost("/request/agent/game/list"), HttpGet("/request/agent/game/list")]
    [NonUnify, DisplayName("agent-game-list")]
    public Task<OldApiResult> List() =>
        Run(async ct =>
        {
            var agentId = await RequireUserIdAsync(ct);
            var siteId = _site.RequireSiteId();
            var http = App.HttpContext;
            var start = ReadDate(http, "StartAt") ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var end = ReadDate(http, "EndAt") ?? DateTime.Today;
            var userName = OldApiFormReader.ReadString(http, "UserName", "User");
            var type = OldApiFormReader.ReadInt(http, "Type", "type");
            var currency = OldApiFormReader.ReadInt(http, "Currency", "currency");

            var childIds = await _biz.Db.Queryable<UserDepth>()
                .Where(d => d.SiteID == siteId && d.UserID == agentId)
                .Select(d => d.ChildID)
                .ToListAsync(ct);
            if (childIds.Count == 0)
                return (object)Array.Empty<object>();

            if (!string.IsNullOrWhiteSpace(userName))
            {
                var users = await _biz.Db.Queryable<User>()
                    .Where(u => u.SiteID == siteId && u.UserName == userName.Trim())
                    .Select(u => u.ID)
                    .Take(1)
                    .ToListAsync(ct);
                var uid = users.FirstOrDefault();
                if (uid <= 0 || !childIds.Contains(uid))
                    return (object)Array.Empty<object>();
                childIds = [uid];
            }

            var query = _biz.Db.Queryable<UserDateGame>()
                .Where(g => g.SiteID == siteId
                    && g.Date >= start && g.Date <= end
                    && childIds.Contains(g.UserID));
            if (type != 0)
                query = query.Where(g => g.Type == type);
            if (currency != 0)
                query = query.Where(g => g.Currency == currency);

            var (page, size) = OldApiShowResult.ReadPaging(http);
            RefAsync<int> total = 0;
            var rows = await query
                .OrderBy(g => g.Date, OrderByType.Desc)
                .Select(g => new
                {
                    g.UserID,
                    g.Date,
                    g.Type,
                    g.Category,
                    g.BetMoney,
                    g.Money,
                    g.Currency
                })
                .ToPageListAsync(page, size, total, ct);
            return (object)new { Items = rows, Total = (int)total };
        });

    [AllowAnonymous]
    [HttpPost("/request/agent/game/reward"), HttpGet("/request/agent/game/reward")]
    [HttpPost("/request/agent/game/rewardinfo"), HttpGet("/request/agent/game/rewardinfo")]
    [NonUnify, DisplayName("agent-game-reward")]
    public Task<OldApiResult> Reward() =>
        Run(async ct =>
        {
            await RequireUserIdAsync(ct);
            // 打赏明细依赖 game_RewardLog 深查；无参数时返回空列表，避免 404。
            return (object)new { Items = Array.Empty<object>(), Total = 0 };
        });

    private async Task<int> RequireUserIdAsync(CancellationToken ct)
    {
        var siteId = _site.RequireSiteId();
        var userId = await _auth.TryResolveUserIdFromCookieAsync(siteId, ct);
        if (userId is null or <= 0)
            throw Oops.Oh("#请先登录#");
        return userId.Value;
    }

    private static DateTime? ReadDate(Microsoft.AspNetCore.Http.HttpContext http, params string[] keys)
    {
        var s = OldApiFormReader.ReadString(http, keys);
        if (string.IsNullOrWhiteSpace(s)) return null;
        return DateTime.TryParse(s, out var dt) ? dt : null;
    }

    private static async Task<OldApiResult> Run<T>(Func<CancellationToken, Task<T>> action) => await OldApiFormReader.RunAsync(action);
}
