using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 租户后台 VIP 申请资格（JWT）。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "siteVipCondition", Order = 533, Description = "VIP申请资格")]
public class SiteVipConditionAppService : IDynamicApiController, ITransient
{
    private readonly SiteVipConditionService _svc;

    /// <summary>
    /// 注入。
    /// </summary>
    /// <param name="svc">VIP 资格</param>
    public SiteVipConditionAppService(SiteVipConditionService svc) => _svc = svc;

    /// <summary>读取。</summary>
    /// <returns>配置</returns>
    [ApiDescriptionSettings(Name = "Get"), HttpPost]
    [DisplayName("VIP资格读取")]
    public Task<SiteVipConditionDto> Get() =>
        _svc.GetAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>保存。</summary>
    /// <param name="input">表单</param>
    [ApiDescriptionSettings(Name = "Save"), HttpPost]
    [DisplayName("VIP资格保存")]
    public Task Save(SiteVipConditionSaveInput input) =>
        _svc.SaveAsync(input ?? new SiteVipConditionSaveInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>等级列表。对齐 levels。</summary>
    [ApiDescriptionSettings(Name = "Levels"), HttpPost]
    [DisplayName("VIP等级列表")]
    public Task<List<SiteVipLevelItemDto>> Levels() =>
        _svc.ListLevelsAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>等级简表。对齐 getlevels。</summary>
    [ApiDescriptionSettings(Name = "GetLevels"), HttpPost]
    [DisplayName("VIP等级简表")]
    public Task<List<SiteVipLevelItemDto>> GetLevels() =>
        _svc.ListLevelsAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>等级详情。对齐 levelinfo。</summary>
    /// <param name="id">Level</param>
    [ApiDescriptionSettings(Name = "LevelInfo"), HttpPost]
    [DisplayName("VIP等级详情")]
    public Task<SiteVipLevelItemDto> LevelInfo(int id) =>
        _svc.LevelInfoAsync(id, App.HttpContext?.RequestAborted ?? default);

    /// <summary>保存等级。对齐 savelevel。</summary>
    /// <param name="input">表单</param>
    [ApiDescriptionSettings(Name = "SaveLevel"), HttpPost]
    [DisplayName("保存VIP等级")]
    public Task SaveLevel(SiteVipLevelSaveInput input) =>
        _svc.SaveLevelAsync(input ?? new SiteVipLevelSaveInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>删除等级。对齐 deletelevel。</summary>
    /// <param name="level">等级</param>
    [ApiDescriptionSettings(Name = "DeleteLevel"), HttpPost]
    [DisplayName("删除VIP等级")]
    public Task DeleteLevel(byte level) =>
        _svc.DeleteLevelAsync(level, App.HttpContext?.RequestAborted ?? default);

    /// <summary>特权列表。对齐 rights。</summary>
    [ApiDescriptionSettings(Name = "Rights"), HttpPost]
    [DisplayName("VIP特权列表")]
    public Task<object> Rights() =>
        _svc.RightsAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>特权详情。对齐 rightinfo。</summary>
    /// <param name="id">特权 ID</param>
    [ApiDescriptionSettings(Name = "RightInfo"), HttpPost]
    [DisplayName("VIP特权详情")]
    public Task<object> RightInfo([FromQuery] string id) =>
        _svc.RightInfoAsync(id, App.HttpContext?.RequestAborted ?? default);

    /// <summary>保存特权原文。对齐 saveright。</summary>
    /// <param name="input">原文</param>
    [ApiDescriptionSettings(Name = "SaveRight"), HttpPost]
    [DisplayName("保存VIP特权")]
    public Task SaveRight(SiteVipRightSaveInput input) =>
        _svc.SaveRightAsync(input ?? new SiteVipRightSaveInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>初始化特权。对齐 initvipright。</summary>
    [ApiDescriptionSettings(Name = "InitVipRight"), HttpPost]
    [DisplayName("初始化VIP特权")]
    public Task InitVipRight() =>
        _svc.InitVipRightAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>清特权缓存。对齐 clearviprightcache。</summary>
    [ApiDescriptionSettings(Name = "ClearVipRightCache"), HttpPost]
    [DisplayName("清除VIP特权缓存")]
    public Task ClearVipRightCache() =>
        _svc.InitVipRightAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>删除特权。对齐 deleteright。</summary>
    /// <param name="id">编号</param>
    [ApiDescriptionSettings(Name = "DeleteRight"), HttpPost]
    [DisplayName("删除VIP特权")]
    public Task DeleteRight(int id) =>
        _svc.DeleteRightAsync(id, App.HttpContext?.RequestAborted ?? default);

    /// <summary>升级日志。对齐 upgradelog。</summary>
    /// <param name="input">筛选</param>
    [ApiDescriptionSettings(Name = "UpgradeLog"), HttpPost]
    [DisplayName("VIP升级日志")]
    public Task<SiteVipUpgradeLogPageOutput> UpgradeLog(SiteVipUpgradeLogPageInput input) =>
        _svc.PageUpgradeLogAsync(input ?? new SiteVipUpgradeLogPageInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>升级日志别名。对齐 upgradelog1。</summary>
    /// <param name="input">筛选</param>
    [ApiDescriptionSettings(Name = "UpgradeLog1"), HttpPost]
    [DisplayName("VIP升级日志1")]
    public Task<SiteVipUpgradeLogPageOutput> UpgradeLog1(SiteVipUpgradeLogPageInput input) =>
        _svc.PageUpgradeLogAsync(input ?? new SiteVipUpgradeLogPageInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>导出升级日志。对齐 exportupgrade。</summary>
    /// <param name="input">筛选</param>
    [ApiDescriptionSettings(Name = "ExportUpgrade"), HttpPost]
    [DisplayName("导出VIP升级日志")]
    public Task<List<SiteVipUpgradeLogItemDto>> ExportUpgrade(SiteVipUpgradeLogPageInput input) =>
        _svc.ExportUpgradeAsync(input ?? new SiteVipUpgradeLogPageInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>批量设会员等级。对齐 setuserslevel。</summary>
    /// <param name="input">等级与用户名</param>
    [ApiDescriptionSettings(Name = "SetUsersLevel"), HttpPost]
    [DisplayName("批量设置会员VIP等级")]
    public Task<int> SetUsersLevel(SiteVipSetUsersLevelInput input) =>
        _svc.SetUsersLevelAsync(input ?? new SiteVipSetUsersLevelInput(), App.HttpContext?.RequestAborted ?? default);
}
