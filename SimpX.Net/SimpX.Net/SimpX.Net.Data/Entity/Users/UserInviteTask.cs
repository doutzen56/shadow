namespace SimpX.Net.Data.Entity.Users;

/// <summary>
/// 邀请任务奖励列表
/// </summary>
[SugarTable("usr_InviteTask")]
public class UserInviteTask
{
    /// <summary>
    /// 主键ID
    /// </summary>
    [SugarColumn(ColumnName = "ID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    /// <summary>
    /// 邀请人ID
    /// </summary>
    [SugarColumn(ColumnName = "InviteID")]
    public int InviteID { get; set; }

    /// <summary>
    /// 被邀请人姓名
    /// </summary>
    [SugarColumn(ColumnName = "UserName")]
    public string UserName { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>
    /// 被邀请人完成额度
    /// </summary>
    [SugarColumn(ColumnName = "Amount")]
    public decimal Amount { get; set; }

    /// <summary>
    /// 被邀请人完成奖励
    /// </summary>
    [SugarColumn(ColumnName = "Reward")]
    public decimal Reward { get; set; }

    /// <summary>
    /// 奖励ID
    /// </summary>
    [SugarColumn(ColumnName = "SourceID")]
    public string SourceID { get; set; }

    /// <summary>
    /// 是否已经领取
    /// </summary>
    [SugarColumn(ColumnName = "IsOpen")]
    public bool IsOpen { get; set; }
}
