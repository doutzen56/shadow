namespace SimpX.Net.Application.Admin;

/// <summary>
/// 用户表格列自定义（对齐 Pro SysColumnCustom，供 vxe 记住列宽/显隐等）。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "columnCustom", Order = 245, Description = "表格列自定义")]
public class ColumnCustomAppService : IDynamicApiController, ITransient
{
    private readonly SqlSugarRepository<SysColumnCustom> _rep;
    private readonly UserManager _userManager;
    private readonly ICacheStateStore _state;

    /// <summary>
    /// 构造表格列自定义服务。
    /// </summary>
    /// <param name="rep">列配置仓储</param>
    /// <param name="userManager">当前用户</param>
    /// <param name="state">状态缓存</param>
    public ColumnCustomAppService(
        SqlSugarRepository<SysColumnCustom> rep,
        UserManager userManager,
        ICacheStateStore state)
    {
        _rep = rep;
        _userManager = userManager;
        _state = state;
    }

    /// <summary>
    /// 获取当前用户某张表的列配置；没有则返回空。
    /// </summary>
    /// <param name="input">表格 Id</param>
    /// <returns>列配置；未保存过为 null</returns>
    [SuppressMonitor]
    [DisplayName("获取用户表格列配置")]
    public async Task<ColumnCustomOutput?> GetDetail([FromQuery] GetColumnCustomInput input)
    {
        var cacheId = $"{_userManager.UserId}:{input.GridId}";
        var cached = _state.Get<ColumnCustomOutput>(CommonCacheKeys.ColumnCustom, cacheId);
        if (cached != null) return cached;

        var temp = await _rep.GetFirstAsync(e => e.UserId == _userManager.UserId && e.GridId == input.GridId);
        if (temp == null) return null;

        var result = new ColumnCustomOutput
        {
            UserId = temp.UserId,
            GridId = temp.GridId,
            FixedData = string.IsNullOrEmpty(temp.FixedData) ? null : JSON.Deserialize<Dictionary<string, string>>(temp.FixedData),
            ResizableData = string.IsNullOrEmpty(temp.ResizableData) ? null : JSON.Deserialize<Dictionary<string, int>>(temp.ResizableData),
            SortData = string.IsNullOrEmpty(temp.SortData) ? null : JSON.Deserialize<Dictionary<string, int>>(temp.SortData),
            VisibleData = string.IsNullOrEmpty(temp.VisibleData) ? null : JSON.Deserialize<Dictionary<string, bool>>(temp.VisibleData),
        };
        _state.Add(CommonCacheKeys.ColumnCustom, cacheId, result, TimeSpan.FromDays(7));
        return result;
    }

    /// <summary>
    /// 保存当前用户某张表的列配置。
    /// </summary>
    /// <param name="input">列状态</param>
    [DisplayName("保存用户表格列配置")]
    public async Task Store(StoreColumnCustomInput input)
    {
        var temp = await _rep.GetFirstAsync(e => e.UserId == _userManager.UserId && e.GridId == input.GridId);
        if (temp is null)
            temp = new SysColumnCustom { UserId = _userManager.UserId, GridId = input.GridId };
        else
            _state.Remove(CommonCacheKeys.ColumnCustom, $"{_userManager.UserId}:{input.GridId}");

        temp.FixedData = JSON.Serialize(input.FixedData);
        temp.ResizableData = JSON.Serialize(input.ResizableData);
        temp.SortData = JSON.Serialize(input.SortData);
        temp.VisibleData = JSON.Serialize(input.VisibleData);
        await _rep.Context.Storageable(temp).ExecuteCommandAsync();
    }

    /// <summary>
    /// 清除当前用户某张表的列配置。
    /// </summary>
    /// <param name="input">表格 Id</param>
    [DisplayName("清除用户表格列配置")]
    public async Task Reset(ResetColumnCustomInput input)
    {
        await _rep.AsDeleteable().Where(e => e.UserId == _userManager.UserId && e.GridId == input.GridId).ExecuteCommandAsync();
        _state.Remove(CommonCacheKeys.ColumnCustom, $"{_userManager.UserId}:{input.GridId}");
    }
}
