namespace SimpX.Net.Application.SuperAdmin;

/// <summary>
/// 总后台字典值管理（/api/platform/dictData）。
/// </summary>
[ApiDescriptionSettings(Module = "platform", Name = "dictData", Order = 51, Description = "系统字典值")]
public class PlatformDictDataAppService : IDynamicApiController, ITransient
{
    private readonly SqlSugarRepository<SysDictData> _sysDictDataRep;
    private readonly ISimpXCache _cache;

    /// <summary>
    /// 注入字典值仓储与缓存。
    /// </summary>
    public PlatformDictDataAppService(SqlSugarRepository<SysDictData> sysDictDataRep, ISimpXCache cache)
    {
        _sysDictDataRep = sysDictDataRep;
        _cache = cache;
    }

    /// <summary>字典值分页。</summary>
    [DisplayName("获取字典值分页列表")]
    public async Task<SqlSugarPagedList<SysDictData>> Page(PageDictDataInput input)
    {
        EnsurePlatformHost();
        return await _sysDictDataRep.AsQueryable()
            .WhereIF(input.DictTypeId > 0, u => u.DictTypeId == input.DictTypeId)
            .WhereIF(!string.IsNullOrWhiteSpace(input.Label), u => u.Label.Contains(input.Label))
            .WhereIF(!string.IsNullOrWhiteSpace(input.Code), u => u.Code.Contains(input.Code))
            .OrderBy(u => new { u.OrderNo, u.Code })
            .ToPagedListAsync(input.Page, input.PageSize);
    }

    /// <summary>按类型 Id 取字典值列表。</summary>
    [DisplayName("获取字典值列表")]
    public async Task<List<SysDictData>> GetList([FromQuery] GetDataDictDataInput input)
    {
        EnsurePlatformHost();
        return await GetDictDataListByDictTypeId(input.DictTypeId);
    }

    /// <summary>新增字典值。</summary>
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("增加字典值")]
    public async Task AddDictData(AddDictDataInput input)
    {
        EnsurePlatformHost();
        var isExist = await _sysDictDataRep.IsAnyAsync(u => u.DictTypeId == input.DictTypeId && (u.Value == input.Value || u.Code == input.Code));
        if (isExist) throw Oops.Oh(ErrorCodeEnum.D3002);
        await _sysDictDataRep.InsertAsync(input.Adapt<SysDictData>());
        await ClearDictCache(input.DictTypeId);
    }

    /// <summary>更新字典值。</summary>
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("更新字典值")]
    public async Task UpdateDictData(UpdateDictDataInput input)
    {
        EnsurePlatformHost();
        var isExist = await _sysDictDataRep.IsAnyAsync(u => u.Id != input.Id && u.DictTypeId == input.DictTypeId && (u.Value == input.Value || u.Code == input.Code));
        if (isExist) throw Oops.Oh(ErrorCodeEnum.D3002);
        await _sysDictDataRep.AsUpdateable(input.Adapt<SysDictData>()).IgnoreColumns(true).ExecuteCommandAsync();
        await ClearDictCache(input.DictTypeId);
    }

    /// <summary>删除字典值。</summary>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("删除字典值")]
    public async Task DeleteDictData(DeleteDictDataInput input)
    {
        EnsurePlatformHost();
        var dictData = await _sysDictDataRep.GetByIdAsync(input.Id) ?? throw Oops.Oh(ErrorCodeEnum.D3003);
        await _sysDictDataRep.DeleteAsync(dictData);
        await ClearDictCache(dictData.DictTypeId);
    }

    /// <summary>
    /// 按字典类型 Id 删除全部字典值（供删除类型时调用）。
    /// </summary>
    /// <param name="dictTypeId">字典类型 Id</param>
    [NonAction]
    public async Task DeleteDictData(long dictTypeId)
    {
        await _sysDictDataRep.DeleteAsync(u => u.DictTypeId == dictTypeId);
        await ClearDictCache(dictTypeId);
    }

    /// <summary>
    /// 按字典类型 Id 取启用相关字典值。
    /// </summary>
    /// <param name="dictTypeId">字典类型 Id</param>
    /// <returns>字典值列表</returns>
    [NonAction]
    public async Task<List<SysDictData>> GetDictDataListByDictTypeId(long dictTypeId)
    {
        return await _sysDictDataRep.AsQueryable()
            .Where(u => u.DictTypeId == dictTypeId)
            .OrderBy(u => new { u.OrderNo, u.Code })
            .ToListAsync();
    }

    /// <summary>改字典值状态。</summary>
    [DisplayName("修改字典值状态")]
    public async Task SetStatus(DictDataInput input)
    {
        EnsurePlatformHost();
        var dictData = await _sysDictDataRep.GetByIdAsync(input.Id) ?? throw Oops.Oh(ErrorCodeEnum.D3003);
        dictData.Status = input.Status;
        await _sysDictDataRep.AsUpdateable(dictData).UpdateColumns(u => new { u.Status }, true).ExecuteCommandAsync();
        await ClearDictCache(dictData.DictTypeId);
    }

    private async Task ClearDictCache(long dictTypeId)
    {
        var dictType = await _sysDictDataRep.Change<SysDictType>().GetByIdAsync(dictTypeId);
        if (dictType == null) return;
        _cache.Remove(DictCacheKeys.Dict, dictType.Code);
        _cache.Remove(DictCacheKeys.TenantDict, dictType.Code);
    }

    private static void EnsurePlatformHost()
    {
        if (!HostTenantMode.IsPlatformHost)
            throw Oops.Oh(ErrorCodeEnum.SA001);
    }
}
