namespace SimpX.Net.Application.Admin;

/// <summary>
/// IP 黑名单旧动作名。
/// </summary>
public partial class IpBlackAppService
{
    /// <summary>对齐旧 blacklist。</summary>
    /// <returns>行</returns>
    [ApiDescriptionSettings(Name = "BlackList"), HttpPost]
    [DisplayName("IP黑名单列表别名")]
    public Task<List<IpBlackListItemDto>> BlackList() =>
        _svc.BlackListAsync(keyword: null, cancellationToken: App.HttpContext?.RequestAborted ?? default);

    /// <summary>对齐旧 blackinfo。</summary>
    /// <param name="id">编号</param>
    /// <returns>详情</returns>
    [ApiDescriptionSettings(Name = "BlackInfo"), HttpPost]
    [DisplayName("IP黑名单详情别名")]
    public Task<IpBlackListItemDto> BlackInfo([FromQuery] int id) =>
        _svc.BlackInfoAsync(id, App.HttpContext?.RequestAborted ?? default);

    /// <summary>对齐旧 editbalckip。</summary>
    /// <param name="id">编号</param>
    /// <returns>详情</returns>
    [ApiDescriptionSettings(Name = "EditBalckIp"), HttpPost]
    [DisplayName("编辑IP黑名单")]
    public Task<IpBlackListItemDto> EditBalckIp([FromQuery] int id) =>
        _svc.EditBalckIpAsync(id, App.HttpContext?.RequestAborted ?? default);

    /// <summary>对齐旧 saveblackip。</summary>
    /// <param name="input">入参</param>
    [ApiDescriptionSettings(Name = "SaveBlackIp"), HttpPost]
    [DisplayName("保存IP黑名单别名")]
    public Task SaveBlackIp(IpBlackSaveInput input) =>
        _svc.SaveBlackIpAsync(input ?? new IpBlackSaveInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>对齐旧 deleteblackip。</summary>
    /// <param name="id">编号</param>
    [ApiDescriptionSettings(Name = "DeleteBlackIp"), HttpPost]
    [DisplayName("删除IP黑名单别名")]
    public Task DeleteBlackIp([FromQuery] int id) =>
        _svc.DeleteBlackIpAsync(id, App.HttpContext?.RequestAborted ?? default);

    /// <summary>对齐旧 batchsaveblackip。</summary>
    /// <param name="items">行</param>
    /// <param name="url">文件</param>
    [ApiDescriptionSettings(Name = "BatchSaveBlackIp"), HttpPost]
    [DisplayName("批量保存IP黑名单")]
    public Task BatchSaveBlackIp(List<IpBlackSaveInput>? items, [FromQuery] string? url = null) =>
        _svc.BatchSaveBlackIpAsync(items, url, App.HttpContext?.RequestAborted ?? default);
}
