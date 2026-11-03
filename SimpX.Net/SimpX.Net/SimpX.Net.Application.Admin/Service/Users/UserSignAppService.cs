namespace SimpX.Net.Application.Admin;

/// <summary>
/// 租户后台签到记录（JWT）。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "userSign", Order = 490, Description = "签到记录")]
public partial class UserSignAppService : IDynamicApiController, ITransient
{
    private readonly UserSignService _svc;

    /// <summary>
    /// 注入。
    /// </summary>
    /// <param name="svc">签到</param>
    public UserSignAppService(UserSignService svc) => _svc = svc;

    /// <summary>分页。</summary>
    /// <param name="input">筛选</param>
    /// <returns>分页</returns>
    [ApiDescriptionSettings(Name = "Page"), HttpPost]
    [DisplayName("签到记录分页")]
    public Task<UserSignPageOutput> Page(UserSignPageInput input) =>
        _svc.PageAsync(input ?? new UserSignPageInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>手工补录（不发奖）。</summary>
    /// <param name="input">会员编号</param>
    /// <returns>编号</returns>
    [ApiDescriptionSettings(Name = "AddManual"), HttpPost]
    [DisplayName("签到手工补录")]
    public Task<int> AddManual(UserSignAddManualInput input) =>
        _svc.AddManualAsync(input?.UserID ?? 0, App.HttpContext?.RequestAborted ?? default);

    // --- merged from UserSignAppService.WaveGaps.cs ---



    /// <summary>
    /// 对齐旧 signinlog。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Signinlog"), HttpPost]
    [DisplayName("signinlog")]
    public Task<object?> Signinlog(Dictionary<string, object?>? input = null) =>
        _svc.SigninlogAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 signinlogreport。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Signinlogreport"), HttpPost]
    [DisplayName("signinlogreport")]
    public Task<object?> Signinlogreport(Dictionary<string, object?>? input = null) =>
        _svc.SigninlogreportAsync(input, App.HttpContext?.RequestAborted ?? default);
}

/// <summary>手工补签入参。</summary>
public sealed class UserSignAddManualInput
{
    /// <summary>会员编号。</summary>
    public int UserID { get; set; }
}
