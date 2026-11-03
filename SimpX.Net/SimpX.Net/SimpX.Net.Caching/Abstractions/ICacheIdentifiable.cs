namespace SimpX.Net.Caching;

/// <summary>
/// 缓存写入时可从对象上取业务标识；没有 Id 属性的类型可以实现这个接口。
/// </summary>
public interface ICacheIdentifiable
{
    /// <summary>
    /// 拼进缓存 Key 的业务标识。
    /// </summary>
    object CacheId { get; }
}
