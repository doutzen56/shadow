using System.Text.Json.Serialization;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 优惠活动列表筛选（对齐旧 /admin/award/events/list）。
/// </summary>
public sealed class SiteEventListInput
{
    /// <summary>模板类型整型；空不限。</summary>
    public string Type { get; set; }

    /// <summary>分类编号；0 不限。</summary>
    public int ColID { get; set; }

    /// <summary>库内状态整型；空不限。</summary>
    public string Status { get; set; }

    /// <summary>标题包含。</summary>
    public string Title { get; set; }

    /// <summary>1=只看置顶（Sort≠0）。</summary>
    public int IsTop { get; set; }

    /// <summary>支持币种名或整型串，匹配 SupportCurrency。</summary>
    public string Currency { get; set; }
}

/// <summary>
/// 活动列表行。
/// </summary>
public sealed class SiteEventListItemDto
{
    /// <summary>活动编号。</summary>
    [JsonPropertyName("ID")]
    public int ID { get; set; }

    /// <summary>标题。</summary>
    [JsonPropertyName("Title")]
    public string Title { get; set; }

    /// <summary>类型整型。</summary>
    [JsonPropertyName("Type")]
    public int Type { get; set; }

    /// <summary>分类编号。</summary>
    [JsonPropertyName("ColID")]
    public int ColID { get; set; }

    /// <summary>分类名。</summary>
    [JsonPropertyName("Column")]
    public string Column { get; set; }

    /// <summary>创建时间。</summary>
    [JsonPropertyName("CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>开始。</summary>
    [JsonPropertyName("StartAt")]
    public DateTime StartAt { get; set; }

    /// <summary>结束。</summary>
    [JsonPropertyName("ExpireAt")]
    public DateTime ExpireAt { get; set; }

    /// <summary>参与人数。</summary>
    [JsonPropertyName("Member")]
    public int Member { get; set; }

    /// <summary>展示状态文案（进行中/待上线/已过期）。</summary>
    [JsonPropertyName("Status")]
    public string Status { get; set; }

    /// <summary>是否开启。</summary>
    [JsonPropertyName("IsOpen")]
    public bool IsOpen { get; set; }

    /// <summary>登录前后任一弹窗。</summary>
    [JsonPropertyName("IsLoginOpen")]
    public bool IsLoginOpen { get; set; }

    /// <summary>登录后弹窗。</summary>
    [JsonPropertyName("IsLoginAfterOpen")]
    public bool IsLoginAfterOpen { get; set; }

    /// <summary>登录前弹窗。</summary>
    [JsonPropertyName("IsLoginBeforeOpen")]
    public bool IsLoginBeforeOpen { get; set; }

    /// <summary>快捷入口。</summary>
    [JsonPropertyName("IsQuickEntrance")]
    public bool IsQuickEntrance { get; set; }

    /// <summary>显示详情。</summary>
    [JsonPropertyName("IsDetail")]
    public bool IsDetail { get; set; }

    /// <summary>新手任务。</summary>
    [JsonPropertyName("IsNewerTask")]
    public bool IsNewerTask { get; set; }

    /// <summary>跳转类型。</summary>
    [JsonPropertyName("JumpType")]
    public byte JumpType { get; set; }

    /// <summary>支持币种。</summary>
    [JsonPropertyName("SupportCurrency")]
    public string SupportCurrency { get; set; }

    /// <summary>备注。</summary>
    [JsonPropertyName("Demo")]
    public string Demo { get; set; }

    /// <summary>是否置顶。</summary>
    [JsonPropertyName("IsTop")]
    public bool IsTop { get; set; }

    /// <summary>是否显示。</summary>
    [JsonPropertyName("IsVisible")]
    public bool IsVisible { get; set; }
}


/// <summary>
/// 优惠活动详情（对齐旧版 info 常用字段；不含游戏树等附属下拉）。
/// </summary>
public sealed class SiteEventDetailDto
{
    /// <summary>编号。</summary>
    [JsonPropertyName("ID")]
    public int ID { get; set; }

    /// <summary>栏目。</summary>
    [JsonPropertyName("ColID")]
    public int ColID { get; set; }

    /// <summary>标题 JSON。</summary>
    [JsonPropertyName("Title")]
    public string Title { get; set; }

    /// <summary>副标题 JSON。</summary>
    [JsonPropertyName("SubTitle")]
    public string SubTitle { get; set; }

    /// <summary>标签 JSON。</summary>
    [JsonPropertyName("Tag")]
    public string Tag { get; set; }

    /// <summary>封面 JSON。</summary>
    [JsonPropertyName("Cover")]
    public string Cover { get; set; }

    /// <summary>摘要 JSON。</summary>
    [JsonPropertyName("Description")]
    public string Description { get; set; }

    /// <summary>正文 JSON。</summary>
    [JsonPropertyName("Content")]
    public string Content { get; set; }

    /// <summary>模板类型。</summary>
    [JsonPropertyName("Type")]
    public int Type { get; set; }

    /// <summary>开始。</summary>
    [JsonPropertyName("StartAt")]
    public DateTime StartAt { get; set; }

    /// <summary>结束。</summary>
    [JsonPropertyName("ExpireAt")]
    public DateTime ExpireAt { get; set; }

    /// <summary>是否显示。</summary>
    [JsonPropertyName("IsVisible")]
    public bool IsVisible { get; set; }

    /// <summary>是否开启。</summary>
    [JsonPropertyName("IsOpen")]
    public bool IsOpen { get; set; }

    /// <summary>登录后弹窗。</summary>
    [JsonPropertyName("IsLoginAfterOpen")]
    public bool IsLoginAfterOpen { get; set; }

    /// <summary>登录前弹窗。</summary>
    [JsonPropertyName("IsLoginBeforeOpen")]
    public bool IsLoginBeforeOpen { get; set; }

    /// <summary>快捷入口。</summary>
    [JsonPropertyName("IsQuickEntrance")]
    public bool IsQuickEntrance { get; set; }

    /// <summary>显示详情。</summary>
    [JsonPropertyName("IsDetail")]
    public bool IsDetail { get; set; }

    /// <summary>新手任务。</summary>
    [JsonPropertyName("IsNewerTask")]
    public bool IsNewerTask { get; set; }

    /// <summary>跳转类型。</summary>
    [JsonPropertyName("JumpType")]
    public byte JumpType { get; set; }

    /// <summary>排序。</summary>
    [JsonPropertyName("Sort")]
    public short Sort { get; set; }

    /// <summary>发放币种。</summary>
    [JsonPropertyName("Currency")]
    public int Currency { get; set; }

    /// <summary>支持币种。</summary>
    [JsonPropertyName("SupportCurrency")]
    public string SupportCurrency { get; set; }

    /// <summary>模板参数 JSON。</summary>
    [JsonPropertyName("Setting")]
    public string Setting { get; set; }

    /// <summary>备注。</summary>
    [JsonPropertyName("Demo")]
    public string Demo { get; set; }

    /// <summary>皮肤。</summary>
    [JsonPropertyName("Skin")]
    public string Skin { get; set; }

    /// <summary>自定义视图。</summary>
    [JsonPropertyName("Views")]
    public string Views { get; set; }

    /// <summary>背景。</summary>
    [JsonPropertyName("Background")]
    public string Background { get; set; }

    /// <summary>排除流水游戏。</summary>
    [JsonPropertyName("FilterGame")]
    public string FilterGame { get; set; }

    /// <summary>排除转入。</summary>
    [JsonPropertyName("FilterPlatform")]
    public string FilterPlatform { get; set; }
}

/// <summary>
/// 优惠活动保存入参。保存时跑类型唯一与 Setting 主校验（对齐旧 EventAgent）。
/// </summary>
public sealed class SiteEventSaveInput
{
    /// <summary>编号；0=新增。</summary>
    public int ID { get; set; }

    /// <summary>栏目。</summary>
    public int ColID { get; set; }

    /// <summary>标题。</summary>
    public string Title { get; set; }

    /// <summary>副标题。</summary>
    public string SubTitle { get; set; }

    /// <summary>标签。</summary>
    public string Tag { get; set; }

    /// <summary>封面。</summary>
    public string Cover { get; set; }

    /// <summary>摘要；空则截取 Content 前缀。</summary>
    public string Description { get; set; }

    /// <summary>正文。</summary>
    public string Content { get; set; }

    /// <summary>模板类型。</summary>
    public int Type { get; set; }

    /// <summary>开始。</summary>
    public DateTime StartAt { get; set; }

    /// <summary>结束。</summary>
    public DateTime ExpireAt { get; set; }

    /// <summary>是否显示。</summary>
    public bool IsVisible { get; set; }

    /// <summary>是否开启。</summary>
    public bool IsOpen { get; set; }

    /// <summary>登录后弹窗。</summary>
    public bool IsLoginAfterOpen { get; set; }

    /// <summary>登录前弹窗。</summary>
    public bool IsLoginBeforeOpen { get; set; }

    /// <summary>快捷入口。</summary>
    public bool IsQuickEntrance { get; set; }

    /// <summary>显示详情。</summary>
    public bool IsDetail { get; set; }

    /// <summary>新手任务。</summary>
    public bool IsNewerTask { get; set; }

    /// <summary>跳转类型。</summary>
    public byte JumpType { get; set; }

    /// <summary>排序。</summary>
    public short Sort { get; set; }

    /// <summary>发放币种。</summary>
    public int Currency { get; set; }

    /// <summary>支持币种。</summary>
    public string SupportCurrency { get; set; }

    /// <summary>模板参数 JSON。</summary>
    public string Setting { get; set; }

    /// <summary>备注。</summary>
    public string Demo { get; set; }

    /// <summary>皮肤。</summary>
    public string Skin { get; set; }

    /// <summary>自定义视图。</summary>
    public string Views { get; set; }

    /// <summary>背景。</summary>
    public string Background { get; set; }

    /// <summary>排除流水游戏。</summary>
    public string FilterGame { get; set; }

    /// <summary>排除转入。</summary>
    public string FilterPlatform { get; set; }
}
