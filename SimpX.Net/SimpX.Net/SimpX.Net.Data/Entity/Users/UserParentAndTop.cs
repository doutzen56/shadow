namespace SimpX.Net.Data.Entity.Users;

[SugarTable("usr_ParentAndTop")]
public class UserParentAndTop
{
    [SugarColumn(ColumnName = "UserID", IsPrimaryKey = true)]
    public int UserID { get; set; }

    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    [SugarColumn(ColumnName = "UserName")]
    public string UserName { get; set; }

    [SugarColumn(ColumnName = "AgentUserID")]
    public int? AgentUserID { get; set; }

    [SugarColumn(ColumnName = "AgentName")]
    public string AgentUserName { get; set; }

    [SugarColumn(ColumnName = "TopUserID")]
    public int? TopUserID { get; set; }

    [SugarColumn(ColumnName = "TopUserName")]
    public string TopUserName { get; set; }
}
