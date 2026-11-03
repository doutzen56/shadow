namespace SimpX.Net.Data.Entity.Users;

/// <summary>
/// 用户的活动参与日志
/// </summary>
[SugarTable("usr_EventLog")]
public class EventLog
{
    /// <summary>
    /// 发放日志编号
    /// </summary>
    [SugarColumn(ColumnName = "LogID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    /// <summary>
    /// 活动ID,关联 site_Event.EventID
    /// </summary>
    [SugarColumn(ColumnName = "EventID")]
    public int EventID { get; set; }

    /// <summary>
    /// 活动类型
    /// </summary>
    [SugarColumn(ColumnName = "Type")]
    public int Type { get; set; }

    /// <summary>
    /// 站点ID
    /// </summary>
    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    /// <summary>
    /// 用户ID
    /// </summary>
    [SugarColumn(ColumnName = "UserID")]
    public int UserID { get; set; }

    /// <summary>
    /// 奖励创建时间
    /// </summary>
    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>
    /// 要发放的金额
    /// </summary>
    [SugarColumn(ColumnName = "Reward")]
    public decimal Reward { get; set; }

    /// <summary>
    /// 要发放的积分
    /// </summary>
    [SugarColumn(ColumnName = "Coin")]
    public short Coin { get; set; }

    /// <summary>
    /// 活动状态
    /// </summary>
    [SugarColumn(ColumnName = "Status")]
    public int Status { get; set; }

    /// <summary>
    /// 失败信息
    /// </summary>
    [SugarColumn(ColumnName = "ErrorMsg")]
    public string ErrorMsg { get; set; }

    /// <summary>
    /// 领取活动的IP地址
    /// </summary>
    [SugarColumn(ColumnName = "IP")]
    public string IP { get; set; }

    /// <summary>
    /// 数字型表示的IP
    /// </summary>
    [SugarColumn(ColumnName = "IPValue")]
    public long IPValue { get; set; }

    /// <summary>
    /// 发放方式（手动/自动）
    /// </summary>
    [SugarColumn(ColumnName = "Put")]
    public int Put { get; set; }

    /// <summary>
    /// 需要的流水金额
    /// </summary>
    [SugarColumn(ColumnName = "Flow")]
    public decimal Flow { get; set; }

    /// <summary>
    /// 预计发放时间 (或过期时间)
    /// </summary>
    [SugarColumn(ColumnName = "ExpectedAt")]
    public DateTime ExpectedAt { get; set; }

    /// <summary>
    /// 实际发放时间/拒绝时间
    /// </summary>
    [SugarColumn(ColumnName = "ReleaseAt")]
    public DateTime ReleaseAt { get; set; }

    /// <summary>
    /// 来源值（与EventID\UserID\SourceID 组成唯一值）
    /// </summary>
    [SugarColumn(ColumnName = "SourceID")]
    public string SourceID { get; set; }

    /// <summary>
    /// 审核备注信息
    /// </summary>
    [SugarColumn(ColumnName = "CheckMessage")]
    public string CheckMessage { get; set; }

    /// <summary>
    /// 发放币种
    /// </summary>
    [SugarColumn(ColumnName = "Currency")]
    public int Currency { get; set; }

    /// <summary>
    /// 领取方式（手动/自动）
    /// </summary>
    [SugarColumn(ColumnName = "IsAuto")]
    public int IsAuto { get; set; }

    /// <summary>
    /// 操作人
    /// </summary>
    [SugarColumn(ColumnName = "ReviewerID")]
    public int ReviewerID { get; set; }
}
