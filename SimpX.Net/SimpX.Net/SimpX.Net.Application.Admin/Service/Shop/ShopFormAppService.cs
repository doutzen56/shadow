using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 商城旧路径：/admin/shop/{column|product|order}/*。
/// </summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "shop", Order = 45)]
public class ShopFormAppService : IDynamicApiController, ITransient
{
    public const string PermissionId = UserMemberFormAppService.MemberListPermissionId;

    private readonly ShopAdminService _shop;

    public ShopFormAppService(ShopAdminService shop) => _shop = shop;

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/shop/column/list"), HttpGet("/admin/shop/column/list")]
    [HttpPost("/admin/shop/column/treelist"), HttpGet("/admin/shop/column/treelist")]
    [NonUnify, DisplayName("shop-column-list")]
    public Task<OldApiResult> Columnlist() =>
        Run(ct => _shop.ListCategoriesAsync(ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/shop/column/info"), HttpGet("/admin/shop/column/info")]
    [NonUnify, DisplayName("shop-column-info")]
    public Task<OldApiResult> Columninfo() =>
        Run(ct =>
        {
            var map = new Dictionary<string, object?>
            {
                ["ID"] = OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id")
            };
            return _shop.InfoAsync(map, ct);
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/shop/column/catedict"), HttpGet("/admin/shop/column/catedict")]
    [NonUnify, DisplayName("shop-column-catedict")]
    public Task<OldApiResult> Columncatedict() =>
        Run(ct => _shop.CateDictAsync(ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/shop/column/children"), HttpGet("/admin/shop/column/children")]
    [NonUnify, DisplayName("shop-column-children")]
    public Task<OldApiResult> Columnchildren() =>
        Run(ct =>
        {
            var map = new Dictionary<string, object?>
            {
                ["ParentID"] = OldApiFormReader.ReadInt(App.HttpContext, "ParentID", "ID", "Id")
            };
            return _shop.ChildrenAsync(map, ct);
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/shop/column/save"), HttpPost("/admin/shop/column/add")]
    [NonUnify, DisplayName("shop-column-save")]
    public Task<OldApiResult> Columnsave() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            var id = await _shop.SaveCategoryAsync(new ShopCategorySaveInput
            {
                ID = OldApiFormReader.ReadInt(http, "ID", "Id"),
                ParentID = OldApiFormReader.ReadInt(http, "ParentID", "parentID"),
                Name = OldApiFormReader.ReadString(http, "Name", "name"),
                Sort = OldApiFormReader.ReadInt(http, "Sort", "sort")
            }, ct);
            return (object?)id;
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/shop/column/del"), HttpPost("/admin/shop/column/delete")]
    [NonUnify, DisplayName("shop-column-del")]
    public Task<OldApiResult> Columndel() =>
        Run(async ct =>
        {
            await _shop.DeleteCategoryAsync(OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id"), ct);
            return (object?)null;
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/shop/product/list"), HttpGet("/admin/shop/product/list")]
    [NonUnify, DisplayName("shop-product-list")]
    public Task<OldApiResult> Productlist() =>
        Run(ct =>
        {
            var http = App.HttpContext;
            var (page, size) = OldApiShowResult.ReadPaging(http);
            return _shop.PageProductsAsync(new ShopProductPageInput
            {
                Page = page,
                PageSize = size,
                TypeID = OldApiFormReader.ReadInt(http, "TypeID", "ColID", "typeID"),
                Name = OldApiFormReader.ReadString(http, "Name", "name", "Title")
            }, ct);
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/shop/product/info"), HttpGet("/admin/shop/product/info")]
    [NonUnify, DisplayName("shop-product-info")]
    public Task<OldApiResult> Productinfo() =>
        Run(ct => _shop.GetProductAsync(OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id"), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/shop/product/save"), HttpPost("/admin/shop/product/add")]
    [NonUnify, DisplayName("shop-product-save")]
    public Task<OldApiResult> Productsave() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            _ = decimal.TryParse(OldApiFormReader.ReadString(http, "Stock", "stock"), out var stock);
            _ = decimal.TryParse(OldApiFormReader.ReadString(http, "WashFlow", "washFlow"), out var wash);
            var id = await _shop.SaveProductAsync(new ShopProductSaveInput
            {
                ID = OldApiFormReader.ReadInt(http, "ID", "Id"),
                Name = OldApiFormReader.ReadString(http, "Name", "name"),
                TypeID = OldApiFormReader.ReadInt(http, "TypeID", "typeID"),
                TypeName = OldApiFormReader.ReadString(http, "TypeName", "typeName"),
                Pic = OldApiFormReader.ReadString(http, "Pic", "pic"),
                Price = OldApiFormReader.ReadString(http, "Price", "price"),
                Points = OldApiFormReader.ReadInt(http, "Points", "points"),
                Description = OldApiFormReader.ReadString(http, "Description", "description"),
                Content = OldApiFormReader.ReadString(http, "Content", "content"),
                Status = OldApiFormReader.ReadInt(http, "Status", "status"),
                Stock = stock,
                WashFlow = wash,
                IsAuto = OldApiFormReader.ReadInt(http, "IsAuto") == 1,
                Unit = OldApiFormReader.ReadString(http, "Unit", "unit"),
                Sort = OldApiFormReader.ReadInt(http, "Sort", "sort")
            }, ct);
            return (object?)id;
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/shop/product/delete")]
    [NonUnify, DisplayName("shop-product-delete")]
    public Task<OldApiResult> Productdelete() =>
        Run(async ct =>
        {
            await _shop.DeleteProductAsync(OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id"), ct);
            return (object?)null;
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/shop/order/list"), HttpGet("/admin/shop/order/list")]
    [NonUnify, DisplayName("shop-order-list")]
    public Task<OldApiResult> Orderlist() =>
        Run(ct =>
        {
            var http = App.HttpContext;
            var (page, size) = OldApiShowResult.ReadPaging(http);
            var statusRaw = OldApiFormReader.ReadString(http, "Status", "status");
            var status = -1;
            if (int.TryParse(statusRaw, out var st)) status = st;
            return _shop.PageOrdersAsync(new ShopOrderPageInput
            {
                Page = page,
                PageSize = size,
                UserID = OldApiFormReader.ReadInt(http, "UserID", "userID"),
                UserName = OldApiFormReader.ReadString(http, "UserName", "User", "user"),
                Status = status
            }, ct);
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/shop/order/update")]
    [NonUnify, DisplayName("shop-order-update")]
    public Task<OldApiResult> Orderupdate() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            await _shop.UpdateOrderStatusAsync(new ShopOrderStatusInput
            {
                ID = OldApiFormReader.ReadInt(http, "ID", "Id"),
                Status = OldApiFormReader.ReadInt(http, "Status", "status"),
                Remark = OldApiFormReader.ReadString(http, "Remark", "remark")
            }, ct);
            return (object?)null;
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/shop/order/info"), HttpGet("/admin/shop/order/info")]
    [NonUnify, DisplayName("shop-order-info")]
    public Task<OldApiResult> Orderinfo() =>
        Task.FromResult(OldApiResult.Fail("#功能尚未接通#"));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/shop/product/savesort")]
    [NonUnify, DisplayName("shop-product-savesort")]
    public Task<OldApiResult> Productsavesort() => Run(ct => _shop.SavesortAsync(ToDict(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/shop/log/importpointlist"), HttpGet("/admin/shop/log/importpointlist")]
    [NonUnify, DisplayName("shop-log-importpointlist")]
    public Task<OldApiResult> Importpointlist() => Run(ct => _shop.ImportpointlistAsync(ToDict(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/shop/log/saveimportpoint")]
    [NonUnify, DisplayName("shop-log-saveimportpoint")]
    public Task<OldApiResult> Saveimportpoint() => Run(ct => _shop.SaveimportpointAsync(ToDict(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/shop/log/editimportpoint")]
    [NonUnify, DisplayName("shop-log-editimportpoint")]
    public Task<OldApiResult> Editimportpoint() => Run(ct => _shop.EditimportpointAsync(ToDict(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/shop/log/deleteimportpoint")]
    [NonUnify, DisplayName("shop-log-deleteimportpoint")]
    public Task<OldApiResult> Deleteimportpoint() => Run(ct => _shop.DeleteimportpointAsync(ToDict(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/shop/log/batchdeleteimportpoint")]
    [NonUnify, DisplayName("shop-log-batchdeleteimportpoint")]
    public Task<OldApiResult> Batchdeleteimportpoint() => Run(ct => _shop.BatchdeleteimportpointAsync(ToDict(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/shop/log/createuserpoint")]
    [NonUnify, DisplayName("shop-log-createuserpoint")]
    public Task<OldApiResult> Createuserpoint() => Run(ct => _shop.CreateuserpointAsync(ToDict(), ct));

    private static Dictionary<string, object?> ToDict()
    {
        var map = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (k, v) in OldApiFormReader.ReadJsonMap(App.HttpContext))
            map[k] = v.ToString();
        return map;
    }

    private static async Task<OldApiResult> Run<T>(Func<CancellationToken, Task<T>> action) => await OldApiFormReader.RunAsync(action);
}
