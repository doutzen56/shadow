namespace SimpX.Net.Application.Admin;

/// <summary>
/// 租户后台站点设置。业务字段对齐旧版 /admin/site/info/get|save；路由 /api/system/siteInfo/*。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "siteInfo", Order = 455, Description = "站点设置")]
public partial class SiteInfoAppService : IDynamicApiController, ITransient
{
    private readonly SiteInfoService _siteInfo;

    /// <summary>
    /// 注入站点设置服务。
    /// </summary>
    /// <param name="siteInfo">站点设置</param>
    public SiteInfoAppService(SiteInfoService siteInfo)
    {
        _siteInfo = siteInfo;
    }

    /// <summary>
    /// 获取当前站点设置。对齐旧版 site/info/get；路由显式挂 get，避免 Furion 把 Get 收成空路径。
    /// </summary>
    /// <returns>与旧版 info 同名字段</returns>
    [ApiDescriptionSettings(Name = "Get"), HttpGet("get"), HttpPost("get")]
    [DisplayName("获取当前站点设置")]
    public Task<SiteInfoGetOutput> Get() =>
        _siteInfo.GetAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 保存当前站点设置。对齐旧版 site/info/save；需 system:siteInfo:save。
    /// </summary>
    /// <param name="input">Name / Currency / Setting / PCHomeTemplateName</param>
    [ApiDescriptionSettings(Name = "Save"), HttpPost]
    [DisplayName("保存当前站点设置")]
    public Task Save(SiteInfoSaveInput input) =>
        _siteInfo.SaveAsync(input, App.HttpContext?.RequestAborted ?? default);

    /// <summary>查看 API 密钥。对齐 api。</summary>
    [ApiDescriptionSettings(Name = "Api"), HttpPost]
    [DisplayName("查看API密钥")]
    public Task<object> Api() =>
        _siteInfo.GetApiKeyAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>更新 API 密钥。对齐 updateapikey。</summary>
    [ApiDescriptionSettings(Name = "UpdateApiKey"), HttpPost]
    [DisplayName("更新API密钥")]
    public Task<object> UpdateApiKey() =>
        _siteInfo.UpdateApiKeyAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>刷新缓存。对齐 cache。</summary>
    /// <param name="key">site / user</param>
    [ApiDescriptionSettings(Name = "Cache"), HttpPost]
    [DisplayName("刷新站点缓存")]
    public Task Cache([FromQuery] string key) =>
        _siteInfo.ClearCacheAsync(key, App.HttpContext?.RequestAborted ?? default);

    /// <summary>在线人数。对齐 online。</summary>
    [ApiDescriptionSettings(Name = "Online"), HttpPost]
    [DisplayName("在线人数")]
    public Task<object> Online() =>
        _siteInfo.OnlineAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>后台域名。对齐 admindomain。</summary>
    [ApiDescriptionSettings(Name = "AdminDomain"), HttpPost]
    [DisplayName("后台域名")]
    public Task<object> AdminDomain() =>
        _siteInfo.AdminDomainAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>保存后台域名。对齐 saveadmindomain。</summary>
    /// <param name="adminDomain">域名后缀</param>
    [ApiDescriptionSettings(Name = "SaveAdminDomain"), HttpPost]
    [DisplayName("保存后台域名")]
    public Task SaveAdminDomain([FromQuery] string adminDomain) =>
        _siteInfo.SaveAdminDomainAsync(adminDomain, App.HttpContext?.RequestAborted ?? default);

    /// <summary>当前站列表。对齐 list。</summary>
    /// <param name="name">站名</param>
    /// <param name="type">状态</param>
    [ApiDescriptionSettings(Name = "List"), HttpPost]
    [DisplayName("站点列表")]
    public Task<List<object>> List([FromQuery] string name, [FromQuery] string type) =>
        _siteInfo.ListAsync(name, type, App.HttpContext?.RequestAborted ?? default);

    // --- merged from SiteInfoAppService.WaveGaps.cs ---



    /// <summary>
    /// 对齐旧 appinfo。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Appinfo"), HttpPost]
    [DisplayName("appinfo")]
    public Task<object?> Appinfo(Dictionary<string, object?>? input = null) =>
        _siteInfo.AppinfoAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 moneytype。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Moneytype"), HttpPost]
    [DisplayName("moneytype")]
    public Task<object?> Moneytype(Dictionary<string, object?>? input = null) =>
        _siteInfo.MoneytypeAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 upload。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Upload"), HttpPost]
    [DisplayName("upload")]
    public Task<object?> Upload(Dictionary<string, object?>? input = null) =>
        _siteInfo.UploadAsync(input, App.HttpContext?.RequestAborted ?? default);
}
