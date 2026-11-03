using System.Reflection;

namespace SimpX.Net.Caching;

/// <summary>
/// 扫描本程序集里所有 *CacheKeys 静态类，把公开字符串常量登记进注册表（跳过 Module 与带 Ignore 的字段）。
/// </summary>
public static class CacheKeyScanner
{
    /// <summary>
    /// 扫描并注册。
    /// </summary>
    /// <param name="registry">注册表</param>
    public static void RegisterAll(CacheRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        var assembly = typeof(CacheKeyScanner).Assembly;
        foreach (var type in assembly.GetTypes())
        {
            if (!type.IsClass || !type.IsAbstract || !type.IsSealed)
                continue;
            if (!type.Name.EndsWith("CacheKeys", StringComparison.Ordinal))
                continue;

            var moduleField = type.GetField("Module", BindingFlags.Public | BindingFlags.Static);
            if (moduleField == null || moduleField.FieldType != typeof(string))
            {
                throw new InvalidOperationException(
                    $"{type.Name} 必须有 public const string Module。");
            }

            var module = moduleField.GetValue(null) as string;
            if (string.IsNullOrWhiteSpace(module))
                throw new InvalidOperationException($"{type.Name}.Module 不能为空。");

            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (field.FieldType != typeof(string))
                    continue;
                if (field.Name == "Module")
                    continue;
                if (!field.IsLiteral && !field.IsInitOnly)
                    continue;
                if (field.GetCustomAttribute<CacheKeyIgnoreAttribute>() != null)
                    continue;

                var name = field.GetValue(null) as string;
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                registry.Register(new CacheDefinition
                {
                    Module = module,
                    Name = name
                });
            }
        }
    }
}
