namespace SimpX.Net.Feature.Translation;

/// <summary>
/// 文本翻译。接入一家翻译服务就实现一个，并注册为 <see cref="ITextTranslator"/>；没有注册时调用翻译会直接报错。
/// </summary>
public interface ITextTranslator
{
    /// <summary>
    /// 把一段文本从源语言翻成目标语言，源语言见 <see cref="TextTranslatorOptions.SourceLang"/>。
    /// </summary>
    /// <param name="text">原文</param>
    /// <param name="targetLang">目标语言代码，如 en、zh-tw</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>译文</returns>
    /// <exception cref="AppFriendlyException">翻译服务返回失败或结果为空</exception>
    Task<string> TranslateAsync(string text, string targetLang, CancellationToken cancellationToken = default);
}
