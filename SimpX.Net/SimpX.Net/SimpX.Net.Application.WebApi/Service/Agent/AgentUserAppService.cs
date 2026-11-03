using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;
using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.WebApi;

/// <summary>
/// 代理会员侧：/request/agent/user/*（Shared AgentInfo / 会员鉴权）。
/// </summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "agent-user", Order = 38)]
public class AgentUserAppService : IDynamicApiController, ITransient
{
    private readonly AgentInfoService _info;
    private readonly MemberAuthService _auth;
    private readonly ICurrentSiteAccessor _site;
    private readonly IBizDbAccessor _biz;

    public AgentUserAppService(
        AgentInfoService info,
        MemberAuthService auth,
        ICurrentSiteAccessor site,
        IBizDbAccessor biz)
    {
        _info = info;
        _auth = auth;
        _site = site;
        _biz = biz;
    }

    [AllowAnonymous]
    [HttpPost("/request/agent/user/agentlist"), HttpGet("/request/agent/user/agentlist")]
    [NonUnify, DisplayName("agent-user-agentlist")]
    public Task<OldApiResult> Agentlist() =>
        Run(async ct =>
        {
            _ = await RequireUserIdAsync(ct);
            var http = App.HttpContext;
            var (page, size) = OldApiShowResult.ReadPaging(http);
            byte? status = null;
            var statusRaw = OldApiFormReader.ReadString(http, "Status", "status");
            if (byte.TryParse(statusRaw, out var st)) status = st;
            return await _info.PageAsync(new AgentInfoPageInput
            {
                Page = page,
                PageSize = size,
                UserName = OldApiFormReader.ReadString(http, "UserName", "User", "user"),
                Status = status
            }, ct);
        });

    [AllowAnonymous]
    [HttpPost("/request/agent/user/updateagent")]
    [NonUnify, DisplayName("agent-user-updateagent")]
    public Task<OldApiResult> Updateagent() =>
        Run(async ct =>
        {
            _ = await RequireUserIdAsync(ct);
            var map = ToDict();
            if (!map.ContainsKey("UserID") && map.TryGetValue("ID", out var id))
                map["UserID"] = id;
            return await _info.UpdateagentAsync(map, ct);
        });

    [AllowAnonymous]
    [HttpPost("/request/agent/user/add")]
    [NonUnify, DisplayName("agent-user-add")]
    public Task<OldApiResult> Add() =>
        Run(async ct =>
        {
            _ = await RequireUserIdAsync(ct);
            return await _info.AddAsync(ToDict(), ct);
        });

    [AllowAnonymous]
    [HttpPost("/request/agent/user/checkagent"), HttpGet("/request/agent/user/checkagent")]
    [NonUnify, DisplayName("agent-user-checkagent")]
    public Task<OldApiResult> Checkagent() =>
        Run(async ct =>
        {
            var userId = await RequireUserIdAsync(ct);
            var siteId = _site.RequireSiteId();
            var rows = await _biz.Db.Queryable<User>()
                .Where(u => u.SiteID == siteId && u.ID == userId)
                .Take(1)
                .ToListAsync(ct);
            var user = rows.FirstOrDefault() ?? throw Oops.Oh("#请先登录#");
            var can = (user.Function & AgentInfoService.FunctionCheckAgent) == AgentInfoService.FunctionCheckAgent;
            return can;
        });

    [AllowAnonymous]
    [HttpPost("/request/agent/user/userlist"), HttpGet("/request/agent/user/userlist")]
    [NonUnify, DisplayName("agent-user-userlist")]
    public Task<OldApiResult> Userlist() =>
        Run(async ct =>
        {
            var agentId = await RequireUserIdAsync(ct);
            var siteId = _site.RequireSiteId();
            var http = App.HttpContext;
            var (page, size) = OldApiShowResult.ReadPaging(http);
            var name = OldApiFormReader.ReadString(http, "UserName", "User", "user");
            var query = _biz.Db.Queryable<User>()
                .Where(u => u.SiteID == siteId && u.AgentID == agentId);
            if (!string.IsNullOrWhiteSpace(name))
                query = query.Where(u => u.UserName.Contains(name.Trim()));
            RefAsync<int> total = 0;
            var rows = await query
                .OrderBy(u => u.ID, OrderByType.Desc)
                .Select(u => new
                {
                    u.ID,
                    u.UserName,
                    u.Type,
                    u.CreateAt,
                    u.Notes
                })
                .ToPageListAsync(page, size, total, ct);
            return new { Items = rows, Total = (int)total };
        });

    [AllowAnonymous]
    [HttpPost("/request/agent/user/updatenotes")]
    [NonUnify, DisplayName("agent-user-updatenotes")]
    public Task<OldApiResult> Updatenotes() =>
        Run(async ct =>
        {
            var agentId = await RequireUserIdAsync(ct);
            var siteId = _site.RequireSiteId();
            var id = OldApiFormReader.ReadInt(App.HttpContext, "Id", "ID", "UserID");
            var notes = OldApiFormReader.ReadString(App.HttpContext, "Notes", "notes");
            if (id <= 0) throw Oops.Oh("#参数错误#");
            var under = await _biz.Db.Queryable<User>()
                .AnyAsync(u => u.SiteID == siteId && u.ID == id && u.AgentID == agentId, ct);
            if (!under)
                throw Oops.Oh("#用户不存在#");
            await _biz.Db.Updateable<User>()
                .SetColumns(u => u.Notes == (notes ?? string.Empty))
                .Where(u => u.SiteID == siteId && u.ID == id)
                .ExecuteCommandAsync(ct);
            return true;
        });

    [AllowAnonymous]
    [HttpPost("/request/agent/team/getuserbalancelist"), HttpGet("/request/agent/team/getuserbalancelist")]
    [NonUnify, DisplayName("agent-team-balancelist")]
    public Task<OldApiResult> Getuserbalancelist() =>
        Run(async ct =>
        {
            var agentId = await RequireUserIdAsync(ct);
            var siteId = _site.RequireSiteId();
            var childIds = await _biz.Db.Queryable<UserDepth>()
                .Where(d => d.SiteID == siteId && d.UserID == agentId && d.Depth == 1)
                .Select(d => d.ChildID)
                .ToListAsync(ct);
            if (childIds.Count == 0)
                return (object)Array.Empty<object>();
            var money = await _biz.Db.Queryable<UserMoney>()
                .Where(m => m.SiteID == siteId && childIds.Contains(m.UserID))
                .Select(m => new { m.UserID, m.Currency, m.TotalMoney, m.LockMoney })
                .ToListAsync(ct);
            return (object)money;
        });

    private async Task<int> RequireUserIdAsync(CancellationToken ct)
    {
        var siteId = _site.RequireSiteId();
        var userId = await _auth.TryResolveUserIdFromCookieAsync(siteId, ct);
        if (userId is null or <= 0)
            throw Oops.Oh("#请先登录#");
        return userId.Value;
    }

    private static Dictionary<string, object?> ToDict()
    {
        var map = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (k, v) in OldApiFormReader.ReadJsonMap(App.HttpContext))
            map[k] = v.ToString();
        return map;
    }

    private static async Task<OldApiResult> Run<T>(Func<CancellationToken, Task<T>> action) => await OldApiFormReader.RunAsync(action);
}
