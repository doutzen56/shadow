using System.Text.Json.Serialization;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 会员分组保存入参（对齐旧 save：Payment 逗号串，Withdraw/Condition JSON）。
/// </summary>
public sealed class UserGroupSaveInput
{
    /// <summary>编号；0=新增。</summary>
    public int ID { get; set; }

    /// <summary>名称。</summary>
    public string Name { get; set; }

    /// <summary>排序。</summary>
    public short Sort { get; set; }

    /// <summary>是否自动升级。</summary>
    public bool IsUpdate { get; set; }

    /// <summary>是否默认。</summary>
    public bool IsDefault { get; set; }

    /// <summary>是否夜间。</summary>
    public bool IsNight { get; set; }

    /// <summary>充值方式串。</summary>
    public string Payment { get; set; }

    /// <summary>提现配置串。</summary>
    public string WithdrawString { get; set; }

    /// <summary>升级条件串。</summary>
    public string ConditionString { get; set; }

    /// <summary>设置信息。</summary>
    public string SettingInfo { get; set; }
}

/// <summary>
/// 会员分组详情。
/// </summary>
public sealed class UserGroupDetailDto
{
    /// <summary>编号。</summary>
    [JsonPropertyName("ID")]
    public int ID { get; set; }

    /// <summary>名称。</summary>
    [JsonPropertyName("Name")]
    public string Name { get; set; }

    /// <summary>排序。</summary>
    [JsonPropertyName("Sort")]
    public short Sort { get; set; }

    /// <summary>是否自动升级。</summary>
    [JsonPropertyName("IsUpdate")]
    public bool IsUpdate { get; set; }

    /// <summary>是否默认。</summary>
    [JsonPropertyName("IsDefault")]
    public bool IsDefault { get; set; }

    /// <summary>是否夜间。</summary>
    [JsonPropertyName("IsNight")]
    public bool IsNight { get; set; }

    /// <summary>人数。</summary>
    [JsonPropertyName("Member")]
    public int Member { get; set; }

    /// <summary>充值方式。</summary>
    [JsonPropertyName("Payment")]
    public string Payment { get; set; }

    /// <summary>提现配置 JSON（对齐旧 Withdraw）。</summary>
    [JsonPropertyName("Withdraw")]
    public string Withdraw { get; set; }

    /// <summary>升级条件 JSON（对齐旧 Condition）。</summary>
    [JsonPropertyName("Condition")]
    public string Condition { get; set; }

    /// <summary>分组扩展 SettingInfo。</summary>
    [JsonPropertyName("SettingInfo")]
    public string SettingInfo { get; set; }
}
