using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 兑币旧路径：/admin/exchange/{rate|review}/*。
/// </summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "exchange", Order = 48)]
public class ExchangeFormAppService : IDynamicApiController, ITransient
{
    public const string PermissionId = UserMemberFormAppService.MemberListPermissionId;

    private readonly ExchangeRateService _rate;
    private readonly ExchangeReviewService _review;

    public ExchangeFormAppService(ExchangeRateService rate, ExchangeReviewService review)
    {
        _rate = rate;
        _review = review;
    }

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/exchange/rate/list"), HttpGet("/admin/exchange/rate/list")]
    [NonUnify, DisplayName("exchange-rate-list")]
    public Task<OldApiResult> Ratelist() =>
        Run(ct => _rate.ListAsync(new ExchangeRateListInput
        {
            Currency = OldApiFormReader.ReadInt(App.HttpContext, "Currency", "currency", "Base")
        }, ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/exchange/rate/getbasecurrency"), HttpGet("/admin/exchange/rate/getbasecurrency")]
    [NonUnify, DisplayName("exchange-getbasecurrency")]
    public Task<OldApiResult> Getbasecurrency() =>
        Run(ct => _rate.GetBaseCurrencyAsync(ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/exchange/rate/info"), HttpGet("/admin/exchange/rate/info")]
    [NonUnify, DisplayName("exchange-rate-info")]
    public Task<OldApiResult> Rateinfo() =>
        Run(ct => _rate.DetailAsync(OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id"), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/exchange/rate/exchangelist"), HttpGet("/admin/exchange/rate/exchangelist")]
    [NonUnify, DisplayName("exchange-exchangelist")]
    public Task<OldApiResult> Exchangelist() =>
        Run(ct =>
        {
            var http = App.HttpContext;
            return _rate.ExchangeListAsync(new ExchangeSettingListInput
            {
                CurrencyType = OldApiFormReader.ReadInt(http, "CurrencyType", "Currency", "currency"),
                Status = OldApiFormReader.ReadString(http, "Status", "status")
            }, ct);
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/exchange/rate/exchangeinfo"), HttpGet("/admin/exchange/rate/exchangeinfo")]
    [NonUnify, DisplayName("exchange-exchangeinfo")]
    public Task<OldApiResult> Exchangeinfo() =>
        Run(ct => _rate.ExchangeInfoAsync(OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id"), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/exchange/review/list"), HttpGet("/admin/exchange/review/list")]
    [NonUnify, DisplayName("exchange-review-list")]
    public Task<OldApiResult> Reviewlist() =>
        Run(ct =>
        {
            var http = App.HttpContext;
            return _review.ListPendingAsync(new ExchangeReviewListInput
            {
                StartAt = ReadDate(http, "StartAt"),
                EndAt = ReadDate(http, "EndAt"),
                UserName = OldApiFormReader.ReadString(http, "UserName", "User", "user"),
                Currency = OldApiFormReader.ReadString(http, "Currency", "currency")
            }, ct);
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/exchange/review/log"), HttpGet("/admin/exchange/review/log")]
    [NonUnify, DisplayName("exchange-review-log")]
    public Task<OldApiResult> Reviewlog() =>
        Run(ct =>
        {
            var http = App.HttpContext;
            return _review.ListLogAsync(new ExchangeReviewLogInput
            {
                TimeType = OldApiFormReader.ReadInt(http, "TimeType"),
                StartAt = ReadDate(http, "StartAt"),
                EndAt = ReadDate(http, "EndAt"),
                UserName = OldApiFormReader.ReadString(http, "UserName", "User", "user"),
                Currency = OldApiFormReader.ReadString(http, "Currency", "currency")
            }, ct);
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/exchange/review/summary"), HttpGet("/admin/exchange/review/summary")]
    [NonUnify, DisplayName("exchange-review-summary")]
    public Task<OldApiResult> Reviewsummary() =>
        Run(ct =>
        {
            var http = App.HttpContext;
            return _review.SummaryAsync(new ExchangeSummaryInput
            {
                StartAt = ReadDate(http, "StartAt"),
                EndAt = ReadDate(http, "EndAt")
            }, ct);
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/exchange/review/check")]
    [NonUnify, DisplayName("exchange-review-check")]
    public Task<OldApiResult> Reviewcheck() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            await _review.CheckAsync(new ExchangeReviewCheckInput
            {
                ID = OldApiFormReader.ReadInt(http, "ID", "Id"),
                Status = (byte)OldApiFormReader.ReadInt(http, "Status", "status"),
                Remark = OldApiFormReader.ReadString(http, "Remark", "remark")
            }, checkerId: 0, ct);
            return (object?)null;
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/exchange/rate/getcurrency"), HttpGet("/admin/exchange/rate/getcurrency")]
    [NonUnify, DisplayName("exchange-rate-getcurrency")]
    public Task<OldApiResult> Getcurrency() => Run(ct => _rate.GetcurrencyAsync(ToDict(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/exchange/rate/getcurrencyinfo"), HttpGet("/admin/exchange/rate/getcurrencyinfo")]
    [NonUnify, DisplayName("exchange-rate-getcurrencyinfo")]
    public Task<OldApiResult> Getcurrencyinfo() => Run(ct => _rate.GetcurrencyinfoAsync(ToDict(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/exchange/rate/getfee"), HttpGet("/admin/exchange/rate/getfee")]
    [NonUnify, DisplayName("exchange-rate-getfee")]
    public Task<OldApiResult> Getfee() => Run(ct => _rate.GetfeeAsync(ToDict(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/exchange/rate/getmode"), HttpGet("/admin/exchange/rate/getmode")]
    [NonUnify, DisplayName("exchange-rate-getmode")]
    public Task<OldApiResult> Getmode() => Run(ct => _rate.GetmodeAsync(ToDict(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/exchange/rate/updatesiterate")]
    [NonUnify, DisplayName("exchange-rate-updatesiterate")]
    public Task<OldApiResult> Updatesiterate() => Run(ct => _rate.UpdatesiterateAsync(ToDict(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/exchange/rate/saveinfo")]
    [NonUnify, DisplayName("exchange-rate-saveinfo")]
    public Task<OldApiResult> Saveinfo() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            await _rate.SaveAsync(new ExchangeRateSaveInput
            {
                ID = OldApiFormReader.ReadInt(http, "ID", "Id"),
                MarketPrice = decimal.TryParse(OldApiFormReader.ReadString(http, "MarketPrice"), out var mp) ? mp : 0,
                ExchangeMode = (byte)OldApiFormReader.ReadInt(http, "ExchangeMode", "Mode"),
                Parameter = decimal.TryParse(OldApiFormReader.ReadString(http, "Parameter"), out var p) ? p : 0
            }, ct);
            return (object?)null;
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/exchange/rate/saveexchange")]
    [NonUnify, DisplayName("exchange-rate-saveexchange")]
    public Task<OldApiResult> Saveexchange() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            await _rate.SaveExchangeAsync(new ExchangeSettingSaveInput
            {
                ID = OldApiFormReader.ReadInt(http, "ID", "Id"),
                ExchangeFee = (byte)OldApiFormReader.ReadInt(http, "ExchangeFee", "Fee"),
                ExchangeCharge = decimal.TryParse(OldApiFormReader.ReadString(http, "ExchangeCharge", "Charge"), out var ch) ? ch : 0,
                ExchangeLimit = OldApiFormReader.ReadInt(http, "ExchangeLimit", "Limit"),
                IsCheck = OldApiFormReader.ReadBool(http, "IsCheck"),
                Status = (byte)OldApiFormReader.ReadInt(http, "Status", "status")
            }, ct);
            return (object?)null;
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/exchange/rate/savebasecurrency")]
    [HttpPost("/admin/exchange/rate/savecurrency")]
    [NonUnify, DisplayName("exchange-rate-savecurrency")]
    public Task<OldApiResult> Savecurrency() =>
        Run(async ct =>
        {
            var currency = OldApiFormReader.ReadInt(App.HttpContext, "Currency", "currency", "Type");
            await _rate.SyncFromSystemAsync(currency, ct);
            return (object?)null;
        });

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
