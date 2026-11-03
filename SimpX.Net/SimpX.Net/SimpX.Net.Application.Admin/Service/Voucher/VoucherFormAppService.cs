using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 彩金/兑换券旧路径：/admin/voucher/{product|order}/*。
/// </summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "voucher", Order = 46)]
public class VoucherFormAppService : IDynamicApiController, ITransient
{
    public const string PermissionId = UserMemberFormAppService.MemberListPermissionId;

    private readonly VoucherAdminService _voucher;

    public VoucherFormAppService(VoucherAdminService voucher) => _voucher = voucher;

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/voucher/product/list"), HttpGet("/admin/voucher/product/list")]
    [NonUnify, DisplayName("voucher-product-list")]
    public Task<OldApiResult> Productlist() =>
        Run(ct => _voucher.ListProductsAsync(ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/voucher/product/info"), HttpGet("/admin/voucher/product/info")]
    [NonUnify, DisplayName("voucher-product-info")]
    public Task<OldApiResult> Productinfo() =>
        Run(ct => _voucher.GetProductAsync(OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id"), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/voucher/product/save")]
    [NonUnify, DisplayName("voucher-product-save")]
    public Task<OldApiResult> Productsave() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            _ = decimal.TryParse(OldApiFormReader.ReadString(http, "Price", "price"), out var price);
            var id = await _voucher.SaveProductAsync(new VoucherProductSaveInput
            {
                ID = OldApiFormReader.ReadInt(http, "ID", "Id"),
                Name = OldApiFormReader.ReadString(http, "Name", "name"),
                Currency = OldApiFormReader.ReadInt(http, "Currency", "currency"),
                Price = price,
                Point = OldApiFormReader.ReadInt(http, "Point", "Points"),
                Stock = OldApiFormReader.ReadInt(http, "Stock", "stock"),
                Status = OldApiFormReader.ReadInt(http, "Status", "status"),
                StartAt = ReadDate(http, "StartAt"),
                EndAt = ReadDate(http, "EndAt")
            }, ct);
            return (object?)id;
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/voucher/product/delete"), HttpPost("/admin/voucher/product/batchdelete")]
    [NonUnify, DisplayName("voucher-product-delete")]
    public Task<OldApiResult> Productdelete() =>
        Run(async ct =>
        {
            await _voucher.DeleteProductAsync(OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id"), ct);
            return (object?)null;
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/voucher/order/list"), HttpGet("/admin/voucher/order/list")]
    [NonUnify, DisplayName("voucher-order-list")]
    public Task<OldApiResult> Orderlist() =>
        Run(ct =>
        {
            var http = App.HttpContext;
            var (page, size) = OldApiShowResult.ReadPaging(http);
            var statusRaw = OldApiFormReader.ReadString(http, "Status", "status");
            var status = -1;
            if (int.TryParse(statusRaw, out var st)) status = st;
            return _voucher.PageOrdersAsync(new VoucherOrderPageInput
            {
                Page = page,
                PageSize = size,
                UserID = OldApiFormReader.ReadInt(http, "UserID", "userID"),
                Status = status
            }, ct);
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/voucher/order/update")]
    [NonUnify, DisplayName("voucher-order-update")]
    public Task<OldApiResult> Orderupdate() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            await _voucher.UpdateOrderStatusAsync(new VoucherOrderStatusInput
            {
                ID = OldApiFormReader.ReadInt(http, "ID", "Id"),
                Status = OldApiFormReader.ReadInt(http, "Status", "status"),
                Remark = OldApiFormReader.ReadString(http, "Remark", "remark")
            }, ct);
            return (object?)null;
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/voucher/order/info"), HttpGet("/admin/voucher/order/info")]
    [HttpPost("/admin/voucher/order/detail"), HttpGet("/admin/voucher/order/detail")]
    [NonUnify, DisplayName("voucher-order-info")]
    public Task<OldApiResult> Orderinfo() => Run(ct => _voucher.DetailAsync(ToDict(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/voucher/column/list"), HttpGet("/admin/voucher/column/list")]
    [NonUnify, DisplayName("voucher-column-list")]
    public Task<OldApiResult> Columnlist() =>
        Run(ct => Task.FromResult<object?>(Array.Empty<object>()));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/voucher/column/info"), HttpGet("/admin/voucher/column/info")]
    [NonUnify, DisplayName("voucher-column-info")]
    public Task<OldApiResult> Columninfo() => Run(ct => _voucher.InfoAsync(ToDict(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/voucher/column/catedict"), HttpGet("/admin/voucher/column/catedict")]
    [NonUnify, DisplayName("voucher-column-catedict")]
    public Task<OldApiResult> Columncatedict() => Run(ct => _voucher.CatedictAsync(ToDict(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/voucher/column/children"), HttpGet("/admin/voucher/column/children")]
    [NonUnify, DisplayName("voucher-column-children")]
    public Task<OldApiResult> Columnchildren() => Run(ct => _voucher.ChildrenAsync(ToDict(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/voucher/column/add")]
    [NonUnify, DisplayName("voucher-column-add")]
    public Task<OldApiResult> Columnadd() => Run(ct => _voucher.AddAsync(ToDict(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/voucher/column/del")]
    [NonUnify, DisplayName("voucher-column-del")]
    public Task<OldApiResult> Columndel() => Run(ct => _voucher.DelAsync(ToDict(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/voucher/log/importpointlist"), HttpGet("/admin/voucher/log/importpointlist")]
    [NonUnify, DisplayName("voucher-log-importpointlist")]
    public Task<OldApiResult> Importpointlist() => Run(ct => _voucher.ImportpointlistAsync(ToDict(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/voucher/log/saveimportpoint")]
    [NonUnify, DisplayName("voucher-log-saveimportpoint")]
    public Task<OldApiResult> Saveimportpoint() => Run(ct => _voucher.SaveimportpointAsync(ToDict(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/voucher/log/editimportpoint")]
    [NonUnify, DisplayName("voucher-log-editimportpoint")]
    public Task<OldApiResult> Editimportpoint() => Run(ct => _voucher.EditimportpointAsync(ToDict(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/voucher/log/deleteimportpoint")]
    [NonUnify, DisplayName("voucher-log-deleteimportpoint")]
    public Task<OldApiResult> Deleteimportpoint() => Run(ct => _voucher.DeleteimportpointAsync(ToDict(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/voucher/log/batchdeleteimportpoint")]
    [NonUnify, DisplayName("voucher-log-batchdeleteimportpoint")]
    public Task<OldApiResult> Batchdeleteimportpoint() => Run(ct => _voucher.BatchdeleteimportpointAsync(ToDict(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/voucher/log/createuserpoint")]
    [NonUnify, DisplayName("voucher-log-createuserpoint")]
    public Task<OldApiResult> Createuserpoint() => Run(ct => _voucher.CreateuserpointAsync(ToDict(), ct));

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
