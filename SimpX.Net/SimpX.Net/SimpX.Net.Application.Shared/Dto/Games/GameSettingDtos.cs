using System.Text.Json.Serialization;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 厂商 API 列表筛选（对齐旧版 gamesetlist）。
/// </summary>
public sealed class GameSettingListInput
{
    /// <summary>开启筛选：空不限；1/0。</summary>
    public string IsOpen { get; set; }

    /// <summary>币种包含（sys Currencys）。</summary>
    public string Currency { get; set; }

    /// <summary>分类包含（sys GameCategorys）。</summary>
    public string GameCategory { get; set; }
}

/// <summary>
/// 厂商 API 列表行（对齐旧版 gamesetlist 投影；标签名/维护时刻本刀简化）。
/// </summary>
public sealed class GameSettingListItemDto
{
    /// <summary>游戏类型整型（旧 Type）。</summary>
    [JsonPropertyName("Type")]
    public int Type { get; set; }

    /// <summary>展示名（旧 Game 标签文案；本刀用站点 Name 或系统名）。</summary>
    [JsonPropertyName("Game")]
    public string Game { get; set; }

    /// <summary>站点自定义名。</summary>
    [JsonPropertyName("Name")]
    public string Name { get; set; }

    /// <summary>是否开启。</summary>
    [JsonPropertyName("IsOpen")]
    public bool IsOpen { get; set; }

    /// <summary>剩余分。</summary>
    [JsonPropertyName("Credit")]
    public decimal Credit { get; set; }

    /// <summary>月结。</summary>
    [JsonPropertyName("IsMonthly")]
    public bool IsMonthly { get; set; }

    /// <summary>免转。</summary>
    [JsonPropertyName("IsFree")]
    public bool IsFree { get; set; }

    /// <summary>条件。</summary>
    [JsonPropertyName("Rate")]
    public decimal Rate { get; set; }

    /// <summary>标签原文。</summary>
    [JsonPropertyName("Label")]
    public string Label { get; set; }

    /// <summary>背景。</summary>
    [JsonPropertyName("Backdrop")]
    public string Backdrop { get; set; }

    /// <summary>币种。</summary>
    [JsonPropertyName("Currencys")]
    public string Currencys { get; set; }

    /// <summary>语言。</summary>
    [JsonPropertyName("Languages")]
    public string Languages { get; set; }

    /// <summary>分类。</summary>
    [JsonPropertyName("GameCategorys")]
    public string GameCategorys { get; set; }

    /// <summary>Logo。</summary>
    [JsonPropertyName("Logo")]
    public string Logo { get; set; }

    /// <summary>推荐背景。</summary>
    [JsonPropertyName("IsBackdrop")]
    public bool IsBackdrop { get; set; }

    /// <summary>详细点位。</summary>
    [JsonPropertyName("DetailRate")]
    public string DetailRate { get; set; }

    /// <summary>最低转账。</summary>
    [JsonPropertyName("MinTransfer")]
    public int MinTransfer { get; set; }

    /// <summary>最高转账。</summary>
    [JsonPropertyName("MaxTransfer")]
    public int MaxTransfer { get; set; }

    /// <summary>分线状态。</summary>
    [JsonPropertyName("Status")]
    public int Status { get; set; }
}

/// <summary>
/// 厂商 API 详情（对齐旧版 setting/info）。
/// </summary>
public sealed class GameSettingDetailDto
{
    /// <summary>类型整型。</summary>
    [JsonPropertyName("Type")]
    public int Type { get; set; }

    /// <summary>展示名。</summary>
    [JsonPropertyName("Game")]
    public string Game { get; set; }

    /// <summary>名称。</summary>
    [JsonPropertyName("Name")]
    public string Name { get; set; }

    /// <summary>开启。</summary>
    [JsonPropertyName("IsOpen")]
    public bool IsOpen { get; set; }

    /// <summary>自有接口。</summary>
    [JsonPropertyName("IsSelf")]
    public bool IsSelf { get; set; }

    /// <summary>计流水。</summary>
    [JsonPropertyName("IsWater")]
    public bool IsWater { get; set; }

    /// <summary>最低转账。</summary>
    [JsonPropertyName("MinTransfer")]
    public int MinTransfer { get; set; }

    /// <summary>最高转账。</summary>
    [JsonPropertyName("MaxTransfer")]
    public int MaxTransfer { get; set; }

    /// <summary>状态。</summary>
    [JsonPropertyName("Status")]
    public int Status { get; set; }

    /// <summary>自有接口参数 JSON；非自有为 null 字符串语义。</summary>
    [JsonPropertyName("Setting")]
    public string Setting { get; set; }
}

/// <summary>
/// 厂商 API 保存入参（对齐旧版 /admin/game/setting/save）。
/// </summary>
public sealed class GameSettingSaveInput
{
    /// <summary>游戏类型整型。</summary>
    public int Game { get; set; }

    /// <summary>自定义名。</summary>
    public string Name { get; set; }

    /// <summary>是否计流水。</summary>
    public bool IsWater { get; set; }

    /// <summary>最低转账。</summary>
    public int MinTransfer { get; set; }

    /// <summary>最高转账。</summary>
    public int MaxTransfer { get; set; }

    /// <summary>自有接口 Setting JSON；可空。</summary>
    public string Setting { get; set; }
}
