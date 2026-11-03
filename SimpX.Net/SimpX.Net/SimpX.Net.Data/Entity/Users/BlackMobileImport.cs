namespace SimpX.Net.Data.Entity.Users;

[SugarTable("usr_BlackMobile")]
public class BlackMobileImport
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
    public string BlackMobile { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnName = "Demo")]
    public string Demo { get; set; }

    /// <summary>
    /// 是否禁止
    /// </summary>
    [SugarColumn(ColumnName = "IsLock")]
    public string IsLock { get; set; }

    /// <summary>
    /// 时间
    /// </summary>
    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }
}
