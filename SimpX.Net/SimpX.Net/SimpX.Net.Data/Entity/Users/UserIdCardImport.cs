namespace SimpX.Net.Data.Entity.Users;

[SugarTable("usr_IdCardImport")]
public class UserIdCardImport
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

    [SugarColumn(ColumnName = "CardNumber")]
    public string CardNumber { get; set; }

    [SugarColumn(ColumnName = "CardFront")]
    public string CardFront { get; set; }

    [SugarColumn(ColumnName = "CardBack")]
    public string CardBack { get; set; }

    /// <summary>
    /// 发证城市
    /// </summary>
    [SugarColumn(ColumnName = "CardCity")]
    public string CardCity { get; set; }

    /// <summary>
    /// 发证日期
    /// </summary>
    [SugarColumn(ColumnName = "CardCreateAt")]
    public DateTime CardCreateAt { get; set; }

    /// <summary>
    /// 证件类型
    /// </summary>
    [SugarColumn(ColumnName = "CardType")]
    public int CardType { get; set; }

    /// <summary>
    /// 生日
    /// </summary>
    [SugarColumn(ColumnName = "CardBirthday")]
    public DateTime CardBirthday { get; set; }

    /// <summary>
    /// 是否已经导入
    /// </summary>
    [SugarColumn(ColumnName = "Status")]
    public bool Status { get; set; }

    /// <summary>
    /// 时间
    /// </summary>
    [SugarColumn(ColumnName = "CreateTime")]
    public DateTime CreateTime { get; set; }
}
