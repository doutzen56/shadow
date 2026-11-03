namespace SimpX.Net.Application.Admin;

/// <summary>
/// 租户后台新闻公告（JWT）。业务字段对齐旧版 /admin/site/news/*。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "siteNews", Order = 457, Description = "新闻公告")]
public partial class SiteNewsAppService : IDynamicApiController, ITransient
{
    private readonly SiteNewsService _news;

    /// <summary>
    /// 注入新闻服务。
    /// </summary>
    /// <param name="news">新闻</param>
    public SiteNewsAppService(SiteNewsService news)
    {
        _news = news;
    }

    /// <summary>
    /// 分页列表。对齐旧版 list。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <returns>分页</returns>
    [ApiDescriptionSettings(Name = "Page"), HttpPost]
    [DisplayName("新闻分页列表")]
    public Task<SiteNewsPageOutput> Page(SiteNewsPageInput input) =>
        _news.PageAsync(input, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 详情。对齐旧版 info。
    /// </summary>
    /// <param name="id">新闻编号</param>
    /// <returns>详情</returns>
    [ApiDescriptionSettings(Name = "Detail"), HttpGet]
    [DisplayName("新闻详情")]
    public Task<SiteNewsDetailDto> Detail([FromQuery] int id) =>
        _news.DetailAsync(id, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 保存。对齐旧版 save；需 system:siteNews:save。
    /// </summary>
    /// <param name="input">保存入参</param>
    [ApiDescriptionSettings(Name = "Save"), HttpPost]
    [DisplayName("保存新闻")]
    public Task Save(SiteNewsSaveInput input) =>
        _news.SaveAsync(input, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 删除。对齐旧版 delete；需 system:siteNews:delete。
    /// </summary>
    /// <param name="id">新闻编号</param>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("删除新闻")]
    public Task Delete([FromQuery] int id) =>
        _news.DeleteAsync(id, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 栏目列表。对齐旧版 columnlist。
    /// </summary>
    /// <returns>栏目</returns>
    [ApiDescriptionSettings(Name = "ColumnList"), HttpGet]
    [DisplayName("新闻栏目列表")]
    public Task<List<SiteNewsColumnItemDto>> ColumnList() =>
        _news.ColumnListAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 置顶/取消置顶。对齐旧版 updatesort；需 system:siteNews:save。
    /// </summary>
    /// <param name="id">新闻编号</param>
    /// <param name="isTop">是否置顶</param>
    [ApiDescriptionSettings(Name = "UpdateTop"), HttpPost]
    [DisplayName("新闻置顶")]
    public Task UpdateTop([FromQuery] int id, [FromQuery] bool isTop) =>
        _news.UpdateTopAsync(id, isTop, App.HttpContext?.RequestAborted ?? default);

    // --- merged from SiteNewsAppService.WaveGaps.cs ---



    /// <summary>
    /// 对齐旧 getcolumninfo。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Getcolumninfo"), HttpPost]
    [DisplayName("getcolumninfo")]
    public Task<object?> Getcolumninfo(Dictionary<string, object?>? input = null) =>
        _news.GetcolumninfoAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 updatesort。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Updatesort"), HttpPost]
    [DisplayName("updatesort")]
    public Task<object?> Updatesort(Dictionary<string, object?>? input = null) =>
        _news.UpdatesortAsync(input, App.HttpContext?.RequestAborted ?? default);
}
