using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 优惠/活动旧路径：/admin/award/{events|log|survery|delotterylog|redenvelopelog}/*（只挂 Shared 已接通方法）。
/// </summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "award", Order = 42)]
public class AwardAppService : IDynamicApiController, ITransient
{
    public const string PermissionId = UserMemberFormAppService.MemberListPermissionId;

    private readonly SiteEventService _events;
    private readonly SiteEventColumnService _columns;
    private readonly EventLogService _logs;
    private readonly IEventLogCheckService _check;
    private readonly SurveyAdminService _survey;
    private readonly PromoChanceLogService _chance;

    public AwardAppService(
        SiteEventService events,
        SiteEventColumnService columns,
        EventLogService logs,
        IEventLogCheckService check,
        SurveyAdminService survey,
        PromoChanceLogService chance)
    {
        _events = events;
        _columns = columns;
        _logs = logs;
        _check = check;
        _survey = survey;
        _chance = chance;
    }

    // ----- events -----

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/award/events/list"), HttpGet("/admin/award/events/list")]
    [HttpPost("/admin/award/events/list2"), HttpGet("/admin/award/events/list2")]
    [NonUnify, DisplayName("award-events-list")]
    public Task<OldApiResult> Eventslist() =>
        Run(ct =>
        {
            var http = App.HttpContext;
            return _events.ListAsync(new SiteEventListInput
            {
                Type = OldApiFormReader.ReadString(http, "Type", "type"),
                Status = OldApiFormReader.ReadString(http, "Status", "status"),
                Title = OldApiFormReader.ReadString(http, "Title", "title"),
                ColID = OldApiFormReader.ReadInt(http, "ColID", "colID", "colId"),
                IsTop = OldApiFormReader.ReadInt(http, "IsTop", "isTop"),
                Currency = OldApiFormReader.ReadString(http, "Currency", "currency")
            }, ct);
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/award/events/info"), HttpGet("/admin/award/events/info")]
    [NonUnify, DisplayName("award-events-info")]
    public Task<OldApiResult> Eventsinfo() =>
        Run(ct =>
        {
            var http = App.HttpContext;
            return _events.InfoAsync(
                OldApiFormReader.ReadInt(http, "ID", "Id"),
                OldApiFormReader.ReadInt(http, "Type", "type"),
                ct);
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/award/events/save")]
    [NonUnify, DisplayName("award-events-save")]
    public Task<OldApiResult> Eventssave() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            await _events.SaveAsync(new SiteEventSaveInput
            {
                ID = OldApiFormReader.ReadInt(http, "ID", "Id"),
                ColID = OldApiFormReader.ReadInt(http, "ColID", "colID"),
                Title = OldApiFormReader.ReadString(http, "Title", "title"),
                SubTitle = OldApiFormReader.ReadString(http, "SubTitle", "subTitle"),
                Tag = OldApiFormReader.ReadString(http, "Tag", "tag"),
                Cover = OldApiFormReader.ReadString(http, "Cover", "cover"),
                Description = OldApiFormReader.ReadString(http, "Description", "description"),
                Content = OldApiFormReader.ReadString(http, "Content", "content"),
                Type = OldApiFormReader.ReadInt(http, "Type", "type"),
                StartAt = ReadDate(http, "StartAt") ?? DateTime.Now.Date,
                ExpireAt = ReadDate(http, "ExpireAt") ?? DateTime.Now.AddDays(30).Date,
                IsVisible = OldApiFormReader.ReadBool(http, "IsVisible"),
                IsOpen = OldApiFormReader.ReadBool(http, "IsOpen"),
                IsLoginAfterOpen = OldApiFormReader.ReadBool(http, "IsLoginAfterOpen"),
                IsLoginBeforeOpen = OldApiFormReader.ReadBool(http, "IsLoginBeforeOpen"),
                IsQuickEntrance = OldApiFormReader.ReadBool(http, "IsQuickEntrance"),
                IsDetail = OldApiFormReader.ReadBool(http, "IsDetail"),
                IsNewerTask = OldApiFormReader.ReadBool(http, "IsNewerTask"),
                JumpType = (byte)OldApiFormReader.ReadInt(http, "JumpType"),
                Sort = (short)OldApiFormReader.ReadInt(http, "Sort", "sort"),
                Currency = OldApiFormReader.ReadInt(http, "Currency", "currency"),
                SupportCurrency = OldApiFormReader.ReadString(http, "SupportCurrency"),
                Setting = OldApiFormReader.ReadString(http, "Setting", "setting"),
                Demo = OldApiFormReader.ReadString(http, "Demo", "demo"),
                Skin = OldApiFormReader.ReadString(http, "Skin", "skin"),
                Views = OldApiFormReader.ReadString(http, "Views", "views"),
                Background = OldApiFormReader.ReadString(http, "Background"),
                FilterGame = OldApiFormReader.ReadString(http, "FilterGame"),
                FilterPlatform = OldApiFormReader.ReadString(http, "FilterPlatform")
            }, ct);
            return (object?)null;
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/award/events/delete")]
    [NonUnify, DisplayName("award-events-delete")]
    public Task<OldApiResult> Eventsdelete() =>
        Run(async ct =>
        {
            await _events.DeleteAsync(OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id"), ct);
            return (object?)null;
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/award/events/columnlist"), HttpGet("/admin/award/events/columnlist")]
    [NonUnify, DisplayName("award-events-columnlist")]
    public Task<OldApiResult> Columnlist() =>
        Run(ct => _columns.ColumnListAsync(ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/award/events/columninfo"), HttpGet("/admin/award/events/columninfo")]
    [NonUnify, DisplayName("award-events-columninfo")]
    public Task<OldApiResult> Columninfo() =>
        Run(async ct =>
        {
            var id = OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id");
            if (id <= 0)
                return (object?)new { ID = 0, SiteID = 0, Name = "{}", Sort = (short)0, EventCount = 0 };
            var list = await _columns.ColumnListAsync(ct);
            var row = list.FirstOrDefault(c => c.ID == id);
            if (row == null)
                return (object?)new { ID = 0, SiteID = 0, Name = "{}", Sort = (short)0, EventCount = 0 };
            return (object?)new
            {
                row.ID,
                SiteID = 0,
                Name = row.Name,
                row.Sort,
                row.EventCount
            };
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/award/events/columnsave")]
    [NonUnify, DisplayName("award-events-columnsave")]
    public Task<OldApiResult> Columnsave() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            var name = OldApiFormReader.ReadString(http, "Translate", "Name", "name");
            await _columns.SaveAsync(new SiteEventColumnSaveInput
            {
                ID = OldApiFormReader.ReadInt(http, "ID", "Id"),
                Name = name,
                Sort = (short)OldApiFormReader.ReadInt(http, "Sort", "sort")
            }, ct);
            return (object?)null;
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/award/events/columndelete")]
    [NonUnify, DisplayName("award-events-columndelete")]
    public Task<OldApiResult> Columndelete() =>
        Run(async ct =>
        {
            await _columns.DeleteAsync(OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id"), ct);
            return (object?)null;
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/award/events/updatesort"), HttpPost("/admin/award/events/savesort")]
    [NonUnify, DisplayName("award-events-updatesort")]
    public Task<OldApiResult> Eventsupdatesort() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            var raw = OldApiFormReader.ReadString(http, "ID", "Sort", "id");
            var ids = (raw ?? string.Empty)
                .Split([',', ';', '|', ' '], StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.TryParse(s.Trim(), out var n) ? n : 0)
                .Where(n => n > 0)
                .ToArray();
            await _events.UpdatesortAsync(new Dictionary<string, object?> { ["ID"] = string.Join(",", ids) }, ct);
            return (object?)null;
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/award/events/updatedemo")]
    [NonUnify, DisplayName("award-events-updatedemo")]
    public Task<OldApiResult> Eventsupdatedemo() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            await _events.UpdatedemoAsync(new Dictionary<string, object?>
            {
                ["ID"] = OldApiFormReader.ReadInt(http, "ID", "Id"),
                ["Demo"] = OldApiFormReader.ReadString(http, "Demo", "demo")
            }, ct);
            return (object?)null;
        });

    // ----- log -----

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/award/log/list"), HttpGet("/admin/award/log/list")]
    [NonUnify, DisplayName("award-log-list")]
    public Task<OldApiResult> Loglist() =>
        Run(ct =>
        {
            var http = App.HttpContext;
            var typeRaw = OldApiFormReader.ReadInt(http, "Type", "type");
            return _logs.ListAsync(new EventLogListInput
            {
                User = OldApiFormReader.ReadString(http, "User", "user", "UserName"),
                Status = OldApiFormReader.ReadString(http, "Status", "status"),
                Type = typeRaw != 0 ? typeRaw : null,
                SourceID = OldApiFormReader.ReadString(http, "SourceID", "sourceID"),
                StartAt = ReadDate(http, "StartAt"),
                EndAt = ReadDate(http, "EndAt")
            }, ct);
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/award/log/info"), HttpGet("/admin/award/log/info")]
    [NonUnify, DisplayName("award-log-info")]
    public Task<OldApiResult> Loginfo() =>
        Run(ct =>
        {
            var map = new Dictionary<string, object?>
            {
                ["ID"] = OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id", "LogID")
            };
            return _logs.InfoAsync(map, ct);
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/award/log/agree")]
    [NonUnify, DisplayName("award-log-agree")]
    public Task<OldApiResult> Logagree() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            await _check.CheckAsync(
                OldApiFormReader.ReadInt(http, "ID", "Id", "LogID"),
                EventLogCheckService.StatusSuccess,
                OldApiFormReader.ReadString(http, "Remark", "CheckMessage", "checkMessage"),
                cancellationToken: ct);
            return (object?)null;
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/award/log/reject")]
    [NonUnify, DisplayName("award-log-reject")]
    public Task<OldApiResult> Logreject() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            await _check.CheckAsync(
                OldApiFormReader.ReadInt(http, "ID", "Id", "LogID"),
                EventLogCheckService.StatusFailed,
                OldApiFormReader.ReadString(http, "Remark", "CheckMessage", "checkMessage"),
                cancellationToken: ct);
            return (object?)null;
        });

    // ----- survey (旧拼写 survery) -----

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/award/survery/list"), HttpGet("/admin/award/survery/list")]
    [NonUnify, DisplayName("award-survery-list")]
    public Task<OldApiResult> Surverylist() =>
        Run(ct => _survey.ListAsync(ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/award/survery/savesurvery")]
    [NonUnify, DisplayName("award-survery-savesurvery")]
    public Task<OldApiResult> Surverysave() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            _ = decimal.TryParse(OldApiFormReader.ReadString(http, "Money", "money"), out var money);
            var id = await _survey.SaveAsync(new SurveySaveInput
            {
                ID = OldApiFormReader.ReadInt(http, "ID", "Id"),
                Title = OldApiFormReader.ReadString(http, "Title", "title"),
                Description = OldApiFormReader.ReadString(http, "Description", "description"),
                Questions = OldApiFormReader.ReadString(http, "Questions", "questions"),
                Answer = OldApiFormReader.ReadString(http, "Answer", "answer"),
                IsOpen = OldApiFormReader.ReadBool(http, "IsOpen"),
                AutoPay = OldApiFormReader.ReadBool(http, "AutoPay"),
                Money = money,
                WashFlow = OldApiFormReader.ReadInt(http, "WashFlow")
            }, ct);
            return (object?)id;
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/award/survery/log"), HttpGet("/admin/award/survery/log")]
    [NonUnify, DisplayName("award-survery-log")]
    public Task<OldApiResult> Surverylog() =>
        Run(ct =>
        {
            var http = App.HttpContext;
            var (page, size) = OldApiShowResult.ReadPaging(http);
            return _survey.PageLogsAsync(new SurveyLogPageInput
            {
                Page = page,
                PageSize = size,
                SurveryID = OldApiFormReader.ReadInt(http, "SurveryID", "SurveyID", "ID"),
                UserID = OldApiFormReader.ReadInt(http, "UserID", "userID")
            }, ct);
        });

    // ----- chance logs -----

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/award/delotterylog/pointlog"), HttpGet("/admin/award/delotterylog/pointlog")]
    [NonUnify, DisplayName("award-delotterylog-pointlog")]
    public Task<OldApiResult> Delotterypointlog() =>
        Run(ct =>
        {
            var http = App.HttpContext;
            var (page, size) = OldApiShowResult.ReadPaging(http);
            return _chance.PageDelotteryAsync(new ChanceLogPageInput
            {
                Page = page,
                PageSize = size,
                UserID = OldApiFormReader.ReadInt(http, "UserID", "userID"),
                Type = OldApiFormReader.ReadInt(http, "Type", "type")
            }, ct);
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/award/redenvelopelog/pointlog"), HttpGet("/admin/award/redenvelopelog/pointlog")]
    [NonUnify, DisplayName("award-redenvelopelog-pointlog")]
    public Task<OldApiResult> Redenvelopepointlog() =>
        Run(ct =>
        {
            var http = App.HttpContext;
            var (page, size) = OldApiShowResult.ReadPaging(http);
            return _chance.PageRedEnvelopeAsync(new ChanceLogPageInput
            {
                Page = page,
                PageSize = size,
                UserID = OldApiFormReader.ReadInt(http, "UserID", "userID"),
                Type = OldApiFormReader.ReadInt(http, "Type", "type")
            }, ct);
        });

    // ----- Shared stub shells（路径先挂） -----

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/award/events/filtergame"), HttpGet("/admin/award/events/filtergame")]
    [NonUnify, DisplayName("award-events-filtergame")]
    public Task<OldApiResult> Filtergame() => Run(ct => _events.FiltergameAsync(ToDict(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/award/events/initplatform")]
    [NonUnify, DisplayName("award-events-initplatform")]
    public Task<OldApiResult> Initplatform() => Run(ct => _events.InitplatformAsync(ToDict(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/award/events/inittransfer")]
    [NonUnify, DisplayName("award-events-inittransfer")]
    public Task<OldApiResult> Inittransfer() => Run(ct => _events.InittransferAsync(ToDict(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/award/events/show0"), HttpGet("/admin/award/events/show0")]
    [NonUnify, DisplayName("award-events-show0")]
    public Task<OldApiResult> Show0() => Run(ct => _events.Show0Async(ToDict(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/award/events/show1"), HttpGet("/admin/award/events/show1")]
    [NonUnify, DisplayName("award-events-show1")]
    public Task<OldApiResult> Show1() => Run(ct => _events.Show1Async(ToDict(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/award/events/show2"), HttpGet("/admin/award/events/show2")]
    [NonUnify, DisplayName("award-events-show2")]
    public Task<OldApiResult> Show2() => Run(ct => _events.Show2Async(ToDict(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/award/events/showtransfer0"), HttpGet("/admin/award/events/showtransfer0")]
    [NonUnify, DisplayName("award-events-showtransfer0")]
    public Task<OldApiResult> Showtransfer0() => Run(ct => _events.Showtransfer0Async(ToDict(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/award/events/savecolumnsort")]
    [NonUnify, DisplayName("award-events-savecolumnsort")]
    public Task<OldApiResult> Savecolumnsort() => Run(ct => _events.UpdatesortAsync(ToDict(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/award/delottery/getproductcount"), HttpGet("/admin/award/delottery/getproductcount")]
    [HttpPost("/admin/award/redenvelope/getproductcount"), HttpGet("/admin/award/redenvelope/getproductcount")]
    [NonUnify, DisplayName("award-getproductcount")]
    public Task<OldApiResult> Getproductcount() => Run(ct => _events.GetproductcountAsync(ToDict(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/award/delottery/updateproductcount")]
    [HttpPost("/admin/award/redenvelope/updateproductcount")]
    [NonUnify, DisplayName("award-updateproductcount")]
    public Task<OldApiResult> Updateproductcount() => Run(ct => _events.UpdateproductcountAsync(ToDict(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/award/log/manualsigninrewart")]
    [NonUnify, DisplayName("award-log-manualsigninrewart")]
    public Task<OldApiResult> Manualsigninrewart() => Run(ct => _logs.ManualsigninrewartAsync(ToDict(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/award/log/save")]
    [NonUnify, DisplayName("award-log-save")]
    public Task<OldApiResult> Logsave() => Run(ct => _logs.SaveAsync(ToDict(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/award/log/savebet")]
    [NonUnify, DisplayName("award-log-savebet")]
    public Task<OldApiResult> Logsavebet() => Run(ct => _logs.SavebetAsync(ToDict(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/award/log/savelotterycount")]
    [NonUnify, DisplayName("award-log-savelotterycount")]
    public Task<OldApiResult> Logsavelotterycount() => Run(ct => _logs.SavelotterycountAsync(ToDict(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/award/log/saveredenvelopecount")]
    [NonUnify, DisplayName("award-log-saveredenvelopecount")]
    public Task<OldApiResult> Logsaveredenvelopecount() => Run(ct => _logs.SaveredenvelopecountAsync(ToDict(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/award/survery/answerinfo"), HttpGet("/admin/award/survery/answerinfo")]
    [NonUnify, DisplayName("award-survery-answerinfo")]
    public Task<OldApiResult> Answerinfo() => Run(ct => _survey.AnswerinfoAsync(ToDict(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/award/survery/pay")]
    [NonUnify, DisplayName("award-survery-pay")]
    public Task<OldApiResult> Surverypay() => Run(ct => _survey.PayAsync(ToDict(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/award/survery/surveryinfo"), HttpGet("/admin/award/survery/surveryinfo")]
    [NonUnify, DisplayName("award-survery-surveryinfo")]
    public Task<OldApiResult> Surveryinfo() => Run(ct => _survey.SurveryinfoAsync(ToDict(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/award/survery/saveanswer")]
    [NonUnify, DisplayName("award-survery-saveanswer")]
    public Task<OldApiResult> Saveanswer() =>
        Task.FromResult(OldApiResult.Fail("#功能尚未接通#"));

    private static Dictionary<string, object?> ToDict()
    {
        var map = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (k, v) in OldApiFormReader.ReadJsonMap(App.HttpContext))
            map[k] = v.ToString();
        return map;
    }

    private static DateTime? ReadDate(Microsoft.AspNetCore.Http.HttpContext http, params string[] keys)
    {
        var s = OldApiFormReader.ReadString(http, keys);
        if (string.IsNullOrWhiteSpace(s)) return null;
        return DateTime.TryParse(s, out var dt) ? dt : null;
    }

    private static async Task<OldApiResult> Run<T>(Func<CancellationToken, Task<T>> action) => await OldApiFormReader.RunAsync(action);
}
