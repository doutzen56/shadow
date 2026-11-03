namespace SimpX.Net.Application.Admin;

/// <summary>
/// 租户后台登录日志（JWT）。对齐旧版 /admin/report/user/loginlog。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "loginLog", Order = 472, Description = "登录日志")]
public class LoginLogAppService : IDynamicApiController, ITransient
{
    private readonly LoginLogService _svc;

    /// <summary>
    /// 注入服务。
    /// </summary>
    /// <param name="svc">服务</param>
    public LoginLogAppService(LoginLogService svc) => _svc = svc;

    /// <summary>
    /// 分页列表。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <returns>分页</returns>
    [ApiDescriptionSettings(Name = "Page"), HttpPost]
    [DisplayName("登录日志")]
    public Task<LoginLogAdminPageOutput> Page(LoginLogAdminPageInput input) =>
        _svc.PageAsync(input ?? new LoginLogAdminPageInput(), App.HttpContext?.RequestAborted ?? default);
}
