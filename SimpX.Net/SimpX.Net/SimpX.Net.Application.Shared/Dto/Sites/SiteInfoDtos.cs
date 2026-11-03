using System.Text.Json;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 站点设置 get 出参。字段名与旧版 /admin/site/info/get 的 info 对齐。
/// </summary>
public sealed class SiteInfoGetOutput
{
    /// <summary>站点编号（旧版 ID）。</summary>
    public int ID { get; set; }

    /// <summary>站名。旧版是 TranslateModel JSON 字符串；不含 { 时旧版返回 {}。</summary>
    public string Name { get; set; }

    /// <summary>主币种。</summary>
    public int Currency { get; set; }

    /// <summary>Logo（经图片地址处理后的展示路径）。</summary>
    public string Logo { get; set; }

    /// <summary>默认头像（经图片地址处理）。</summary>
    public string UserLogo { get; set; }

    /// <summary>站点图标（经图片地址处理）。</summary>
    public string Icon { get; set; }

    /// <summary>APIKey 的 MD5（展示用，不是明文密钥）。</summary>
    public string Key { get; set; }

    /// <summary>PC 是否体育首页（来自 PCSetting.IsSportHome）。</summary>
    public string SiteIsSportHome { get; set; }

    /// <summary>站点 HomeTemplateName 列。</summary>
    public string HomeTemplateName { get; set; }

    /// <summary>PCSetting.HomeTemplateName。</summary>
    public string PCHomeTemplateName { get; set; }

    /// <summary>Setting 全量键值（对齐旧版 Setting.ToJson() 可读字段）。</summary>
    public Dictionary<string, object> Setting { get; set; }
}

/// <summary>
/// 站点设置 save 入参。对齐旧版 Form：Name、Currency、Setting.*、PCSetting.HomeTemplateName。
/// </summary>
public sealed class SiteInfoSaveInput
{
    /// <summary>站名。旧版必填。</summary>
    public string Name { get; set; }

    /// <summary>主币种。</summary>
    public int Currency { get; set; }

    /// <summary>PC 首页模板名；空则按旧版默认 casino。</summary>
    public string PCHomeTemplateName { get; set; }

    /// <summary>
    /// Setting 字段表。键与旧版 Setting 属性名一致；服务端会保留原 GoogleSecret，不允许客户端覆盖。
    /// </summary>
    public Dictionary<string, JsonElement> Setting { get; set; }
}
