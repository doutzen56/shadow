namespace SimpX.Net.Caching;

/// <summary>
/// 缓存回源查库时打开的范围：实现方应保证期间读主库（兼容读写分离）。
/// 未注册实现时，回源直接查库，不做特殊处理。
/// </summary>
public interface ICacheReadThroughScope
{
    /// <summary>
    /// 打开读主库范围；释放时恢复原来的主从设置。
    /// </summary>
    /// <returns>可释放范围</returns>
    IDisposable Begin();
}
