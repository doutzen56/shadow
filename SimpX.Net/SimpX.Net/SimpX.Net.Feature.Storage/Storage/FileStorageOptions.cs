namespace SimpX.Net.Feature.Storage;

/// <summary>
/// 文件存储配置，从配置里的 FileStorage 这一段读取。
/// </summary>
public sealed class FileStorageOptions : IConfigurableOptions
{
    /// <summary>
    /// 存储方式：Local 写到站点目录，Oss 对象存储（还没实现，调用会失败）。
    /// 不写时按 Local。
    /// </summary>
    public string Provider { get; set; }
}
