using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;
using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.WebApi;

/// <summary>
/// 会员活动旧路径：/request/events/{redenvelope|survery|order}/*（只挂 Shared 已接通读接口）。
/// </summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "events", Order = 36)]
public class EventsAppService : IDynamicApiController, ITransient
{
    /// <summary>旧 EventType.NewDailyTasks。</summary>
    public const int EventTypeNewDailyTasks = 2;

    /// <summary>旧 EventType.NewWeekTasks。</summary>
    public const int EventTypeNewWeekTasks = 3;

    /// <summary>旧 EventType.Rescue。</summary>
    public const int EventTypeRescue = 5;

    /// <summary>旧 EventType.Competition。</summary>
    public const int EventTypeCompetition = 7;

    /// <summary>旧 EventType.LongCheckIn。</summary>
    public const int EventTypeLongCheckIn = 13;

    /// <summary>旧 EventType.DailyTasks。</summary>
    public const int EventTypeDailyTasks = 14;

    /// <summary>旧 EventType.WeekTasks。</summary>
    public const int EventTypeWeekTasks = 15;

    /// <summary>旧 EventType.Washing。</summary>
    public const int EventTypeWashing = 17;

    /// <summary>旧 EventType.FirstRecharge。</summary>
    public const int EventTypeFirstRecharge = 19;

    /// <summary>旧 EventType.BindData。</summary>
    public const int EventTypeBindData = 21;

    /// <summary>旧 EventType.RedEnvelope。</summary>
    public const int EventTypeRedEnvelope = 22;

    /// <summary>旧 EventType.InviteTask。</summary>
    public const int EventTypeInviteTask = 24;

    private readonly SiteEventService _events;
    private readonly SurveyAdminService _survey;
    private readonly EventLogService _logs;
    private readonly MemberAuthService _auth;
    private readonly ICurrentSiteAccessor _site;
    private readonly IBizDbAccessor _biz;

    public EventsAppService(
        SiteEventService events,
        SurveyAdminService survey,
        EventLogService logs,
        MemberAuthService auth,
        ICurrentSiteAccessor site,
        IBizDbAccessor biz)
    {
        _events = events;
        _survey = survey;
        _logs = logs;
        _auth = auth;
        _site = site;
        _biz = biz;
    }

    [OldApiGuest, AllowAnonymous]
    [HttpPost("/request/events/redenvelope/info"), HttpGet("/request/events/redenvelope/info")]
    [NonUnify, DisplayName("redenvelope-info")]
    public Task<OldApiResult> Redenvelopeinfo() =>
        Run(ct => ResolveEventInfoAsync(EventTypeRedEnvelope, "红包", ct));

    [OldApiGuest, AllowAnonymous]
    [HttpPost("/request/events/firstrecharge/info"), HttpGet("/request/events/firstrecharge/info")]
    [NonUnify, DisplayName("firstrecharge-info")]
    public Task<OldApiResult> Firstrechargeinfo() =>
        Run(ct => ResolveEventInfoAsync(EventTypeFirstRecharge, "首充", ct));

    [OldApiGuest, AllowAnonymous]
    [HttpPost("/request/events/dailytasks/info"), HttpGet("/request/events/dailytasks/info")]
    [NonUnify, DisplayName("dailytasks-info")]
    public Task<OldApiResult> Dailytasksinfo() =>
        Run(ct => ResolveEventInfoAsync(EventTypeDailyTasks, "每日", ct));

    [OldApiGuest, AllowAnonymous]
    [HttpPost("/request/events/longcheckin/info"), HttpGet("/request/events/longcheckin/info")]
    [NonUnify, DisplayName("longcheckin-info")]
    public Task<OldApiResult> Longcheckininfo() =>
        Run(ct => ResolveEventInfoAsync(EventTypeLongCheckIn, "签到", ct));

    [OldApiGuest, AllowAnonymous]
    [HttpPost("/request/events/rescue/info"), HttpGet("/request/events/rescue/info")]
    [NonUnify, DisplayName("rescue-info")]
    public Task<OldApiResult> Rescueinfo() =>
        Run(ct => ResolveEventInfoAsync(EventTypeRescue, "救援", ct));

    [OldApiGuest, AllowAnonymous]
    [HttpPost("/request/events/washing/info"), HttpGet("/request/events/washing/info")]
    [NonUnify, DisplayName("washing-info")]
    public Task<OldApiResult> Washinginfo() =>
        Run(ct => ResolveEventInfoAsync(EventTypeWashing, "洗码", ct));

    [OldApiGuest, AllowAnonymous]
    [HttpPost("/request/events/weektasks/info"), HttpGet("/request/events/weektasks/info")]
    [NonUnify, DisplayName("weektasks-info")]
    public Task<OldApiResult> Weektasksinfo() =>
        Run(ct => ResolveEventInfoAsync(EventTypeWeekTasks, "每周", ct));

    [OldApiGuest, AllowAnonymous]
    [HttpPost("/request/events/newdailytasks/info"), HttpGet("/request/events/newdailytasks/info")]
    [NonUnify, DisplayName("newdailytasks-info")]
    public Task<OldApiResult> Newdailytasksinfo() =>
        Run(ct => ResolveEventInfoAsync(EventTypeNewDailyTasks, "每日", ct));

    [OldApiGuest, AllowAnonymous]
    [HttpPost("/request/events/newweektasks/info"), HttpGet("/request/events/newweektasks/info")]
    [NonUnify, DisplayName("newweektasks-info")]
    public Task<OldApiResult> Newweektasksinfo() =>
        Run(ct => ResolveEventInfoAsync(EventTypeNewWeekTasks, "每周", ct));

    [OldApiGuest, AllowAnonymous]
    [HttpPost("/request/events/competition/info"), HttpGet("/request/events/competition/info")]
    [NonUnify, DisplayName("competition-info")]
    public Task<OldApiResult> Competitioninfo() =>
        Run(ct => ResolveEventInfoAsync(EventTypeCompetition, "擂台", ct));

    [OldApiGuest, AllowAnonymous]
    [HttpPost("/request/events/invitetask/info"), HttpGet("/request/events/invitetask/info")]
    [NonUnify, DisplayName("invitetask-info")]
    public Task<OldApiResult> Invitetaskinfo() =>
        Run(ct => ResolveEventInfoAsync(EventTypeInviteTask, "邀请", ct));

    [OldApiGuest, AllowAnonymous]
    [HttpPost("/request/events/binddata/info"), HttpGet("/request/events/binddata/info")]
    [NonUnify, DisplayName("binddata-info")]
    public Task<OldApiResult> Binddatainfo() =>
        Run(ct => ResolveEventInfoAsync(EventTypeBindData, "绑定", ct));

    [OldApiGuest, AllowAnonymous]
    [HttpPost("/request/events/survery/list"), HttpGet("/request/events/survery/list")]
    [NonUnify, DisplayName("survery-list")]
    public Task<OldApiResult> Surverylist() =>
        Run(ct => _survey.ListAsync(ct));

    [OldApiGuest, AllowAnonymous]
    [HttpPost("/request/events/survery/surveryinfo"), HttpGet("/request/events/survery/surveryinfo")]
    [NonUnify, DisplayName("survery-info")]
    public Task<OldApiResult> Surveryinfo() =>
        Run(async ct =>
        {
            var id = OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id");
            var list = await _survey.ListAsync(ct);
            var row = id > 0 ? list.FirstOrDefault(x => x.ID == id) : list.FirstOrDefault();
            return (object?)row ?? throw Oops.Oh("#问卷不存在#");
        });

    [AllowAnonymous]
    [HttpPost("/request/events/survery/saveanswer")]
    [NonUnify, DisplayName("survery-save")]
    public Task<OldApiResult> Surverysave() =>
        Run<object>(_ => throw Oops.Oh("#功能尚未接通#"));

    [OldApiGuest, AllowAnonymous]
    [HttpPost("/request/events/competition/test"), HttpGet("/request/events/competition/test")]
    [HttpPost("/request/events/competition/bavctest"), HttpGet("/request/events/competition/bavctest")]
    [NonUnify, DisplayName("competition-test")]
    public Task<OldApiResult> CompetitionTest() =>
        Run(_ => Task.FromResult<object>(true));

    [OldApiGuest, AllowAnonymous]
    [HttpPost("/request/events/order/types"), HttpGet("/request/events/order/types")]
    [NonUnify, DisplayName("events-order-types")]
    public Task<OldApiResult> Ordertypes() =>
        Run(async ct =>
        {
            var list = await _events.ListAsync(new SiteEventListInput(), ct);
            var dict = list.GroupBy(e => e.Type)
                .ToDictionary(g => g.Key.ToString(), g => g.First().Title ?? g.Key.ToString());
            return (object)dict;
        });

    [OldApiGuest, AllowAnonymous]
    [HttpPost("/request/events/order/status"), HttpGet("/request/events/order/status")]
    [NonUnify, DisplayName("events-order-status")]
    public Task<OldApiResult> Orderstatus() =>
        Run(ct =>
        {
            _ = ct;
            return Task.FromResult<object>(new Dictionary<string, string>
            {
                ["0"] = "#未处理#",
                ["1"] = "#失败#",
                ["2"] = "#成功#"
            });
        });

    [AllowAnonymous]
    [HttpPost("/request/events/order/list"), HttpGet("/request/events/order/list")]
    [NonUnify, DisplayName("events-order-list")]
    public Task<OldApiResult> Orderlist() =>
        Run(async ct =>
        {
            var siteId = _site.RequireSiteId();
            var userId = await _auth.TryResolveUserIdFromCookieAsync(siteId, ct);
            if (userId is null or <= 0)
                throw Oops.Oh("#请先登录#");
            var users = await _biz.Db.Queryable<User>()
                .Where(u => u.SiteID == siteId && u.ID == userId.Value)
                .Select(u => u.UserName)
                .Take(1)
                .ToListAsync(ct);
            var userName = users.FirstOrDefault();
            if (string.IsNullOrEmpty(userName))
                throw Oops.Oh("#请先登录#");
            return await _logs.ListAsync(new EventLogListInput
            {
                User = userName,
                Type = OldApiFormReader.ReadInt(App.HttpContext, "Type", "type") is var t && t != 0 ? t : null,
                Status = OldApiFormReader.ReadString(App.HttpContext, "Status", "status")
            }, ct);
        });

    // --- open / record / getpoint：活动领取深链路未迁完时挂空壳，避免路径 404 ---
    [OldApiGuest, AllowAnonymous]
    [HttpPost("/request/events/binddata/open"), HttpGet("/request/events/binddata/open")]
    [HttpPost("/request/events/competition/open"), HttpGet("/request/events/competition/open")]
    [HttpPost("/request/events/dailytasks/open"), HttpGet("/request/events/dailytasks/open")]
    [HttpPost("/request/events/dailytasks/opensingle"), HttpGet("/request/events/dailytasks/opensingle")]
    [HttpPost("/request/events/firstrecharge/open"), HttpGet("/request/events/firstrecharge/open")]
    [HttpPost("/request/events/fullweek/open"), HttpGet("/request/events/fullweek/open")]
    [HttpPost("/request/events/invitetask/open"), HttpGet("/request/events/invitetask/open")]
    [HttpPost("/request/events/longcheckin/open"), HttpGet("/request/events/longcheckin/open")]
    [HttpPost("/request/events/newdailytasks/open"), HttpGet("/request/events/newdailytasks/open")]
    [HttpPost("/request/events/newweektasks/open"), HttpGet("/request/events/newweektasks/open")]
    [HttpPost("/request/events/redenvelope/open"), HttpGet("/request/events/redenvelope/open")]
    [HttpPost("/request/events/redenvelope/openred"), HttpGet("/request/events/redenvelope/openred")]
    [HttpPost("/request/events/rescue/open"), HttpGet("/request/events/rescue/open")]
    [HttpPost("/request/events/washing/open"), HttpGet("/request/events/washing/open")]
    [HttpPost("/request/events/weektasks/open"), HttpGet("/request/events/weektasks/open")]
    [NonUnify, DisplayName("events-open-stub")]
    public Task<OldApiResult> EventOpenStub() =>
        Run(_ => Task.FromResult<object>(new { Open = false, Message = "#功能尚未接通#" }));

    [AllowAnonymous]
    [HttpPost("/request/events/binddata/record"), HttpGet("/request/events/binddata/record")]
    [HttpPost("/request/events/competition/record"), HttpGet("/request/events/competition/record")]
    [HttpPost("/request/events/dailytasks/record"), HttpGet("/request/events/dailytasks/record")]
    [HttpPost("/request/events/firstrecharge/record"), HttpGet("/request/events/firstrecharge/record")]
    [HttpPost("/request/events/fullweek/record"), HttpGet("/request/events/fullweek/record")]
    [HttpPost("/request/events/invitetask/record"), HttpGet("/request/events/invitetask/record")]
    [HttpPost("/request/events/invitetask/inviterecord"), HttpGet("/request/events/invitetask/inviterecord")]
    [HttpPost("/request/events/longcheckin/record"), HttpGet("/request/events/longcheckin/record")]
    [HttpPost("/request/events/newdailytasks/record"), HttpGet("/request/events/newdailytasks/record")]
    [HttpPost("/request/events/newweektasks/record"), HttpGet("/request/events/newweektasks/record")]
    [HttpPost("/request/events/redenvelope/record"), HttpGet("/request/events/redenvelope/record")]
    [HttpPost("/request/events/rescue/record"), HttpGet("/request/events/rescue/record")]
    [HttpPost("/request/events/washing/record"), HttpGet("/request/events/washing/record")]
    [HttpPost("/request/events/weektasks/record"), HttpGet("/request/events/weektasks/record")]
    [NonUnify, DisplayName("events-record")]
    public Task<OldApiResult> EventRecord() => Orderlist();

    [AllowAnonymous]
    [HttpPost("/request/events/binddata/getpoint"), HttpGet("/request/events/binddata/getpoint")]
    [HttpPost("/request/events/competition/getpoint"), HttpGet("/request/events/competition/getpoint")]
    [HttpPost("/request/events/dailytasks/getpoint"), HttpGet("/request/events/dailytasks/getpoint")]
    [HttpPost("/request/events/firstrecharge/getpoint"), HttpGet("/request/events/firstrecharge/getpoint")]
    [HttpPost("/request/events/fullweek/getpoint"), HttpGet("/request/events/fullweek/getpoint")]
    [HttpPost("/request/events/invitetask/getpoint"), HttpGet("/request/events/invitetask/getpoint")]
    [HttpPost("/request/events/newdailytasks/getpoint"), HttpGet("/request/events/newdailytasks/getpoint")]
    [HttpPost("/request/events/newweektasks/getpoint"), HttpGet("/request/events/newweektasks/getpoint")]
    [HttpPost("/request/events/redenvelope/getpoint"), HttpGet("/request/events/redenvelope/getpoint")]
    [HttpPost("/request/events/redenvelope/getredpoint"), HttpGet("/request/events/redenvelope/getredpoint")]
    [HttpPost("/request/events/rescue/getpoint"), HttpGet("/request/events/rescue/getpoint")]
    [HttpPost("/request/events/washing/getpoint"), HttpGet("/request/events/washing/getpoint")]
    [HttpPost("/request/events/weektasks/getpoint"), HttpGet("/request/events/weektasks/getpoint")]
    [NonUnify, DisplayName("events-getpoint-stub")]
    public Task<OldApiResult> EventGetpointStub() =>
        Run<object>(async ct =>
        {
            await RequireLoginAsync(ct);
            throw Oops.Oh("#功能尚未接通#");
        });

    [OldApiGuest, AllowAnonymous]
    [HttpPost("/request/events/competition/allrank"), HttpGet("/request/events/competition/allrank")]
    [HttpPost("/request/events/competition/allrankhistory"), HttpGet("/request/events/competition/allrankhistory")]
    [HttpPost("/request/events/competition/myrank"), HttpGet("/request/events/competition/myrank")]
    [HttpPost("/request/events/competition/infodetal"), HttpGet("/request/events/competition/infodetal")]
    [HttpPost("/request/events/fullweek/info"), HttpGet("/request/events/fullweek/info")]
    [HttpPost("/request/events/invitetask/detail"), HttpGet("/request/events/invitetask/detail")]
    [HttpPost("/request/events/longcheckin/getday"), HttpGet("/request/events/longcheckin/getday")]
    [HttpPost("/request/events/delottery/info"), HttpGet("/request/events/delottery/info")]
    [HttpPost("/request/events/delottery/winner"), HttpGet("/request/events/delottery/winner")]
    [NonUnify, DisplayName("events-rank-stub")]
    public Task<OldApiResult> EventRankStub() =>
        Run(_ => Task.FromResult<object>(Array.Empty<object>()));

    private async Task<int> RequireLoginAsync(CancellationToken ct)
    {
        var siteId = _site.RequireSiteId();
        var userId = await _auth.TryResolveUserIdFromCookieAsync(siteId, ct);
        if (userId is null or <= 0)
            throw Oops.Oh("#请先登录#");
        return userId.Value;
    }

    private async Task<object> ResolveEventInfoAsync(int defaultType, string titleHint, CancellationToken ct)
    {
        var http = App.HttpContext;
        var id = OldApiFormReader.ReadInt(http, "ID", "Id");
        var typeRaw = OldApiFormReader.ReadString(http, "Type", "type");
        var type = int.TryParse(typeRaw, out var t) && t != 0 ? t : defaultType;
        if (id > 0)
            return await _events.InfoAsync(id, type, ct);

        var list = await _events.ListAsync(new SiteEventListInput { Type = type.ToString() }, ct);
        if (list.Count == 0)
            list = await _events.ListAsync(new SiteEventListInput(), ct);
        var first = list.FirstOrDefault(e => e.IsOpen && (e.Type == type || (e.Title ?? string.Empty).Contains(titleHint)))
                    ?? list.FirstOrDefault(e => e.IsOpen)
                    ?? list.FirstOrDefault();
        if (first == null)
            throw Oops.Oh("#活动已经结束#");
        return await _events.InfoAsync(first.ID, first.Type, ct);
    }

    private static async Task<OldApiResult> Run<T>(Func<CancellationToken, Task<T>> action) => await OldApiFormReader.RunAsync(action);
}
