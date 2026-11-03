using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 代理网旧路径：/admin/proxy/*（Shared 仅 ProxyTeam 团队列表已接通）。
/// </summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "proxy", Order = 44)]
public class ProxyAppService : IDynamicApiController, ITransient
{
    public const string PermissionId = UserMemberFormAppService.MemberListPermissionId;

    private readonly ProxyTeamService _team;
    private readonly AgentInfoService _info;
    private readonly AgentStatReportService _stats;
    private readonly GameUserQueryService _gameUsers;

    public ProxyAppService(
        ProxyTeamService team,
        AgentInfoService info,
        AgentStatReportService stats,
        GameUserQueryService gameUsers)
    {
        _team = team;
        _info = info;
        _stats = stats;
        _gameUsers = gameUsers;
    }

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/proxy/info/team"), HttpGet("/admin/proxy/info/team")]
    [HttpPost("/admin/proxy/userlist"), HttpGet("/admin/proxy/userlist")]
    [HttpPost("/admin/proxy/channel"), HttpGet("/admin/proxy/channel")]
    [NonUnify, DisplayName("proxy-team")]
    public Task<OldApiResult> Team() =>
        Run(ct =>
        {
            var http = App.HttpContext;
            return _team.ListAsync(new ProxyTeamListInput
            {
                UserID = OldApiFormReader.ReadInt(http, "UserID", "ID", "Id", "AgentID"),
                MaxDepth = OldApiFormReader.ReadInt(http, "MaxDepth", "Depth")
            }, ct);
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/proxy/info"), HttpGet("/admin/proxy/info")]
    [HttpPost("/admin/proxy/userinfo"), HttpGet("/admin/proxy/userinfo")]
    [HttpPost("/admin/proxy/signinfo"), HttpGet("/admin/proxy/signinfo")]
    [NonUnify, DisplayName("proxy-info")]
    public Task<OldApiResult> Info() =>
        Run(async ct =>
        {
            var id = OldApiFormReader.ReadInt(App.HttpContext, "UserID", "ID", "Id", "AgentID");
            if (id <= 0)
                throw Oops.Oh("#参数错误#");
            return await _info.DetailAsync(id, ct);
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/proxy/checkagent")]
    [HttpPost("/admin/proxy/editcheckagent")]
    [NonUnify, DisplayName("proxy-checkagent")]
    public Task<OldApiResult> Checkagent() =>
        Run(ct =>
        {
            var map = OldApiFormReader.ReadJsonMap(App.HttpContext)
                .ToDictionary(kv => kv.Key, kv => (object?)kv.Value.ToString(), StringComparer.OrdinalIgnoreCase);
            return _info.CheckAsync(map, ct);
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/proxy/add")]
    [HttpPost("/admin/proxy/edit")]
    [NonUnify, DisplayName("proxy-add-edit")]
    public Task<OldApiResult> AddOrEdit() =>
        Run(ct =>
        {
            var map = OldApiFormReader.ReadJsonMap(App.HttpContext)
                .ToDictionary(kv => kv.Key, kv => (object?)kv.Value.ToString(), StringComparer.OrdinalIgnoreCase);
            var path = (App.HttpContext?.Request.Path.Value ?? string.Empty).ToLowerInvariant();
            return path.Contains("/edit")
                ? _info.UpdateagentAsync(map, ct)
                : _info.AddAsync(map, ct);
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/proxy/report"), HttpGet("/admin/proxy/report")]
    [HttpPost("/admin/proxy/reportdetail"), HttpGet("/admin/proxy/reportdetail")]
    [HttpPost("/admin/proxy/statis"), HttpGet("/admin/proxy/statis")]
    [HttpPost("/admin/proxy/userreport"), HttpGet("/admin/proxy/userreport")]
    [NonUnify, DisplayName("proxy-report")]
    public Task<OldApiResult> Report() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            var id = OldApiFormReader.ReadInt(http, "UserID", "ID", "Id", "AgentID");
            DateTime? start = null, end = null;
            if (DateTime.TryParse(OldApiFormReader.ReadString(http, "StartAt"), out var s)) start = s;
            if (DateTime.TryParse(OldApiFormReader.ReadString(http, "EndAt"), out var e)) end = e;
            return await _stats.ListAsync(new AgentStatReportListInput
            {
                UserID = id,
                StartAt = start ?? DateTime.Today.AddDays(-7),
                EndAt = end ?? DateTime.Today,
                Currency = -1
            }, ct);
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/proxy/gameuser"), HttpGet("/admin/proxy/gameuser")]
    [NonUnify, DisplayName("proxy-gameuser")]
    public Task<OldApiResult> Gameuser() =>
        Run(ct =>
        {
            var http = App.HttpContext;
            var currencyText = OldApiFormReader.ReadString(http, "Currency");
            if (string.IsNullOrWhiteSpace(currencyText))
                throw Oops.Oh("#货币不能为空#");
            _ = int.TryParse(currencyText, out var currency);
            return _gameUsers.ProxyGameUserAsync(
                OldApiFormReader.ReadString(http, "UserName", "User"),
                currency,
                ct);
        });

    private static async Task<OldApiResult> Run<T>(Func<CancellationToken, Task<T>> action) => await OldApiFormReader.RunAsync(action);
}
