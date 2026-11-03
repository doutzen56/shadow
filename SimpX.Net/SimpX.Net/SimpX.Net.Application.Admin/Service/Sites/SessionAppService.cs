using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 后台会话（JWT）。对齐旧 /admin/info/* 缺口。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "adminSession", Order = 580, Description = "后台会话")]
public partial class SessionAppService : IDynamicApiController, ITransient
{
    private readonly AdminSessionService _svc;

    /// <summary>
    /// 注入。
    /// </summary>
    /// <param name="svc">服务</param>
    public SessionAppService(AdminSessionService svc) => _svc = svc;

    // --- merged from SessionAppService.WaveGaps.cs ---



    /// <summary>
    /// 对齐旧 channel。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Channel"), HttpPost]
    [DisplayName("channel")]
    public Task<object?> Channel(Dictionary<string, object?>? input = null) =>
        _svc.ChannelAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 face。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Face"), HttpPost]
    [DisplayName("face")]
    public Task<object?> Face(Dictionary<string, object?>? input = null) =>
        _svc.FaceAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 get。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Get"), HttpPost]
    [DisplayName("get")]
    public Task<object?> Get(Dictionary<string, object?>? input = null) =>
        _svc.GetAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 getcode。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Getcode"), HttpPost]
    [DisplayName("getcode")]
    public Task<object?> Getcode(Dictionary<string, object?>? input = null) =>
        _svc.GetcodeAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 init。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Init"), HttpPost]
    [DisplayName("init")]
    public Task<object?> Init(Dictionary<string, object?>? input = null) =>
        _svc.InitAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 newnotify。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Newnotify"), HttpPost]
    [DisplayName("newnotify")]
    public Task<object?> Newnotify(Dictionary<string, object?>? input = null) =>
        _svc.NewnotifyAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 password。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Password"), HttpPost]
    [DisplayName("password")]
    public Task<object?> Password(Dictionary<string, object?>? input = null) =>
        _svc.PasswordAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 save。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Save"), HttpPost]
    [DisplayName("save")]
    public Task<object?> Save(Dictionary<string, object?>? input = null) =>
        _svc.SaveAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 savecode。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Savecode"), HttpPost]
    [DisplayName("savecode")]
    public Task<object?> Savecode(Dictionary<string, object?>? input = null) =>
        _svc.SavecodeAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 test。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Test"), HttpPost]
    [DisplayName("test")]
    public Task<object?> Test(Dictionary<string, object?>? input = null) =>
        _svc.TestAsync(input, App.HttpContext?.RequestAborted ?? default);
}
