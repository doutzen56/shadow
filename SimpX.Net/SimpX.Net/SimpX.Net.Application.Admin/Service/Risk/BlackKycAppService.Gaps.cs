namespace SimpX.Net.Application.Admin;

/// <summary>
/// 证件黑名单旧动作名。
/// </summary>
public partial class BlackKycAppService
{
    /// <summary>对齐旧 addbalckkyc。</summary>
    /// <param name="kyc">证件</param>
    /// <param name="demo">备注</param>
    /// <param name="isCheck">兼容</param>
    [ApiDescriptionSettings(Name = "AddBalckKyc"), HttpPost]
    [DisplayName("添加证件黑名单")]
    public Task AddBalckKyc([FromQuery] string? kyc, [FromQuery] string? demo = null, [FromQuery] bool isCheck = false) =>
        _svc.AddBalckKycAsync(kyc, demo, isCheck, App.HttpContext?.RequestAborted ?? default);

    /// <summary>对齐旧 createbalckkyc。</summary>
    /// <returns>说明</returns>
    [ApiDescriptionSettings(Name = "CreateBalckKyc"), HttpPost]
    [DisplayName("创建证件黑名单页")]
    public Task<string> CreateBalckKyc() => _svc.CreateBalckKycAsync();

    /// <summary>对齐旧 savebalckkyc。</summary>
    /// <param name="url">文件</param>
    [ApiDescriptionSettings(Name = "SaveBalckKyc"), HttpPost]
    [DisplayName("导入证件黑名单")]
    public Task SaveBalckKyc([FromQuery] string? url) => _svc.SaveBalckKycImportAsync(url);

    /// <summary>对齐旧 importbalckkyclist。</summary>
    /// <param name="userName">关键字</param>
    /// <returns>行</returns>
    [ApiDescriptionSettings(Name = "ImportBalckKycList"), HttpPost]
    [DisplayName("证件黑名单列表")]
    public Task<List<BlackKycListItemDto>> ImportBalckKycList([FromQuery] string? userName) =>
        _svc.ImportBalckKycListAsync(userName, App.HttpContext?.RequestAborted ?? default);

    /// <summary>对齐旧 editbalckkyc。</summary>
    /// <param name="input">入参</param>
    [ApiDescriptionSettings(Name = "EditBalckKyc"), HttpPost]
    [DisplayName("编辑证件黑名单")]
    public Task EditBalckKyc(BlackKycSaveInput input) =>
        _svc.EditBalckKycAsync(input ?? new BlackKycSaveInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>对齐旧 deletebalckkyc。</summary>
    /// <param name="id">编号</param>
    [ApiDescriptionSettings(Name = "DeleteBalckKyc"), HttpPost]
    [DisplayName("删除证件黑名单")]
    public Task DeleteBalckKyc([FromQuery] int id) =>
        _svc.DeleteBalckKycAsync(id, App.HttpContext?.RequestAborted ?? default);

    /// <summary>对齐旧 batchdeletebalckkyc。</summary>
    /// <param name="ids">编号</param>
    [ApiDescriptionSettings(Name = "BatchDeleteBalckKyc"), HttpPost]
    [DisplayName("批量删除证件黑名单")]
    public Task BatchDeleteBalckKyc([FromQuery] int[]? ids) =>
        _svc.BatchDeleteBalckKycAsync(ids, App.HttpContext?.RequestAborted ?? default);

    /// <summary>对齐旧 checkblackkyc。</summary>
    /// <param name="blackKyc">证件</param>
    /// <returns>是否命中</returns>
    [ApiDescriptionSettings(Name = "CheckBlackKyc"), HttpPost]
    [DisplayName("检查证件黑名单")]
    public Task<bool> CheckBlackKyc([FromQuery] string? blackKyc) =>
        _svc.CheckBlackKycAsync(blackKyc, App.HttpContext?.RequestAborted ?? default);
}
