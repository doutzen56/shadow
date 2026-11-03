using Magicodes.ExporterAndImporter.Core.Models;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 后台 Excel 导入导出：模板导出、数据导出（按字典翻译）、导入（经文件服务暂存）。依赖后台的字典和文件数据。
/// </summary>
public static class ExcelDataUtil
{
    private static readonly FileAppService FileAppService = App.GetRequiredService<FileAppService>();
    private static readonly SqlSugarRepository<SysDictData> SysDictDataRep = App.GetRequiredService<SqlSugarRepository<SysDictData>>();


    /// <summary>
    /// 导出模板Excel
    /// </summary>
    /// <returns></returns>
    public static async Task<IActionResult> ExportExcelTemplate<T>(string fileName = null) where T : class, new()
    {
        IImporter importer = new ExcelImporter();
        var res = await importer.GenerateTemplateBytes<T>();

        return new FileContentResult(res, "application/octet-stream") { FileDownloadName = $"{(string.IsNullOrEmpty(fileName) ? typeof(T).Name : fileName)}.xlsx" };
    }

    /// <summary>
    /// 导出数据excel
    /// </summary>
    /// <returns></returns>
    public static async Task<IActionResult> ExportExcelData<T>(ICollection<T> data, string fileName = null) where T : class, new()
    {
        var export = new ExcelExporter();
        var res = await export.ExportAsByteArray<T>(data);

        return new FileContentResult(res, "application/octet-stream") { FileDownloadName = $"{(string.IsNullOrEmpty(fileName) ? typeof(T).Name : fileName)}.xlsx" };
    }

    /// <summary>
    /// 导出数据excel,包括字典转换
    /// </summary>
    /// <returns></returns>
    public static async Task<IActionResult> ExportExcelData<TSource, TTarget>(ISugarQueryable<TSource> query, Func<TSource, TTarget, TTarget> action = null)
        where TSource : class, new() where TTarget : class, new()
    {
        var propMappings = GetExportPropertMap<TSource, TTarget>();
        var data = query.ToList();
        //相同属性复制值，字典值转换
        var result = new List<TTarget>();
        foreach (var item in data)
        {
            var newData = new TTarget();
            foreach (var dict in propMappings)
            {
                var targetProp = dict.Value.Item3;
                if (targetProp != null)
                {
                    var propertyInfo = dict.Value.Item2;
                    var sourceVal = propertyInfo.GetValue(item, null);
                    if (sourceVal == null)
                    {
                        continue;
                    }

                    var map = dict.Value.Item1;
                    if (map != null && map.TryGetValue(sourceVal, out string newVal1))
                    {
                        targetProp.SetValue(newData, newVal1);
                    }
                    else
                    {
                        if (targetProp.PropertyType.FullName == propertyInfo.PropertyType.FullName)
                        {
                            targetProp.SetValue(newData, sourceVal);
                        }
                        else
                        {
                            var newVal = sourceVal.ToString().ParseTo(targetProp.PropertyType);
                            targetProp.SetValue(newData, newVal);
                        }
                    }
                }
                if (action != null)
                {
                    newData = action(item, newData);
                }
            }
            result.Add(newData);
        }
        var export = new ExcelExporter();
        var res = await export.ExportAsByteArray(result);

        return new FileContentResult(res, "application/octet-stream") { FileDownloadName = typeof(TTarget).Name + ".xlsx" };
    }

    /// <summary>
    /// 导入数据Excel
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    public static async Task<ICollection<T>> ImportExcelData<T>([Required] IFormFile file) where T : class, new()
    {
        IImporter importer = new ExcelImporter();
        var res = await importer.Import<T>(file.OpenReadStream());
        var message = string.Empty;

        if (!res.HasError) return res.Data;

        if (res.Exception != null)
            message += $"\r\n{res.Exception.Message}";
        foreach (DataRowErrorInfo drErrorInfo in res.RowErrors)
        {
            int rowNum = drErrorInfo.RowIndex;
            foreach (var item in drErrorInfo.FieldErrors)
                message += $"\r\n{item.Key}：{item.Value}（文件第{drErrorInfo.RowIndex}行）";
        }
        message += "\r\n字段缺失：" + string.Join("，", res.TemplateErrors.Select(m => m.RequireColumnName).ToList());
        throw Oops.Oh("导入异常:" + message);
    }

    /// <summary>
    /// 导入Excel数据并错误标记
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="file"></param>
    /// <param name="importResultCallback"></param>
    /// <returns></returns>
    public static async Task<ICollection<T>> ImportExcelData<T>([Required] IFormFile file, Func<ImportResult<T>, ImportResult<T>> importResultCallback = null) where T : class, new()
    {
        IImporter importer = new ExcelImporter();
        var resultStream = new MemoryStream();
        var res = await importer.Import<T>(file.OpenReadStream(), resultStream, importResultCallback);
        resultStream.Seek(0, SeekOrigin.Begin);
        var userId = App.User?.FindFirst(ClaimConst.UserId)?.Value;
        SaveExcelTemp(userId, resultStream);

        var message = string.Empty;
        if (!res.HasError) return res.Data;

        if (res.Exception != null)
            message += $"\r\n{res.Exception.Message}";
        foreach (DataRowErrorInfo drErrorInfo in res.RowErrors)
        {
            message = drErrorInfo.FieldErrors.Aggregate(message, (current, item) => current + $"\r\n{item.Key}：{item.Value}（文件第{drErrorInfo.RowIndex}行）");
        }
        if (res.TemplateErrors.Count > 0)
            message += "\r\n字段缺失：" + string.Join("，", res.TemplateErrors.Select(m => m.RequireColumnName).ToList());

        if (message.Length > 200)
            message = message.Substring(0, 200) + "...\r\n异常过多，建议下载错误标记文件查看详细错误信息并重新导入。";
        throw Oops.Oh("导入异常:" + message);
    }

    /// <summary>
    /// 把带错误标记的 Excel 按字节放进临时缓存，下载时再取出来。
    /// </summary>
    /// <param name="userId">当前用户 Id</param>
    /// <param name="resultStream">错误标记文件</param>
    private static void SaveExcelTemp(string userId, MemoryStream resultStream)
    {
        resultStream.Seek(0, SeekOrigin.Begin);
        App.GetRequiredService<ICacheStateStore>().Add(CommonCacheKeys.ExcelTemp, userId ?? string.Empty, resultStream.ToArray(), TimeSpan.FromMinutes(5));
    }

    /// <summary>
    /// 导入数据Excel
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="file"></param>
    /// <returns></returns>
    public static async Task<List<T>> ImportExcelDataAsync<T>([Required] IFormFile file) where T : class, new()
    {
        var newFile = await FileAppService.UploadFile(new UploadFileInput { File = file });

        await using var fileStream = await FileAppService.GetFileStream(newFile);

        IImporter importer = new ExcelImporter();
        var res = await importer.Import<T>(fileStream);

        // 删除文件
        await FileAppService.DeleteFile(new BaseIdInput { Id = newFile.Id });

        if (res == null)
            throw Oops.Oh("导入数据为空");
        if (res.Exception != null)
            throw Oops.Oh("导入异常:" + res.Exception);
        if (res.TemplateErrors?.Count > 0)
            throw Oops.Oh("模板异常:" + res.TemplateErrors.Select(x => $"[{x.RequireColumnName}]{x.Message}").Join("\n"));

        return res.Data.ToList();
    }

    // 例：List<Dm_ApplyDemo> ls = ExcelDataUtil.ParseList<Dm_ApplyDemoInport, Dm_ApplyDemo>(importResult.Data);
    /// <summary>
    /// 对象转换 含字典转换
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <typeparam name="TTarget"></typeparam>
    /// <param name="data"></param>
    /// <param name="action"></param>
    /// <returns></returns>
    public static List<TTarget> ParseList<TSource, TTarget>(IEnumerable<TSource> data, Func<TSource, TTarget, TTarget> action = null) where TTarget : new()
    {
        var propMappings = GetImportPropertMap<TSource, TTarget>();
        // 相同属性复制值，字典值转换
        var result = new List<TTarget>();
        foreach (var item in data)
        {
            var newData = new TTarget();
            foreach (var dict in propMappings)
            {
                var targeProp = dict.Value.Item3;
                if (targeProp != null)
                {
                    var propertyInfo = dict.Value.Item2;
                    var sourceVal = propertyInfo.GetValue(item, null);
                    if (sourceVal == null)
                        continue;

                    var map = dict.Value.Item1;
                    if (map != null && map.ContainsKey(sourceVal.ToString()))
                    {
                        var newVal = map[sourceVal.ToString()];
                        targeProp.SetValue(newData, newVal);
                    }
                    else
                    {
                        if (targeProp.PropertyType.FullName == propertyInfo.PropertyType.FullName)
                        {
                            targeProp.SetValue(newData, sourceVal);
                        }
                        else
                        {
                            var newVal = sourceVal.ToString().ParseTo(targeProp.PropertyType);
                            targeProp.SetValue(newData, newVal);
                        }
                    }
                }
            }
            if (action != null)
                newData = action(item, newData);

            if (newData != null)
                result.Add(newData);
        }
        return result;
    }

    /// <summary>
    /// 获取导入属性映射
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <typeparam name="TTarget"></typeparam>
    /// <returns>整理导入对象的 属性名称， 字典数据，原属性信息，目标属性信息 </returns>
    private static Dictionary<string, Tuple<Dictionary<string, object>, PropertyInfo, PropertyInfo>> GetImportPropertMap<TSource, TTarget>() where TTarget : new()
    {
        // 整理导入对象的属性名称，<字典数据，原属性信息，目标属性信息>
        var propMappings = new Dictionary<string, Tuple<Dictionary<string, object>, PropertyInfo, PropertyInfo>>();

        var dictService = App.GetRequiredService<SqlSugarRepository<SysDictData>>();
        var tSourceProps = typeof(TSource).GetProperties().ToList();
        var tTargetProps = typeof(TTarget).GetProperties().ToDictionary(u => u.Name);
        foreach (var propertyInfo in tSourceProps)
        {
            var attrs = propertyInfo.GetCustomAttribute<ImportDictAttribute>();
            if (attrs != null && !string.IsNullOrWhiteSpace(attrs.TypeCode))
            {
                var targetProp = tTargetProps[attrs.TargetPropName];
                var mappingValues = dictService.Context.Queryable<SysDictType, SysDictData>((u, a) =>
                    new JoinQueryInfos(JoinType.Inner, u.Id == a.DictTypeId))
                    .Where(u => u.Code == attrs.TypeCode)
                    .Where((u, a) => u.Status == StatusEnum.Enable && a.Status == StatusEnum.Enable)
                    .Select((u, a) => new
                    {
                        Label = a.Label,
                        Value = a.Value
                    }).ToList()
                    .ToDictionary(u => u.Label, u => u.Value.ParseTo(targetProp.PropertyType));
                propMappings.Add(propertyInfo.Name, new Tuple<Dictionary<string, object>, PropertyInfo, PropertyInfo>(mappingValues, propertyInfo, targetProp));
            }
            else
            {
                propMappings.Add(propertyInfo.Name, new Tuple<Dictionary<string, object>, PropertyInfo, PropertyInfo>(
                    null, propertyInfo, tTargetProps.ContainsKey(propertyInfo.Name) ? tTargetProps[propertyInfo.Name] : null));
            }
        }

        return propMappings;
    }

    /// <summary>
    /// 获取导出属性映射
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <typeparam name="TTarget"></typeparam>
    /// <returns>整理导入对象的 属性名称， 字典数据，原属性信息，目标属性信息 </returns>
    private static Dictionary<string, Tuple<Dictionary<object, string>, PropertyInfo, PropertyInfo>> GetExportPropertMap<TSource, TTarget>() where TTarget : new()
    {
        // 整理导入对象的属性名称，<字典数据，原属性信息，目标属性信息>
        var propMappings = new Dictionary<string, Tuple<Dictionary<object, string>, PropertyInfo, PropertyInfo>>();

        var targetProps = typeof(TTarget).GetProperties().ToList();
        var sourceProps = typeof(TSource).GetProperties().ToDictionary(u => u.Name);
        foreach (var propertyInfo in targetProps)
        {
            var attrs = propertyInfo.GetCustomAttribute<ImportDictAttribute>();
            if (attrs != null && !string.IsNullOrWhiteSpace(attrs.TypeCode))
            {
                var targetProp = sourceProps[attrs.TargetPropName];
                var mappingValues = SysDictDataRep.Context.Queryable<SysDictType, SysDictData>((u, a) =>
                    new JoinQueryInfos(JoinType.Inner, u.Id == a.DictTypeId))
                    .Where(u => u.Code == attrs.TypeCode)
                    .Where((u, a) => u.Status == StatusEnum.Enable && a.Status == StatusEnum.Enable)
                    .Select((u, a) => new
                    {
                        a.Label,
                        a.Value
                    }).ToList()
                    .ToDictionary(u => u.Value.ParseTo(targetProp.PropertyType), u => u.Label);
                propMappings.Add(propertyInfo.Name, new Tuple<Dictionary<object, string>, PropertyInfo, PropertyInfo>(mappingValues, targetProp, propertyInfo));
            }
            else
            {
                propMappings.Add(propertyInfo.Name, new Tuple<Dictionary<object, string>, PropertyInfo, PropertyInfo>(
                    null, sourceProps.TryGetValue(propertyInfo.Name, out PropertyInfo prop) ? prop : null, propertyInfo));
            }
        }

        return propMappings;
    }

    /// <summary>
    /// 获取属性映射
    /// </summary>
    /// <typeparam name="TTarget"></typeparam>
    /// <returns>整理导入对象的 属性名称， 字典数据，原属性信息，目标属性信息 </returns>
    private static Dictionary<string, Tuple<string, string>> GetExportDictMap<TTarget>() where TTarget : new()
    {
        // 整理导入对象的属性名称，目标属性名，字典Code
        var propMappings = new Dictionary<string, Tuple<string, string>>();
        var tTargetProps = typeof(TTarget).GetProperties();
        foreach (var propertyInfo in tTargetProps)
        {
            var attrs = propertyInfo.GetCustomAttribute<ImportDictAttribute>();
            if (attrs != null && !string.IsNullOrWhiteSpace(attrs.TypeCode))
            {
                propMappings.Add(propertyInfo.Name, new Tuple<string, string>(attrs.TargetPropName, attrs.TypeCode));
            }
        }

        return propMappings;
    }
}
