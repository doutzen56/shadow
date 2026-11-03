namespace SimpX.Net.Application.Shared;

/// <summary>
/// 活动日志列表筛选。
/// </summary>
public sealed class EventLogListInput
{
    /// <summary>状态；空=全部；0 待审 / 1 失败 / 2 通过。</summary>
    public string Status { get; set; }

    /// <summary>活动类型；空=全部。</summary>
    public int? Type { get; set; }

    /// <summary>用户名（精确）。</summary>
    public string User { get; set; }

    /// <summary>来源单号。</summary>
    public string SourceID { get; set; }

    /// <summary>创建起。</summary>
    public DateTime? StartAt { get; set; }

    /// <summary>创建止。</summary>
    public DateTime? EndAt { get; set; }
}

/// <summary>
/// 活动日志列表行。
/// </summary>
public sealed class EventLogListItemDto
{
    /// <summary>日志编号。</summary>
    public int ID { get; set; }

    /// <summary>活动编号。</summary>
    public int EventID { get; set; }

    /// <summary>活动类型。</summary>
    public int Type { get; set; }

    /// <summary>会员编号。</summary>
    public int UserID { get; set; }

    /// <summary>用户名。</summary>
    public string UserName { get; set; }

    /// <summary>奖金。</summary>
    public decimal Reward { get; set; }

    /// <summary>流水要求。</summary>
    public decimal Flow { get; set; }

    /// <summary>状态。</summary>
    public int Status { get; set; }

    /// <summary>发放方式。</summary>
    public int Put { get; set; }

    /// <summary>领取方式。</summary>
    public int IsAuto { get; set; }

    /// <summary>币种。</summary>
    public int Currency { get; set; }

    /// <summary>来源单号。</summary>
    public string SourceID { get; set; }

    /// <summary>创建时间。</summary>
    public DateTime CreateAt { get; set; }

    /// <summary>审核备注。</summary>
    public string CheckMessage { get; set; }

    /// <summary>活动标题（可能为空）。</summary>
    public string EventTitle { get; set; }
}

/// <summary>
/// 活动日志审核入参。对齐旧活动审核通过/拒绝。
/// </summary>
public sealed class EventLogCheckInput
{
    /// <summary>活动日志编号。</summary>
    public int ID { get; set; }

    /// <summary>目标状态：2=通过，1=拒绝。</summary>
    public int Status { get; set; }

    /// <summary>审核备注。</summary>
    public string CheckMessage { get; set; }
}
