using System.Reflection;

namespace SimpX.Net.Caching;

/// <summary>
/// 从方法参数里按「参数名」或「参数名.属性名」取出失效标识。
/// </summary>
public static class CacheAttributeArgumentResolver
{
    /// <summary>
    /// 解析 Attribute 上的参数路径。
    /// </summary>
    /// <param name="method">方法</param>
    /// <param name="arguments">实参</param>
    /// <param name="attribute">失效声明</param>
    /// <returns>标识值；ByName 且无路径时返回 null</returns>
    public static object? Resolve(MethodInfo method, object?[] arguments, CacheRemoveAttribute attribute)
    {
        if (attribute.Mode == CacheRemoveMode.ByName && string.IsNullOrWhiteSpace(attribute.ArgumentPath))
            return null;

        var path = attribute.ArgumentPath
            ?? throw new InvalidOperationException("按 Key 失效时参数路径不能为空。");

        var parts = path.Split('.', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var parameters = method.GetParameters();
        var index = -1;
        for (var i = 0; i < parameters.Length; i++)
        {
            if (string.Equals(parameters[i].Name, parts[0], StringComparison.Ordinal))
            {
                index = i;
                break;
            }
        }

        if (index < 0)
        {
            throw new InvalidOperationException(
                $"方法 '{method.Name}' 上找不到参数 '{parts[0]}'。");
        }

        if (parameters[index].ParameterType == typeof(CancellationToken))
        {
            throw new InvalidOperationException(
                $"不能把 CancellationToken 参数 '{parts[0]}' 当作缓存标识。");
        }

        var value = arguments[index];
        if (parts.Length == 1)
            return value;

        if (value is null)
            return null;

        var prop = value.GetType().GetProperty(
            parts[1],
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
        if (prop is null)
        {
            throw new InvalidOperationException(
                $"类型 '{value.GetType().Name}' 上找不到属性 '{parts[1]}'。");
        }

        return prop.GetValue(value);
    }
}
