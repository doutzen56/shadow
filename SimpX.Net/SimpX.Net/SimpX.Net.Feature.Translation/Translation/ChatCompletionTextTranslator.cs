using System.Text.Json;
using System.Text.Json.Serialization;
using SimpX.Net.Feature.Http;

namespace SimpX.Net.Feature.Translation;

/// <summary>
/// 默认文本翻译：调用兼容 chat/completions 协议的大模型接口，用固定的系统提示词约束只翻译、不改写。
/// 接口地址、密钥、模型都从 <see cref="TextTranslatorOptions"/> 读取，不绑定具体厂商。
/// 出站 HTTP 走 <see cref="IHttpCaller"/>（Furion HttpRemote 薄封装）。
/// </summary>
public sealed class ChatCompletionTextTranslator : ITextTranslator
{
    private readonly IHttpCaller _httpCaller;
    private readonly IOptionsMonitor<TextTranslatorOptions> _options;

    /// <summary>
    /// 注入 HTTP 调用与翻译配置。
    /// </summary>
    /// <param name="httpCaller">外部 HTTP 调用</param>
    /// <param name="options">翻译配置</param>
    public ChatCompletionTextTranslator(IHttpCaller httpCaller, IOptionsMonitor<TextTranslatorOptions> options)
    {
        _httpCaller = httpCaller;
        _options = options;
    }

    /// <summary>
    /// 调用对话补全接口翻译一段文本。
    /// </summary>
    /// <param name="text">原文</param>
    /// <param name="targetLang">目标语言代码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>译文</returns>
    /// <exception cref="AppFriendlyException">没开通、接口返回失败或结果为空</exception>
    public async Task<string> TranslateAsync(string text, string targetLang, CancellationToken cancellationToken = default)
    {
        var options = _options.CurrentValue;
        if (!options.IsConfigured) throw Oops.Oh("未配置翻译服务");

        var requestBody = new ChatRequest
        {
            Model = options.Model,
            Temperature = 0.3,
            MaxTokens = 2000,
            Messages =
            [
                new ChatMessage { Role = "system", Content = BuildSystemPrompt(options.SourceLang, targetLang) },
                new ChatMessage { Role = "user", Content = text }
            ]
        };

        // EnsureSuccess 关：自己解析错误正文，避免把整段响应塞进通用异常
        var response = await _httpCaller.SendAsync(new HttpCallRequest
        {
            Method = HttpMethod.Post,
            Url = options.ApiUrl,
            JsonContent = requestBody,
            Headers = new Dictionary<string, string>
            {
                ["Authorization"] = $"Bearer {options.ApiKey}"
            },
            Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds),
            EnsureSuccessStatusCode = false
        }, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = TryDeserialize<ChatErrorResponse>(response.Body)?.Error?.Message;
            throw Oops.Oh($"翻译服务返回错误：{error ?? $"HTTP {(int)response.StatusCode}"}");
        }

        var content = TryDeserialize<ChatResponse>(response.Body)?.Choices?.FirstOrDefault()?.Message?.Content;
        if (string.IsNullOrWhiteSpace(content)) throw Oops.Oh("翻译服务返回的结果为空");

        return content.Trim();
    }

    /// <summary>
    /// 尝试把接口返回反序列化成指定类型，格式不对时返回空。
    /// </summary>
    private static T TryDeserialize<T>(string json) where T : class
    {
        try { return JsonSerializer.Deserialize<T>(json); }
        catch (JsonException) { return null; }
    }

    /// <summary>
    /// 生成系统提示词：只做逐句翻译，占位符和符号原样保留。
    /// </summary>
    private static string BuildSystemPrompt(string sourceLang, string targetLang)
    {
        return $@"作为企业软件系统专业翻译，严格遵守以下铁律：

■ 核心原则
1. 严格逐符号翻译（{sourceLang}→{targetLang}）
2. 禁止添加/删除/改写任何内容
3. 保持批量翻译的编号格式

■ 符号保留规则
! 所有符号必须原样保留：
• 编程符号：\${{ }} <% %> @ # & |
• UI占位符：{{0}} %s [ ]
• 货币单位：¥100.00 kg cm²
• 中文符号：【 】 《 》 ：

■ 中文符号位置规范
# 三级处理机制：
1. 成对符号必须保持完整结构：
   ✓ 正确：【Warning】Text
   ✗ 禁止：Warning【 】Text

2. 独立符号位置：
   • 优先句尾 → Text】?
   • 次选句首 → 】Text?
   • 禁止句中 → Text】Text?

3. 跨字符串符号处理：
   • 前段含【时 → 保留在段尾（""Synchronize【""）
   • 后段含】时 → 保留在段首（""】authorization data?""）
   • 符号后接字母时添加空格：】 Authorization

■ 语法规范
• 外文 → 被动语态（""Item was created""）
• 中文 → 主动语态（""已创建项目""）
• 禁止推测上下文（只翻译当前字符串内容）

■ 错误预防（绝对禁止）
✗ 将中文符号改为西式符号（】→]）
✗ 移动非中文符号位置
✗ 添加原文不存在的内容
✗ 合并/拆分原始字符串

■ 批量处理
▸ 严格保持原始JSON结构
▸ 语言键名精确匹配（zh-cn/en/it等）";
    }

    /// <summary>对话补全请求。</summary>
    private sealed class ChatRequest
    {
        [JsonPropertyName("model")] public string Model { get; set; }
        [JsonPropertyName("messages")] public ChatMessage[] Messages { get; set; }
        [JsonPropertyName("temperature")] public double Temperature { get; set; }
        [JsonPropertyName("max_tokens")] public int MaxTokens { get; set; }
    }

    /// <summary>一条对话消息。</summary>
    private sealed class ChatMessage
    {
        [JsonPropertyName("role")] public string Role { get; set; }
        [JsonPropertyName("content")] public string Content { get; set; }
    }

    /// <summary>对话补全返回。</summary>
    private sealed class ChatResponse
    {
        [JsonPropertyName("choices")] public ChatChoice[] Choices { get; set; }
    }

    /// <summary>一个候选结果。</summary>
    private sealed class ChatChoice
    {
        [JsonPropertyName("message")] public ChatMessage Message { get; set; }
    }

    /// <summary>接口错误返回。</summary>
    private sealed class ChatErrorResponse
    {
        [JsonPropertyName("error")] public ChatError Error { get; set; }
    }

    /// <summary>接口错误说明。</summary>
    private sealed class ChatError
    {
        [JsonPropertyName("message")] public string Message { get; set; }
    }
}
