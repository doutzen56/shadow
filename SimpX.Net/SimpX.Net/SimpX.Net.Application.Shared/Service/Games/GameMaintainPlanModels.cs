using System.Text.Json.Serialization;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 系统游戏维护计划。对齐旧 MaintainPlan。
/// </summary>
public sealed class GameMaintainPlanDto
{
    /// <summary>游戏类型。</summary>
    [JsonPropertyName("Type")]
    public int Type { get; set; }

    /// <summary>频率：0 临时 / 1 每周 / 2 每月。</summary>
    [JsonPropertyName("Frequency")]
    public int Frequency { get; set; }

    /// <summary>频率值（临时=开始日期，每周=星期几，每月=第几天）。</summary>
    [JsonPropertyName("FrequencyValue")]
    public string FrequencyValue { get; set; }

    /// <summary>结束日期（仅临时；空=与开始同一天）。</summary>
    [JsonPropertyName("EndDate")]
    public string EndDate { get; set; }

    /// <summary>开始时刻 HH:mm。</summary>
    [JsonPropertyName("StartAt")]
    public string StartAt { get; set; }

    /// <summary>结束时刻 HH:mm。</summary>
    [JsonPropertyName("EndAt")]
    public string EndAt { get; set; }

    /// <summary>维护分类 CSV。</summary>
    [JsonPropertyName("Maintenance")]
    public string Maintenance { get; set; }

    /// <summary>本周期跳过标记：1 表示人工打断。</summary>
    [JsonPropertyName("SkipCycleFlag")]
    public int SkipCycleFlag { get; set; }

    /// <summary>跳过过期时间；过了之后即使标记仍可再维护。</summary>
    [JsonPropertyName("SkipCycleExpireAt")]
    public DateTime? SkipCycleExpireAt { get; set; }
}

/// <summary>维护计划窗口判定结果。</summary>
public enum GameMaintainPlanWindow
{
    /// <summary>不在窗口内。</summary>
    None = 0,

    /// <summary>应进入维护。</summary>
    Enter = 1,

    /// <summary>应恢复正常。</summary>
    Resume = 2
}

/// <summary>频率常量（对齐旧 FrequencyType）。</summary>
public static class GameMaintainFrequency
{
    /// <summary>临时。</summary>
    public const int Temporary = 0;

    /// <summary>每周。</summary>
    public const int EveryWeek = 1;

    /// <summary>每月。</summary>
    public const int EveryMonth = 2;
}

/// <summary>系统游戏状态常量。</summary>
public static class GameMaintainStatus
{
    /// <summary>正常。</summary>
    public const int Normal = 1;

    /// <summary>维护。</summary>
    public const int Maintain = 2;

    /// <summary>部分维护。</summary>
    public const int MaintainPart = 3;
}
