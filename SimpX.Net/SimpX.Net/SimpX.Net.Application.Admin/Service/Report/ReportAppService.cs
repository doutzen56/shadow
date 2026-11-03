using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 报表旧路径：/admin/report/{site|user|bill|chart}/*。
/// </summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "report", Order = 41)]
public class ReportAppService : IDynamicApiController, ITransient
{
    public const string PermissionId = UserMemberFormAppService.MemberListPermissionId;

    private readonly SiteFundDailyStatService _siteFund;
    private readonly LoginLogService _loginLog;
    private readonly SettlementBillService _bill;
    private readonly GameDateReportService _gameDate;
    private readonly UserBalanceSummaryService _balance;
    private readonly AgentStatReportService _agentStat;
    private readonly Deposit123ReportService _deposit123;
    private readonly UserSignService _sign;
    private readonly SitePointItemService _points;
    private readonly MoneyLogService _moneyLog;

    public ReportAppService(
        SiteFundDailyStatService siteFund,
        LoginLogService loginLog,
        SettlementBillService bill,
        GameDateReportService gameDate,
        UserBalanceSummaryService balance,
        AgentStatReportService agentStat,
        Deposit123ReportService deposit123,
        UserSignService sign,
        SitePointItemService points,
        MoneyLogService moneyLog)
    {
        _siteFund = siteFund;
        _loginLog = loginLog;
        _bill = bill;
        _gameDate = gameDate;
        _balance = balance;
        _agentStat = agentStat;
        _deposit123 = deposit123;
        _sign = sign;
        _points = points;
        _moneyLog = moneyLog;
    }

    private Dictionary<string, object?> Form() =>
        OldApiFormReader.ReadJsonMap(App.HttpContext)
            .ToDictionary(kv => kv.Key, kv => (object?)kv.Value.ToString(), StringComparer.OrdinalIgnoreCase);

    // ----- site 首页/运营统计（旧 /admin/report/site/*）-----

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/report/site/activeusers"), HttpGet("/admin/report/site/activeusers")]
    [NonUnify, DisplayName("report-activeusers")]
    public Task<OldApiResult> Activeusers() => Run(ct => _siteFund.ActiveusersAsync(Form(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/report/site/betamount"), HttpGet("/admin/report/site/betamount")]
    [NonUnify, DisplayName("report-betamount")]
    public Task<OldApiResult> Betamount() => Run(ct => _siteFund.BetamountAsync(Form(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/report/site/chartlist"), HttpGet("/admin/report/site/chartlist")]
    [HttpPost("/admin/chart/productcollect"), HttpGet("/admin/chart/productcollect")]
    [HttpPost("/admin/chart/userstatistics"), HttpGet("/admin/chart/userstatistics")]
    [HttpPost("/admin/report/user/chart"), HttpGet("/admin/report/user/chart")]
    [NonUnify, DisplayName("report-chartlist")]
    public Task<OldApiResult> Chartlist() => Run(ct => _siteFund.ChartlistAsync(Form(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/report/site/gamecreditlog"), HttpGet("/admin/report/site/gamecreditlog")]
    [NonUnify, DisplayName("report-gamecreditlog")]
    public Task<OldApiResult> Gamecreditlog() => Run(ct => _siteFund.GamecreditlogAsync(Form(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/report/site/getplatformbalance"), HttpGet("/admin/report/site/getplatformbalance")]
    [NonUnify, DisplayName("report-getplatformbalance")]
    public Task<OldApiResult> Getplatformbalance() => Run(ct => _siteFund.GetplatformbalanceAsync(Form(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/report/site/ordercount"), HttpGet("/admin/report/site/ordercount")]
    [NonUnify, DisplayName("report-ordercount")]
    public Task<OldApiResult> Ordercount() => Run(ct => _siteFund.OrdercountAsync(Form(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/report/site/profitofindex"), HttpGet("/admin/report/site/profitofindex")]
    [HttpPost("/admin/chart/profitpayoff"), HttpGet("/admin/chart/profitpayoff")]
    [NonUnify, DisplayName("report-profitofindex")]
    public Task<OldApiResult> Profitofindex() => Run(ct => _siteFund.ProfitofindexAsync(Form(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/report/site/rebateactive"), HttpGet("/admin/report/site/rebateactive")]
    [NonUnify, DisplayName("report-rebateactive")]
    public Task<OldApiResult> Rebateactive() => Run(ct => _siteFund.RebateactiveAsync(Form(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/report/site/rechargeandwitdraw"), HttpGet("/admin/report/site/rechargeandwitdraw")]
    [HttpPost("/admin/chart/rechargeandwitdrawtotal"), HttpGet("/admin/chart/rechargeandwitdrawtotal")]
    [NonUnify, DisplayName("report-rechargeandwitdraw")]
    public Task<OldApiResult> Rechargeandwitdraw() => Run(ct => _siteFund.RechargeandwitdrawAsync(Form(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/report/site/rechargeusers"), HttpGet("/admin/report/site/rechargeusers")]
    [NonUnify, DisplayName("report-rechargeusers")]
    public Task<OldApiResult> Rechargeusers() => Run(ct => _siteFund.RechargeusersAsync(Form(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/report/site/rechargeusersdetails"), HttpGet("/admin/report/site/rechargeusersdetails")]
    [NonUnify, DisplayName("report-rechargeusersdetails")]
    public Task<OldApiResult> Rechargeusersdetails() => Run(ct => _siteFund.RechargeusersdetailsAsync(Form(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/report/site/systemdatareport"), HttpGet("/admin/report/site/systemdatareport")]
    [NonUnify, DisplayName("report-systemdatareport")]
    public Task<OldApiResult> Systemdatareport() => Run(ct => _siteFund.SystemdatareportAsync(Form(), ct));

    // ----- user -----

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/report/user/loginlog"), HttpGet("/admin/report/user/loginlog")]
    [HttpPost("/admin/report/admin/loginlog"), HttpGet("/admin/report/admin/loginlog")]
    [HttpPost("/admin/report/user/myloginlog"), HttpGet("/admin/report/user/myloginlog")]
    [HttpPost("/admin/report/user/userlog"), HttpGet("/admin/report/user/userlog")]
    [NonUnify, DisplayName("report-loginlog")]
    public Task<OldApiResult> Loginlog() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            var (page, size) = OldApiShowResult.ReadPaging(http);
            int? platform = null;
            var platRaw = OldApiFormReader.ReadString(http, "Platform", "platform");
            if (int.TryParse(platRaw, out var plat) && plat >= 0) platform = plat;
            var outp = await _loginLog.PageAsync(new LoginLogAdminPageInput
            {
                Page = page,
                PageSize = size,
                User = OldApiFormReader.ReadString(http, "User", "user"),
                StartAt = OldApiFormReader.ReadString(http, "StartAt", "StartDate"),
                EndAt = OldApiFormReader.ReadString(http, "EndAt", "EndDate"),
                Platform = platform,
                IPAddress = OldApiFormReader.ReadString(http, "IPAddress", "ipAddress", "IP"),
                IsLast = OldApiFormReader.ReadString(http, "IsLast", "isLast")
            }, ct);
            return OldApiShowResult.OfPage(outp.Data, outp.Total, outp.Page, outp.PageSize);
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/report/user/pointlog"), HttpGet("/admin/report/user/pointlog")]
    [HttpPost("/admin/report/user/addpointlog"), HttpGet("/admin/report/user/addpointlog")]
    [HttpPost("/admin/report/user/subpointlog"), HttpGet("/admin/report/user/subpointlog")]
    [NonUnify, DisplayName("report-pointlog")]
    public Task<OldApiResult> Pointlog() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            var (page, size) = OldApiShowResult.ReadPaging(http);
            var outp = await _points.PageLogsAsync(new PointLogPageInput
            {
                Page = page,
                PageSize = size,
                UserID = OldApiFormReader.ReadInt(http, "UserID", "ID", "Id")
            }, ct);
            return OldApiShowResult.OfPage(outp.Items ?? [], outp.Total, page, size);
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/report/user/balance"), HttpGet("/admin/report/user/balance")]
    [HttpPost("/admin/report/money/locked"), HttpGet("/admin/report/money/locked")]
    [NonUnify, DisplayName("report-balance")]
    public Task<OldApiResult> Balance() =>
        Run(async ct =>
        {
            var rows = await _balance.ListWalletAsync(ct);
            return OldApiShowResult.OfList(rows);
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/report/user/balanceinfo"), HttpGet("/admin/report/user/balanceinfo")]
    [NonUnify, DisplayName("report-balanceinfo")]
    public Task<OldApiResult> Balanceinfo() =>
        Run(ct => _balance.BalanceinfoAsync(Form(), ct));

    // ----- game / agent -----

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/report/game/date"), HttpGet("/admin/report/game/date")]
    [HttpPost("/admin/report/game/report"), HttpGet("/admin/report/game/report")]
    [HttpPost("/admin/report/game/gamereport"), HttpGet("/admin/report/game/gamereport")]
    [HttpPost("/admin/report/game/gameplatreport"), HttpGet("/admin/report/game/gameplatreport")]
    [HttpPost("/admin/report/game/userreport"), HttpGet("/admin/report/game/userreport")]
    [HttpPost("/admin/report/game/userreportall"), HttpGet("/admin/report/game/userreportall")]
    [HttpPost("/admin/report/agent/gamerate"), HttpGet("/admin/report/agent/gamerate")]
    [HttpPost("/admin/report/agent/gameratebyagentname"), HttpGet("/admin/report/agent/gameratebyagentname")]
    [NonUnify, DisplayName("report-game-date")]
    public Task<OldApiResult> Gamedate() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            DateTime? start = null, end = null;
            if (DateTime.TryParse(OldApiFormReader.ReadString(http, "StartAt", "StartDate"), out var s)) start = s;
            if (DateTime.TryParse(OldApiFormReader.ReadString(http, "EndAt", "EndDate"), out var e)) end = e;
            var curRaw = OldApiFormReader.ReadString(http, "Currency");
            var currency = string.IsNullOrWhiteSpace(curRaw) ? -1 : OldApiFormReader.ReadInt(http, "Currency");
            var rows = await _gameDate.ListAsync(new GameDateReportListInput
            {
                StartAt = start,
                EndAt = end,
                Currency = currency,
                Type = OldApiFormReader.ReadInt(http, "Type", "Game"),
                Category = OldApiFormReader.ReadInt(http, "Category"),
                UserID = OldApiFormReader.ReadInt(http, "UserID", "UserId")
            }, ct);
            return OldApiShowResult.OfList(rows);
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/report/agent/statistics"), HttpGet("/admin/report/agent/statistics")]
    [HttpPost("/admin/report/agent/profit"), HttpGet("/admin/report/agent/profit")]
    [NonUnify, DisplayName("report-agent-statistics")]
    public Task<OldApiResult> Agentstatistics() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            DateTime? start = null, end = null;
            if (DateTime.TryParse(OldApiFormReader.ReadString(http, "StartAt", "StartDate"), out var s)) start = s;
            if (DateTime.TryParse(OldApiFormReader.ReadString(http, "EndAt", "EndDate"), out var e)) end = e;
            var curRaw = OldApiFormReader.ReadString(http, "Currency");
            var currency = string.IsNullOrWhiteSpace(curRaw) ? -1 : OldApiFormReader.ReadInt(http, "Currency");
            var rows = await _agentStat.ListAsync(new AgentStatReportListInput
            {
                StartAt = start,
                EndAt = end,
                Currency = currency,
                UserID = OldApiFormReader.ReadInt(http, "UserID", "UserId"),
                UserName = OldApiFormReader.ReadString(http, "UserName", "User", "Agent")
            }, ct);
            return OldApiShowResult.OfList(rows);
        });

    // ----- bill / tip / reconcile / chart -----

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/report/bill/list"), HttpGet("/admin/report/bill/list")]
    [HttpPost("/admin/report/bill/listold"), HttpGet("/admin/report/bill/listold")]
    [HttpPost("/admin/report/bill/billdate"), HttpGet("/admin/report/bill/billdate")]
    [NonUnify, DisplayName("report-bill-list")]
    public Task<OldApiResult> Billlist() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            var outp = await _bill.ListAsync(new SettlementBillListInput
            {
                Month = OldApiFormReader.ReadString(http, "Month", "month", "StartAt", "StartDate"),
                Game = OldApiFormReader.ReadString(http, "Game", "game"),
                Category = OldApiFormReader.ReadString(http, "Category", "category"),
                Currency = OldApiFormReader.ReadString(http, "Currency", "currency")
            }, ct);
            return OldApiShowResult.OfList(outp.Data ?? [], data: outp.Summary);
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/report/chart/fields"), HttpGet("/admin/report/chart/fields")]
    [HttpPost("/admin/report/chart/sitelist"), HttpGet("/admin/report/chart/sitelist")]
    [NonUnify, DisplayName("report-chart-fields")]
    public Task<OldApiResult> Chartfields() => Run(ct => _gameDate.FieldsAsync(Form(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/report/chart/info"), HttpGet("/admin/report/chart/info")]
    [NonUnify, DisplayName("report-chart-info")]
    public Task<OldApiResult> Chartinfo() => Run(ct => _gameDate.InfoAsync(Form(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/report/chart/siteview"), HttpGet("/admin/report/chart/siteview")]
    [NonUnify, DisplayName("report-chart-siteview")]
    public Task<OldApiResult> Chartsiteview() => Run(ct => _gameDate.SiteviewAsync(Form(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/report/chart/agentstatistics"), HttpGet("/admin/report/chart/agentstatistics")]
    [NonUnify, DisplayName("report-chart-agentstatistics")]
    public Task<OldApiResult> Chartagentstatistics() => Run(ct => _gameDate.AgentstatisticsAsync(Form(), ct));

    /// <summary>存款一二三。对齐旧 /admin/report/user/deposit（首充/二充/三充）。</summary>
    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/report/user/deposit"), HttpGet("/admin/report/user/deposit")]
    [NonUnify, DisplayName("report-user-deposit")]
    public Task<OldApiResult> Userdeposit() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            var rows = await _deposit123.ListAsync(new Deposit123ListInput
            {
                StartAt = ReadDate(http, "StartAt"),
                EndAt = ReadDate(http, "EndAt"),
                User = OldApiFormReader.ReadString(http, "User", "UserName"),
                Currency = string.IsNullOrWhiteSpace(OldApiFormReader.ReadString(http, "Currency"))
                    ? -1
                    : OldApiFormReader.ReadInt(http, "Currency"),
                RechargeCount = OldApiFormReader.ReadInt(http, "rechargecount", "RechargeCount"),
                BelongUser = OldApiFormReader.ReadString(http, "BelongUser", "belongUser")
            }, ct);
            return OldApiShowResult.OfList(rows);
        });

    /// <summary>会员帐变。对齐旧版 /admin/report/money/log。</summary>
    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/report/money/log"), HttpGet("/admin/report/money/log")]
    [NonUnify, DisplayName("report-money-log")]
    public Task<OldApiResult> Moneylog() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            var (page, size) = OldApiShowResult.ReadPaging(http);
            var output = await _moneyLog.PageAsync(new MoneyLogAdminPageInput
            {
                Page = page,
                PageSize = size,
                User = OldApiFormReader.ReadString(http, "User", "UserName"),
                UserID = OldApiFormReader.ReadInt(http, "UserID", "userID"),
                StartAt = ReadDate(http, "StartAt"),
                EndAt = ReadDate(http, "EndAt"),
                Content = OldApiFormReader.ReadString(http, "Content"),
                Type = OldApiFormReader.ReadString(http, "Type"),
                Currency = OldApiFormReader.ReadString(http, "Currency"),
                SourceID = OldApiFormReader.ReadString(http, "SourceID", "SourceId"),
                BelongUser = OldApiFormReader.ReadString(http, "BelongUser")
            }, ct);
            var rows = (output.Data ?? []).Select(t => new
            {
                t.ID,
                t.UserID,
                t.UserName,
                t.UpUserName,
                t.Money,
                t.LockMoney,
                t.Balance,
                t.CreateAt,
                t.IP,
                Type = t.TypeValue ?? t.Type.ToString(),
                TypeValue = t.TypeValue,
                TypeID = t.Type,
                t.SourceID,
                t.Description,
                Currency = t.CurrencyCode ?? OldApiMoneyDisplay.FormatCurrency(t.Currency)
            });
            return OldApiShowResult.OfPage(rows, output.Total, output.Page, output.PageSize, output.Summary);
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/report/user/signinlog"), HttpGet("/admin/report/user/signinlog")]
    [HttpPost("/admin/report/user/signinlogreport"), HttpGet("/admin/report/user/signinlogreport")]
    [NonUnify, DisplayName("report-user-signinlog")]
    public Task<OldApiResult> Usersigninlog() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            var (page, size) = OldApiShowResult.ReadPaging(http);
            var outp = await _sign.PageAsync(new UserSignPageInput
            {
                Page = page,
                PageSize = size,
                UserID = OldApiFormReader.ReadInt(http, "UserID", "userID"),
                UserName = OldApiFormReader.ReadString(http, "User", "UserName", "user"),
                StartAt = ReadDate(http, "StartAt", "startAt"),
                EndAt = ReadDate(http, "EndAt", "endAt")
            }, ct);
            return OldApiShowResult.OfPage(outp.Items ?? [], outp.Total, page, size);
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/report/user/addsignlog")]
    [NonUnify, DisplayName("report-user-addsignlog")]
    public Task<OldApiResult> Useraddsignlog() =>
        Run(async ct =>
        {
            var id = await _sign.AddManualAsync(OldApiFormReader.ReadInt(App.HttpContext, "UserID", "userID"), ct);
            return (object?)id;
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/report/user/profit"), HttpGet("/admin/report/user/profit")]
    [NonUnify, DisplayName("report-user-profit")]
    public Task<OldApiResult> Userprofit() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            var rows = await _siteFund.ListProfitAsync(new SiteFundDailyListInput
            {
                StartAt = ReadDate(http, "StartAt"),
                EndAt = ReadDate(http, "EndAt")
            }, ct);
            return OldApiShowResult.OfList(rows);
        });

    private static DateTime? ReadDate(Microsoft.AspNetCore.Http.HttpContext http, params string[] keys)
    {
        var s = OldApiFormReader.ReadString(http, keys);
        if (string.IsNullOrWhiteSpace(s)) return null;
        return DateTime.TryParse(s, out var dt) ? dt : null;
    }

    private static async Task<OldApiResult> Run<T>(Func<CancellationToken, Task<T>> action) => await OldApiFormReader.RunAsync(action);
}
