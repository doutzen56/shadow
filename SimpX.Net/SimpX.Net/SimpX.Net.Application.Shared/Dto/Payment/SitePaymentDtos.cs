using System.Text.Json.Serialization;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 充值接口设定列表筛选（对齐旧 settinglist）。
/// </summary>
public sealed class SitePaymentListInput
{
    /// <summary>状态整型；空不限。</summary>
    public string Status { get; set; }

    /// <summary>分类；0 不限。</summary>
    public int CateID { get; set; }

    /// <summary>名称包含。</summary>
    public string Name { get; set; }

    /// <summary>币种整型；空不限。</summary>
    public string Currency { get; set; }
}

/// <summary>
/// 充值通道列表行（覆盖旧页常用列）。
/// </summary>
public sealed class SitePaymentListItemDto
{
    /// <summary>通道编号。</summary>
    [JsonPropertyName("ID")]
    public int ID { get; set; }

    /// <summary>分类名。</summary>
    [JsonPropertyName("Category")]
    public string Category { get; set; }

    /// <summary>开放分组名。</summary>
    [JsonPropertyName("GroupNames")]
    public string GroupNames { get; set; }

    /// <summary>渠道名。</summary>
    [JsonPropertyName("Name")]
    public string Name { get; set; }

    /// <summary>币种。</summary>
    [JsonPropertyName("Currency")]
    public int Currency { get; set; }

    /// <summary>内部备注。</summary>
    [JsonPropertyName("Remark")]
    public string Remark { get; set; }

    /// <summary>系统接口标识（无 SystemPayment 实体时用 SysID）。</summary>
    [JsonPropertyName("Payment")]
    public string Payment { get; set; }

    /// <summary>最低。</summary>
    [JsonPropertyName("MinMoney")]
    public decimal MinMoney { get; set; }

    /// <summary>最高。</summary>
    [JsonPropertyName("MaxMoney")]
    public decimal MaxMoney { get; set; }

    /// <summary>日累计停用额。</summary>
    [JsonPropertyName("MaxPay")]
    public decimal MaxPay { get; set; }

    /// <summary>今日收款（列表对齐旧页；无实时汇总时为 0）。</summary>
    [JsonPropertyName("Paid")]
    public decimal Paid { get; set; }

    /// <summary>排序。</summary>
    [JsonPropertyName("Sort")]
    public short Sort { get; set; }

    /// <summary>手续费率。</summary>
    [JsonPropertyName("Fee")]
    public decimal Fee { get; set; }

    /// <summary>成本费率。</summary>
    [JsonPropertyName("Cost")]
    public decimal Cost { get; set; }

    /// <summary>状态。</summary>
    [JsonPropertyName("Status")]
    public int Status { get; set; }
}

/// <summary>
/// 充值通道详情（对齐旧 savesetting 回显字段）。
/// </summary>
public sealed class SitePaymentDetailDto
{
    /// <summary>编号。</summary>
    [JsonPropertyName("ID")]
    public int ID { get; set; }

    /// <summary>分类。</summary>
    [JsonPropertyName("CateID")]
    public int CateID { get; set; }

    /// <summary>名称。</summary>
    [JsonPropertyName("Name")]
    public string Name { get; set; }

    /// <summary>备注。</summary>
    [JsonPropertyName("Remark")]
    public string Remark { get; set; }

    /// <summary>系统渠道。</summary>
    [JsonPropertyName("SysID")]
    public int SysID { get; set; }

    /// <summary>最低。</summary>
    [JsonPropertyName("MinMoney")]
    public decimal MinMoney { get; set; }

    /// <summary>最高。</summary>
    [JsonPropertyName("MaxMoney")]
    public decimal MaxMoney { get; set; }

    /// <summary>日累计停用额。</summary>
    [JsonPropertyName("MaxPay")]
    public decimal MaxPay { get; set; }

    /// <summary>状态。</summary>
    [JsonPropertyName("Status")]
    public int Status { get; set; }

    /// <summary>币种。</summary>
    [JsonPropertyName("Currency")]
    public int Currency { get; set; }

    /// <summary>排序。</summary>
    [JsonPropertyName("Sort")]
    public short Sort { get; set; }

    /// <summary>手续费比例（库内小数）。</summary>
    [JsonPropertyName("Fee")]
    public decimal Fee { get; set; }

    /// <summary>成本比例。</summary>
    [JsonPropertyName("Cost")]
    public decimal Cost { get; set; }

    /// <summary>Setting 原文。</summary>
    [JsonPropertyName("Setting")]
    public string Setting { get; set; }

    /// <summary>是否提示。</summary>
    [JsonPropertyName("IsNotify")]
    public bool IsNotify { get; set; }

    /// <summary>适用分组编号列表。</summary>
    [JsonPropertyName("GroupIDs")]
    public List<int> GroupIDs { get; set; }
}

/// <summary>
/// 充值通道保存入参（对齐旧 savesetting 核心校验与字段）。
/// </summary>
public sealed class SitePaymentSaveInput
{
    /// <summary>编号；0=新增。</summary>
    public int ID { get; set; }

    /// <summary>分类。</summary>
    public int CateID { get; set; }

    /// <summary>名称。</summary>
    public string Name { get; set; }

    /// <summary>备注。</summary>
    public string Remark { get; set; }

    /// <summary>系统渠道。</summary>
    public int SysID { get; set; }

    /// <summary>最低。</summary>
    public decimal MinMoney { get; set; }

    /// <summary>最高。</summary>
    public decimal MaxMoney { get; set; }

    /// <summary>日累计停用额。</summary>
    public decimal MaxPay { get; set; }

    /// <summary>状态。</summary>
    public int Status { get; set; }

    /// <summary>币种。</summary>
    public int Currency { get; set; }

    /// <summary>排序。</summary>
    public short Sort { get; set; }

    /// <summary>手续费比例（与旧页一致：提交百分数时前端先 /100，或直接传库内小数）。</summary>
    public decimal Fee { get; set; }

    /// <summary>成本比例。</summary>
    public decimal Cost { get; set; }

    /// <summary>Setting 原文或 JSON 对象串。</summary>
    public string Setting { get; set; }

    /// <summary>是否提示。</summary>
    public bool IsNotify { get; set; }

    /// <summary>适用分组编号。</summary>
    public List<int> GroupIDs { get; set; }
}
