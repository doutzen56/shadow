namespace SimpX.Net.Caching;

/// <summary>
/// 缓存名注册表：启动扫描 key 文件；运行期可补类别。
/// </summary>
public sealed class CacheRegistry
{
    private readonly Dictionary<string, CacheDefinition> _definitions =
        new(StringComparer.Ordinal);

    /// <summary>
    /// 登记一条；模块+名称重复时抛异常。
    /// </summary>
    /// <param name="definition">定义</param>
    public void Register(CacheDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (string.IsNullOrWhiteSpace(definition.Module))
            throw new ArgumentException("缓存模块名不能为空。", nameof(definition));
        if (string.IsNullOrWhiteSpace(definition.Name))
            throw new ArgumentException("缓存名不能为空。", nameof(definition));

        if (!_definitions.TryAdd(definition.RegistryKey, definition))
        {
            throw new InvalidOperationException(
                $"缓存定义 '{definition.RegistryKey}' 已注册，不能重复。");
        }
    }

    /// <summary>
    /// 按「模块:名称」或仅名称取定义；仅名称时要求全局唯一。
    /// </summary>
    /// <param name="nameOrRegistryKey">常量或 module:name</param>
    /// <returns>定义</returns>
    public CacheDefinition GetRequired(string nameOrRegistryKey)
    {
        if (string.IsNullOrWhiteSpace(nameOrRegistryKey))
            throw new ArgumentException("缓存名不能为空。", nameof(nameOrRegistryKey));

        if (_definitions.TryGetValue(nameOrRegistryKey, out var byKey))
            return byKey;

        CacheDefinition? matched = null;
        foreach (var item in _definitions.Values)
        {
            if (!string.Equals(item.Name, nameOrRegistryKey, StringComparison.Ordinal))
                continue;
            if (matched != null)
            {
                throw new InvalidOperationException(
                    $"缓存名 '{nameOrRegistryKey}' 在多个模块中存在，请用「模块:名称」引用。");
            }
            matched = item;
        }

        if (matched != null)
            return matched;

        throw new KeyNotFoundException($"缓存定义 '{nameOrRegistryKey}' 尚未登记，请在对应 *CacheKeys 文件里加常量。");
    }

    /// <summary>
    /// 首次使用时打上数据/状态类别；与已有类别冲突则抛错。
    /// </summary>
    /// <param name="definition">定义</param>
    /// <param name="kind">本次接口约定的类别</param>
    public void BindKind(CacheDefinition definition, CacheKind kind)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (definition.Kind is null)
        {
            definition.Kind = kind;
            return;
        }

        if (definition.Kind != kind)
        {
            throw new InvalidOperationException(
                $"缓存 '{definition.RegistryKey}' 已按 {definition.Kind} 使用，不能再当 {kind}。");
        }
    }

    /// <summary>
    /// 全部已登记定义（只读）。
    /// </summary>
    public IReadOnlyCollection<CacheDefinition> All => _definitions.Values.ToList();
}
