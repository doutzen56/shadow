namespace SimpX.Net.Application.Admin;

/// <summary>
/// 租户后台会员分组（JWT）。对齐旧版 list|info|save|delete。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "userGroup", Order = 469, Description = "会员分组")]
public class UserGroupAppService : IDynamicApiController, ITransient
{
    private readonly UserGroupService _svc;

    /// <summary>
    /// 注入会员分组服务。
    /// </summary>
    /// <param name="svc">会员分组服务</param>
    public UserGroupAppService(UserGroupService svc) => _svc = svc;

    /// <summary>
    /// 列表。
    /// </summary>
    /// <returns>行列表</returns>
    [ApiDescriptionSettings(Name = "List"), HttpPost]
    [DisplayName("会员分组列表")]
    public Task<List<UserGroupListItemDto>> List() =>
        _svc.ListAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 详情。
    /// </summary>
    /// <param name="id">编号</param>
    /// <returns>详情</returns>
    [ApiDescriptionSettings(Name = "Info"), HttpPost]
    [DisplayName("会员分组详情")]
    public Task<UserGroupDetailDto> Info([FromQuery] int id) =>
        _svc.InfoAsync(id, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 保存。
    /// </summary>
    /// <param name="input">入参</param>
    [ApiDescriptionSettings(Name = "Save"), HttpPost]
    [DisplayName("保存会员分组")]
    public Task Save(UserGroupSaveInput input) =>
        _svc.SaveAsync(input ?? new UserGroupSaveInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 删除。
    /// </summary>
    /// <param name="id">编号</param>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("删除会员分组")]
    public Task Delete([FromQuery] int id) =>
        _svc.DeleteAsync(id, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 批量改组。对齐旧 batchedit。
    /// </summary>
    /// <param name="user">用户名逗号串</param>
    /// <param name="groupId">分组</param>
    [ApiDescriptionSettings(Name = "BatchEdit"), HttpPost]
    [DisplayName("批量改会员分组")]
    public Task BatchEdit([FromQuery] string user, [FromQuery] int groupId) =>
        _svc.BatchEditAsync(user, groupId, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 刷新组内人数。对齐旧 updatemember。
    /// </summary>
    /// <param name="id">分组</param>
    /// <returns>人数</returns>
    [ApiDescriptionSettings(Name = "UpdateMember"), HttpPost]
    [DisplayName("刷新分组人数")]
    public async Task<object> UpdateMember([FromQuery] int id)
    {
        var member = await _svc.UpdateMemberAsync(id, App.HttpContext?.RequestAborted ?? default);
        return new { Member = member };
    }

    /// <summary>
    /// 设默认分组。对齐旧 updategroupisdefault。
    /// </summary>
    /// <param name="id">分组</param>
    [ApiDescriptionSettings(Name = "UpdateGroupIsDefault"), HttpPost]
    [DisplayName("设默认会员分组")]
    public Task UpdateGroupIsDefault([FromQuery] int id) =>
        _svc.UpdateGroupIsDefaultAsync(id, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 保存排序。对齐旧 savesort。
    /// </summary>
    /// <param name="input">编号数组</param>
    [ApiDescriptionSettings(Name = "SaveSort"), HttpPost]
    [DisplayName("会员分组排序")]
    public Task SaveSort(UserGroupSaveSortInput input) =>
        _svc.SaveSortAsync(input?.ID ?? [], App.HttpContext?.RequestAborted ?? default);
}

/// <summary>分组排序入参（旧 ID 数组）。</summary>
public sealed class UserGroupSaveSortInput
{
    /// <summary>分组编号顺序。</summary>
    public int[] ID { get; set; }
}
