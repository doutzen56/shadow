using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;
using SimpX.Net.Feature.Sms;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 残留旧路径真接：upload / proxy token·transfer / excel 导出 / 电销短信。
/// </summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "residual", Order = 99)]
public class ResidualAppService : IDynamicApiController, ITransient
{
    public const string PermissionId = UserMemberFormAppService.MemberListPermissionId;

    private readonly SiteUploadService _upload;
    private readonly ProxyMoneyService _proxyMoney;
    private readonly SiteFileDownloadService _files;
    private readonly SiteAdminAuthService _adminAuth;
    private readonly SmsLogService _smsLog;
    private readonly IEnumerable<ISmsSender> _smsSenders;
    private readonly IBizDbAccessor _biz;
    private readonly ICurrentSiteAccessor _site;

    public ResidualAppService(
        SiteUploadService upload,
        ProxyMoneyService proxyMoney,
        SiteFileDownloadService files,
        SiteAdminAuthService adminAuth,
        SmsLogService smsLog,
        IEnumerable<ISmsSender> smsSenders,
        IBizDbAccessor biz,
        ICurrentSiteAccessor site)
    {
        _upload = upload;
        _proxyMoney = proxyMoney;
        _files = files;
        _adminAuth = adminAuth;
        _smsLog = smsLog;
        _smsSenders = smsSenders;
        _biz = biz;
        _site = site;
    }

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/system/upload")]
    [HttpPost("/admin/site/app/upload")]
    [NonUnify, DisplayName("upload")]
    public Task<OldApiResult> Upload() =>
        Run(ct => _upload.UploadAsync(ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/proxy/token"), HttpGet("/admin/proxy/token")]
    [NonUnify, DisplayName("proxy-token")]
    public Task<OldApiResult> ProxyToken() =>
        Run(async ct =>
        {
            var userId = await ResolveProxyUserIdAsync(ct);
            return await _proxyMoney.IssueTokenAsync(userId, ct);
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/proxy/transfer")]
    [NonUnify, DisplayName("proxy-transfer")]
    public Task<OldApiResult> ProxyTransfer() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            var agentId = await ResolveProxyUserIdAsync(ct);
            await _proxyMoney.TransferInAsync(
                agentId,
                OldApiFormReader.ReadString(http, "UserName", "User"),
                OldApiFormReader.ReadString(http, "PayPassword", "Password"),
                OldApiFormReader.ReadDecimal(http, "Money"),
                OldApiFormReader.ReadInt(http, "Currency"),
                ct);
            return (object)"#转账成功#";
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/proxy/transferout")]
    [NonUnify, DisplayName("proxy-transferout")]
    public Task<OldApiResult> ProxyTransferOut() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            var agentId = await ResolveProxyUserIdAsync(ct);
            await _proxyMoney.TransferOutAsync(
                agentId,
                OldApiFormReader.ReadString(http, "UserName", "User"),
                OldApiFormReader.ReadString(http, "PayPassword", "Password"),
                OldApiFormReader.ReadDecimal(http, "Money"),
                OldApiFormReader.ReadInt(http, "Currency"),
                ct);
            return (object)"#转账成功#";
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/report/user/exceluserstatistics"), HttpGet("/admin/report/user/exceluserstatistics")]
    [NonUnify, DisplayName("excel-user-statistics")]
    public Task<OldApiResult> ExcelUserStatistics() =>
        Run(ct =>
        {
            var http = App.HttpContext;
            DateTime? start = null, end = null;
            if (DateTime.TryParse(OldApiFormReader.ReadString(http, "StartAt"), out var s)) start = s;
            if (DateTime.TryParse(OldApiFormReader.ReadString(http, "EndAt"), out var e)) end = e;
            return _files.ExcelUserStatisticsAsync(start, end, ct);
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/customer/mobile/send")]
    [NonUnify, DisplayName("customer-mobile-send")]
    public Task<OldApiResult> MobileSend() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            var phones = OldApiFormReader.ReadString(http, "User", "Mobile", "Phones");
            if (string.IsNullOrWhiteSpace(phones))
                throw Oops.Oh("#请填写有效的手机号码#");
            var text = OldApiFormReader.ReadString(http, "Content", "Text", "Message");
            if (string.IsNullOrWhiteSpace(text))
                throw Oops.Oh("#填写内容不能为空#");
            var userId = OldApiFormReader.ReadInt(http, "UserId", "UserID");
            var sender = _smsSenders.FirstOrDefault();
            var count = 0;
            var errors = new List<string>();
            foreach (var item in phones.Split('#', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var success = false;
                var msg = string.Empty;
                var parts = item.Split(',');
                if (parts.Length == 2)
                {
                    var phone = item.Replace(",", string.Empty);
                    try
                    {
                        if (sender == null)
                            throw Oops.Oh("#短信通道未配置#");
                        await sender.SendAsync(new SmsMessage
                        {
                            PhoneNumber = phone,
                            TemplateId = "admin-manual",
                            Parameters = new Dictionary<string, string> { ["content"] = text }
                        }, ct);
                        success = true;
                        count++;
                    }
                    catch (Exception ex)
                    {
                        msg = ex.Message;
                        errors.Add($"{item}--{msg}");
                    }
                }
                else
                {
                    msg = "电话号码错误";
                    errors.Add($"{item}--{msg}");
                }

                await _smsLog.AddLogAsync(userId, item, text, success, msg, ct);
            }

            var outMsg = (count == 0 ? "" : $"success:{count}")
                + (errors.Count == 0 ? "" : $"  error: {string.Join("|", errors)}");
            return (object)outMsg;
        });

    private async Task<int> ResolveProxyUserIdAsync(CancellationToken ct)
    {
        var formId = OldApiFormReader.ReadInt(App.HttpContext, "UserID", "ID", "Id", "AgentID");
        if (formId > 0)
            return formId;

        var admin = await _adminAuth.TryGetCurrentAdminAsync(ct);
        if (admin == null)
            throw Oops.Oh("#请先登录#");

        var siteId = _site.RequireSiteId();
        var byName = await _biz.Db.Queryable<SimpX.Net.Data.Entity.Users.User>()
            .Where(u => u.SiteID == siteId && u.UserName == admin.AdminName)
            .Select(u => u.ID)
            .Take(1)
            .ToListAsync(ct);
        if (byName.Count > 0 && byName[0] > 0)
            return byName[0];

        var byId = await _biz.Db.Queryable<SimpX.Net.Data.Entity.Users.User>()
            .AnyAsync(u => u.SiteID == siteId && u.ID == admin.ID, ct);
        if (byId)
            return admin.ID;

        throw Oops.Oh("#用户不存在#");
    }

    private static async Task<OldApiResult> Run<T>(Func<CancellationToken, Task<T>> action) => await OldApiFormReader.RunAsync(action);
}
