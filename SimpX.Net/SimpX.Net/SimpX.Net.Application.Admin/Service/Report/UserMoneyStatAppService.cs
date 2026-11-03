using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 租户后台会员资金日统计（JWT，只读）。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "userMoneyStat", Order = 530, Description = "会员统计")]
public class UserMoneyStatAppService : IDynamicApiController, ITransient
{
    private readonly UserMoneyStatService _svc;

    /// <summary>
    /// 注入。
    /// </summary>
    /// <param name="svc">会员统计</param>
    public UserMoneyStatAppService(UserMoneyStatService svc) => _svc = svc;

    /// <summary>列表。</summary>
    /// <param name="input">筛选</param>
    /// <returns>行列表</returns>
    [ApiDescriptionSettings(Name = "List"), HttpPost]
    [DisplayName("会员统计列表")]
    public Task<List<UserMoneyStatItemDto>> List(UserMoneyStatListInput input) =>
        _svc.ListAsync(input ?? new UserMoneyStatListInput(), App.HttpContext?.RequestAborted ?? default);
}
