using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>支付设定旧路径：/admin/payment/*。</summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "payment", Order = 39)]
public class PaymentFormAppService : IDynamicApiController, ITransient
{
    public const string PermissionId = RechargeFormAppService.PermissionId;

    private readonly SitePaymentService _payment;

    public PaymentFormAppService(SitePaymentService payment) => _payment = payment;

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/payment/getpaymentsetting"), HttpGet("/admin/payment/getpaymentsetting")]
    [NonUnify, DisplayName("payment-getpaymentsetting")]
    public Task<OldApiResult> Getpaymentsetting() =>
        Run(ct => _payment.GetpaymentsettingAsync(ToDict(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/payment/getsyspaymentgroup"), HttpGet("/admin/payment/getsyspaymentgroup")]
    [NonUnify, DisplayName("payment-getsyspaymentgroup")]
    public Task<OldApiResult> Getsyspaymentgroup() =>
        Run(ct => _payment.GetsyspaymentgroupAsync(ToDict(), ct));

    private static Dictionary<string, object?> ToDict()
    {
        var map = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (k, v) in OldApiFormReader.ReadJsonMap(App.HttpContext))
            map[k] = v.ToString();
        return map;
    }

    private static async Task<OldApiResult> Run(Func<CancellationToken, Task<object?>> action) => await OldApiFormReader.RunAsync(action);
}
