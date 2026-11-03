using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;
using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.WebApi;

/// <summary>
/// 会员彩金兑换旧路径：/request/voucher/product/{list|order}。
/// </summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "voucher", Order = 38)]
public class VoucherAppService : IDynamicApiController, ITransient
{
    private readonly VoucherAdminService _voucher;
    private readonly MemberAuthService _auth;
    private readonly ICurrentSiteAccessor _site;
    private readonly IBizDbAccessor _biz;

    public VoucherAppService(
        VoucherAdminService voucher,
        MemberAuthService auth,
        ICurrentSiteAccessor site,
        IBizDbAccessor biz)
    {
        _voucher = voucher;
        _auth = auth;
        _site = site;
        _biz = biz;
    }

    [OldApiGuest, AllowAnonymous]
    [HttpPost("/request/voucher/product/list"), HttpGet("/request/voucher/product/list")]
    [NonUnify, DisplayName("voucher-product-list")]
    public Task<OldApiResult> Productlist() =>
        Run(ct =>
        {
            var http = App.HttpContext;
            var sortRaw = OldApiFormReader.ReadString(http, "Sort", "sort");
            var sort = int.TryParse(sortRaw, out var s) ? s : 1;
            return _voucher.ListValidProductsAsync(
                OldApiFormReader.ReadString(http, "Name", "name"),
                sort,
                ct);
        });

    [AllowAnonymous]
    [HttpPost("/request/voucher/product/order")]
    [NonUnify, DisplayName("voucher-product-order")]
    public Task<OldApiResult> Productorder() =>
        Run(async ct =>
        {
            var (userId, userName) = await RequireMemberAsync(ct);
            var msg = await _voucher.PlaceMemberOrderAsync(
                userId,
                userName,
                OldApiFormReader.ReadString(App.HttpContext, "ProductID", "productID", "Name"),
                ct);
            return (object?)msg;
        });

    private async Task<(int UserId, string UserName)> RequireMemberAsync(CancellationToken ct)
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
        return (userId.Value, users.FirstOrDefault() ?? string.Empty);
    }

    private static async Task<OldApiResult> Run<T>(Func<CancellationToken, Task<T>> action) => await OldApiFormReader.RunAsync(action);
}
