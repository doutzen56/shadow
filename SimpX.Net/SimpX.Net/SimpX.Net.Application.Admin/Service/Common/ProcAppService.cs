namespace SimpX.Net.Application.Admin;

/// <summary>
/// 系统存储过程服务 🧩
/// </summary>
[ApiDescriptionSettings(Module = "dev", Name = "proc", Order = 102)]
public class ProcAppService : IDynamicApiController, ITransient
{
    private readonly ISqlSugarClient _db;

    public ProcAppService(ISqlSugarClient db)
    {
        _db = db;
    }

    /// <summary>
    /// 导出存储过程数据-指定列，没有指定的字段会被隐藏 🔖
    /// </summary>
    /// <returns></returns>
    public async Task<IActionResult> PocExport2(ExportProcInput input)
    {
        var db = _db.AsTenant().GetConnectionScope(input.ConfigId);

        // 存储过程名无法参数化，先做白名单校验再调用，防止注入
        SqlSecurityHelper.ValidateProcedureName(input.ProcId, nameof(input.ProcId));
        var dt = await db.Ado.UseStoredProcedure().GetDataTableAsync(input.ProcId, input.ProcParams);

        var headers = new Dictionary<string, Tuple<string, int>>();
        var index = 1;
        foreach (var val in input.EHeader)
        {
            headers.Add(val.Key.ToUpper(), new Tuple<string, int>(val.Value, index));
            index++;
        }
        var excelExporter = new ExcelExporter();
        var da = await excelExporter.ExportAsByteArray(dt, new ProcExporterHeaderFilter(headers));
        return new FileContentResult(da, "application/octet-stream") { FileDownloadName = input.ProcId + ".xlsx" };
    }

    /// <summary>
    /// 根据模板导出存储过程数据 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public async Task<IActionResult> PocExport(ExportProcByTMPInput input)
    {
        var db = _db.AsTenant().GetConnectionScope(input.ConfigId);

        // 存储过程名无法参数化，先做白名单校验再调用，防止注入
        SqlSecurityHelper.ValidateProcedureName(input.ProcId, nameof(input.ProcId));
        var dt = await db.Ado.UseStoredProcedure().GetDataTableAsync(input.ProcId, input.ProcParams);

        var excelExporter = new ExcelExporter();
        var template = GetSafeTemplatePath(input.Template);
        var bs = await excelExporter.ExportBytesByTemplate(dt, template);
        return new FileContentResult(bs, "application/octet-stream") { FileDownloadName = input.ProcId + ".xlsx" };
    }

    /// <summary>
    /// 获取安全的导出模板绝对路径，防止路径穿越
    /// </summary>
    /// <param name="templateName">模板名称（不含扩展名）</param>
    /// <returns>模板文件的绝对路径</returns>
    private static string GetSafeTemplatePath(string templateName)
    {
        if (string.IsNullOrWhiteSpace(templateName))
            throw Oops.Oh("模板名称不能为空");

        var templateRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "template"));
        var fullPath = Path.GetFullPath(Path.Combine(templateRoot, templateName + ".xlsx"));

        // 确认解析后的绝对路径仍位于模板根目录内，防止 ../ 形式的路径穿越
        var rootWithSeparator = templateRoot.EndsWith(Path.DirectorySeparatorChar)
            ? templateRoot
            : templateRoot + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
            throw Oops.Oh("非法的模板名称");

        if (!File.Exists(fullPath))
            throw Oops.Oh($"模板文件【{templateName}】不存在");

        return fullPath;
    }

    /// <summary>
    /// 获取存储过程返回表-Oracle、达梦参数顺序不能错 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public async Task<DataSet> ProcTable(BaseProcInput input)
    {
        var db = _db.AsTenant().GetConnectionScope(input.ConfigId);

        // 存储过程名无法参数化，先做白名单校验再调用，防止注入
        SqlSecurityHelper.ValidateProcedureName(input.ProcId, nameof(input.ProcId));
        var dt = await db.Ado.UseStoredProcedure().GetDataTableAsync(input.ProcId, input.ProcParams);
        return new DataSet { Tables = { dt } };
    }

    /// <summary>
    /// 获取存储过程返回数据集-Oracle、达梦参数顺序不能错
    /// Oracle 返回table、table1，其他返回table1、table2。适用于报表、复杂详细页面等 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost]
    public async Task<DataSet> CommonDataSet(BaseProcInput input)
    {
        var db = _db.AsTenant().GetConnectionScope(input.ConfigId);

        // 存储过程名无法参数化，先做白名单校验再调用，防止注入
        SqlSecurityHelper.ValidateProcedureName(input.ProcId, nameof(input.ProcId));
        return await db.Ado.UseStoredProcedure().GetDataSetAllAsync(input.ProcId, input.ProcParams);
    }
}