using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 租户后台积分商城（JWT）。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "shopAdmin", Order = 543, Description = "积分商城")]
public partial class ShopAdminAppService : IDynamicApiController, ITransient
{
    private readonly ShopAdminService _svc;

    /// <summary>
    /// 注入。
    /// </summary>
    /// <param name="svc">商城</param>
    public ShopAdminAppService(ShopAdminService svc) => _svc = svc;

    /// <summary>分类列表。</summary>
    /// <returns>行列表</returns>
    [ApiDescriptionSettings(Name = "ListCategories"), HttpPost]
    [DisplayName("商城分类列表")]
    public Task<List<ShopCategoryItemDto>> ListCategories() =>
        _svc.ListCategoriesAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>保存分类。</summary>
    /// <param name="input">表单</param>
    /// <returns>编号</returns>
    [ApiDescriptionSettings(Name = "SaveCategory"), HttpPost]
    [DisplayName("商城分类保存")]
    public Task<int> SaveCategory(ShopCategorySaveInput input) =>
        _svc.SaveCategoryAsync(input ?? new ShopCategorySaveInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>删除分类。</summary>
    /// <param name="id">编号</param>
    [ApiDescriptionSettings(Name = "DeleteCategory"), HttpPost]
    [DisplayName("商城分类删除")]
    public Task DeleteCategory(int id) =>
        _svc.DeleteCategoryAsync(id, App.HttpContext?.RequestAborted ?? default);

    /// <summary>商品分页。</summary>
    /// <param name="input">筛选</param>
    /// <returns>分页</returns>
    [ApiDescriptionSettings(Name = "PageProducts"), HttpPost]
    [DisplayName("商城商品分页")]
    public Task<ShopProductPageOutput> PageProducts(ShopProductPageInput input) =>
        _svc.PageProductsAsync(input ?? new ShopProductPageInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>保存商品。</summary>
    /// <param name="input">表单</param>
    /// <returns>编号</returns>
    [ApiDescriptionSettings(Name = "SaveProduct"), HttpPost]
    [DisplayName("商城商品保存")]
    public Task<int> SaveProduct(ShopProductSaveInput input) =>
        _svc.SaveProductAsync(input ?? new ShopProductSaveInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>删除商品。</summary>
    /// <param name="id">编号</param>
    [ApiDescriptionSettings(Name = "DeleteProduct"), HttpPost]
    [DisplayName("商城商品删除")]
    public Task DeleteProduct(int id) =>
        _svc.DeleteProductAsync(id, App.HttpContext?.RequestAborted ?? default);

    /// <summary>订单分页。</summary>
    /// <param name="input">筛选</param>
    /// <returns>分页</returns>
    [ApiDescriptionSettings(Name = "PageOrders"), HttpPost]
    [DisplayName("商城订单分页")]
    public Task<ShopOrderPageOutput> PageOrders(ShopOrderPageInput input) =>
        _svc.PageOrdersAsync(input ?? new ShopOrderPageInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>改订单状态。</summary>
    /// <param name="input">表单</param>
    [ApiDescriptionSettings(Name = "UpdateOrderStatus"), HttpPost]
    [DisplayName("商城订单改状态")]
    public Task UpdateOrderStatus(ShopOrderStatusInput input) =>
        _svc.UpdateOrderStatusAsync(input ?? new ShopOrderStatusInput(), App.HttpContext?.RequestAborted ?? default);

    // --- merged from ShopAdminAppService.WaveGaps.cs ---



    /// <summary>
    /// 对齐旧 add。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("add")]
    public Task<object?> Add(Dictionary<string, object?>? input = null) =>
        _svc.AddAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 batchdeleteimportpoint。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Batchdeleteimportpoint"), HttpPost]
    [DisplayName("batchdeleteimportpoint")]
    public Task<object?> Batchdeleteimportpoint(Dictionary<string, object?>? input = null) =>
        _svc.BatchdeleteimportpointAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 catedict。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Catedict"), HttpPost]
    [DisplayName("catedict")]
    public Task<object?> Catedict(Dictionary<string, object?>? input = null) =>
        _svc.CatedictAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 children。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Children"), HttpPost]
    [DisplayName("children")]
    public Task<object?> Children(Dictionary<string, object?>? input = null) =>
        _svc.ChildrenAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 createuserpoint。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Createuserpoint"), HttpPost]
    [DisplayName("createuserpoint")]
    public Task<object?> Createuserpoint(Dictionary<string, object?>? input = null) =>
        _svc.CreateuserpointAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 deleteimportpoint。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Deleteimportpoint"), HttpPost]
    [DisplayName("deleteimportpoint")]
    public Task<object?> Deleteimportpoint(Dictionary<string, object?>? input = null) =>
        _svc.DeleteimportpointAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 editimportpoint。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Editimportpoint"), HttpPost]
    [DisplayName("editimportpoint")]
    public Task<object?> Editimportpoint(Dictionary<string, object?>? input = null) =>
        _svc.EditimportpointAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 importpointlist。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Importpointlist"), HttpPost]
    [DisplayName("importpointlist")]
    public Task<object?> Importpointlist(Dictionary<string, object?>? input = null) =>
        _svc.ImportpointlistAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 info。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Info"), HttpPost]
    [DisplayName("info")]
    public Task<object?> Info(Dictionary<string, object?>? input = null) =>
        _svc.InfoAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 pointlog。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Pointlog"), HttpPost]
    [DisplayName("pointlog")]
    public Task<object?> Pointlog(Dictionary<string, object?>? input = null) =>
        _svc.PointlogAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 saveimportpoint。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Saveimportpoint"), HttpPost]
    [DisplayName("saveimportpoint")]
    public Task<object?> Saveimportpoint(Dictionary<string, object?>? input = null) =>
        _svc.SaveimportpointAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 savesort。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Savesort"), HttpPost]
    [DisplayName("savesort")]
    public Task<object?> Savesort(Dictionary<string, object?>? input = null) =>
        _svc.SavesortAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 treelist。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Treelist"), HttpPost]
    [DisplayName("treelist")]
    public Task<object?> Treelist(Dictionary<string, object?>? input = null) =>
        _svc.TreelistAsync(input, App.HttpContext?.RequestAborted ?? default);
}
