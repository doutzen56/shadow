using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;
using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.WebApi;

/// <summary>
/// 会员商城旧路径：/request/shop/product/{list|catedict|order}。
/// </summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "shop", Order = 37)]
public class ShopAppService : IDynamicApiController, ITransient
{
    private readonly ShopAdminService _shop;
    private readonly MemberAuthService _auth;
    private readonly ICurrentSiteAccessor _site;
    private readonly IBizDbAccessor _biz;

    public ShopAppService(
        ShopAdminService shop,
        MemberAuthService auth,
        ICurrentSiteAccessor site,
        IBizDbAccessor biz)
    {
        _shop = shop;
        _auth = auth;
        _site = site;
        _biz = biz;
    }

    [OldApiGuest, AllowAnonymous]
    [HttpPost("/request/shop/product/list"), HttpGet("/request/shop/product/list")]
    [NonUnify, DisplayName("shop-product-list")]
    public Task<OldApiResult> Productlist() =>
        Run(ct =>
        {
            var http = App.HttpContext;
            return _shop.ListOnlineProductsAsync(new ShopProductPageInput
            {
                TypeID = OldApiFormReader.ReadInt(http, "TypeID", "typeID"),
                Name = OldApiFormReader.ReadString(http, "Name", "name")
            }, ct);
        });

    [OldApiGuest, AllowAnonymous]
    [HttpPost("/request/shop/product/catedict"), HttpGet("/request/shop/product/catedict")]
    [NonUnify, DisplayName("shop-catedict")]
    public Task<OldApiResult> Productcatedict() =>
        Run(ct => _shop.CateDictAsync(ct));

    [AllowAnonymous]
    [HttpPost("/request/shop/product/order")]
    [NonUnify, DisplayName("shop-product-order")]
    public Task<OldApiResult> Productorder() =>
        Run(async ct =>
        {
            var (userId, userName) = await RequireMemberAsync(ct);
            var msg = await _shop.PlaceMemberOrderAsync(
                userId,
                userName,
                OldApiFormReader.ReadInt(App.HttpContext, "ProductID", "productID", "Id"),
                ct);
            return (object?)msg;
        });

    private async Task<(int UserId, string UserName)> RequireMemberAsync(CancellationToken ct)
    {
        var siteId = _site.RequireSiteId();
        var userId = await _auth.TryResolveUserIdFromCookieAsync(siteId, ct);
        if (userId is null or <= 0)
            throw Oops.Oh("#请登录再操作#");
        var users = await _biz.Db.Queryable<User>()
            .Where(u => u.SiteID == siteId && u.ID == userId.Value)
            .Select(u => u.UserName)
            .Take(1)
            .ToListAsync(ct);
        return (userId.Value, users.FirstOrDefault() ?? string.Empty);
    }

    private static async Task<OldApiResult> Run<T>(Func<CancellationToken, Task<T>> action) => await OldApiFormReader.RunAsync(action);
}
