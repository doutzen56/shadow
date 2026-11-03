namespace SimpX.Net.Data.Entity.Users;

[SugarTable("usr_BlackKyc")]
public class BlackKycImport
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
    [SugarColumn(ColumnName = "BlackKyc")]
    public string BlackKyc { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnName = "Demo")]
    public string Demo { get; set; }

    /// <summary>
    /// 时间
    /// </summary>
    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>
    /// 关联用户
    /// </summary>
    [SugarColumn(ColumnName = "RelatedUsers")]
    public string RelatedUsers { get; set; }
}
