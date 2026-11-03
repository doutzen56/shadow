namespace SimpX.Net.Application.Admin;

/// <summary>
/// 租户后台会员帐变（JWT）。对齐旧版 /admin/report/money/log。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "moneyLog", Order = 460, Description = "会员帐变")]
public class MoneyLogAppService : IDynamicApiController, ITransient
{
    private readonly MoneyLogService _svc;

    /// <summary>
    /// 注入服务。
    /// </summary>
    /// <param name="svc">服务</param>
    public MoneyLogAppService(MoneyLogService svc)
    {
        _svc = svc;
    }

    /// <summary>
    /// 分页列表。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <returns>分页与合计</returns>
    [ApiDescriptionSettings(Name = "Page"), HttpPost]
    [DisplayName("会员帐变分页")]
    public Task<MoneyLogAdminPageOutput> Page(MoneyLogAdminPageInput input) =>
        _svc.PageAsync(input, App.HttpContext?.RequestAborted ?? default);
}
