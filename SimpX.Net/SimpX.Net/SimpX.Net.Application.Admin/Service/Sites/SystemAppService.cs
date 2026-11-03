using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 后台系统辅助（JWT）。对齐旧 /admin/system/* 缺口。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "adminSystem", Order = 580, Description = "后台系统辅助")]
public partial class SystemAppService : IDynamicApiController, ITransient
{
    private readonly AdminSystemService _svc;

    /// <summary>
    /// 注入。
    /// </summary>
    /// <param name="svc">服务</param>
    public SystemAppService(AdminSystemService svc) => _svc = svc;

    // --- merged from SystemAppService.WaveGaps.cs ---



    /// <summary>
    /// 对齐旧 billinfo。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Billinfo"), HttpPost]
    [DisplayName("billinfo")]
    public Task<object?> Billinfo(Dictionary<string, object?>? input = null) =>
        _svc.BillinfoAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 billlist。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Billlist"), HttpPost]
    [DisplayName("billlist")]
    public Task<object?> Billlist(Dictionary<string, object?>? input = null) =>
        _svc.BilllistAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 functioninfo。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Functioninfo"), HttpPost]
    [DisplayName("functioninfo")]
    public Task<object?> Functioninfo(Dictionary<string, object?>? input = null) =>
        _svc.FunctioninfoAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 kindupload。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Kindupload"), HttpPost]
    [DisplayName("kindupload")]
    public Task<object?> Kindupload(Dictionary<string, object?>? input = null) =>
        _svc.KinduploadAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 layupload。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Layupload"), HttpPost]
    [DisplayName("layupload")]
    public Task<object?> Layupload(Dictionary<string, object?>? input = null) =>
        _svc.LayuploadAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 updatefunction。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Updatefunction"), HttpPost]
    [DisplayName("updatefunction")]
    public Task<object?> Updatefunction(Dictionary<string, object?>? input = null) =>
        _svc.UpdatefunctionAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 updatenews。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Updatenews"), HttpPost]
    [DisplayName("updatenews")]
    public Task<object?> Updatenews(Dictionary<string, object?>? input = null) =>
        _svc.UpdatenewsAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 upload。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Upload"), HttpPost]
    [DisplayName("upload")]
    public Task<object?> Upload(Dictionary<string, object?>? input = null) =>
        _svc.UploadAsync(input, App.HttpContext?.RequestAborted ?? default);
}
