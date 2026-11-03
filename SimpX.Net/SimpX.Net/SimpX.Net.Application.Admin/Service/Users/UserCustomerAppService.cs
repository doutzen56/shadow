using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 客户关系旧路径：/admin/user/customer/{message|mail|feedback}/*。
/// </summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "user-customer", Order = 49)]
public class UserCustomerAppService : IDynamicApiController, ITransient
{
    public const string PermissionId = UserMemberFormAppService.MemberListPermissionId;

    private readonly UserMessageService _messages;
    private readonly SiteFeedbackService _feedback;
    private readonly SmsLogService _sms;

    public UserCustomerAppService(
        UserMessageService messages,
        SiteFeedbackService feedback,
        SmsLogService sms)
    {
        _messages = messages;
        _feedback = feedback;
        _sms = sms;
    }

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/customer/message/list"), HttpGet("/admin/user/customer/message/list")]
    [HttpPost("/admin/user/customer/mail/list"), HttpGet("/admin/user/customer/mail/list")]
    [NonUnify, DisplayName("customer-message-list")]
    public Task<OldApiResult> List() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            var (page, size) = OldApiShowResult.ReadPaging(http);
            bool? isRead = null;
            var readRaw = OldApiFormReader.ReadString(http, "IsRead", "isRead");
            if (bool.TryParse(readRaw, out var br)) isRead = br;
            else if (readRaw is "1" or "0") isRead = readRaw == "1";
            DateTime? msgStart = null, msgEnd = null;
            if (DateTime.TryParse(OldApiFormReader.ReadString(http, "StartAt", "startAt"), out var msgSa)) msgStart = msgSa;
            if (DateTime.TryParse(OldApiFormReader.ReadString(http, "EndAt", "endAt"), out var msgEa)) msgEnd = msgEa;
            var outp = await _messages.PageAsync(new UserMessagePageInput
            {
                Page = page,
                PageSize = size,
                UserID = OldApiFormReader.ReadInt(http, "UserID", "userID"),
                User = OldApiFormReader.ReadString(http, "User", "UserName", "user"),
                Title = OldApiFormReader.ReadString(http, "Title", "title"),
                IsRead = isRead,
                StartAt = msgStart,
                EndAt = msgEnd
            }, ct);
            return OldApiShowResult.OfPage(outp.Items ?? [], outp.Total, page, size);
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/customer/message/info"), HttpGet("/admin/user/customer/message/info")]
    [HttpPost("/admin/user/customer/mail/info"), HttpGet("/admin/user/customer/mail/info")]
    [NonUnify, DisplayName("customer-message-info")]
    public Task<OldApiResult> Info() =>
        Run(ct => _messages.DetailAsync(OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id"), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/customer/message/send")]
    [HttpPost("/admin/user/customer/mail/send")]
    [NonUnify, DisplayName("customer-message-send")]
    public Task<OldApiResult> Send() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            var id = await _messages.SendAsync(new UserMessageSendInput
            {
                UserID = OldApiFormReader.ReadInt(http, "UserID", "userID"),
                User = OldApiFormReader.ReadString(http, "User", "UserName", "user"),
                Groups = OldApiFormReader.ReadString(http, "Groups", "GroupID", "GroupIds"),
                Title = OldApiFormReader.ReadString(http, "Title", "title"),
                Content = OldApiFormReader.ReadString(http, "Content", "content", "Body"),
                Type = OldApiFormReader.ReadInt(http, "Type", "type"),
                SupportCurrency = OldApiFormReader.ReadString(http, "SupportCurrency")
            }, ct);
            return (object?)id;
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/customer/message/delete")]
    [HttpPost("/admin/user/customer/mail/delete")]
    [NonUnify, DisplayName("customer-message-delete")]
    public Task<OldApiResult> Delete() =>
        Run(async ct =>
        {
            await _messages.DeleteAsync(OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id"), ct);
            return (object?)null;
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/customer/feedback/list"), HttpGet("/admin/user/customer/feedback/list")]
    [NonUnify, DisplayName("customer-feedback-list")]
    public Task<OldApiResult> Feedbacklist() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            DateTime? fbStart = null, fbEnd = null;
            if (DateTime.TryParse(OldApiFormReader.ReadString(http, "StartAt", "startAt"), out var fbSa)) fbStart = fbSa;
            if (DateTime.TryParse(OldApiFormReader.ReadString(http, "EndAt", "endAt"), out var fbEa)) fbEnd = fbEa;
            var rows = await _feedback.ListAsync(new SiteFeedbackListInput
            {
                UserID = OldApiFormReader.ReadInt(http, "UserID", "userID"),
                User = OldApiFormReader.ReadString(http, "User", "UserName", "user"),
                Content = OldApiFormReader.ReadString(http, "Content", "content"),
                Mobile = OldApiFormReader.ReadString(http, "Mobile", "mobile"),
                Email = OldApiFormReader.ReadString(http, "Email", "email"),
                StartAt = fbStart,
                EndAt = fbEnd
            }, ct);
            return OldApiShowResult.OfList(rows);
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/customer/feedback/delete")]
    [NonUnify, DisplayName("customer-feedback-delete")]
    public Task<OldApiResult> Feedbackdelete() =>
        Run(async ct =>
        {
            await _feedback.DeleteAsync(OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id"), ct);
            return (object?)null;
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/customer/sms/list"), HttpGet("/admin/user/customer/sms/list")]
    [NonUnify, DisplayName("customer-sms-list")]
    public Task<OldApiResult> Smslist() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            var (page, size) = OldApiShowResult.ReadPaging(http);
            DateTime? smsStart = null, smsEnd = null;
            if (DateTime.TryParse(OldApiFormReader.ReadString(http, "StartAt", "startAt"), out var smsSa)) smsStart = smsSa;
            if (DateTime.TryParse(OldApiFormReader.ReadString(http, "EndAt", "endAt"), out var smsEa)) smsEnd = smsEa;
            bool? smsOk = null;
            var okRaw = OldApiFormReader.ReadString(http, "IsSuccess", "isSuccess");
            if (bool.TryParse(okRaw, out var okb)) smsOk = okb;
            else if (okRaw is "1" or "0") smsOk = okRaw == "1";
            var outp = await _sms.PageAsync(new SmsLogPageInput
            {
                Page = page,
                PageSize = size,
                User = OldApiFormReader.ReadString(http, "User", "UserName", "user"),
                Mobile = OldApiFormReader.ReadString(http, "Mobile", "mobile"),
                IsSuccess = smsOk,
                StartAt = smsStart,
                EndAt = smsEnd
            }, ct);
            return OldApiShowResult.OfPage(outp.Items ?? [], outp.Total, page, size);
        });

    /// <summary>修改站内信/邮件。对齐旧 message/update、mail/update（仅 POST）。</summary>
    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/customer/message/update")]
    [HttpPost("/admin/user/customer/mail/update")]
    [NonUnify, DisplayName("customer-message-update")]
    public Task<OldApiResult> Update() =>
        Run(ct =>
        {
            var map = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var (k, v) in OldApiFormReader.ReadJsonMap(App.HttpContext))
                map[k] = v.ToString();
            return _messages.UpdateAsync(map, ct);
        });

    private static async Task<OldApiResult> Run<T>(Func<CancellationToken, Task<T>> action) => await OldApiFormReader.RunAsync(action);
}
