namespace SimpX.Net.Data.Entity.Users;

/// <summary>
/// 用户的标记
/// </summary>
[SugarTable("usr_IdCard")]
public class UserIdCard
{
    [SugarColumn(ColumnName = "CardID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    [SugarColumn(ColumnName = "UserID")]
    public int UserID { get; set; }

    [SugarColumn(ColumnName = "UserName")]
    public string UserName { get; set; }

    /// <summary>证件上的姓名。</summary>
    [SugarColumn(ColumnName = "CardName")]
    public string CardName { get; set; }

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

    [SugarColumn(ColumnName = "Status")]
    public int Status { get; set; }

    [SugarColumn(ColumnName = "Remark")]
    public string Remark { get; set; }

    [SugarColumn(ColumnName = "CheckDateTime")]
    public DateTime? CheckDateTime { get; set; }

    [SugarColumn(ColumnName = "Checker")]
    public int? Checker { get; set; }

    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }

    [SugarColumn(ColumnName = "Country")]
    public string Country { get; set; }

    /// <summary>
    /// 性别 1男，2女 0空
    /// </summary>
    [SugarColumn(ColumnName = "Gender")]
    public int Gender { get; set; }
}
