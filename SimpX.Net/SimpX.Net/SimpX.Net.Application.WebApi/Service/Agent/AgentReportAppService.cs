using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;
using SimpX.Net.Data.Entity.Reports;
using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.WebApi;

/// <summary>
/// 代理报表：对齐旧 BetWin.Handler.Request.agent.report / chart。
/// GET+POST：旧 QF/GetParam 读 Form+Query；写操作仅 POST。
/// 一旧方法一路经，禁止刷覆盖率合并。
/// </summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "agent-report", Order = 39)]
public class AgentReportAppService : IDynamicApiController, ITransient
{
    private readonly AgentStatReportService _stats;
    private readonly MemberAuthService _auth;
    private readonly ICurrentSiteAccessor _site;
    private readonly IBizDbAccessor _biz;

    public AgentReportAppService(
        AgentStatReportService stats,
        MemberAuthService auth,
        ICurrentSiteAccessor site,
        IBizDbAccessor biz)
    {
        _stats = stats;
        _auth = auth;
        _site = site;
        _biz = biz;
    }

    /// <summary>旧 report.commission — 日佣金统计。</summary>
    [AllowAnonymous]
    [HttpPost("/request/agent/report/commission"), HttpGet("/request/agent/report/commission")]
    [NonUnify, DisplayName("agent-report-commission")]
    public Task<OldApiResult> Commission() => StatSummaryAsync();

    /// <summary>旧 chart.statis — 与 commission 同源日统计（旧 chart 另文件，此处读同一表）。</summary>
    [AllowAnonymous]
    [HttpPost("/request/agent/chart/statis"), HttpGet("/request/agent/chart/statis")]
    [NonUnify, DisplayName("agent-chart-statis")]
    public Task<OldApiResult> ChartStatis() => StatSummaryAsync();

    /// <summary>旧 chart.finace。</summary>
    [AllowAnonymous]
    [HttpPost("/request/agent/chart/finace"), HttpGet("/request/agent/chart/finace")]
    [NonUnify, DisplayName("agent-chart-finace")]
    public Task<OldApiResult> ChartFinace() => StatSummaryAsync();

    /// <summary>旧 chart.finacesingle。</summary>
    [AllowAnonymous]
    [HttpPost("/request/agent/chart/finacesingle"), HttpGet("/request/agent/chart/finacesingle")]
    [NonUnify, DisplayName("agent-chart-finacesingle")]
    public Task<OldApiResult> ChartFinacesingle() => StatSummaryAsync();

    /// <summary>旧 report.agentinfo / userstat。</summary>
    [AllowAnonymous]
    [HttpPost("/request/agent/report/agentinfo"), HttpGet("/request/agent/report/agentinfo")]
    [HttpPost("/request/agent/report/userstat"), HttpGet("/request/agent/report/userstat")]
    [NonUnify, DisplayName("agent-report-agentinfo")]
    public Task<OldApiResult> Agentinfo() =>
        Run(async ct =>
        {
            var userId = await RequireUserIdAsync(ct);
            var http = App.HttpContext;
            return await _stats.ListAsync(new AgentStatReportListInput
            {
                UserID = userId,
                StartAt = ReadDate(http, "StartAt") ?? DateTime.Today,
                EndAt = ReadDate(http, "EndAt") ?? DateTime.Today,
                Currency = -1
            }, ct);
        });

    /// <summary>旧 report.gamereport — 团队 UserDateGame 明细（非 commission）。</summary>
    [AllowAnonymous]
    [HttpPost("/request/agent/report/gamereport"), HttpGet("/request/agent/report/gamereport")]
    [NonUnify, DisplayName("agent-report-gamereport")]
    public Task<OldApiResult> Gamereport() =>
        Run(async ct =>
        {
            var agentId = await RequireUserIdAsync(ct);
            var siteId = _site.RequireSiteId();
            var http = App.HttpContext;
            var start = ReadDate(http, "StartAt") ?? DateTime.Today.AddDays(-7);
            var end = ReadDate(http, "EndAt") ?? DateTime.Today;
            var childIds = await _biz.Db.Queryable<UserDepth>()
                .Where(d => d.SiteID == siteId && d.UserID == agentId)
                .Select(d => d.ChildID)
                .ToListAsync(ct);
            if (childIds.Count == 0)
                return (object)new { Items = Array.Empty<object>(), Total = 0 };

            var userName = OldApiFormReader.ReadString(http, "UserName", "User");
            if (!string.IsNullOrWhiteSpace(userName))
            {
                var uid = await _biz.Db.Queryable<User>()
                    .Where(u => u.SiteID == siteId && u.UserName == userName.Trim())
                    .Select(u => u.ID)
                    .FirstAsync(ct);
                if (uid <= 0 || !childIds.Contains(uid))
                    return (object)new { Items = Array.Empty<object>(), Total = 0 };
                childIds = [uid];
            }

            var (page, size) = OldApiShowResult.ReadPaging(http);
            RefAsync<int> total = 0;
            var rows = await _biz.Db.Queryable<UserDateGame>()
                .Where(g => g.SiteID == siteId && g.Date >= start && g.Date <= end && childIds.Contains(g.UserID))
                .OrderBy(g => g.Date, OrderByType.Desc)
                .Select(g => new { g.UserID, g.Date, g.Type, g.Category, g.BetMoney, g.Money, g.Currency })
                .ToPageListAsync(page, size, total, ct);
            return (object)new { Items = rows, Total = (int)total };
        });

    /// <summary>旧 report.all — 团队盈亏总览；暂用日统计汇总（MoneyTypeIsProfit 全量迁移前）。</summary>
    [AllowAnonymous]
    [HttpPost("/request/agent/report/all"), HttpGet("/request/agent/report/all")]
    [NonUnify, DisplayName("agent-report-all")]
    public Task<OldApiResult> All() => StatSummaryAsync();

    /// <summary>旧 report.suball — 下级盈亏；暂用日统计（按子代理再拆后期）。</summary>
    [AllowAnonymous]
    [HttpPost("/request/agent/report/suball"), HttpGet("/request/agent/report/suball")]
    [NonUnify, DisplayName("agent-report-suball")]
    public Task<OldApiResult> Suball() => StatSummaryAsync();

    /// <summary>旧 report.moneylog。</summary>
    [AllowAnonymous]
    [HttpPost("/request/agent/report/moneylog"), HttpGet("/request/agent/report/moneylog")]
    [NonUnify, DisplayName("agent-report-moneylog")]
    public Task<OldApiResult> Moneylog() => MoneyLogCoreAsync();

    /// <summary>旧 report.moneydetails。</summary>
    [AllowAnonymous]
    [HttpPost("/request/agent/report/moneydetails"), HttpGet("/request/agent/report/moneydetails")]
    [NonUnify, DisplayName("agent-report-moneydetails")]
    public Task<OldApiResult> Moneydetails() => MoneyLogCoreAsync();

    /// <summary>旧 chart.inviteurl — usr_Domain。</summary>
    [AllowAnonymous]
    [HttpPost("/request/agent/chart/inviteurl"), HttpGet("/request/agent/chart/inviteurl")]
    [NonUnify, DisplayName("agent-chart-inviteurl")]
    public Task<OldApiResult> Inviteurl() =>
        Run(async ct =>
        {
            var agentId = await RequireUserIdAsync(ct);
            var siteId = _site.RequireSiteId();
            return (object)await _biz.Db.Queryable<UserDomain>()
                .Where(d => d.SiteID == siteId && d.UserID == agentId)
                .Select(d => new { d.ID, Domain = d.Name, d.InviteID, d.CreateAt })
                .Take(50)
                .ToListAsync(ct);
        });

    // --- 厂商注单 / chart 细项：旧各有独立方法；厂商明细延后，分路径挂空避免误合并 ---
    [AllowAnonymous]
    [HttpPost("/request/agent/report/slotlog"), HttpGet("/request/agent/report/slotlog")]
    [NonUnify, DisplayName("agent-report-slotlog")]
    public Task<OldApiResult> Slotlog() => VendorEmptyAsync();

    [AllowAnonymous]
    [HttpPost("/request/agent/report/livelog"), HttpGet("/request/agent/report/livelog")]
    [NonUnify, DisplayName("agent-report-livelog")]
    public Task<OldApiResult> Livelog() => VendorEmptyAsync();

    [AllowAnonymous]
    [HttpPost("/request/agent/report/sportlog"), HttpGet("/request/agent/report/sportlog")]
    [NonUnify, DisplayName("agent-report-sportlog")]
    public Task<OldApiResult> Sportlog() => VendorEmptyAsync();

    [AllowAnonymous]
    [HttpPost("/request/agent/report/fishlog"), HttpGet("/request/agent/report/fishlog")]
    [NonUnify, DisplayName("agent-report-fishlog")]
    public Task<OldApiResult> Fishlog() => VendorEmptyAsync();

    [AllowAnonymous]
    [HttpPost("/request/agent/report/chesslog"), HttpGet("/request/agent/report/chesslog")]
    [HttpPost("/request/agent/report/chessinfo"), HttpGet("/request/agent/report/chessinfo")]
    [NonUnify, DisplayName("agent-report-chess")]
    public Task<OldApiResult> Chesslog() => VendorEmptyAsync();

    [AllowAnonymous]
    [HttpPost("/request/agent/report/esportlog"), HttpGet("/request/agent/report/esportlog")]
    [NonUnify, DisplayName("agent-report-esportlog")]
    public Task<OldApiResult> Esportlog() => VendorEmptyAsync();

    [AllowAnonymous]
    [HttpPost("/request/agent/report/lotterylog"), HttpGet("/request/agent/report/lotterylog")]
    [HttpPost("/request/agent/report/lotteryinfo"), HttpGet("/request/agent/report/lotteryinfo")]
    [NonUnify, DisplayName("agent-report-lottery")]
    public Task<OldApiResult> Lotterylog() => VendorEmptyAsync();

    [AllowAnonymous]
    [HttpPost("/request/agent/report/animallog"), HttpGet("/request/agent/report/animallog")]
    [NonUnify, DisplayName("agent-report-animallog")]
    public Task<OldApiResult> Animallog() => VendorEmptyAsync();

    [AllowAnonymous]
    [HttpPost("/request/agent/report/minilog"), HttpGet("/request/agent/report/minilog")]
    [NonUnify, DisplayName("agent-report-minilog")]
    public Task<OldApiResult> Minilog() => VendorEmptyAsync();

    [AllowAnonymous]
    [HttpPost("/request/agent/chart/gamedetail"), HttpGet("/request/agent/chart/gamedetail")]
    [NonUnify, DisplayName("agent-chart-gamedetail")]
    public Task<OldApiResult> ChartGamedetail() => VendorEmptyAsync();

    [AllowAnonymous]
    [HttpPost("/request/agent/chart/gamerate"), HttpGet("/request/agent/chart/gamerate")]
    [NonUnify, DisplayName("agent-chart-gamerate")]
    public Task<OldApiResult> ChartGamerate() => VendorEmptyAsync();

    [AllowAnonymous]
    [HttpPost("/request/agent/chart/reportdetail"), HttpGet("/request/agent/chart/reportdetail")]
    [NonUnify, DisplayName("agent-chart-reportdetail")]
    public Task<OldApiResult> ChartReportdetail() => VendorEmptyAsync();

    [AllowAnonymous]
    [HttpPost("/request/agent/chart/promote"), HttpGet("/request/agent/chart/promote")]
    [NonUnify, DisplayName("agent-chart-promote")]
    public Task<OldApiResult> ChartPromote() => VendorEmptyAsync();

    [AllowAnonymous]
    [HttpPost("/request/agent/chart/promoterechargenumber"), HttpGet("/request/agent/chart/promoterechargenumber")]
    [NonUnify, DisplayName("agent-chart-promoterecharge")]
    public Task<OldApiResult> ChartPromoterecharge() => VendorEmptyAsync();

    [AllowAnonymous]
    [HttpPost("/request/agent/chart/commissionlog"), HttpGet("/request/agent/chart/commissionlog")]
    [NonUnify, DisplayName("agent-chart-commissionlog")]
    public Task<OldApiResult> ChartCommissionlog() => VendorEmptyAsync();

    [AllowAnonymous]
    [HttpPost("/request/agent/chart/commissionlogdetail"), HttpGet("/request/agent/chart/commissionlogdetail")]
    [NonUnify, DisplayName("agent-chart-commissionlogdetail")]
    public Task<OldApiResult> ChartCommissionlogdetail() => VendorEmptyAsync();

    private Task<OldApiResult> StatSummaryAsync() =>
        Run(async ct =>
        {
            var userId = await RequireUserIdAsync(ct);
            var http = App.HttpContext;
            var list = await _stats.ListAsync(new AgentStatReportListInput
            {
                UserID = userId,
                StartAt = ReadDate(http, "StartAt") ?? DateTime.Today,
                EndAt = ReadDate(http, "EndAt") ?? DateTime.Today,
                Currency = -1
            }, ct);
            return new
            {
                Items = list,
                Summary = new
                {
                    GameMoney = list.Sum(x => x.GameMoney),
                    MoneyEvent = list.Sum(x => x.MoneyEvent),
                    MoneyRebate = list.Sum(x => x.MoneyRebate),
                    RechargeFee = list.Sum(x => x.RechargeFee),
                    WithdrawFee = list.Sum(x => x.WithdrawFee)
                }
            };
        });

    private Task<OldApiResult> MoneyLogCoreAsync() =>
        Run(async ct =>
        {
            var agentId = await RequireUserIdAsync(ct);
            var siteId = _site.RequireSiteId();
            var http = App.HttpContext;
            var start = ReadDate(http, "StartAt") ?? DateTime.Today.AddDays(-7);
            var end = ReadDate(http, "EndAt") ?? DateTime.Today.AddDays(1);
            var userName = OldApiFormReader.ReadString(http, "UserName", "User");
            var type = OldApiFormReader.ReadInt(http, "Type", "type");
            var currency = OldApiFormReader.ReadInt(http, "Currency", "currency");

            var childIds = await _biz.Db.Queryable<UserDepth>()
                .Where(d => d.SiteID == siteId && d.UserID == agentId)
                .Select(d => d.ChildID)
                .ToListAsync(ct);
            childIds.Add(agentId);

            if (!string.IsNullOrWhiteSpace(userName))
            {
                var uid = await _biz.Db.Queryable<User>()
                    .Where(u => u.SiteID == siteId && u.UserName == userName.Trim())
                    .Select(u => u.ID)
                    .FirstAsync(ct);
                if (uid <= 0 || !childIds.Contains(uid))
                    return (object)new { Items = Array.Empty<object>(), Total = 0 };
                childIds = [uid];
            }

            var query = _biz.Db.Queryable<MoneyLog>()
                .Where(m => m.SiteID == siteId
                    && childIds.Contains(m.UserID)
                    && m.CreateAt >= start && m.CreateAt < end);
            if (type != 0)
                query = query.Where(m => m.Type == type);
            if (currency != 0)
                query = query.Where(m => m.Currency == currency);

            var (page, size) = OldApiShowResult.ReadPaging(http);
            RefAsync<int> total = 0;
            var rows = await query
                .OrderBy(m => m.CreateAt, OrderByType.Desc)
                .Select(m => new
                {
                    m.ID,
                    m.UserID,
                    m.Money,
                    m.Balance,
                    m.Type,
                    m.CreateAt,
                    m.Currency,
                    m.Description
                })
                .ToPageListAsync(page, size, total, ct);
            return (object)new { Items = rows, Total = (int)total };
        });

    private Task<OldApiResult> VendorEmptyAsync() =>
        Run(async ct =>
        {
            await RequireUserIdAsync(ct);
            // 后期：厂商注单真接（用户已声明延后）
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
