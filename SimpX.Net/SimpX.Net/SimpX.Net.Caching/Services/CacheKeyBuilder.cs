using Microsoft.Extensions.Options;

namespace SimpX.Net.Caching;

/// <summary>
/// 按定义和配置拼完整缓存 Key：前缀 + 模块 + 名称 + 标识。
/// </summary>
public sealed class CacheKeyBuilder
{
    private readonly CacheOptions _options;

    /// <summary>
    /// 注入缓存配置。
    /// </summary>
    /// <param name="options">缓存配置</param>
    public CacheKeyBuilder(IOptions<CacheOptions> options)
    {
        _options = options.Value;
    }

    /// <summary>
    /// 拼一条完整 Key。
    /// </summary>
    /// <param name="definition">定义</param>
    /// <param name="id">业务标识</param>
    /// <returns>完整 Key</returns>
    public string Build(CacheDefinition definition, object? id)
    {
        var prefix = _options.Prefix ?? string.Empty;
        var identity = definition.IdFormatter(id);
        if (string.IsNullOrEmpty(identity))
            return $"{prefix}{definition.Module}:{definition.Name}:";
        return $"{prefix}{definition.Module}:{definition.Name}:{identity}";
    }

    /// <summary>
    /// 该缓存名下全部 Key 的扫描前缀（含末尾冒号，避免误伤同名前缀）。
    /// </summary>
    /// <param name="definition">定义</param>
    /// <returns>扫描前缀</returns>
    public string BuildNamePrefix(CacheDefinition definition)
    {
        var prefix = _options.Prefix ?? string.Empty;
        return $"{prefix}{definition.Module}:{definition.Name}:";
    }
}
