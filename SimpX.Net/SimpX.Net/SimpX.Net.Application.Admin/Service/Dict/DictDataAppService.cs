namespace SimpX.Net.Application.Admin;

/// <summary>
/// 租户后台内部字典值服务（无 HTTP；管理入口在总后台 /api/platform/dictData） 🧩
/// </summary>
public class DictDataAppService : ITransient
{
    private readonly SqlSugarRepository<SysDictData> _sysDictDataRep;
    public readonly ISugarQueryable<SysDictData> VSysDictData;
    private readonly ISimpXCache _cache;
    private readonly UserManager _userManager;
    private readonly LangTextCacheManager _langTextCacheManager;

    public DictDataAppService(SqlSugarRepository<SysDictData> sysDictDataRep,
        ISimpXCache cache,
        UserManager userManager,
        LangTextCacheManager langTextCacheManager)
    {
        _userManager = userManager;
        _sysDictDataRep = sysDictDataRep;
        _cache = cache;
        _langTextCacheManager = langTextCacheManager;
        VSysDictData = _sysDictDataRep.Context.UnionAll(
            _sysDictDataRep.AsQueryable(),
            _sysDictDataRep.Change<SysDictDataTenant>().AsQueryable()
            //.WhereIF(_userManager.SuperAdmin, d => d.TenantId == _userManager.TenantId)
            .Select<SysDictData>());
    }

    /// <summary>
    /// 获取字典值分页列表 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("获取字典值分页列表")]
    public async Task<SqlSugarPagedList<SysDictData>> Page(PageDictDataInput input)
    {
        var langCode = _userManager.LangCode;
        var baseQuery = VSysDictData
            .Where(u => u.DictTypeId == input.DictTypeId)
            .WhereIF(!string.IsNullOrEmpty(input.Code?.Trim()), u => u.Code.Contains(input.Code))
            .WhereIF(!string.IsNullOrEmpty(input.Label?.Trim()), u => u.Label.Contains(input.Label))
            .OrderBy(u => new { u.OrderNo, u.Code });
        var pageList = await baseQuery.ToPagedListAsync(input.Page, input.PageSize);
        var list = pageList.Items;
        var ids = list.Select(d => d.Id).Distinct().ToList();
        var translations = await _langTextCacheManager.GetTranslations(
                               "SysDictData",
                               "Label",
                               ids,
                               langCode);
        foreach (var item in list)
        {
            if (translations.TryGetValue(item.Id, out var translatedLabel) && !string.IsNullOrEmpty(translatedLabel))
            {
                item.Label = translatedLabel;
            }
        }
        pageList.Items = list;
        return pageList;
    }

    /// <summary>
    /// 获取字典值列表 🔖
    /// </summary>
    /// <returns></returns>
    [DisplayName("获取字典值列表")]
    public async Task<List<SysDictData>> GetList([FromQuery] GetDataDictDataInput input)
    {
        var langCode = _userManager.LangCode;
        var list = await GetDictDataListByDictTypeId(input.DictTypeId);
        var ids = list.Select(d => d.Id).Distinct().ToList();
        var translations = await _langTextCacheManager.GetTranslations(
                               "SysDictData",
                               "Label",
                               ids,
                               langCode);
        foreach (var item in list)
        {
            if (translations.TryGetValue(item.Id, out var translatedLabel) && !string.IsNullOrEmpty(translatedLabel))
            {
                item.Label = translatedLabel;
            }
        }
        return await GetDictDataListByDictTypeId(input.DictTypeId);
    }

    /// <summary>
    /// 增加字典值 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("增加字典值")]
    public async Task AddDictData(AddDictDataInput input)
    {
        var isExist = await VSysDictData.AnyAsync(u => u.Value == input.Value && u.DictTypeId == input.DictTypeId);
        if (isExist) throw Oops.Oh(ErrorCodeEnum.D3003);

        var dictType = await _sysDictDataRep.Change<SysDictType>().GetByIdAsync(input.DictTypeId);
        if (dictType.SysFlag == YesNoEnum.Y && !_userManager.SuperAdmin) throw Oops.Oh(ErrorCodeEnum.D3008);

        Remove(dictType);

        dynamic dictData = dictType.IsTenant == YesNoEnum.Y ? input.Adapt<SysDictDataTenant>() : input.Adapt<SysDictData>();
        await _sysDictDataRep.Context.Insertable(dictData).ExecuteCommandAsync();
    }

    /// <summary>
    /// 更新字典值 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [UnitOfWork]
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("更新字典值")]
    public async Task UpdateDictData(UpdateDictDataInput input)
    {
        var isExist = await VSysDictData.AnyAsync(u => u.Id == input.Id);
        if (!isExist) throw Oops.Oh(ErrorCodeEnum.D3004);

        isExist = await VSysDictData.AnyAsync(u => u.Value == input.Value && u.DictTypeId == input.DictTypeId && u.Id != input.Id);
        if (isExist) throw Oops.Oh(ErrorCodeEnum.D3003);

        var dictType = await _sysDictDataRep.Change<SysDictType>().GetByIdAsync(input.DictTypeId);
        if (dictType.SysFlag == YesNoEnum.Y && !_userManager.SuperAdmin) throw Oops.Oh(ErrorCodeEnum.D3009);

        Remove(dictType);
        dynamic dictData = dictType.IsTenant == YesNoEnum.Y ? input.Adapt<SysDictDataTenant>() : input.Adapt<SysDictData>();
        await _sysDictDataRep.Context.Updateable(dictData).ExecuteCommandAsync();
    }

    /// <summary>
    /// 删除字典值 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [UnitOfWork]
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("删除字典值")]
    public async Task DeleteDictData(DeleteDictDataInput input)
    {
        var dictData = await VSysDictData.FirstAsync(u => u.Id == input.Id) ?? throw Oops.Oh(ErrorCodeEnum.D3004);

        var dictType = await _sysDictDataRep.Change<SysDictType>().GetByIdAsync(dictData.DictTypeId);
        if (dictType.SysFlag == YesNoEnum.Y && !_userManager.SuperAdmin) throw Oops.Oh(ErrorCodeEnum.D3010);

        Remove(dictType);
        dynamic entity = dictType.IsTenant == YesNoEnum.Y ? input.Adapt<SysDictDataTenant>() : input.Adapt<SysDictData>();
        await _sysDictDataRep.Context.Deleteable(entity).ExecuteCommandAsync();
    }

    /// <summary>
    /// 获取字典值详情 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("获取字典值详情")]
    public async Task<SysDictData> GetDetail([FromQuery] DictDataInput input)
    {
        return (await VSysDictData.FirstAsync(u => u.Id == input.Id))?.Adapt<SysDictData>();
    }

    /// <summary>
    /// 修改字典值状态 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [UnitOfWork]
    [DisplayName("修改字典值状态")]
    public async Task SetStatus(DictDataInput input)
    {
        var dictData = await VSysDictData.FirstAsync(u => u.Id == input.Id) ?? throw Oops.Oh(ErrorCodeEnum.D3004);

        var dictType = await _sysDictDataRep.Change<SysDictType>().GetByIdAsync(dictData.DictTypeId);
        if (dictType.SysFlag == YesNoEnum.Y && !_userManager.SuperAdmin) throw Oops.Oh(ErrorCodeEnum.D3009);

        Remove(dictType);

        dictData.Status = input.Status;
        dynamic entity = dictType.IsTenant == YesNoEnum.Y ? input.Adapt<SysDictDataTenant>() : input.Adapt<SysDictData>();
        await _sysDictDataRep.Context.Updateable(entity).ExecuteCommandAsync();
    }

    /// <summary>
    /// 根据字典类型Id获取字典值集合
    /// </summary>
    /// <param name="dictTypeId"></param>
    /// <returns></returns>
    [NonAction]
    public async Task<List<SysDictData>> GetDictDataListByDictTypeId(long dictTypeId)
    {
        return await GetDataListByIdOrCode(dictTypeId, null);
    }

    /// <summary>
    /// 根据字典类型编码获取字典值集合 🔖
    /// </summary>
    /// <param name="code"></param>
    /// <returns></returns>
    [DisplayName("根据字典类型编码获取字典值集合")]
    public async Task<List<SysDictData>> GetDataList(string code)
    {
        return await GetDataListByIdOrCode(null, code);
    }

    /// <summary>
    /// 获取字典值集合 🔖
    /// </summary>
    /// <param name="typeId"></param>
    /// <param name="code"></param>
    /// <returns></returns>
    [NonAction]
    public async Task<List<SysDictData>> GetDataListByIdOrCode(long? typeId, string code)
    {
        if (string.IsNullOrWhiteSpace(code) && typeId == null ||
            !string.IsNullOrWhiteSpace(code) && typeId != null)
            throw Oops.Oh(ErrorCodeEnum.D3011);

        var dictType = await _sysDictDataRep.Change<SysDictType>().AsQueryable()
            .Where(u => u.Status == StatusEnum.Enable)
            .WhereIF(!string.IsNullOrWhiteSpace(code), u => u.Code == code)
            .WhereIF(typeId != null, u => u.Id == typeId)
            .FirstAsync();
        if (dictType == null) return null;

        var tenantDict = dictType.IsTenant == YesNoEnum.Y;
        var cacheName = tenantDict ? DictCacheKeys.TenantDict : DictCacheKeys.Dict;
        var cacheId = tenantDict ? $"{_userManager.TenantId}:{dictType.Code}" : dictType.Code;
        var dictDataList = _cache.Get<List<SysDictData>>(cacheName, cacheId);
        if (dictDataList == null)
        {
            //平台字典和租户字典分开缓存
            if (dictType.IsTenant == YesNoEnum.Y)
            {
                dictDataList = await _sysDictDataRep.Change<SysDictDataTenant>().AsQueryable()
                       .Where(u => u.DictTypeId == dictType.Id)
                       .Where(u => u.Status == StatusEnum.Enable)
                       .WhereIF(_userManager.SuperAdmin, d => d.TenantId == _userManager.TenantId).Select<SysDictData>()
                       .OrderBy(u => new { u.OrderNo, u.Value, u.Code })
                       .ToListAsync();
            }
            else
            {
                dictDataList = await _sysDictDataRep.AsQueryable()
                    .Where(u => u.DictTypeId == dictType.Id)
                    .Where(u => u.Status == StatusEnum.Enable)
                    .OrderBy(u => new { u.OrderNo, u.Value, u.Code })
                    .ToListAsync();
            }

            _cache.Add(cacheName, cacheId, dictDataList, TimeSpan.FromHours(1));
        }
        return dictDataList;
    }

    /// <summary>
    /// 根据查询条件获取字典值集合 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("根据查询条件获取字典值集合")]
    public async Task<List<SysDictData>> GetDataList([FromQuery] QueryDictDataInput input)
    {
        var dataList = await GetDataList(input.Value);
        if (input.Status.HasValue) return dataList.Where(u => u.Status == (StatusEnum)input.Status.Value).ToList();
        return dataList;
    }

    /// <summary>
    /// 根据字典类型Id删除字典值
    /// </summary>
    /// <param name="dictTypeId"></param>
    /// <returns></returns>
    [NonAction]
    public async Task DeleteDictData(long dictTypeId)
    {
        var dictType = await _sysDictDataRep.Change<SysDictType>().AsQueryable().Where(u => u.Id == dictTypeId).FirstAsync();
        Remove(dictType);

        if (dictType?.IsTenant == YesNoEnum.Y)
            await _sysDictDataRep.Change<SysDictDataTenant>().DeleteAsync(u => u.DictTypeId == dictTypeId);
        else
            await _sysDictDataRep.DeleteAsync(u => u.DictTypeId == dictTypeId);
    }

    /// <summary>
    /// 通过字典数据Value查询显示文本Label
    /// 适用于列表中根据字典数据值找文本的子查询 _dictDataAppService.MapDictValueToLabel(() =>obj.Type, "org_type",obj);
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="mappingFiled"></param>
    /// <param name="dictTypeCode"></param>
    /// <param name="parameter"></param>
    /// <returns></returns>
    public string MapDictValueToLabel<T>(Expression<Func<object>> mappingFiled, string dictTypeCode, T parameter)
    {
        return VSysDictData.InnerJoin<SysDictType>((d, dt) => d.DictTypeId.Equals(dt.Id) && dt.Code == dictTypeCode)
            .SetContext(d => d.Value, mappingFiled, parameter).FirstOrDefault()?.Label;
    }

    /// <summary>
    /// 通过字典数据显示文本Label查询Value
    /// 适用于列表数据导入根据字典数据文本找值的子查询 _dictDataAppService.MapDictLabelToValue(() => obj.Type, "org_type",obj);
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="mappingFiled"></param>
    /// <param name="dictTypeCode"></param>
    /// <param name="parameter"></param>
    /// <returns></returns>
    public string MapDictLabelToValue<T>(Expression<Func<object>> mappingFiled, string dictTypeCode, T parameter)
    {
        return VSysDictData.InnerJoin<SysDictType>((d, dt) => d.DictTypeId.Equals(dt.Id) && dt.Code == dictTypeCode)
            .SetContext(d => d.Label, mappingFiled, parameter).FirstOrDefault()?.Value;
    }

    /// <summary>
    /// 清理字典数据缓存
    /// </summary>
    /// <param name="dictType"></param>
    private void Remove(SysDictType dictType)
    {
        if (dictType == null || string.IsNullOrEmpty(dictType.Code)) return;
        _cache.Remove(DictCacheKeys.Dict, dictType.Code);
        _cache.Remove(DictCacheKeys.TenantDict, $"{_userManager.TenantId}:{dictType.Code}");
    }
}