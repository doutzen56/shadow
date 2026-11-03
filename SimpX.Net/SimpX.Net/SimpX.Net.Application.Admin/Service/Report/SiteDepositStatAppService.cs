using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 租户后台存款统计（JWT，只读）。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "siteDepositStat", Order = 536, Description = "存款统计")]
public class SiteDepositStatAppService : IDynamicApiController, ITransient
{
    private readonly SiteDepositStatService _svc;

    /// <summary>
    /// 注入。
    /// </summary>
    /// <param name="svc">存款统计</param>
    public SiteDepositStatAppService(SiteDepositStatService svc) => _svc = svc;

    /// <summary>列表。</summary>
    /// <param name="input">筛选</param>
    /// <returns>行列表</returns>
    [ApiDescriptionSettings(Name = "List"), HttpPost]
    [DisplayName("存款统计列表")]
    public Task<List<SiteDepositStatItemDto>> List(SiteFundDailyListInput input) =>
        _svc.ListAsync(input ?? new SiteFundDailyListInput(), App.HttpContext?.RequestAborted ?? default);
}
