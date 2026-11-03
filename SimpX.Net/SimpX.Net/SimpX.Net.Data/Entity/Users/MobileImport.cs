namespace SimpX.Net.Data.Entity.Users;

[SugarTable("usr_Mobile")]
public class MobileImport
{
    /// <summary>
    /// 用户ID
    /// </summary>
    [SugarColumn(ColumnName = "ID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    /// <summary>
    /// 用户手机号
    /// </summary>
    [SugarColumn(ColumnName = "Mobile")]
    public string Mobile { get; set; }

    /// <summary>
    /// 时间
    /// </summary>
    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }
}
