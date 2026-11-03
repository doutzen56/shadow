using System.Collections.Concurrent;
using System.Reflection;

namespace SimpX.Net.Caching;

/// <summary>
/// 从缓存值对象上取业务标识：优先 <see cref="ICacheIdentifiable"/>，否则公开的 Id 属性。
/// </summary>
public static class CacheEntityId
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo?> IdProps = new();

    /// <summary>
    /// 取出标识；取不到就抛错，提示改用带 id 参数的重载。
    /// </summary>
    /// <param name="value">实体或实现了可识别接口的对象</param>
    /// <returns>业务标识</returns>
    /// <exception cref="ArgumentNullException">值为空</exception>
    /// <exception cref="InvalidOperationException">对象上没有可用的 Id</exception>
    public static object Get(object value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (value is ICacheIdentifiable identifiable)
            return identifiable.CacheId
                   ?? throw new InvalidOperationException("CacheId 不能为空。");

        var prop = IdProps.GetOrAdd(value.GetType(), static t =>
            t.GetProperty("Id", BindingFlags.Instance | BindingFlags.Public));
        if (prop == null)
        {
            throw new InvalidOperationException(
                $"类型 {value.GetType().Name} 没有 Id，也未实现 ICacheIdentifiable，请用带 id 参数的 Add/Remove。");
        }

        return prop.GetValue(value)
               ?? throw new InvalidOperationException($"类型 {value.GetType().Name} 的 Id 为空。");
    }
}
