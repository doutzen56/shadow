using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.WebApi;

/// <summary>
/// 会员 /request/user/* 剩余旧路径（银行/返水/VIP/订单等）。
/// </summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "user-extra", Order = 22)]
public class UserExtraAppService : IDynamicApiController, ITransient
{
    private readonly MemberAuthService _auth;
    private readonly ICurrentSiteAccessor _site;
    private readonly SiteVipConditionService _vip;
    private readonly BankAccountService _banks;
    private readonly SiteConfigService _siteConfig;
    private readonly ProxyMoneyService _proxyMoney;
    private readonly SitePointItemService _points;

    public UserExtraAppService(
        MemberAuthService auth,
        ICurrentSiteAccessor site,
        SiteVipConditionService vip,
        BankAccountService banks,
        SiteConfigService siteConfig,
        ProxyMoneyService proxyMoney,
        SitePointItemService points)
    {
        _auth = auth;
        _site = site;
        _vip = vip;
        _banks = banks;
        _siteConfig = siteConfig;
        _proxyMoney = proxyMoney;
        _points = points;
    }

    [OldApiGuest, AllowAnonymous]
    [HttpPost("/request/user/banktype"), HttpGet("/request/user/banktype")]
    [HttpPost("/request/user/bankcountry"), HttpGet("/request/user/bankcountry")]
    [HttpPost("/request/user/bankcountrylist"), HttpGet("/request/user/bankcountrylist")]
    [NonUnify, DisplayName("user-banktype")]
    public Task<OldApiResult> Banktype() =>
        Run(ct => _banks.BankCountryListAsync(ct));

    [OldApiGuest, AllowAnonymous]
    [HttpPost("/request/user/bankinfo"), HttpGet("/request/user/bankinfo")]
    [NonUnify, DisplayName("user-bankinfo")]
    public Task<OldApiResult> Bankinfo() =>
        Run(ct => _siteConfig.GetBankListAsync(ct));

    [AllowAnonymous]
    [HttpPost("/request/user/pointlog"), HttpGet("/request/user/pointlog")]
    [NonUnify, DisplayName("user-pointlog")]
    public Task<OldApiResult> Pointlog() =>
        Run(async ct =>
        {
            var userId = await RequireUserIdAsync(ct);
            var (page, size) = OldApiShowResult.ReadPaging(App.HttpContext);
            return await _points.PageLogsAsync(new PointLogPageInput
            {
                Page = page,
                PageSize = size,
                UserID = userId
            }, ct);
        });

    [AllowAnonymous]
    [HttpPost("/request/user/channel"), HttpGet("/request/user/channel")]
    [HttpPost("/request/user/channelpusherman"), HttpGet("/request/user/channelpusherman")]
    [HttpPost("/request/user/exchangelog"), HttpGet("/request/user/exchangelog")]
    [HttpPost("/request/user/orderlist"), HttpGet("/request/user/orderlist")]
    [HttpPost("/request/user/orderstatus"), HttpGet("/request/user/orderstatus")]
    [HttpPost("/request/user/returndate"), HttpGet("/request/user/returndate")]
    [HttpPost("/request/user/returndetail"), HttpGet("/request/user/returndetail")]
    [HttpPost("/request/user/returnrecord"), HttpGet("/request/user/returnrecord")]
    [HttpPost("/request/user/returnrule"), HttpGet("/request/user/returnrule")]
    [HttpPost("/request/user/myinviteusers"), HttpGet("/request/user/myinviteusers")]
    [HttpPost("/request/user/levelinfo"), HttpGet("/request/user/levelinfo")]
    [HttpPost("/request/user/signinfo"), HttpGet("/request/user/signinfo")]
    [HttpPost("/request/user/getuseridcard"), HttpGet("/request/user/getuseridcard")]
    [NonUnify, DisplayName("user-extra-read")]
    public Task<OldApiResult> ExtraRead() =>
        Run(async ct =>
        {
            await RequireUserIdAsync(ct);
            return (object)new { Items = Array.Empty<object>(), Total = 0 };
        });

    [AllowAnonymous]
    [HttpPost("/request/user/vipinfo"), HttpGet("/request/user/vipinfo")]
    [HttpPost("/request/user/vipopen"), HttpGet("/request/user/vipopen")]
    [NonUnify, DisplayName("user-vip")]
    public Task<OldApiResult> Vip() =>
        Run(ct => _vip.GetAsync(ct));

    [AllowAnonymous]
    [HttpPost("/request/user/token"), HttpGet("/request/user/token")]
    [NonUnify, DisplayName("user-token")]
    public Task<OldApiResult> Token() =>
        Run(async ct =>
        {
            var userId = await RequireUserIdAsync(ct);
            return await _proxyMoney.IssueTokenAsync(userId, ct);
        });

    [AllowAnonymous]
    [HttpPost("/request/user/autoreturn"), HttpGet("/request/user/autoreturn")]
    [HttpPost("/request/user/autoreturnv2"), HttpGet("/request/user/autoreturnv2")]
    [HttpPost("/request/user/returntry")]
    [HttpPost("/request/user/safeverify")]
    [HttpPost("/request/user/updateuseraddress")]
    [HttpPost("/request/user/userkycprocess")]
    [NonUnify, DisplayName("user-extra-write")]
    public Task<OldApiResult> ExtraWrite() =>
        Run<object>(_ => throw Oops.Oh("#功能尚未接通#"));

    private async Task<int> RequireUserIdAsync(CancellationToken ct)
    {
        var siteId = _site.RequireSiteId();
        var userId = await _auth.TryResolveUserIdFromCookieAsync(siteId, ct);
        if (userId is null or <= 0)
            throw Oops.Oh("#请先登录#");
        return userId.Value;
    }

    private static async Task<OldApiResult> Run<T>(Func<CancellationToken, Task<T>> action) => await OldApiFormReader.RunAsync(action);
}
