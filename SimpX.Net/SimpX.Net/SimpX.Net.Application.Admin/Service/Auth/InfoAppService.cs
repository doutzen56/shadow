using Furion.UnifyResult;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 站点后台兼容 info 兼容：菜单等。
/// </summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "info", Order = 21)]
public class InfoAppService : IDynamicApiController, ITransient
{
    private readonly SiteAdminMenuService _menus;

    /// <summary>
    /// 注入菜单服务。
    /// </summary>
    /// <param name="menus">站点后台菜单</param>
    public InfoAppService(SiteAdminMenuService menus)
    {
        _menus = menus;
    }

    /// <summary>
    /// 左侧菜单。对应旧版 POST/GET /admin/info/menu。未登录时由兼容鉴权返回 ErrorType=Login。
    /// </summary>
    /// <returns>旧版返回格式；info 是菜单 JSON 数组字符串</returns>
    [AllowAnonymous]
    [HttpPost("/admin/info/menu")]
    [HttpGet("/admin/info/menu")]
    [NonUnify]
    [DisplayName("站点后台菜单")]
    public Task<OldApiResult> Menu() =>
        _menus.GetMenuAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 权限探针（AUTH-04）：需要「会员列表」权限 GUID。
    /// </summary>
    /// <returns>旧版返回格式</returns>
    [AllowAnonymous]
    [OldApiPermission("1e7cc96020b843b988ed6df39024867e")]
    [HttpPost("/admin/info/pingpermission")]
    [HttpGet("/admin/info/pingpermission")]
    [NonUnify]
    [DisplayName("站点后台权限探针")]
    public OldApiResult PingPermission() => OldApiResult.Ok("ok");
}
