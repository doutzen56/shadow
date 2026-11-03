namespace SimpX.Net.Feature.Storage;

/// <summary>
/// 文件存储：按“存储键”存取文件内容，不关心业务表和请求。
/// 存储键是相对路径，用正斜杠分隔，如 <c>upload/2026/10/26/123.png</c>。
/// 按配置 FileStorage:Provider 选择：Local 用 <see cref="LocalFileStorage"/>（存站点根目录下），Oss 用尚未实现的 <see cref="OssFileStorage"/>。应用先注册了别的实现时用应用的。
/// </summary>
public interface IFileStorage
{
    /// <summary>
    /// 存储名称，会记到文件记录上，方便排查文件存在哪。
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 保存文件，同名存储键已存在时覆盖。
    /// </summary>
    /// <param name="key">存储键</param>
    /// <param name="content">文件内容，调用方负责释放</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    Task SaveAsync(string key, Stream content, CancellationToken cancellationToken = default);

    /// <summary>
    /// 打开文件读取流，调用方负责释放。
    /// </summary>
    /// <param name="key">存储键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件读取流</returns>
    /// <exception cref="AppFriendlyException">文件不存在</exception>
    Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// 判断文件是否存在。
    /// </summary>
    /// <param name="key">存储键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>存在返回 true</returns>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除文件，文件不存在时直接返回。
    /// </summary>
    /// <param name="key">存储键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    Task DeleteAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// 取文件的访问地址，记到文件记录的 Url 上给前端用。
    /// </summary>
    /// <param name="key">存储键</param>
    /// <returns>访问地址；本地存储返回相对站点根的路径</returns>
    string GetUrl(string key);
}
