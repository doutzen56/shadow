namespace SimpX.Net.Data.Entity.Users;

[SugarTable("usr_Event")]
public class EventImport
{
    /// <summary>
    /// 用户ID
    /// </summary>
    [SugarColumn(ColumnName = "ID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    [SugarColumn(ColumnName = "UserName")]
    public string UserName { get; set; }

    /// <summary>
    /// 时间
    /// </summary>
    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }
}
