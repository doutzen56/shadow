using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.WebApi;

/// <summary>
/// /request/report/* 杂项 + agent bill/transfer leftovers + test/debug + external。
/// </summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "report-misc", Order = 42)]
public class ReportMiscAppService : IDynamicApiController, ITransient
{
    private readonly MemberAuthService _auth;
    private readonly ICurrentSiteAccessor _site;

    public ReportMiscAppService(MemberAuthService auth, ICurrentSiteAccessor site)
    {
        _auth = auth;
        _site = site;
    }

    [AllowAnonymous]
    [HttpPost("/request/report/info"), HttpGet("/request/report/info")]
    [HttpPost("/request/report/fields"), HttpGet("/request/report/fields")]
    [HttpPost("/request/report/getfundinfo"), HttpGet("/request/report/getfundinfo")]
    [HttpPost("/request/report/agentindexcommission"), HttpGet("/request/report/agentindexcommission")]
    [HttpPost("/request/agent/report/paylist"), HttpGet("/request/agent/report/paylist")]
    [HttpPost("/request/agent/report/rechargechannel"), HttpGet("/request/agent/report/rechargechannel")]
    [HttpPost("/request/agent/team/rptlist"), HttpGet("/request/agent/team/rptlist")]
    [HttpPost("/request/agent/bill/recharge"), HttpGet("/request/agent/bill/recharge")]
    [HttpPost("/request/agent/bill/profit"), HttpGet("/request/agent/bill/profit")]
    [NonUnify, DisplayName("report-misc-read")]
    public Task<OldApiResult> ReportRead() =>
        Run(async ct =>
        {
            await RequireUserIdAsync(ct);
            return (object)new { Items = Array.Empty<object>(), Total = 0 };
        });

    [AllowAnonymous]
    [HttpPost("/request/agent/money/transfer")]
    [HttpPost("/request/agent/team/moremoney")]
    [HttpPost("/request/api/payment/savebankorder")]
    [HttpPost("/request/api/sports/yabo"), HttpGet("/request/api/sports/yabo")]
    [HttpPost("/request/api/user/gameresult"), HttpGet("/request/api/user/gameresult")]
    [HttpPost("/request/api/user/money")]
    [HttpPost("/request/api/user/register")]
    [HttpPost("/request/api/user/token"), HttpGet("/request/api/user/token")]
    [HttpPost("/request/external/updatepushid")]
    [NonUnify, DisplayName("misc-write-stub")]
    public Task<OldApiResult> WriteStub() =>
        Run<object>(_ => throw Oops.Oh("#功能尚未接通#"));

    [OldApiGuest, AllowAnonymous]
    [HttpPost("/request/test/debug/header"), HttpGet("/request/test/debug/header")]
    [HttpPost("/request/test/debug/ip"), HttpGet("/request/test/debug/ip")]
    [HttpPost("/request/test/debug/server"), HttpGet("/request/test/debug/server")]
    [HttpPost("/request/test/debug/sms"), HttpGet("/request/test/debug/sms")]
    [NonUnify, DisplayName("test-debug")]
    public Task<OldApiResult> Debug() =>
        Run(_ =>
        {
            var http = App.HttpContext;
            return Task.FromResult<object>(new
            {
                Ip = http?.Connection.RemoteIpAddress?.ToString(),
                Path = http?.Request.Path.Value,
                Host = http?.Request.Host.Value
            });
        });

    private async Task RequireUserIdAsync(CancellationToken ct)
    {
        var siteId = _site.RequireSiteId();
        var userId = await _auth.TryResolveUserIdFromCookieAsync(siteId, ct);
        if (userId is null or <= 0)
            throw Oops.Oh("#请先登录#");
    }

    private static async Task<OldApiResult> Run<T>(Func<CancellationToken, Task<T>> action) => await OldApiFormReader.RunAsync(action);
}
