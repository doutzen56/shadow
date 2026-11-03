namespace SimpX.Net.Caching;

/// <summary>
/// 一条缓存名登记：只回答叫什么；类别在首次读写时按走数据还是状态接口打上。
/// </summary>
public sealed class CacheDefinition
{
    /// <summary>
    /// 模块名，进入 Key 的一段，例如 permission。
    /// </summary>
    public required string Module { get; init; }

    /// <summary>
    /// 缓存逻辑名，例如 user-button。
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// 首次被数据或状态接口使用后写入；未用过为 null。
    /// </summary>
    public CacheKind? Kind { get; internal set; }

    /// <summary>
    /// 把业务标识转成 Key 里的标识段；默认 ToString。
    /// </summary>
    public Func<object?, string> IdFormatter { get; init; } = static id =>
        id is null ? string.Empty : Convert.ToString(id) ?? string.Empty;

    /// <summary>
    /// 注册用的完整逻辑键：模块:名称。
    /// </summary>
    public string RegistryKey => $"{Module}:{Name}";
}
