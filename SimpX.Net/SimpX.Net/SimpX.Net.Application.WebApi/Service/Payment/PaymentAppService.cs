using System.Globalization;
using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;
using SimpX.Net.Data.Entity.Sites;

namespace SimpX.Net.Application.WebApi;

/// <summary>
/// 会员兼容支付路由：/request/payment/*。
/// </summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "payment", Order = 25)]
public class PaymentAppService : IDynamicApiController, ITransient
{
    private readonly MemberAuthService _memberAuth;
    private readonly ICurrentSiteAccessor _site;
    private readonly RechargeOrderCreateService _create;
    private readonly IBizDbAccessor _biz;

    /// <summary>
    /// 注入会员会话、定站、建单。
    /// </summary>
    public PaymentAppService(
        MemberAuthService memberAuth,
        ICurrentSiteAccessor site,
        RechargeOrderCreateService create,
        IBizDbAccessor biz)
    {
        _memberAuth = memberAuth;
        _site = site;
        _create = create;
        _biz = biz;
    }

    /// <summary>
    /// 会员充值下单。对齐旧 POST /request/payment/recharge。
    /// </summary>
    /// <returns>旧版返回；成功时 info 为支付跳转/原文</returns>
    [AllowAnonymous]
    [HttpPost("/request/payment/recharge")]
    [NonUnify]
    [DisplayName("会员充值下单")]
    public async Task<OldApiResult> Recharge()
    {
        var http = App.HttpContext;
        var ct = http?.RequestAborted ?? default;
        var siteId = _site.RequireSiteId();
        var userId = await _memberAuth.TryResolveUserIdFromCookieAsync(siteId, ct);
        if (userId == null)
            return OldApiResult.NeedLogin();

        if (!decimal.TryParse(
                OldApiRequestParam.Read(http, "Money"),
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out var money))
            money = 0;
        if (!int.TryParse(OldApiRequestParam.Read(http, "PayID"), out var payId))
            payId = 0;
        var activeId = OldApiRequestParam.Read(http, "ActiveID");
        if (string.IsNullOrWhiteSpace(activeId))
            activeId = "0";

        try
        {
            var output = await _create.CreateAsync(
                new RechargeOrderCreateInput
                {
                    UserID = userId.Value,
                    PayID = payId,
                    Money = money,
                    ActiveID = activeId,
                    ClientIP = http?.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0"
                },
                ct);
            var info = string.IsNullOrWhiteSpace(output.PayContent)
                ? output.PayUrl
                : output.PayContent;
            return OldApiResult.Ok(info ?? string.Empty);
        }
        catch (InvalidOperationException ex)
        {
            return OldApiResult.Fail(ex.Message);
        }
    }

    [OldApiGuest, AllowAnonymous]
    [HttpPost("/request/payment/rechargelist"), HttpGet("/request/payment/rechargelist")]
    [HttpPost("/request/payment/rechargenumber"), HttpGet("/request/payment/rechargenumber")]
    [HttpPost("/request/payment/specialaccessories"), HttpGet("/request/payment/specialaccessories")]
    [NonUnify, DisplayName("payment-rechargelist")]
    public async Task<OldApiResult> Rechargelist()
    {
        try
        {
            var siteId = _site.RequireSiteId();
            var ct = App.HttpContext?.RequestAborted ?? default;
            var rows = await _biz.Db.Queryable<SitePayment>()
                .Where(p => p.SiteID == siteId)
                .OrderBy(p => p.Sort, OrderByType.Desc)
                .Select(p => new { p.ID, p.Name, p.CateID, p.Description, p.Sort })
                .Take(200)
                .ToListAsync(ct);
            return OldApiResult.Ok(string.Empty, rows);
        }
        catch (Exception ex)
        {
            return OldApiResult.Fail(ex.Message);
        }
    }
}
