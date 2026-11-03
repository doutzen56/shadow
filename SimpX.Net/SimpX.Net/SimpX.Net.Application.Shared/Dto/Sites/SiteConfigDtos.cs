using System.Text.Json;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 站点参数配置：querystring 字段整段读写入参。
/// </summary>
public sealed class SiteConfigSettingSaveInput
{
    /// <summary>
    /// 表单键值。对齐旧版 Form.Fill&lt;Setting&gt;，键与 Setting 属性名一致。
    /// </summary>
    public Dictionary<string, JsonElement> Setting { get; set; }
}

/// <summary>
/// 银行/虚拟币勾选。对齐 savesitebank / savesitecoin。
/// </summary>
public sealed class SiteConfigBankToggleInput
{
    /// <summary>银行枚举名（Bank）。</summary>
    public string Bank { get; set; }

    /// <summary>虚拟币枚举名（Coin）。</summary>
    public string Coin { get; set; }

    /// <summary>1=选中，0=取消。</summary>
    public int Value { get; set; }
}

/// <summary>
/// 提示音单项保存。对齐 savesound：type + url。
/// </summary>
public sealed class SiteConfigSoundSaveInput
{
    /// <summary>声音类型键。</summary>
    public string Type { get; set; }

    /// <summary>文件地址。</summary>
    public string Url { get; set; }
}

/// <summary>
/// VIP 开始时间。对齐 savevip：StartAt。
/// </summary>
public sealed class SiteConfigVipSaveInput
{
    /// <summary>VIP 系统开始时间。</summary>
    public DateTime StartAt { get; set; }
}

/// <summary>
/// 注册站内信。对齐 saveregistermessage。
/// </summary>
public sealed class SiteConfigRegisterMessageSaveInput
{
    /// <summary>标题。</summary>
    public string Title { get; set; }

    /// <summary>内容。</summary>
    public string Content { get; set; }
}

/// <summary>
/// 手机验证码提示。对齐 saveregistermessagecode：Title 实际写入 PhoneCodeMessage。
/// </summary>
public sealed class SiteConfigRegisterMessageCodeSaveInput
{
    /// <summary>提示原文（旧版字段名 Title）。</summary>
    public string Title { get; set; }
}

/// <summary>
/// 短信区号勾选。对齐 savesmsareaconfig。
/// </summary>
public sealed class SiteConfigSmsAreaToggleInput
{
    /// <summary>区号。</summary>
    public string AreaCode { get; set; }

    /// <summary>1=选中，0=取消。</summary>
    public int Value { get; set; }
}

/// <summary>
/// 短信区号排序。对齐 savesmsareaconfigsort。
/// </summary>
public sealed class SiteConfigSmsAreaSortInput
{
    /// <summary>逗号分隔区号列表。</summary>
    public string SmsArea { get; set; }
}

/// <summary>
/// 背景配置保存。对齐 savebackgroundconfig。
/// </summary>
public sealed class SiteConfigBackgroundSaveInput
{
    /// <summary>勾选的背景类型，逗号分隔或表单里值为 1 的键。</summary>
    public Dictionary<string, JsonElement> Selected { get; set; }

    /// <summary>节日类型。</summary>
    public string Festival { get; set; }

    /// <summary>是否开放节日背景。</summary>
    public string FestivalStatus { get; set; }
}

/// <summary>
/// SEO 保存。对齐 saveseosetting（写在 Site.PCSetting）。
/// </summary>
public sealed class SiteConfigSeoSaveInput
{
    /// <summary>标题 JSON。</summary>
    public string Title { get; set; }

    /// <summary>关键词 JSON。</summary>
    public string Keyword { get; set; }

    /// <summary>描述 JSON。</summary>
    public string Description { get; set; }

    /// <summary>页脚。</summary>
    public string Footer { get; set; }
}

/// <summary>
/// Telegram 原文保存。对齐 savetelegram（整段 JSON）。
/// </summary>
public sealed class SiteConfigTelegramSaveInput
{
    /// <summary>Telegram 配置 JSON 原文。</summary>
    public string Telegram { get; set; }
}

/// <summary>
/// 短信测试。对齐 sendsmstest。
/// </summary>
public sealed class SiteConfigSmsTestInput
{
    /// <summary>语言。</summary>
    public string Language { get; set; }

    /// <summary>区号。</summary>
    public string Area { get; set; }

    /// <summary>手机号。</summary>
    public string Phone { get; set; }

    /// <summary>内容。</summary>
    public string Context { get; set; }
}

/// <summary>
/// 资金类型勾选保存。对齐 savemoneytypeconfig。
/// </summary>
public sealed class SiteConfigMoneyTypeSaveInput
{
    /// <summary>勾选表；值为 1 的键写入 MoneyType。</summary>
    public Dictionary<string, JsonElement> Selected { get; set; }
}

/// <summary>
/// 短信网关保存。对齐 savesmssetting。
/// </summary>
public sealed class SiteConfigSmsSaveInput
{
    /// <summary>网关类型等主字段。</summary>
    public Dictionary<string, JsonElement> Setting { get; set; }

    /// <summary>Setting.* 子参数，合并进 Config。</summary>
    public Dictionary<string, JsonElement> Config { get; set; }
}

/// <summary>
/// Jackpot 设置保存。对齐 savejackpotconfig。
/// </summary>
public sealed class SiteConfigJackpotSaveInput
{
    /// <summary>TimeSpan / TopRank / Live / Slot / Sport / Lottery 等。</summary>
    public Dictionary<string, JsonElement> Setting { get; set; }

    /// <summary>人工干预列表 JSON（InterventionList）。</summary>
    public string InterventionList { get; set; }
}

/// <summary>
/// 删除 Jackpot 行。</summary>
public sealed class SiteConfigIdInput
{
    /// <summary>编号。</summary>
    public int ID { get; set; }
}
