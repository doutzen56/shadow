namespace SimpX.Net.Feature.Storage;

/// <summary>
/// 对象存储占位。配置写成 Oss 时会注册它，调用任何方法都会直接失败，避免悄悄写到本机磁盘。
/// </summary>
public sealed class OssFileStorage : IFileStorage
{
    private const string Message = "对象存储还没接上。请把 FileStorage:Provider 改成 Local，或等对象存储实现后再用。";

    /// <inheritdoc />
    public string Name => "Oss";

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">对象存储还没实现</exception>
    public Task SaveAsync(string key, Stream content, CancellationToken cancellationToken = default)
        => throw new InvalidOperationException(Message);

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">对象存储还没实现</exception>
    public Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken = default)
        => throw new InvalidOperationException(Message);

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">对象存储还没实现</exception>
    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        => throw new InvalidOperationException(Message);

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">对象存储还没实现</exception>
    public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
        => throw new InvalidOperationException(Message);

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">对象存储还没实现</exception>
    public string GetUrl(string key)
        => throw new InvalidOperationException(Message);
}
