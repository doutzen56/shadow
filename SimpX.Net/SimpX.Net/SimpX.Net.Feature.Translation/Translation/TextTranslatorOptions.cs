namespace SimpX.Net.Feature.Translation;

/// <summary>
/// 文本翻译配置，从配置里的 TextTranslator 这一段读取。密钥不要提交到代码库，放在不入库的 TextTranslator.json 或环境变量里。
/// </summary>
public sealed class TextTranslatorOptions : IConfigurableOptions
{
    /// <summary>
    /// 源语言代码。
    /// </summary>
    public string SourceLang { get; set; } = "zh-cn";

    /// <summary>
    /// 对话补全接口地址（兼容 chat/completions 协议的大模型服务都可以）。为空表示没开通翻译。
    /// </summary>
    public string ApiUrl { get; set; }

    /// <summary>
    /// 接口密钥，按 Bearer 方式放在请求头里。
    /// </summary>
    public string ApiKey { get; set; }

    /// <summary>
    /// 模型名称。
    /// </summary>
    public string Model { get; set; }

    /// <summary>
    /// 请求超时（秒）。
    /// </summary>
    public int TimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// 地址、密钥、模型都填了才算开通。
    /// </summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiUrl) && !string.IsNullOrWhiteSpace(ApiKey) && !string.IsNullOrWhiteSpace(Model);
}
