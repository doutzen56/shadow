using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 租户后台 VIP 升级日志（JWT，只读）。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "vipUpgradeLog", Order = 540, Description = "VIP升级日志")]
public class VipUpgradeLogAppService : IDynamicApiController, ITransient
{
    private readonly VipUpgradeLogService _svc;

    /// <summary>
    /// 注入。
    /// </summary>
    /// <param name="svc">升级日志</param>
    public VipUpgradeLogAppService(VipUpgradeLogService svc) => _svc = svc;

    /// <summary>分页。</summary>
    /// <param name="input">筛选</param>
    /// <returns>分页</returns>
    [ApiDescriptionSettings(Name = "Page"), HttpPost]
    [DisplayName("VIP升级日志分页")]
    public Task<VipUpgradeLogPageOutput> Page(VipUpgradeLogPageInput input) =>
        _svc.PageAsync(input ?? new VipUpgradeLogPageInput(), App.HttpContext?.RequestAborted ?? default);
}
