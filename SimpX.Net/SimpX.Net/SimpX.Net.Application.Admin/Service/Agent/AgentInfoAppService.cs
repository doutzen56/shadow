namespace SimpX.Net.Application.Admin;

/// <summary>
/// 租户后台代理信息（JWT）。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "agentInfo", Order = 486, Description = "代理信息")]
public partial class AgentInfoAppService : IDynamicApiController, ITransient
{
    private readonly AgentInfoService _svc;

    /// <summary>
    /// 注入。
    /// </summary>
    /// <param name="svc">代理信息</param>
    public AgentInfoAppService(AgentInfoService svc) => _svc = svc;

    /// <summary>分页。</summary>
    /// <param name="input">筛选</param>
    /// <returns>分页</returns>
    [ApiDescriptionSettings(Name = "Page"), HttpPost]
    [DisplayName("代理信息分页")]
    public Task<AgentInfoPageOutput> Page(AgentInfoPageInput input) =>
        _svc.PageAsync(input ?? new AgentInfoPageInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>详情。</summary>
    /// <param name="id">编号</param>
    /// <returns>详情</returns>
    [ApiDescriptionSettings(Name = "Detail"), HttpGet]
    [DisplayName("代理信息详情")]
    public Task<AgentInfoDetailDto> Detail([FromQuery] int id) =>
        _svc.DetailAsync(id, App.HttpContext?.RequestAborted ?? default);

    /// <summary>保存。</summary>
    /// <param name="input">表单</param>
    [ApiDescriptionSettings(Name = "Save"), HttpPost]
    [DisplayName("代理信息保存")]
    public Task Save(AgentInfoSaveInput input) =>
        _svc.SaveAsync(input ?? new AgentInfoSaveInput(), App.HttpContext?.RequestAborted ?? default);

    // --- merged from AgentInfoAppService.WaveGaps.cs ---



    /// <summary>
    /// 对齐旧 add。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("add")]
    public Task<object?> Add(Dictionary<string, object?>? input = null) =>
        _svc.AddAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 agentlist。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Agentlist"), HttpPost]
    [DisplayName("agentlist")]
    public Task<object?> Agentlist(Dictionary<string, object?>? input = null) =>
        _svc.AgentlistAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 applylist。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Applylist"), HttpPost]
    [DisplayName("applylist")]
    public Task<object?> Applylist(Dictionary<string, object?>? input = null) =>
        _svc.ApplylistAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 check。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Check"), HttpPost]
    [DisplayName("check")]
    public Task<object?> Check(Dictionary<string, object?>? input = null) =>
        _svc.CheckAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 delete。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("delete")]
    public Task<object?> Delete(Dictionary<string, object?>? input = null) =>
        _svc.DeleteAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 execapply。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Execapply"), HttpPost]
    [DisplayName("execapply")]
    public Task<object?> Execapply(Dictionary<string, object?>? input = null) =>
        _svc.ExecapplyAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 getblockgame。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Getblockgame"), HttpPost]
    [DisplayName("getblockgame")]
    public Task<object?> Getblockgame(Dictionary<string, object?>? input = null) =>
        _svc.GetblockgameAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 permission。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Permission"), HttpPost]
    [DisplayName("permission")]
    public Task<object?> Permission(Dictionary<string, object?>? input = null) =>
        _svc.PermissionAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 updateagent。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Updateagent"), HttpPost]
    [DisplayName("updateagent")]
    public Task<object?> Updateagent(Dictionary<string, object?>? input = null) =>
        _svc.UpdateagentAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 updatecommission。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Updatecommission"), HttpPost]
    [DisplayName("updatecommission")]
    public Task<object?> Updatecommission(Dictionary<string, object?>? input = null) =>
        _svc.UpdatecommissionAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 updateinviteopen。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Updateinviteopen"), HttpPost]
    [DisplayName("updateinviteopen")]
    public Task<object?> Updateinviteopen(Dictionary<string, object?>? input = null) =>
        _svc.UpdateinviteopenAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 updatereturn。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Updatereturn"), HttpPost]
    [DisplayName("updatereturn")]
    public Task<object?> Updatereturn(Dictionary<string, object?>? input = null) =>
        _svc.UpdatereturnAsync(input, App.HttpContext?.RequestAborted ?? default);
}
