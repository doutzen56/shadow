namespace SimpX.Net.Data.Entity.Users;

[SugarTable("usr_External")]
public class UserExternal
{
    [SugarColumn(ColumnName = "UserID", IsPrimaryKey = true)]
    public int UserID { get; set; }

    [SugarColumn(ColumnName = "SiteID", IsPrimaryKey = true)]
    public int SiteID { get; set; }

    [SugarColumn(ColumnName = "PushID")]
    public string PushID { get; set; }

    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }
}
