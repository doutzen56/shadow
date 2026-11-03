namespace SimpX.Net.Application.Admin;

/// <summary>
/// 租户后台会员日志（JWT）。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "userLog", Order = 492, Description = "会员日志")]
public class UserLogAppService : IDynamicApiController, ITransient
{
    private readonly UserLogQueryService _svc;

    /// <summary>
    /// 注入。
    /// </summary>
    /// <param name="svc">会员日志</param>
    public UserLogAppService(UserLogQueryService svc) => _svc = svc;

    /// <summary>分页。</summary>
    /// <param name="input">筛选</param>
    /// <returns>分页</returns>
    [ApiDescriptionSettings(Name = "Page"), HttpPost]
    [DisplayName("会员日志分页")]
    public Task<UserLogPageOutput> Page(UserLogPageInput input) =>
        _svc.PageAsync(input ?? new UserLogPageInput(), App.HttpContext?.RequestAborted ?? default);
}
