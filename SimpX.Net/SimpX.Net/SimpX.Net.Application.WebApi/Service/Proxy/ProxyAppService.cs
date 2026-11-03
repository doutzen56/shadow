using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.WebApi;

/// <summary>
/// 会员代理后台旧路径 /request/proxy/*：与 agent 域重叠；未深接前挂空读/未接通写。
/// </summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "proxy", Order = 41)]
public class ProxyAppService : IDynamicApiController, ITransient
{
    private readonly MemberAuthService _auth;
    private readonly ICurrentSiteAccessor _site;
    private readonly ProxyTeamService _team;
    private readonly AgentInfoService _agents;
    private readonly ProxyMoneyService _proxyMoney;

    public ProxyAppService(
        MemberAuthService auth,
        ICurrentSiteAccessor site,
        ProxyTeamService team,
        AgentInfoService agents,
        ProxyMoneyService proxyMoney)
    {
        _auth = auth;
        _site = site;
        _team = team;
        _agents = agents;
        _proxyMoney = proxyMoney;
    }

    [AllowAnonymous]
    [HttpPost("/request/proxy/getinfo"), HttpGet("/request/proxy/getinfo")]
    [HttpPost("/request/proxy/info"), HttpGet("/request/proxy/info")]
    [HttpPost("/request/proxy/poxyinfo"), HttpGet("/request/proxy/poxyinfo")]
    [HttpPost("/request/proxy/signinfo"), HttpGet("/request/proxy/signinfo")]
    [HttpPost("/request/proxy/safeinfo"), HttpGet("/request/proxy/safeinfo")]
    [NonUnify, DisplayName("proxy-info")]
    public Task<OldApiResult> Info() =>
        Run(async ct =>
        {
            var userId = await RequireUserIdAsync(ct);
            return await _agents.DetailAsync(userId, ct);
        });

    [AllowAnonymous]
    [HttpPost("/request/proxy/agentlist"), HttpGet("/request/proxy/agentlist")]
    [HttpPost("/request/proxy/proxyuserlist"), HttpGet("/request/proxy/proxyuserlist")]
    [HttpPost("/request/proxy/subuser"), HttpGet("/request/proxy/subuser")]
    [HttpPost("/request/proxy/getusertree"), HttpGet("/request/proxy/getusertree")]
    [HttpPost("/request/proxy/channel"), HttpGet("/request/proxy/channel")]
    [NonUnify, DisplayName("proxy-team")]
    public Task<OldApiResult> Team() =>
        Run(async ct =>
        {
            var userId = await RequireUserIdAsync(ct);
            return await _team.ListAsync(new ProxyTeamListInput { UserID = userId }, ct);
        });

    [AllowAnonymous]
    [HttpPost("/request/proxy/islogin"), HttpGet("/request/proxy/islogin")]
    [HttpPost("/request/proxy/isagentuser"), HttpGet("/request/proxy/isagentuser")]
    [HttpPost("/request/proxy/checkagent"), HttpGet("/request/proxy/checkagent")]
    [HttpPost("/request/proxy/editcheckagent"), HttpGet("/request/proxy/editcheckagent")]
    [HttpPost("/request/proxy/ensureagent"), HttpGet("/request/proxy/ensureagent")]
    [NonUnify, DisplayName("proxy-flags")]
    public Task<OldApiResult> Flags() =>
        Run(async ct =>
        {
            var siteId = _site.RequireSiteId();
            var userId = await _auth.TryResolveUserIdFromCookieAsync(siteId, ct);
            return (object)new { IsLogin = userId is > 0, IsAgent = userId is > 0 };
        });

    [AllowAnonymous]
    [HttpPost("/request/proxy/getbalance"), HttpGet("/request/proxy/getbalance")]
    [HttpPost("/request/proxy/finace"), HttpGet("/request/proxy/finace")]
    [HttpPost("/request/proxy/statis"), HttpGet("/request/proxy/statis")]
    [HttpPost("/request/proxy/gamedetail"), HttpGet("/request/proxy/gamedetail")]
    [HttpPost("/request/proxy/gameuser"), HttpGet("/request/proxy/gameuser")]
    [HttpPost("/request/proxy/reportdetail"), HttpGet("/request/proxy/reportdetail")]
    [NonUnify, DisplayName("proxy-report-stub")]
    public Task<OldApiResult> ReportStub() =>
        Run(async ct =>
        {
            await RequireUserIdAsync(ct);
            return (object)new { Items = Array.Empty<object>(), Total = 0 };
        });

    [AllowAnonymous]
    [HttpPost("/request/proxy/userinfo"), HttpGet("/request/proxy/userinfo")]
    [HttpPost("/request/proxy/userlist"), HttpGet("/request/proxy/userlist")]
    [NonUnify, DisplayName("proxy-user")]
    public Task<OldApiResult> Userlist() => Team();

    [AllowAnonymous]
    [HttpPost("/request/proxy/token"), HttpGet("/request/proxy/token")]
    [NonUnify, DisplayName("proxy-token")]
    public Task<OldApiResult> Token() =>
        Run(async ct =>
        {
            var userId = await RequireUserIdAsync(ct);
            return await _proxyMoney.IssueTokenAsync(userId, ct);
        });

    [AllowAnonymous]
    [HttpPost("/request/proxy/transfer")]
    [NonUnify, DisplayName("proxy-transfer")]
    public Task<OldApiResult> Transfer() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            var agentId = await RequireUserIdAsync(ct);
            await _proxyMoney.TransferInAsync(
                agentId,
                OldApiFormReader.ReadString(http, "UserName", "User"),
                OldApiFormReader.ReadString(http, "PayPassword", "Password"),
                OldApiFormReader.ReadDecimal(http, "Money"),
                OldApiFormReader.ReadInt(http, "Currency"),
                ct);
            return (object)"#转账成功#";
        });

    [AllowAnonymous]
    [HttpPost("/request/proxy/transferout")]
    [NonUnify, DisplayName("proxy-transferout")]
    public Task<OldApiResult> TransferOut() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            var agentId = await RequireUserIdAsync(ct);
            await _proxyMoney.TransferOutAsync(
                agentId,
                OldApiFormReader.ReadString(http, "UserName", "User"),
                OldApiFormReader.ReadString(http, "PayPassword", "Password"),
                OldApiFormReader.ReadDecimal(http, "Money"),
                OldApiFormReader.ReadInt(http, "Currency"),
                ct);
            return (object)"#转账成功#";
        });

    [AllowAnonymous]
    [HttpPost("/request/proxy/add")]
    [HttpPost("/request/proxy/edit")]
    [HttpPost("/request/proxy/setenable")]
    [HttpPost("/request/proxy/resetpassword")]
    [HttpPost("/request/proxy/updateemail")]
    [HttpPost("/request/proxy/updatemobile")]
    [HttpPost("/request/proxy/updatepassword")]
    [HttpPost("/request/proxy/updatepaypassword")]
    [HttpPost("/request/proxy/transfertocenter")]
    [HttpPost("/request/proxy/transfertocenterbysub")]
    [NonUnify, DisplayName("proxy-write-stub")]
    public Task<OldApiResult> WriteStub() =>
        Run<object>(_ => throw Oops.Oh("#功能尚未接通#"));

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
