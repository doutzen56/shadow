namespace SimpX.Net.Application.Admin;

/// <summary>
/// 手机黑名单旧动作名。
/// </summary>
public partial class BlackMobileAppService
{
    /// <summary>对齐旧 addbalckmobile。</summary>
    /// <param name="mobile">手机号</param>
    /// <param name="demo">备注</param>
    [ApiDescriptionSettings(Name = "AddBalckMobile"), HttpPost]
    [DisplayName("添加手机黑名单")]
    public Task AddBalckMobile([FromQuery] string? mobile, [FromQuery] string? demo = null) =>
        _svc.AddBalckMobileAsync(mobile, demo, App.HttpContext?.RequestAborted ?? default);

    /// <summary>对齐旧 createbalckmobile。</summary>
    /// <returns>说明</returns>
    [ApiDescriptionSettings(Name = "CreateBalckMobile"), HttpPost]
    [DisplayName("创建手机黑名单页")]
    public Task<string> CreateBalckMobile() => _svc.CreateBalckMobileAsync();

    /// <summary>对齐旧 savebalckmobile。</summary>
    /// <param name="url">文件</param>
    [ApiDescriptionSettings(Name = "SaveBalckMobile"), HttpPost]
    [DisplayName("导入手机黑名单")]
    public Task SaveBalckMobile([FromQuery] string? url) => _svc.SaveBalckMobileImportAsync(url);

    /// <summary>对齐旧 importbalckmobilelist。</summary>
    /// <param name="userName">关键字</param>
    /// <returns>行</returns>
    [ApiDescriptionSettings(Name = "ImportBalckMobileList"), HttpPost]
    [DisplayName("手机黑名单导入列表")]
    public Task<List<BlackMobileListItemDto>> ImportBalckMobileList([FromQuery] string? userName) =>
        _svc.ImportBalckMobileListAsync(userName, App.HttpContext?.RequestAborted ?? default);

    /// <summary>对齐旧 editbalckmobile。</summary>
    /// <param name="input">入参</param>
    [ApiDescriptionSettings(Name = "EditBalckMobile"), HttpPost]
    [DisplayName("编辑手机黑名单")]
    public Task EditBalckMobile(BlackMobileSaveInput input) =>
        _svc.EditBalckMobileAsync(input ?? new BlackMobileSaveInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>对齐旧 deletebalckmobile。</summary>
    /// <param name="id">编号</param>
    [ApiDescriptionSettings(Name = "DeleteBalckMobile"), HttpPost]
    [DisplayName("删除手机黑名单")]
    public Task DeleteBalckMobile([FromQuery] int id) =>
        _svc.DeleteBalckMobileAsync(id, App.HttpContext?.RequestAborted ?? default);

    /// <summary>对齐旧 batchdeletebalckmobile。</summary>
    /// <param name="ids">编号</param>
    [ApiDescriptionSettings(Name = "BatchDeleteBalckMobile"), HttpPost]
    [DisplayName("批量删除手机黑名单")]
    public Task BatchDeleteBalckMobile([FromQuery] int[]? ids) =>
        _svc.BatchDeleteBalckMobileAsync(ids, App.HttpContext?.RequestAborted ?? default);

    /// <summary>对齐旧 batchlockbalckmobile。</summary>
    /// <param name="ids">编号</param>
    [ApiDescriptionSettings(Name = "BatchLockBalckMobile"), HttpPost]
    [DisplayName("批量禁止手机黑名单")]
    public Task BatchLockBalckMobile([FromQuery] int[]? ids) =>
        _svc.BatchLockBalckMobileAsync(ids, App.HttpContext?.RequestAborted ?? default);
}
