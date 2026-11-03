namespace SimpX.Net.Core;

public sealed class LocalizationSettingsOptions : IConfigurableOptions
{
    /// <summary>
    /// 语言列表
    /// </summary>
    public List<string> SupportedCultures { get; set; }

    /// <summary>
    /// 默认语言
    /// </summary>
    public string DefaultCulture { get; set; }

    /// <summary>
    /// 固定时间区域为特定时区（多语言）
    /// </summary>
    public string DateTimeFormatCulture { get; set; }
}