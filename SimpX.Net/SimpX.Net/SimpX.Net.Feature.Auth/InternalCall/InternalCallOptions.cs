namespace SimpX.Net.Feature.Auth;

/// <summary>
/// 宿主之间内部调用的配置，从配置里的 InternalCall 这一段读取。各宿主要配同一对 AccessKey / AccessSecret。
/// </summary>
public sealed class InternalCallOptions : IConfigurableOptions
{
    /// <summary>
    /// 调用方身份标识，签名时放在请求头 accessKey。
    /// </summary>
    public string AccessKey { get; set; }

    /// <summary>
    /// 签名密钥。只放部署配置，不写日志；开发环境的默认值上线前一定要换掉。
    /// </summary>
    public string AccessSecret { get; set; }

    /// <summary>
    /// 其它宿主的地址，键是宿主名（如 Job、Admin、Payment），值是站点根地址。
    /// </summary>
    public Dictionary<string, string> Services { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 单次调用超时（秒）。
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
}
