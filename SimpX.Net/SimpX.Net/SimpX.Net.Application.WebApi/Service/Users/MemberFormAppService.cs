using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.WebApi;

/// <summary>
/// 会员侧会员域旧路径：/request/member/{message|vip}/*。
/// </summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "member-form", Order = 23)]
public class MemberFormAppService : IDynamicApiController, ITransient
{
    private readonly UserMessageService _messages;
    private readonly SiteVipConditionService _vip;
    private readonly UserInviteService _invite;
    private readonly ProxyTeamService _team;
    private readonly AgentInfoService _agents;
    private readonly UserSignService _sign;
    private readonly MemberAuthService _auth;
    private readonly ICurrentSiteAccessor _site;

    public MemberFormAppService(
        UserMessageService messages,
        SiteVipConditionService vip,
        UserInviteService invite,
        ProxyTeamService team,
        AgentInfoService agents,
        UserSignService sign,
        MemberAuthService auth,
        ICurrentSiteAccessor site)
    {
        _messages = messages;
        _vip = vip;
        _invite = invite;
        _team = team;
        _agents = agents;
        _sign = sign;
        _auth = auth;
        _site = site;
    }

    [AllowAnonymous]
    [HttpPost("/request/member/message/list"), HttpGet("/request/member/message/list")]
    [NonUnify, DisplayName("member-message-list")]
    public Task<OldApiResult> Messagelist() =>
        Run(async ct =>
        {
            var userId = await RequireUserIdAsync(ct);
            var (page, size) = OldApiShowResult.ReadPaging(App.HttpContext);
            return await _messages.PageAsync(new UserMessagePageInput
            {
                Page = page,
                PageSize = size,
                UserID = userId
            }, ct);
        });

    [AllowAnonymous]
    [HttpPost("/request/member/message/info"), HttpGet("/request/member/message/info")]
    [NonUnify, DisplayName("member-message-info")]
    public Task<OldApiResult> Messageinfo() =>
        Run(async ct =>
        {
            _ = await RequireUserIdAsync(ct);
            return await _messages.DetailAsync(OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id"), ct);
        });

    [AllowAnonymous]
    [HttpPost("/request/member/message/delete")]
    [NonUnify, DisplayName("member-message-delete")]
    public Task<OldApiResult> Messagedelete() =>
        Run(async ct =>
        {
            _ = await RequireUserIdAsync(ct);
            await _messages.DeleteAsync(OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id"), ct);
            return (object?)null;
        });

    [AllowAnonymous]
    [HttpPost("/request/member/message/unreadmsgqty"), HttpGet("/request/member/message/unreadmsgqty")]
    [NonUnify, DisplayName("member-message-unread")]
    public Task<OldApiResult> Messageunread() =>
        Run(async ct => await _messages.UnreadQtyAsync(await RequireUserIdAsync(ct), ct));

    [AllowAnonymous]
    [HttpPost("/request/member/message/read")]
    [NonUnify, DisplayName("member-message-read")]
    public Task<OldApiResult> Messageread() =>
        Run(async ct =>
        {
            await _messages.MarkAllReadAsync(await RequireUserIdAsync(ct), ct);
            return (object?)"#信息全部已读#";
        });

    [OldApiGuest, AllowAnonymous]
    [HttpPost("/request/member/vip/getlevels"), HttpGet("/request/member/vip/getlevels")]
    [NonUnify, DisplayName("member-vip-levels")]
    public Task<OldApiResult> Viplevels() =>
        Run(ct => _vip.ListLevelsAsync(ct));

    [OldApiGuest, AllowAnonymous]
    [HttpPost("/request/site/vip/pointrule"), HttpGet("/request/site/vip/pointrule")]
    [NonUnify, DisplayName("site-vip-pointrule")]
    public Task<OldApiResult> Sitevippointrule() =>
        Run(ct => _vip.PointRuleAsync(ct));

    [AllowAnonymous]
    [HttpPost("/request/member/invite/list"), HttpGet("/request/member/invite/list")]
    [NonUnify, DisplayName("member-invite-list")]
    public Task<OldApiResult> Invitelist() =>
        Run(async ct =>
        {
            var userId = await RequireUserIdAsync(ct);
            return await _invite.ListAsync(new UserInviteListInput
            {
                User = userId.ToString()
            }, ct);
        });

    [AllowAnonymous]
    [HttpPost("/request/agent/team/list"), HttpGet("/request/agent/team/list")]
    [HttpPost("/request/agent/user/list"), HttpGet("/request/agent/user/list")]
    [HttpPost("/request/agent/team/userlist"), HttpGet("/request/agent/team/userlist")]
    [NonUnify, DisplayName("agent-team")]
    public Task<OldApiResult> Agentteam() =>
        Run(async ct =>
        {
            var userId = await RequireUserIdAsync(ct);
            return await _team.ListAsync(new ProxyTeamListInput
            {
                UserID = userId,
                MaxDepth = OldApiFormReader.ReadInt(App.HttpContext, "MaxDepth", "Depth")
            }, ct);
        });

    [AllowAnonymous]
    [HttpPost("/request/agent/team/membercount"), HttpGet("/request/agent/team/membercount")]
    [NonUnify, DisplayName("agent-team-membercount")]
    public Task<OldApiResult> Agentteammembercount() =>
        Run(async ct =>
        {
            var userId = await RequireUserIdAsync(ct);
            var type = OldApiFormReader.ReadString(App.HttpContext, "Type", "type");
            var directly = string.Equals(type, "Directly", StringComparison.OrdinalIgnoreCase);
            return await _team.MemberCountAsync(userId, directly, ct);
        });

    [AllowAnonymous]
    [HttpPost("/request/agent/team/moneytotal"), HttpGet("/request/agent/team/moneytotal")]
    [NonUnify, DisplayName("agent-team-moneytotal")]
    public Task<OldApiResult> Agentteammoneytotal() =>
        Run(async ct =>
        {
            var userId = await RequireUserIdAsync(ct);
            return await _team.MoneyTotalAsync(userId, ct);
        });

    [AllowAnonymous]
    [HttpPost("/request/agent/user/info"), HttpGet("/request/agent/user/info")]
    [NonUnify, DisplayName("agent-user-info")]
    public Task<OldApiResult> Agentuserinfo() =>
        Run(async ct =>
        {
            var userId = await RequireUserIdAsync(ct);
            return await _agents.DetailAsync(userId, ct);
        });

    [OldApiGuest, AllowAnonymous]
    [HttpPost("/request/site/vip/index"), HttpGet("/request/site/vip/index")]
    [NonUnify, DisplayName("site-vip-index")]
    public Task<OldApiResult> Sitevipindex() =>
        Run(ct => _vip.GetAsync(ct));

    [AllowAnonymous]
    [HttpPost("/request/member/signin/list"), HttpGet("/request/member/signin/list")]
    [HttpPost("/request/member/signin/signresult"), HttpGet("/request/member/signin/signresult")]
    [NonUnify, DisplayName("member-signin")]
    public Task<OldApiResult> Signin() =>
        Run(async ct =>
        {
            var userId = await RequireUserIdAsync(ct);
            return await _sign.PageAsync(new UserSignPageInput { UserID = userId }, ct);
        });

    [AllowAnonymous]
    [HttpPost("/request/member/invite/open"), HttpGet("/request/member/invite/open")]
    [HttpPost("/request/member/invite/joinsetting"), HttpGet("/request/member/invite/joinsetting")]
    [NonUnify, DisplayName("member-invite-open")]
    public Task<OldApiResult> InviteOpen() =>
        Run(_ => Task.FromResult<object>(new { Open = false }));

    [AllowAnonymous]
    [HttpPost("/request/member/invite/save")]
    [HttpPost("/request/member/invite/join")]
    [HttpPost("/request/member/invite/delete")]
    [NonUnify, DisplayName("member-invite-write")]
    public Task<OldApiResult> InviteWrite() =>
        Run<object>(_ => throw Oops.Oh("#功能尚未接通#"));

    [OldApiGuest, AllowAnonymous]
    [HttpPost("/request/member/forget/info"), HttpGet("/request/member/forget/info")]
    [HttpPost("/request/member/forget/email")]
    [HttpPost("/request/member/forget/mobile")]
    [HttpPost("/request/member/forget/question")]
    [NonUnify, DisplayName("member-forget")]
    public Task<OldApiResult> Forget() =>
        Run<object>(_ => throw Oops.Oh("#功能尚未接通#"));

    [AllowAnonymous]
    [HttpPost("/request/member/report/profit"), HttpGet("/request/member/report/profit")]
    [NonUnify, DisplayName("member-report-profit")]
    public Task<OldApiResult> ReportProfit() =>
        Run(async ct =>
        {
            await RequireUserIdAsync(ct);
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

    private static async Task<OldApiResult> Run<T>(Func<CancellationToken, Task<T>> action) => await OldApiFormReader.RunAsync(action);
}
