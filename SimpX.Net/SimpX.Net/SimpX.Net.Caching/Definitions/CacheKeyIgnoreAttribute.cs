namespace SimpX.Net.Caching;

/// <summary>
/// 标在 key 文件的常量上：不是缓存名（例如在线用户哈希里的固定标识），启动扫描时跳过。
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class CacheKeyIgnoreAttribute : Attribute
{
}
