namespace SimpX.Net.Application.SuperAdmin;

/// <summary>
/// 总后台业务站点列表（JWT）。对齐旧 sitelist 字段；不按域名定站。
/// </summary>
[ApiDescriptionSettings(Module = "platform", Name = "bizSite", Order = 470, Description = "站点列表")]
public class PlatformBizSiteAppService : IDynamicApiController, ITransient
{
    private readonly SystemSiteService _sites;

    /// <summary>
    /// 注入站点查询。
    /// </summary>
    /// <param name="sites">站点</param>
    public PlatformBizSiteAppService(SystemSiteService sites) => _sites = sites;

    /// <summary>
    /// 列表。SiteID=0 返回多站。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <returns>行列表</returns>
    [ApiDescriptionSettings(Name = "List"), HttpPost]
    [DisplayName("总后台站点列表")]
    public Task<List<SiteListItemDto>> List(PlatformBizSiteListInput input)
    {
        input ??= new PlatformBizSiteListInput();
        int? status = null;
        if (!string.IsNullOrWhiteSpace(input.Status) && int.TryParse(input.Status.Trim(), out var st))
            status = st;
        return _sites.ListItemsAsync(input.SiteID, input.Name, status, App.HttpContext?.RequestAborted ?? default);
    }
}
