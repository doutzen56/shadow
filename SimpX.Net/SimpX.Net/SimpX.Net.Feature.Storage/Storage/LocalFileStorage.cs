using Microsoft.AspNetCore.Hosting;

namespace SimpX.Net.Feature.Storage;

/// <summary>
/// 默认文件存储：存到宿主的站点根目录（wwwroot）下，存储键就是相对站点根的路径，静态文件中间件可以直接访问。
/// </summary>
public sealed class LocalFileStorage : IFileStorage
{
    private readonly string _rootPath;

    /// <summary>
    /// 用宿主的站点根目录作为存储根；宿主没有 wwwroot 时用内容根下的 wwwroot。
    /// </summary>
    /// <param name="environment">宿主环境</param>
    public LocalFileStorage(IWebHostEnvironment environment)
    {
        _rootPath = Path.GetFullPath(environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot"));
    }

    /// <inheritdoc />
    public string Name => "Local";

    /// <inheritdoc />
    public async Task SaveAsync(string key, Stream content, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolvePath(key);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var file = File.Create(fullPath);
        await content.CopyToAsync(file, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolvePath(key);
        if (!File.Exists(fullPath)) throw Oops.Oh($"文件不存在：{key}");

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
        return Task.FromResult(stream);
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(File.Exists(ResolvePath(key)));
    }

    /// <inheritdoc />
    public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolvePath(key);
        if (File.Exists(fullPath)) File.Delete(fullPath);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public string GetUrl(string key) => NormalizeKey(key);

    /// <summary>
    /// 把存储键换成存储根下的物理路径。存储键可能来自数据库或调用方，
    /// 带 <c>..</c> 跳出存储根的一律拒绝，防止读写到站点根以外的文件。
    /// </summary>
    /// <param name="key">存储键</param>
    /// <returns>物理路径</returns>
    /// <exception cref="AppFriendlyException">存储键为空或跳出了存储根</exception>
    private string ResolvePath(string key)
    {
        var normalized = NormalizeKey(key);
        if (string.IsNullOrEmpty(normalized)) throw Oops.Oh("文件存储键不能为空");

        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, normalized));
        var rootWithSeparator = _rootPath.EndsWith(Path.DirectorySeparatorChar) ? _rootPath : _rootPath + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
            throw Oops.Oh("文件存储键不合法");

        return fullPath;
    }

    /// <summary>
    /// 统一存储键格式：反斜杠换成正斜杠，去掉开头的斜杠。
    /// </summary>
    /// <param name="key">存储键</param>
    /// <returns>统一后的存储键</returns>
    private static string NormalizeKey(string key) => (key ?? string.Empty).Replace('\\', '/').TrimStart('/');
}
