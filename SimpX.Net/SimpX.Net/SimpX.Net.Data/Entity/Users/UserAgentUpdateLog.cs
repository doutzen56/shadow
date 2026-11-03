namespace SimpX.Net.Data.Entity.Users;

[SugarTable("usr_AgentUpdateLog")]
public class UserAgentUpdateLog
{
    [SugarColumn(ColumnName = "LogID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    [SugarColumn(ColumnName = "UserID")]
    public int UserID { get; set; }

    /// <summary>
    /// 旧代理列表
    /// </summary>
    [SugarColumn(ColumnName = "OldAgentParents")]
    public string OldAgentParents { get; set; }

    [SugarColumn(ColumnName = "NewAgentParents")]
    public string NewAgentParents { get; set; }

    /// <summary>
    /// AdminID
    /// </summary>
    [SugarColumn(ColumnName = "AdminID")]
    public int AdminID { get; set; }

    /// <summary>
    /// CreateAt
    /// </summary>
    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>
    /// 是否执行完成
    /// </summary>
    [SugarColumn(ColumnName = "Status")]
    public bool Status { get; set; }

    /// <summary>
    /// CreateAt
    /// </summary>
    [SugarColumn(ColumnName = "FinishAt")]
    public DateTime FinishAt { get; set; }
}
