namespace SimpX.Net.Application.Admin;

/// <summary>
/// 一条已注册的缓存定义，给缓存管理页看有哪些缓存、能不能整表清空。
/// </summary>
public sealed class CacheDefinitionOutput
{
    /// <summary>
    /// 模块名。
    /// </summary>
    public string Module { get; set; }

    /// <summary>
    /// 缓存名。
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 模块加名称，例如 auth:token-blacklist。
    /// </summary>
    public string RegistryKey { get; set; }

    /// <summary>
    /// 类别：Data 是业务数据，State 是锁、计数、黑名单这类状态。
    /// </summary>
    public string Kind { get; set; }

    /// <summary>
    /// 过期秒数。现在过期在业务调用处指定，这里固定为空。
    /// </summary>
    public int? ExpirationSeconds { get; set; }

    /// <summary>
    /// 为 true 时清空会影响登录黑名单、锁或验证码，页面上要单独确认。
    /// </summary>
    public bool Caution { get; set; }
}
