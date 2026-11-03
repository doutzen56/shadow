namespace SimpX.Net.Application.Admin;

/// <summary>
/// 租户后台站内信息（JWT）。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "userMessage", Order = 488, Description = "站内信息")]
public partial class UserMessageAppService : IDynamicApiController, ITransient
{
    private readonly UserMessageService _svc;

    /// <summary>
    /// 注入。
    /// </summary>
    /// <param name="svc">站内信</param>
    public UserMessageAppService(UserMessageService svc) => _svc = svc;

    /// <summary>分页。</summary>
    /// <param name="input">筛选</param>
    /// <returns>分页</returns>
    [ApiDescriptionSettings(Name = "Page"), HttpPost]
    [DisplayName("站内信分页")]
    public Task<UserMessagePageOutput> Page(UserMessagePageInput input) =>
        _svc.PageAsync(input ?? new UserMessagePageInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>详情。</summary>
    /// <param name="id">编号</param>
    /// <returns>详情</returns>
    [ApiDescriptionSettings(Name = "Detail"), HttpGet]
    [DisplayName("站内信详情")]
    public Task<UserMessageDetailDto> Detail([FromQuery] int id) =>
        _svc.DetailAsync(id, App.HttpContext?.RequestAborted ?? default);

    /// <summary>发送。</summary>
    /// <param name="input">表单</param>
    /// <returns>编号</returns>
    [ApiDescriptionSettings(Name = "Send"), HttpPost]
    [DisplayName("站内信发送")]
    public Task<int> Send(UserMessageSendInput input) =>
        _svc.SendAsync(input ?? new UserMessageSendInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>删除。</summary>
    /// <param name="input">编号</param>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("站内信删除")]
    public Task Delete(UserMessageDeleteInput input) =>
        _svc.DeleteAsync(input?.ID ?? 0, App.HttpContext?.RequestAborted ?? default);

    // --- merged from UserMessageAppService.WaveGaps.cs ---



    /// <summary>
    /// 对齐旧 update。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("update")]
    public Task<object?> Update(Dictionary<string, object?>? input = null) =>
        _svc.UpdateAsync(input, App.HttpContext?.RequestAborted ?? default);
}

/// <summary>删除站内信入参。</summary>
public sealed class UserMessageDeleteInput
{
    /// <summary>编号。</summary>
    public int ID { get; set; }
}
