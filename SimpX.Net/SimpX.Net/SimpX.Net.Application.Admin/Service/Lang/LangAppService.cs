namespace SimpX.Net.Application.Admin;

/// <summary>
/// 语言服务 🧩
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "lang", Order = 100, Description = "语言服务")]
public partial class LangAppService : IDynamicApiController, ITransient
{
    private readonly SqlSugarRepository<SysLang> _sysLangRep;

    public LangAppService(SqlSugarRepository<SysLang> sysLangRep)
    {
        _sysLangRep = sysLangRep;
    }

    /// <summary>
    /// 分页查询语言 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("分页查询语言")]
    [ApiDescriptionSettings(Name = "Page"), HttpPost]
    public async Task<SqlSugarPagedList<SysLangOutput>> Page(PageSysLangInput input)
    {
        input.Keyword = input.Keyword?.Trim();
        var query = _sysLangRep.AsQueryable()
            .WhereIF(!string.IsNullOrWhiteSpace(input.Keyword), u => u.Name.Contains(input.Keyword) || u.Code.Contains(input.Keyword) || u.IsoCode.Contains(input.Keyword) || u.UrlCode.Contains(input.Keyword))
            .WhereIF(!string.IsNullOrWhiteSpace(input.Name), u => u.Name.Contains(input.Name.Trim()))
            .WhereIF(!string.IsNullOrWhiteSpace(input.Code), u => u.Code.Contains(input.Code.Trim()))
            .WhereIF(!string.IsNullOrWhiteSpace(input.IsoCode), u => u.IsoCode.Contains(input.IsoCode.Trim()))
            .WhereIF(!string.IsNullOrWhiteSpace(input.UrlCode), u => u.UrlCode.Contains(input.UrlCode.Trim()))
            .Select<SysLangOutput>();
        return await query.OrderBuilder(input).ToPagedListAsync(input.Page, input.PageSize);
    }

    /// <summary>
    /// 获取语言详情 ℹ️
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("获取语言详情")]
    [ApiDescriptionSettings(Name = "Detail"), HttpGet]
    public async Task<SysLang> Detail([FromQuery] QueryByIdSysLangInput input)
    {
        return await _sysLangRep.GetFirstAsync(u => u.Id == input.Id);
    }

    /// <summary>
    /// 增加语言 ➕
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("增加语言")]
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    public async Task<long> Add(AddSysLangInput input)
    {
        var entity = input.Adapt<SysLang>();
        return await _sysLangRep.InsertAsync(entity) ? entity.Id : 0;
    }

    /// <summary>
    /// 更新语言 ✏️
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("更新语言")]
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    public async Task Update(UpdateSysLangInput input)
    {
        var entity = input.Adapt<SysLang>();
        await _sysLangRep.AsUpdateable(entity)
        .ExecuteCommandAsync();
    }

    /// <summary>
    /// 删除语言 ❌
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("删除语言")]
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    public async Task Delete(DeleteSysLangInput input)
    {
        var entity = await _sysLangRep.GetFirstAsync(u => u.Id == input.Id) ?? throw Oops.Oh(ErrorCodeEnum.D1002);
        await _sysLangRep.DeleteAsync(entity);   //真删除
    }

    /// <summary>
    /// 批量删除语言。入参是主键列表，和翻译表批量删除同一套写法。
    /// </summary>
    /// <param name="input">要删的语言主键列表</param>
    [DisplayName("批量删除语言")]
    [ApiDescriptionSettings(Name = "BatchDelete"), HttpPost]
    public async Task BatchDelete([Required(ErrorMessage = "主键列表不能为空")] List<DeleteSysLangInput> input)
    {
        var ids = input.Where(u => u.Id.HasValue).Select(u => u.Id!.Value).Distinct().ToList();
        if (ids.Count == 0) return;

        var list = await _sysLangRep.AsQueryable().Where(u => ids.Contains(u.Id)).ToListAsync();
        if (list.Count == 0) return;

        await _sysLangRep.DeleteAsync(list);
    }

    /// <summary>
    /// 获取下拉列表数据 🔖
    /// </summary>
    /// <returns></returns>
    [AllowAnonymous]
    [DisplayName("获取下拉列表数据")]
    [ApiDescriptionSettings(Name = "DropdownData"), HttpPost]
    public async Task<dynamic> DropdownData()
    {
        var data = await _sysLangRep.Context.Queryable<SysLang>()
            .Where(m => m.Active == true)
            .Select(u => new
            {
                Code = u.Code,
                Value = u.UrlCode,
                Label = $"{u.Name}"
            }).ToListAsync();
        return data;
    }
}