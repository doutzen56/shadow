namespace SimpX.Net.Application.Admin;

/// <summary>
/// 翻译服务 🧩
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "langText", Order = 100, Description = "翻译服务")]
public partial class LangTextAppService : IDynamicApiController, ITransient
{
    private readonly SqlSugarRepository<SysLangText> _sysLangTextRep;
    private readonly ISqlSugarClient _sqlSugarClient;
    private readonly LangTextCacheManager _langTextCacheManager;
    private readonly IEnumerable<ITextTranslator> _translators;

    /// <summary>
    /// 注入翻译表仓储、缓存和已注册的文本翻译。
    /// </summary>
    /// <param name="sysLangTextRep">翻译表仓储</param>
    /// <param name="langTextCacheManager">翻译缓存</param>
    /// <param name="sqlSugarClient">数据库客户端</param>
    /// <param name="translators">已注册的文本翻译，没开通时为空</param>
    public LangTextAppService(
        SqlSugarRepository<SysLangText> sysLangTextRep,
        LangTextCacheManager langTextCacheManager,
        ISqlSugarClient sqlSugarClient,
        IEnumerable<ITextTranslator> translators)
    {
        _sysLangTextRep = sysLangTextRep;
        _sqlSugarClient = sqlSugarClient;
        _langTextCacheManager = langTextCacheManager;
        _translators = translators;
    }

    /// <summary>
    /// 分页查询翻译表 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("分页查询翻译表")]
    [ApiDescriptionSettings(Name = "Page"), HttpPost]
    public async Task<SqlSugarPagedList<SysLangTextOutput>> Page(PageSysLangTextInput input)
    {
        input.Keyword = input.Keyword?.Trim();
        var query = _sysLangTextRep.AsQueryable()
            .WhereIF(!string.IsNullOrWhiteSpace(input.Keyword), u => u.EntityName.Contains(input.Keyword) || u.FieldName.Contains(input.Keyword) || u.LangCode.Contains(input.Keyword) || u.Content.Contains(input.Keyword))
            .WhereIF(!string.IsNullOrWhiteSpace(input.EntityName), u => u.EntityName.Contains(input.EntityName.Trim()))
            .WhereIF(!string.IsNullOrWhiteSpace(input.FieldName), u => u.FieldName.Contains(input.FieldName.Trim()))
            .WhereIF(!string.IsNullOrWhiteSpace(input.LangCode), u => u.LangCode.Contains(input.LangCode.Trim()))
            .WhereIF(!string.IsNullOrWhiteSpace(input.Content), u => u.Content.Contains(input.Content.Trim()))
            .WhereIF(input.EntityId != null, u => u.EntityId == input.EntityId)
            .Select<SysLangTextOutput>();
        return await query.OrderBuilder(input).ToPagedListAsync(input.Page, input.PageSize);
    }

    [DisplayName("获取翻译表")]
    [ApiDescriptionSettings(Name = "List"), HttpPost]
    public async Task<List<SysLangTextOutput>> List(ListSysLangTextInput input)
    {
        var query = _sysLangTextRep.AsQueryable()
            .Where(u => u.EntityName == input.EntityName.Trim() && u.FieldName == input.FieldName.Trim() && u.EntityId == input.EntityId)
            .WhereIF(!string.IsNullOrWhiteSpace(input.LangCode), u => u.LangCode == input.LangCode.Trim())
            .Select<SysLangTextOutput>();
        return await query.ToListAsync();
    }

    /// <summary>
    /// 获取翻译表详情 ℹ️
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("获取翻译表详情")]
    [ApiDescriptionSettings(Name = "Detail"), HttpGet]
    public async Task<SysLangText> Detail([FromQuery] QueryByIdSysLangTextInput input)
    {
        return await _sysLangTextRep.GetFirstAsync(u => u.Id == input.Id);
    }

    /// <summary>
    /// 增加翻译表 ➕
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("增加翻译表")]
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    public async Task<long> Add(AddSysLangTextInput input)
    {
        var entity = input.Adapt<SysLangText>();
        return await _sysLangTextRep.InsertAsync(entity) ? entity.Id : 0;
    }

    /// <summary>
    /// 更新翻译表 ✏️
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("更新翻译表")]
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    public async Task Update(UpdateSysLangTextInput input)
    {
        var entity = input.Adapt<SysLangText>();
        await _sysLangTextRep.AsUpdateable(entity)
        .ExecuteCommandAsync();
        _langTextCacheManager.UpdateCache(entity.EntityName, entity.FieldName, entity.EntityId, entity.LangCode, entity.Content);
    }

    /// <summary>
    /// 删除翻译表 ❌
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("删除翻译表")]
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    public async Task Delete(DeleteSysLangTextInput input)
    {
        var entity = await _sysLangTextRep.GetFirstAsync(u => u.Id == input.Id) ?? throw Oops.Oh(ErrorCodeEnum.D1002);

        await _sysLangTextRep.DeleteAsync(entity);   //真删除
        _langTextCacheManager.DeleteCache(entity.EntityName, entity.FieldName, entity.EntityId, entity.LangCode);
    }

    /// <summary>
    /// 批量删除翻译表 ❌
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("批量删除翻译表")]
    [ApiDescriptionSettings(Name = "BatchDelete"), HttpPost]
    public async Task BatchDelete([Required(ErrorMessage = "主键列表不能为空")] List<DeleteSysLangTextInput> input)
    {
        var exp = Expressionable.Create<SysLangText>();
        foreach (var row in input) exp = exp.Or(it => it.Id == row.Id);
        var list = await _sysLangTextRep.AsQueryable().Where(exp.ToExpression()).ToListAsync();

        await _sysLangTextRep.DeleteAsync(list);   //真删除
        foreach (var item in list)
        {
            _langTextCacheManager.DeleteCache(item.EntityName, item.FieldName, item.EntityId, item.LangCode);
        }
    }

    private static readonly object _sysLangTextBatchSaveLock = new object();

    /// <summary>
    /// 批量保存翻译表 ✏️
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("批量保存翻译表")]
    [ApiDescriptionSettings(Name = "BatchSave"), HttpPost]
    public void BatchSave([Required(ErrorMessage = "列表不能为空")] List<ImportSysLangTextInput> input)
    {
        lock (_sysLangTextBatchSaveLock)
        {
            // 校验并过滤必填基本类型为null的字段
            var rows = input.Where(x =>
            {
                if (!string.IsNullOrWhiteSpace(x.Error)) return false;
                if (x.EntityId == null)
                {
                    x.Error = "所属实体ID不能为空";
                    return false;
                }
                return true;
            }).Adapt<List<SysLangText>>();

            var storageable = _sysLangTextRep.Context.Storageable(rows)
                .SplitError(it => string.IsNullOrWhiteSpace(it.Item.EntityName), "所属实体名不能为空")
                .SplitError(it => it.Item.EntityName?.Length > 255, "所属实体名长度不能超过255个字符")
                .SplitError(it => string.IsNullOrWhiteSpace(it.Item.FieldName), "字段名不能为空")
                .SplitError(it => it.Item.FieldName?.Length > 255, "字段名长度不能超过255个字符")
                .SplitError(it => string.IsNullOrWhiteSpace(it.Item.LangCode), "语言代码不能为空")
                .SplitError(it => it.Item.LangCode?.Length > 255, "语言代码长度不能超过255个字符")
                .SplitError(it => string.IsNullOrWhiteSpace(it.Item.Content), "翻译内容不能为空")
                .WhereColumns(it => new { it.EntityId, it.EntityName, it.FieldName, it.LangCode })
                .SplitInsert(it => it.NotAny())
                .SplitUpdate(it => it.Any())
                .ToStorage();

            storageable.AsInsertable.ExecuteCommand();// 不存在插入
            storageable.AsUpdateable.UpdateColumns(it => new
            {
                it.EntityName,
                it.EntityId,
                it.FieldName,
                it.LangCode,
                it.Content,
            }).ExecuteCommand();// 存在更新
            foreach (var item in rows)
            {
                _langTextCacheManager.DeleteCache(item.EntityName, item.FieldName, item.EntityId, item.LangCode);
            }
            if (storageable.ErrorList.Any())
            {
                throw Oops.Oh($"处理过程中出现以下错误：{string.Join("；", storageable.ErrorList.Distinct())}");
            }
        }
    }

    /// <summary>
    /// 导出翻译表记录 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("导出翻译表记录")]
    [ApiDescriptionSettings(Name = "Export"), HttpPost, NonUnify]
    public async Task<IActionResult> Export(PageSysLangTextInput input)
    {
        var list = (await Page(input)).Items?.Adapt<List<ExportSysLangTextOutput>>() ?? new();
        if (input.SelectKeyList?.Count > 0) list = list.Where(x => input.SelectKeyList.Contains(x.Id)).ToList();
        return ExcelHelper.ExportTemplate(list, "翻译表导出记录");
    }

    /// <summary>
    /// 下载翻译表数据导入模板 ⬇️
    /// </summary>
    /// <returns></returns>
    [DisplayName("下载翻译表数据导入模板")]
    [ApiDescriptionSettings(Name = "Import"), HttpGet, NonUnify]
    public IActionResult DownloadTemplate()
    {
        return ExcelHelper.ExportTemplate(new List<ExportSysLangTextOutput>(), "翻译表导入模板");
    }

    private static readonly object _sysLangTextImportLock = new object();

    /// <summary>
    /// 导入翻译表记录 💾
    /// </summary>
    /// <returns></returns>
    [DisplayName("导入翻译表记录")]
    [ApiDescriptionSettings(Name = "Import"), HttpPost, NonUnify, UnitOfWork]
    public IActionResult ImportData([Required] IFormFile file)
    {
        lock (_sysLangTextImportLock)
        {
            var stream = ExcelHelper.ImportData<ImportSysLangTextInput, SysLangText>(file, (list, markerErrorAction) =>
            {
                _sqlSugarClient.Utilities.PageEach(list, 2048, pageItems =>
                {
                    // 校验并过滤必填基本类型为null的字段
                    var rows = pageItems.Where(x =>
                    {
                        if (!string.IsNullOrWhiteSpace(x.Error)) return false;
                        if (x.EntityId == null)
                        {
                            x.Error = "所属实体ID不能为空";
                            return false;
                        }
                        return true;
                    }).Adapt<List<SysLangText>>();

                    var storageable = _sysLangTextRep.Context.Storageable(rows)
                        .SplitError(it => string.IsNullOrWhiteSpace(it.Item.EntityName), "所属实体名不能为空")
                        .SplitError(it => it.Item.EntityName?.Length > 255, "所属实体名长度不能超过255个字符")
                        .SplitError(it => string.IsNullOrWhiteSpace(it.Item.FieldName), "字段名不能为空")
                        .SplitError(it => it.Item.FieldName?.Length > 255, "字段名长度不能超过255个字符")
                        .SplitError(it => string.IsNullOrWhiteSpace(it.Item.LangCode), "语言代码不能为空")
                        .SplitError(it => it.Item.LangCode?.Length > 255, "语言代码长度不能超过255个字符")
                        .SplitError(it => string.IsNullOrWhiteSpace(it.Item.Content), "翻译内容不能为空")
                        .SplitError(it => it.Item.Content?.Length > 255, "翻译内容长度不能超过255个字符")
                        .WhereColumns(it => new { it.EntityId, it.EntityName, it.FieldName, it.LangCode })
                        .SplitInsert(it => it.NotAny())
                        .SplitUpdate(it => it.Any())
                        .ToStorage();

                    storageable.AsInsertable.ExecuteCommand();// 不存在插入
                    storageable.AsUpdateable.UpdateColumns(it => new
                    {
                        it.EntityName,
                        it.EntityId,
                        it.FieldName,
                        it.LangCode,
                        it.Content,
                    }).ExecuteCommand();// 存在更新

                    foreach (var item in rows)
                    {
                        _langTextCacheManager.DeleteCache(item.EntityName, item.FieldName, item.EntityId, item.LangCode);
                    }
                    // 标记错误信息
                    markerErrorAction.Invoke(storageable, pageItems, rows);
                });
            });

            return stream;
        }
    }

    /// <summary>
    /// AI 翻译 🔖。具体用哪个翻译服务由注册的 <see cref="ITextTranslator"/> 决定。
    /// </summary>
    /// <param name="input">原文和目标语言</param>
    /// <returns>译文</returns>
    /// <exception cref="AppFriendlyException">没开通翻译服务或翻译失败</exception>
    [DisplayName("AI 翻译")]
    [ApiDescriptionSettings(Name = "AiTranslateText"), HttpPost]
    public async Task<string> AiTranslateText(AiTranslateTextInput input)
    {
        var translator = _translators.LastOrDefault() ?? throw Oops.Oh("未配置翻译服务");
        return await translator.TranslateAsync(input.OriginalText, input.TargetLang);
    }
}