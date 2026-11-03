namespace SimpX.Net.Data.Entity.Users;

/// <summary>
/// 加款,减款审核表
/// </summary>
[SugarTable("usr_MoneyReview")]
public class MoneyReview
{
    /// <summary>
    /// 标识
    /// </summary>
    [SugarColumn(ColumnName = "ReviewID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

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
    /// 减款或加款金额
    /// </summary>
    [SugarColumn(ColumnName = "Money")]
    public decimal Money { get; set; }

    /// <summary>
    /// 资金类型
    /// </summary>
    [SugarColumn(ColumnName = "Type")]
    public int Type { get; set; }

    /// <summary>
    /// 要求的打码量流水倍数
    /// </summary>
    [SugarColumn(ColumnName = "Flow")]
    public decimal Flow { get; set; }

    /// <summary>
    /// 要求的最低流水
    /// </summary>
    [SugarColumn(ColumnName = "MinFlow")]
    public decimal MinFlow { get; set; }

    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>
    /// 提交该笔入款记录的管理员ID
    /// </summary>
    [SugarColumn(ColumnName = "CreatorID")]
    public int CreatorID { get; set; }

    /// <summary>
    /// 流水号
    /// </summary>
    [SugarColumn(ColumnName = "SourceID")]
    public string SourceID { get; set; }

    /// <summary>
    /// 业务类型,1-加款,2-减款
    /// </summary>
    [SugarColumn(ColumnName = "Action")]
    public int Action { get; set; }

    /// <summary>
    /// 审核的管理员ID
    /// </summary>
    [SugarColumn(ColumnName = "ReviewerID")]
    public int ReviewerID { get; set; }

    /// <summary>
    /// 审核时间
    /// </summary>
    [SugarColumn(ColumnName = "ReviewAt")]
    public DateTime ReviewAt { get; set; }

    /// <summary>
    /// 审核状态,0-待审核,1-审核失败,2-审核通过
    /// </summary>
    [SugarColumn(ColumnName = "Status")]
    public int Status { get; set; }

    /// <summary>
    /// 币种
    /// </summary>
    [SugarColumn(ColumnName = "Currency")]
    public int Currency { get; set; }

    /// <summary>
    /// 备注说明
    /// </summary>
    [SugarColumn(ColumnName = "LogDesc")]
    public string Description { get; set; }

    /// <summary>
    /// 排除流水活动ID
    /// </summary>
    [SugarColumn(ColumnName = "EventID")]
    public int EventID { get; set; }

    /// <summary>
    /// 排除转入游戏ID
    /// </summary>
    [SugarColumn(ColumnName = "EventIDNoTrans")]
    public int EventIDNoTrans { get; set; }

    /// <summary>
    /// 充值渠道
    /// </summary>
    [SugarColumn(ColumnName = "PayID")]
    public int PayID { get; set; }
}
