namespace SimpX.Net.Application.SuperAdmin;

/// <summary>
/// 总后台字典类型管理（/api/platform/dictType）。
/// </summary>
[ApiDescriptionSettings(Module = "platform", Name = "dictType", Order = 50, Description = "系统字典类型")]
public class PlatformDictTypeAppService : IDynamicApiController, ITransient
{
    private readonly SqlSugarRepository<SysDictType> _sysDictTypeRep;
    private readonly PlatformDictDataAppService _dictDataAppService;
    private readonly ISimpXCache _cache;

    /// <summary>
    /// 注入字典类型仓储、字典值服务与缓存。
    /// </summary>
    public PlatformDictTypeAppService(
        SqlSugarRepository<SysDictType> sysDictTypeRep,
        PlatformDictDataAppService dictDataAppService,
        ISimpXCache cache)
    {
        _sysDictTypeRep = sysDictTypeRep;
        _dictDataAppService = dictDataAppService;
        _cache = cache;
    }

    /// <summary>字典类型分页。</summary>
    [DisplayName("获取字典类型分页列表")]
    public async Task<SqlSugarPagedList<SysDictType>> Page(PageDictTypeInput input)
    {
        EnsurePlatformHost();
        return await _sysDictTypeRep.AsQueryable()
            .WhereIF(!string.IsNullOrEmpty(input.Code?.Trim()), u => u.Code.Contains(input.Code))
            .WhereIF(!string.IsNullOrEmpty(input.Name?.Trim()), u => u.Name.Contains(input.Name))
            .OrderBy(u => new { u.OrderNo, u.Code })
            .ToPagedListAsync(input.Page, input.PageSize);
    }

    /// <summary>字典类型列表。</summary>
    [DisplayName("获取字典类型列表")]
    public async Task<List<SysDictType>> GetList()
    {
        EnsurePlatformHost();
        return await _sysDictTypeRep.AsQueryable().OrderBy(u => new { u.OrderNo, u.Code }).ToListAsync();
    }

    /// <summary>按编码取字典值。</summary>
    [DisplayName("获取字典类型-值列表")]
    public async Task<List<SysDictData>> GetDataList([FromQuery] GetDataDictTypeInput input)
    {
        EnsurePlatformHost();
        var dictType = await _sysDictTypeRep.GetFirstAsync(u => u.Code == input.Code)
            ?? throw Oops.Oh(ErrorCodeEnum.D3000);
        return await _dictDataAppService.GetDictDataListByDictTypeId(dictType.Id);
    }

    /// <summary>新增字典类型。</summary>
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("添加字典类型")]
    public async Task AddDictType(AddDictTypeInput input)
    {
        EnsurePlatformHost();
        if (input.Code.ToLower().EndsWith("enum")) throw Oops.Oh(ErrorCodeEnum.D3006);
        var isExist = await _sysDictTypeRep.IsAnyAsync(u => u.Code == input.Code);
        if (isExist) throw Oops.Oh(ErrorCodeEnum.D3001);
        input.IsTenant = YesNoEnum.N;
        await _sysDictTypeRep.InsertAsync(input.Adapt<SysDictType>());
    }

    /// <summary>更新字典类型。</summary>
    [UnitOfWork]
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("更新字典类型")]
    public async Task UpdateDictType(UpdateDictTypeInput input)
    {
        EnsurePlatformHost();
        var dict = await _sysDictTypeRep.GetFirstAsync(x => x.Id == input.Id) ?? throw Oops.Oh(ErrorCodeEnum.D3000);
        if (dict.IsTenant != input.IsTenant) throw Oops.Oh(ErrorCodeEnum.D3012);
        if (dict.Code.ToLower().EndsWith("enum") && input.Code != dict.Code) throw Oops.Oh(ErrorCodeEnum.D3007);
        var isExist = await _sysDictTypeRep.IsAnyAsync(u => u.Code == input.Code && u.Id != input.Id);
        if (isExist) throw Oops.Oh(ErrorCodeEnum.D3001);
        _cache.Remove(DictCacheKeys.Dict, input.Code);
        await _sysDictTypeRep.UpdateAsync(input.Adapt<SysDictType>());
    }

    /// <summary>删除字典类型。</summary>
    [UnitOfWork]
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("删除字典类型")]
    public async Task DeleteDictType(DeleteDictTypeInput input)
    {
        EnsurePlatformHost();
        var dictType = await _sysDictTypeRep.GetByIdAsync(input.Id) ?? throw Oops.Oh(ErrorCodeEnum.D3000);
        await _sysDictTypeRep.DeleteAsync(dictType);
        await _dictDataAppService.DeleteDictData(input.Id);
    }

    /// <summary>字典类型详情。</summary>
    [DisplayName("获取字典类型详情")]
    public async Task<SysDictType> GetDetail([FromQuery] DictTypeInput input)
    {
        EnsurePlatformHost();
        return await _sysDictTypeRep.GetByIdAsync(input.Id);
    }

    /// <summary>改字典类型状态。</summary>
    [UnitOfWork]
    [DisplayName("修改字典类型状态")]
    public async Task SetStatus(DictTypeInput input)
    {
        EnsurePlatformHost();
        var dictType = await _sysDictTypeRep.GetByIdAsync(input.Id) ?? throw Oops.Oh(ErrorCodeEnum.D3000);
        _cache.Remove(DictCacheKeys.Dict, dictType.Code);
        dictType.Status = input.Status;
        await _sysDictTypeRep.AsUpdateable(dictType).UpdateColumns(u => new { u.Status }, true).ExecuteCommandAsync();
    }

    private static void EnsurePlatformHost()
    {
        if (!HostTenantMode.IsPlatformHost)
            throw Oops.Oh(ErrorCodeEnum.SA001);
    }
}
