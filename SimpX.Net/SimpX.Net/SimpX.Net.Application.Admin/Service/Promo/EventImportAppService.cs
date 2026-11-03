using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 租户后台活动会员导入（JWT）。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "eventImport", Order = 541, Description = "活动会员导入")]
public class EventImportAppService : IDynamicApiController, ITransient
{
    private readonly EventImportAdminService _svc;

    /// <summary>
    /// 注入。
    /// </summary>
    /// <param name="svc">导入</param>
    public EventImportAppService(EventImportAdminService svc) => _svc = svc;

    /// <summary>分页。</summary>
    /// <param name="input">筛选</param>
    /// <returns>分页</returns>
    [ApiDescriptionSettings(Name = "Page"), HttpPost]
    [DisplayName("活动会员导入分页")]
    public Task<SimpleImportPageOutput> Page(SimpleImportPageInput input) =>
        _svc.PageAsync(input ?? new SimpleImportPageInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>批量追加。</summary>
    /// <param name="input">文本</param>
    /// <returns>条数</returns>
    [ApiDescriptionSettings(Name = "AddBatch"), HttpPost]
    [DisplayName("活动会员批量导入")]
    public Task<int> AddBatch(SimpleImportBatchInput input) =>
        _svc.AddBatchAsync(input ?? new SimpleImportBatchInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>删除。</summary>
    /// <param name="id">编号</param>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("活动会员导入删除")]
    public Task Delete(int id) =>
        _svc.DeleteAsync(id, App.HttpContext?.RequestAborted ?? default);
}

/// <summary>
/// 租户后台号码导入（JWT）。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "mobileImport", Order = 542, Description = "号码导入")]
public class MobileImportAppService : IDynamicApiController, ITransient
{
    private readonly MobileImportAdminService _svc;

    /// <summary>
    /// 注入。
    /// </summary>
    /// <param name="svc">导入</param>
    public MobileImportAppService(MobileImportAdminService svc) => _svc = svc;

    /// <summary>分页。</summary>
    /// <param name="input">筛选</param>
    /// <returns>分页</returns>
    [ApiDescriptionSettings(Name = "Page"), HttpPost]
    [DisplayName("号码导入分页")]
    public Task<SimpleImportPageOutput> Page(SimpleImportPageInput input) =>
        _svc.PageAsync(input ?? new SimpleImportPageInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>批量追加。</summary>
    /// <param name="input">文本</param>
    /// <returns>条数</returns>
    [ApiDescriptionSettings(Name = "AddBatch"), HttpPost]
    [DisplayName("号码批量导入")]
    public Task<int> AddBatch(SimpleImportBatchInput input) =>
        _svc.AddBatchAsync(input ?? new SimpleImportBatchInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>删除。</summary>
    /// <param name="id">编号</param>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("号码导入删除")]
    public Task Delete(int id) =>
        _svc.DeleteAsync(id, App.HttpContext?.RequestAborted ?? default);
}
