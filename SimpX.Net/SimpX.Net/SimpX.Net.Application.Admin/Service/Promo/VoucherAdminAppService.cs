using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// voucherAdmin 矩阵缺口旧动作名。
/// </summary>
public partial class VoucherAdminAppService
{

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
    /// 对齐旧 batchdelete。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Batchdelete"), HttpPost]
    [DisplayName("batchdelete")]
    public Task<object?> Batchdelete(Dictionary<string, object?>? input = null) =>
        _svc.BatchdeleteAsync(input, App.HttpContext?.RequestAborted ?? default);


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
    /// 对齐旧 del。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Del"), HttpPost]
    [DisplayName("del")]
    public Task<object?> Del(Dictionary<string, object?>? input = null) =>
        _svc.DelAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 delete。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("delete")]
    public Task<object?> Delete(Dictionary<string, object?>? input = null) =>
        _svc.DeleteAsync(input, App.HttpContext?.RequestAborted ?? default);


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
    /// 对齐旧 detail。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Detail"), HttpPost]
    [DisplayName("detail")]
    public Task<object?> Detail(Dictionary<string, object?>? input = null) =>
        _svc.DetailAsync(input, App.HttpContext?.RequestAborted ?? default);


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

}
