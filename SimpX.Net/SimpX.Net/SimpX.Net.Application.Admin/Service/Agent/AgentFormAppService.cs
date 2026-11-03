using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 代理旧路径：/admin/user/agent/{info|commission|returnplan|grade|invite|domain|usragent}/*。
/// </summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "agent", Order = 43)]
public class AgentFormAppService : IDynamicApiController, ITransient
{
    public const string PermissionId = UserMemberFormAppService.MemberListPermissionId;

    private readonly AgentInfoService _info;
    private readonly AgentApplyService _apply;
    private readonly AgentCommissionService _commission;
    private readonly AgentReturnService _returns;
    private readonly AgentGradeService _grade;
    private readonly UserInviteService _invite;
    private readonly AgentDomainService _domain;

    public AgentFormAppService(
        AgentInfoService info,
        AgentApplyService apply,
        AgentCommissionService commission,
        AgentReturnService returns,
        AgentGradeService grade,
        UserInviteService invite,
        AgentDomainService domain)
    {
        _info = info;
        _apply = apply;
        _commission = commission;
        _returns = returns;
        _grade = grade;
        _invite = invite;
        _domain = domain;
    }

    // ----- info / usragent -----

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/agent/info/agentlist"), HttpGet("/admin/user/agent/info/agentlist")]
    [HttpPost("/admin/user/agent/usragent/list"), HttpGet("/admin/user/agent/usragent/list")]
    [NonUnify, DisplayName("agent-info-agentlist")]
    public Task<OldApiResult> Agentlist() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            var (page, size) = OldApiShowResult.ReadPaging(http);
            byte? status = null;
            var statusRaw = OldApiFormReader.ReadString(http, "Status", "status");
            if (byte.TryParse(statusRaw, out var st)) status = st;
            var belongRaw = OldApiFormReader.ReadString(http, "BelongUser", "belongUser");
            var belong = belongRaw is "1" or "true" or "True";
            DateTime? startAt = null;
            DateTime? endAt = null;
            var startRaw = OldApiFormReader.ReadString(http, "StartAt", "startAt");
            var endRaw = OldApiFormReader.ReadString(http, "EndAt", "endAt");
            if (DateTime.TryParse(startRaw, out var sa)) startAt = sa;
            if (DateTime.TryParse(endRaw, out var ea)) endAt = ea;
            var pageOut = await _info.PageAsync(new AgentInfoPageInput
            {
                Page = page,
                PageSize = size,
                UserName = OldApiFormReader.ReadString(http, "UserName", "User", "user"),
                Agent = OldApiFormReader.ReadString(http, "Agent", "AgentName", "ParentName"),
                BelongUser = belong,
                Status = status,
                StartAt = startAt,
                EndAt = endAt
            }, ct);
            return OldApiShowResult.OfPage(pageOut.Items ?? [], pageOut.Total, page, size);
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/agent/info/applylist"), HttpGet("/admin/user/agent/info/applylist")]
    [NonUnify, DisplayName("agent-info-applylist")]
    public Task<OldApiResult> Applylist() =>
        Run(ct =>
        {
            var http = App.HttpContext;
            var (page, size) = OldApiShowResult.ReadPaging(http);
            int? status = null;
            var statusRaw = OldApiFormReader.ReadString(http, "Status", "status");
            if (int.TryParse(statusRaw, out var st)) status = st;
            else if (OldApiMoneyDisplay.TryParseCheckStatus(statusRaw, out var stEnum))
                status = stEnum;
            DateTime? startAt = null;
            DateTime? endAt = null;
            if (DateTime.TryParse(OldApiFormReader.ReadString(http, "StartAt", "startAt"), out var sa))
                startAt = sa;
            if (DateTime.TryParse(OldApiFormReader.ReadString(http, "EndAt", "endAt"), out var ea))
                endAt = ea;
            return _apply.PageAsync(new AgentApplyPageInput
            {
                Page = page,
                PageSize = size,
                User = OldApiFormReader.ReadString(http, "User", "UserName", "user"),
                Status = status,
                StartAt = startAt,
                EndAt = endAt,
            }, ct);
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/agent/info/execapply")]
    [NonUnify, DisplayName("agent-info-execapply")]
    public Task<OldApiResult> Execapply() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            await _apply.CheckAsync(new AgentApplyCheckInput
            {
                ID = OldApiFormReader.ReadInt(http, "ID", "Id"),
                Status = OldApiFormReader.ReadInt(http, "Status", "status")
            }, ct);
            return (object?)null;
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/agent/info/updateagent")]
    [NonUnify, DisplayName("agent-info-updateagent")]
    public Task<OldApiResult> Updateagent() => Run(ct => _info.UpdateagentAsync(ToDict(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/agent/info/updatecommission")]
    [NonUnify, DisplayName("agent-info-updatecommission")]
    public Task<OldApiResult> Updatecommission() => Run(ct => _info.UpdatecommissionAsync(ToDict(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/agent/info/updatereturn")]
    [NonUnify, DisplayName("agent-info-updatereturn")]
    public Task<OldApiResult> Updatereturn() => Run(ct => _info.UpdatereturnAsync(ToDict(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/agent/info/permission"), HttpGet("/admin/user/agent/info/permission")]
    [NonUnify, DisplayName("agent-info-permission")]
    public Task<OldApiResult> Permission() => Run(ct => _info.PermissionAsync(ToDict(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/agent/info/add")]
    [NonUnify, DisplayName("agent-info-add")]
    public Task<OldApiResult> Infoadd() => Run(ct => _info.AddAsync(ToDict(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/agent/usragent/check")]
    [HttpPost("/admin/user/agent/grade/check")]
    [NonUnify, DisplayName("agent-usragent-check")]
    public Task<OldApiResult> Usragentcheck() => Run(ct => _info.CheckAsync(ToDict(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/agent/usragent/save")]
    [NonUnify, DisplayName("agent-usragent-save")]
    public Task<OldApiResult> Usragentsave() => Run(ct => _info.UsragentSaveAsync(ToDict(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/agent/usragent/getblockgame"), HttpGet("/admin/user/agent/usragent/getblockgame")]
    [NonUnify, DisplayName("agent-usragent-getblockgame")]
    public Task<OldApiResult> Usragentgetblockgame() => Run(ct => _info.GetblockgameAsync(ToDict(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/agent/usragent/saveblockgame")]
    [NonUnify, DisplayName("agent-usragent-saveblockgame")]
    public Task<OldApiResult> Usragentsaveblockgame() => Run(ct => _info.SaveblockgameAsync(ToDict(), ct));

    // ----- commission -----

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/agent/commission/list"), HttpGet("/admin/user/agent/commission/list")]
    [HttpPost("/admin/user/agent/commission/list0"), HttpGet("/admin/user/agent/commission/list0")]
    [NonUnify, DisplayName("agent-commission-list")]
    public Task<OldApiResult> Commissionlist() =>
        Run(ct => _commission.ListAsync(ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/agent/commission/info"), HttpGet("/admin/user/agent/commission/info")]
    [NonUnify, DisplayName("agent-commission-info")]
    public Task<OldApiResult> Commissioninfo() =>
        Run(ct => _commission.DetailAsync(OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id"), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/agent/commission/delete")]
    [NonUnify, DisplayName("agent-commission-delete")]
    public Task<OldApiResult> Commissiondelete() =>
        Run(async ct =>
        {
            await _commission.DeleteAsync(OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id"), ct);
            return (object?)null;
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/agent/commission/agentrate"), HttpGet("/admin/user/agent/commission/agentrate")]
    [NonUnify, DisplayName("agent-commission-agentrate")]
    public Task<OldApiResult> Commissionagentrate() => Run(ct => _commission.AgentrateAsync(ToDict(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/agent/commission/saveagentrate")]
    [NonUnify, DisplayName("agent-commission-saveagentrate")]
    public Task<OldApiResult> Commissionsaveagentrate() => Run(ct => _commission.SaveagentrateAsync(ToDict(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/agent/commission/updateagentcount")]
    [NonUnify, DisplayName("agent-commission-updateagentcount")]
    public Task<OldApiResult> Commissionupdateagentcount() => Run(ct => _commission.UpdateagentcountAsync(ToDict(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/agent/commission/commissionlist"), HttpGet("/admin/user/agent/commission/commissionlist")]
    [NonUnify, DisplayName("agent-commission-commissionlist")]
    public Task<OldApiResult> Commissioncommissionlist() => Run(ct => _commission.CommissionlistAsync(ToDict(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/agent/commission/allagentcommissionlist"), HttpGet("/admin/user/agent/commission/allagentcommissionlist")]
    [NonUnify, DisplayName("agent-commission-allagentcommissionlist")]
    public Task<OldApiResult> Commissionallagentcommissionlist() =>
        Run(ct => _commission.AllagentcommissionlistAsync(ToDict(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/agent/domain/background"), HttpGet("/admin/user/agent/domain/background")]
    [NonUnify, DisplayName("agent-domain-background")]
    public Task<OldApiResult> Domainbackground() => Run(ct => _domain.BackgroundAsync(ToDict(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/agent/returnplan/loglist"), HttpGet("/admin/user/agent/returnplan/loglist")]
    [NonUnify, DisplayName("agent-returnplan-loglist")]
    public Task<OldApiResult> Returnplanloglist() => Run(ct => _returns.LoglistAsync(ToDict(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/agent/returnplan/logdetail"), HttpGet("/admin/user/agent/returnplan/logdetail")]
    [NonUnify, DisplayName("agent-returnplan-logdetail")]
    public Task<OldApiResult> Returnplanlogdetail() => Run(ct => _returns.LogdetailAsync(ToDict(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/agent/returnplan/reexereturnmoney")]
    [NonUnify, DisplayName("agent-returnplan-reexereturnmoney")]
    public Task<OldApiResult> Returnplanreexereturnmoney() => Run(ct => _returns.ReexereturnmoneyAsync(ToDict(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/agent/commission/save")]
    [NonUnify, DisplayName("agent-commission-save")]
    public Task<OldApiResult> Commissionsave() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            var id = await _commission.SaveAsync(new AgentCommissionSaveInput
            {
                ID = OldApiFormReader.ReadInt(http, "ID", "Id"),
                Name = OldApiFormReader.ReadString(http, "Name", "name"),
                MemberMoney = ReadDecimal(http, "MemberMoney"),
                Award = ReadDecimal(http, "Award"),
                Return = ReadDecimal(http, "Return"),
                Cost = ReadDecimal(http, "Cost"),
                IsOpen = OldApiFormReader.ReadBool(http, "IsOpen"),
                IsContract = OldApiFormReader.ReadBool(http, "IsContract"),
                SettingString = OldApiFormReader.ReadString(http, "SettingString", "Setting"),
                MaxRate = ReadDecimal(http, "MaxRate")
            }, ct);
            return (object?)id;
        });

    // ----- returnplan -----

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/agent/returnplan/list"), HttpGet("/admin/user/agent/returnplan/list")]
    [HttpPost("/admin/user/agent/returnplan/list0"), HttpGet("/admin/user/agent/returnplan/list0")]
    [NonUnify, DisplayName("agent-returnplan-list")]
    public Task<OldApiResult> Returnlist() =>
        Run(ct => _returns.ListAsync(ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/agent/returnplan/info"), HttpGet("/admin/user/agent/returnplan/info")]
    [NonUnify, DisplayName("agent-returnplan-info")]
    public Task<OldApiResult> Returninfo() =>
        Run(ct => _returns.DetailAsync(OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id"), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/agent/returnplan/delete")]
    [NonUnify, DisplayName("agent-returnplan-delete")]
    public Task<OldApiResult> Returndelete() =>
        Run(async ct =>
        {
            await _returns.DeleteAsync(OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id"), ct);
            return (object?)null;
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/agent/returnplan/save")]
    [NonUnify, DisplayName("agent-returnplan-save")]
    public Task<OldApiResult> Returnsave() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            var id = await _returns.SaveAsync(new AgentReturnSaveInput
            {
                ID = OldApiFormReader.ReadInt(http, "ID", "Id"),
                Name = OldApiFormReader.ReadString(http, "Name", "name"),
                SettingString = OldApiFormReader.ReadString(http, "SettingString", "Setting"),
                IsOpen = OldApiFormReader.ReadBool(http, "IsOpen")
            }, ct);
            return (object?)id;
        });

    // ----- grade -----

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/agent/grade/list"), HttpGet("/admin/user/agent/grade/list")]
    [NonUnify, DisplayName("agent-grade-list")]
    public Task<OldApiResult> Gradelist() =>
        Run(ct =>
        {
            var http = App.HttpContext;
            int? type = null;
            var typeRaw = OldApiFormReader.ReadString(http, "Type", "type");
            if (int.TryParse(typeRaw, out var t)) type = t;
            else if (string.Equals(typeRaw, "Member", StringComparison.OrdinalIgnoreCase)
                     || string.Equals(typeRaw, "会员", StringComparison.OrdinalIgnoreCase))
                type = 0;
            else if (string.Equals(typeRaw, "Agent", StringComparison.OrdinalIgnoreCase)
                     || string.Equals(typeRaw, "代理", StringComparison.OrdinalIgnoreCase))
                type = 1;
            return _grade.ListAsync(new AgentGradeListInput
            {
                UserName = OldApiFormReader.ReadString(http, "UserName", "User", "user"),
                Type = type
            }, ct);
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/agent/grade/save")]
    [HttpPost("/admin/user/agent/grade/add")]
    [NonUnify, DisplayName("agent-grade-save")]
    public Task<OldApiResult> Gradesave() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            var parentRaw = OldApiFormReader.ReadString(http, "ParentID", "AgentID");
            var parentId = -1;
            if (!string.IsNullOrWhiteSpace(parentRaw) && int.TryParse(parentRaw, out var pid))
                parentId = pid;
            await _grade.SaveAsync(new AgentGradeSaveInput
            {
                UserID = OldApiFormReader.ReadInt(http, "UserID", "ID", "Id"),
                UserName = OldApiFormReader.ReadString(http, "UserName", "User"),
                ParentID = parentId,
                ParentName = OldApiFormReader.ReadString(http, "ParentName", "Agent", "AgentName"),
                AsAgent = string.IsNullOrEmpty(OldApiFormReader.ReadString(http, "AsAgent"))
                    || OldApiFormReader.ReadBool(http, "AsAgent"),
                CommissionID = OldApiFormReader.ReadInt(http, "CommissionID"),
                ReturnID = OldApiFormReader.ReadInt(http, "ReturnID")
            }, ct);
            return (object?)null;
        });

    // ----- invite -----

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/agent/invite/list"), HttpGet("/admin/user/agent/invite/list")]
    [NonUnify, DisplayName("agent-invite-list")]
    public Task<OldApiResult> Invitelist() =>
        Run(ct =>
        {
            var http = App.HttpContext;
            bool? isOpen = null;
            var openRaw = OldApiFormReader.ReadString(http, "IsOpen", "isOpen");
            if (bool.TryParse(openRaw, out var ob)) isOpen = ob;
            else if (openRaw is "1" or "0") isOpen = openRaw == "1";
            return _invite.ListAsync(new UserInviteListInput
            {
                Invite = OldApiFormReader.ReadString(http, "Invite", "ID"),
                ID = OldApiFormReader.ReadString(http, "ID", "Invite"),
                User = OldApiFormReader.ReadString(http, "User", "UserName"),
                Type = OldApiFormReader.ReadString(http, "Type", "type"),
                IsOpen = isOpen
            }, ct);
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/agent/invite/info"), HttpGet("/admin/user/agent/invite/info")]
    [NonUnify, DisplayName("agent-invite-info")]
    public Task<OldApiResult> Inviteinfo() =>
        Run(ct => _invite.InfoAsync(OldApiFormReader.ReadString(App.HttpContext, "ID", "Invite", "Id"), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/agent/invite/delete")]
    [NonUnify, DisplayName("agent-invite-delete")]
    public Task<OldApiResult> Invitedelete() =>
        Run(async ct =>
        {
            await _invite.DeleteAsync(OldApiFormReader.ReadString(App.HttpContext, "ID", "Invite", "Id"), ct);
            return (object?)null;
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/agent/invite/save")]
    [NonUnify, DisplayName("agent-invite-save")]
    public Task<OldApiResult> Invitesave() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            await _invite.SaveAsync(new UserInviteSaveInput
            {
                ID = OldApiFormReader.ReadString(http, "ID", "Invite"),
                UserID = OldApiFormReader.ReadInt(http, "UserID"),
                User = OldApiFormReader.ReadString(http, "User", "UserName"),
                IsOpen = string.IsNullOrEmpty(OldApiFormReader.ReadString(http, "IsOpen"))
                    || OldApiFormReader.ReadBool(http, "IsOpen"),
                Game = OldApiFormReader.ReadString(http, "Game", "game")
            }, ct);
            return (object?)null;
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/agent/invite/updateinviteopen")]
    [NonUnify, DisplayName("agent-invite-updateinviteopen")]
    public Task<OldApiResult> Updateinviteopen() => Run(ct => _info.UpdateinviteopenAsync(ToDict(), ct));

    // ----- domain -----

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/agent/domain/list"), HttpGet("/admin/user/agent/domain/list")]
    [NonUnify, DisplayName("agent-domain-list")]
    public Task<OldApiResult> Domainlist() =>
        Run(ct =>
        {
            var http = App.HttpContext;
            var (page, size) = OldApiShowResult.ReadPaging(http);
            return _domain.PageAsync(new AgentDomainPageInput
            {
                Page = page,
                PageSize = size,
                User = OldApiFormReader.ReadString(http, "User", "UserName"),
                Invite = OldApiFormReader.ReadString(http, "Invite"),
                Domain = OldApiFormReader.ReadString(http, "Domain", "Name"),
                StartAt = ReadDate(http, "StartAt"),
                EndAt = ReadDate(http, "EndAt")
            }, ct);
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/agent/domain/info"), HttpGet("/admin/user/agent/domain/info")]
    [NonUnify, DisplayName("agent-domain-info")]
    public Task<OldApiResult> Domaininfo() =>
        Run(ct => _domain.DetailAsync(OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id"), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/agent/domain/delete")]
    [NonUnify, DisplayName("agent-domain-delete")]
    public Task<OldApiResult> Domaindelete() =>
        Run(async ct =>
        {
            await _domain.DeleteAsync(OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id"), ct);
            return (object?)null;
        });

    /// <summary>保存代理域名。对应旧 /admin/user/agent/domain/save。</summary>
    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/agent/domain/save")]
    [NonUnify, DisplayName("agent-domain-save")]
    public Task<OldApiResult> Domainsave() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            await _domain.SaveAsync(new AgentDomainSaveInput
            {
                ID = OldApiFormReader.ReadInt(http, "ID", "Id"),
                Name = OldApiFormReader.ReadString(http, "Name", "Domain"),
                InviteID = OldApiFormReader.ReadString(http, "InviteID", "Invite"),
                SiteName = OldApiFormReader.ReadString(http, "SiteName"),
                Logo = OldApiFormReader.ReadString(http, "Logo"),
                DefaultBackground = OldApiFormReader.ReadString(http, "DefaultBackground"),
                DefaultTemplate = OldApiFormReader.ReadString(http, "DefaultTemplate")
            }, ct);
            return (object?)null;
        });

    private static Dictionary<string, object?> ToDict()
    {
        var map = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (k, v) in OldApiFormReader.ReadJsonMap(App.HttpContext))
            map[k] = v.ToString();
        return map;
    }

    private static decimal ReadDecimal(Microsoft.AspNetCore.Http.HttpContext http, params string[] keys)
    {
        _ = decimal.TryParse(OldApiFormReader.ReadString(http, keys), out var n);
        return n;
    }

    private static DateTime? ReadDate(Microsoft.AspNetCore.Http.HttpContext http, params string[] keys)
    {
        var s = OldApiFormReader.ReadString(http, keys);
        if (string.IsNullOrWhiteSpace(s)) return null;
        return DateTime.TryParse(s, out var dt) ? dt : null;
    }

    private static async Task<OldApiResult> Run<T>(Func<CancellationToken, Task<T>> action) => await OldApiFormReader.RunAsync(action);
}
