namespace SimpX.Net.Application.Admin;

/// <summary>
/// 系统常量服务 🧩
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "const", Order = 280)]
public class ConstAppService : IDynamicApiController, ITransient
{
    private readonly ISimpXCache _cache;

    /// <summary>
    /// 注入数据缓存。
    /// </summary>
    /// <param name="cache">数据缓存</param>
    public ConstAppService(ISimpXCache cache)
    {
        _cache = cache;
    }

    /// <summary>
    /// 获取所有常量列表 🔖
    /// </summary>
    /// <returns></returns>
    [DisplayName("获取所有常量列表")]
    public async Task<List<ConstOutput>> GetList()
    {
        var constList = _cache.GetOrAdd(DictCacheKeys.Const, "list", () =>
        {
            var typeList = GetConstAttributeList();
            return typeList.Select(u => new ConstOutput
            {
                Name = u.CustomAttributes.ToList().FirstOrDefault()?.ConstructorArguments.ToList().FirstOrDefault().Value?.ToString() ?? u.Name,
                Code = u.Name,
                Data = GetData(Convert.ToString(u.Name)).GetAwaiter().GetResult()
            }).ToList();
        }, TimeSpan.FromDays(1));
        return await Task.FromResult(constList);
    }

    /// <summary>
    /// 根据类名获取常量数据 🔖
    /// </summary>
    /// <param name="typeName"></param>
    /// <returns></returns>
    [DisplayName("根据类名获取常量数据")]
    public async Task<List<ConstOutput>> GetData([Required] string typeName)
    {
        var constList = _cache.GetOrAdd(DictCacheKeys.Const, typeName.ToUpper(), () =>
        {
            var typeList = GetConstAttributeList();
            var type = typeList.FirstOrDefault(u => u.Name == typeName);
            if (type == null) return null;

            var isEnum = type.BaseType!.Name == "Enum";
            return type.GetFields()?
                .Where(isEnum, u => u.FieldType.Name == typeName)
                .Select(u => new ConstOutput
                {
                    Name = u.Name,
                    Code = isEnum ? (int)u.GetValue(BindingFlags.Instance)! : u.GetValue(BindingFlags.Instance)
                }).ToList();
        }, TimeSpan.FromDays(1));
        return await Task.FromResult(constList);
    }

    /// <summary>
    /// 获取常量特性类型列表
    /// </summary>
    /// <returns></returns>
    private List<Type> GetConstAttributeList()
    {
        return App.EffectiveTypes.Where(u => u.CustomAttributes.Any(c => c.AttributeType == typeof(ConstAttribute))).ToList();
    }
}