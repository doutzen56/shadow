using Furion.UnifyResult;

namespace SimpX.Net.Application.SuperAdmin;

/// <summary>
/// 总后台菜单兼容路由。
/// </summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "betwin-system-menu", Order = 21)]
public class SystemMenuAppService : IDynamicApiController, ITransient
{
    private readonly SystemAdminMenuService _menus;

    /// <summary>
    /// 注入总后台菜单服务。
    /// </summary>
    /// <param name="menus">总后台菜单</param>
    public SystemMenuAppService(SystemAdminMenuService menus)
    {
        _menus = menus;
    }

    /// <summary>
    /// 总后台菜单。对应旧版 /system/config/account/menu。
    /// </summary>
    /// <returns>旧版返回格式</returns>
    [AllowAnonymous]
    [HttpPost("/system/config/account/menu")]
    [HttpGet("/system/config/account/menu")]
    [NonUnify]
    [DisplayName("总后台菜单")]
    public Task<OldApiResult> Menu() =>
        _menus.GetMenuAsync(App.HttpContext?.RequestAborted ?? default);
}
