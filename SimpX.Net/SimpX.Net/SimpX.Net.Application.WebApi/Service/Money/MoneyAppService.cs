using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.WebApi;

/// <summary>会员资金兼容：/request/money/*。</summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "money", Order = 24)]
public class MoneyAppService : IDynamicApiController, ITransient
{
    private readonly MemberMoneyQueryService _svc;
    private readonly ExchangeRateService _rates;

    public MoneyAppService(MemberMoneyQueryService svc, ExchangeRateService rates)
    {
        _svc = svc;
        _rates = rates;
    }

    [AllowAnonymous]
    [HttpPost("/request/money/getbalance")]
    [HttpGet("/request/money/getbalance")]
    [NonUnify]
    [DisplayName("会员余额")]
    public async Task<OldApiResult> Getbalance() => await _svc.GetBalanceAsync(App.HttpContext?.RequestAborted ?? default);

    [AllowAnonymous]
    [HttpPost("/request/money/moneylog")]
    [HttpGet("/request/money/moneylog")]
    [NonUnify]
    [DisplayName("会员帐变")]
    public async Task<OldApiResult> Moneylog()
    {
        var http = App.HttpContext;
        var (page, size) = OldApiShowResult.ReadPaging(http);
        DateTime? start = null, end = null;
        if (DateTime.TryParse(OldApiRequestParam.Read(http, "StartAt"), out var s)) start = s;
        if (DateTime.TryParse(OldApiRequestParam.Read(http, "EndAt"), out var e)) end = e;
        return await _svc.MoneyLogAsync(
            start,
            end,
            OldApiRequestParam.Read(http, "Type"),
            OldApiRequestParam.Read(http, "Currency"),
            OldApiRequestParam.Read(http, "SourceID"),
            page,
            size,
            http?.RequestAborted ?? default);
    }

    [AllowAnonymous]
    [HttpPost("/request/money/moneytype")]
    [HttpGet("/request/money/moneytype")]
    [NonUnify]
    [DisplayName("会员资金类型")]
    public async Task<OldApiResult> Moneytype() => await _svc.MoneyTypeAsync(App.HttpContext?.RequestAborted ?? default);

    [AllowAnonymous]
    [HttpPost("/request/money/locked")]
    [HttpGet("/request/money/locked")]
    [NonUnify]
    [DisplayName("会员锁定资金")]
    public async Task<OldApiResult> Locked()
    {
        var (page, size) = OldApiShowResult.ReadPaging(App.HttpContext);
        return await _svc.LockedAsync(page, size, App.HttpContext?.RequestAborted ?? default);
    }

    [AllowAnonymous]
    [HttpPost("/request/money/rechargeorderlist")]
    [HttpGet("/request/money/rechargeorderlist")]
    [NonUnify]
    [DisplayName("会员充值订单")]
    public async Task<OldApiResult> Rechargeorderlist()
    {
        var http = App.HttpContext;
        var (page, size) = OldApiShowResult.ReadPaging(http);
        DateTime? start = null, end = null;
        if (DateTime.TryParse(OldApiRequestParam.Read(http, "StartAt"), out var s)) start = s;
        if (DateTime.TryParse(OldApiRequestParam.Read(http, "EndAt"), out var e)) end = e;
        bool? isPay = null;
        var isPayText = OldApiRequestParam.Read(http, "IsPay");
        if (!string.IsNullOrWhiteSpace(isPayText))
            isPay = isPayText == "1";
        _ = int.TryParse(OldApiRequestParam.Read(http, "PayID"), out var payId);
        return await _svc.RechargeOrderListAsync(
            start, end, OldApiRequestParam.Read(http, "ID"), payId, isPay, page, size,
            http?.RequestAborted ?? default);
    }

    [AllowAnonymous]
    [HttpPost("/request/money/withdraworderlist")]
    [HttpGet("/request/money/withdraworderlist")]
    [NonUnify]
    [DisplayName("会员提现订单")]
    public async Task<OldApiResult> Withdraworderlist()
    {
        var http = App.HttpContext;
        var (page, size) = OldApiShowResult.ReadPaging(http);
        DateTime? start = null, end = null;
        if (DateTime.TryParse(OldApiRequestParam.Read(http, "StartAt"), out var s)) start = s;
        if (DateTime.TryParse(OldApiRequestParam.Read(http, "EndAt"), out var e)) end = e;
        _ = int.TryParse(OldApiRequestParam.Read(http, "ID"), out var id);
        return await _svc.WithdrawOrderListAsync(
            start, end, id, OldApiRequestParam.Read(http, "Status"), page, size,
            http?.RequestAborted ?? default);
    }

    [AllowAnonymous]
    [HttpPost("/request/money/exchangeinfo"), HttpGet("/request/money/exchangeinfo")]
    [NonUnify, DisplayName("money-exchangeinfo")]
    public async Task<OldApiResult> Exchangeinfo()
    {
        try
        {
            var id = OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id");
            if (id <= 0)
                throw Oops.Oh("#参数错误#");
            var info = await _rates.ExchangeInfoAsync(id, App.HttpContext?.RequestAborted ?? default);
            return OldApiResult.Ok(string.Empty, info);
        }
        catch (Exception ex)
        {
            return OldApiResult.Fail(ex.Message);
        }
    }

    [AllowAnonymous]
    [HttpPost("/request/money/exchangecurcceny"), HttpGet("/request/money/exchangecurcceny")]
    [NonUnify, DisplayName("money-exchangecurrency")]
    public Task<OldApiResult> Exchangecurrency() =>
        Task.FromResult(OldApiResult.Fail("#功能尚未接通#"));

    [AllowAnonymous]
    [HttpPost("/request/money/rechargeorderinfo"), HttpGet("/request/money/rechargeorderinfo")]
    [HttpPost("/request/money/withdrawinfo"), HttpGet("/request/money/withdrawinfo")]
    [NonUnify, DisplayName("money-orderinfo")]
    public Task<OldApiResult> Orderinfo() =>
        Task.FromResult(OldApiResult.Ok(string.Empty, new { }));
}
