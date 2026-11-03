namespace SimpX.Net.Data.Entity.Users;

/// <summary>
/// 用户的银行帐号
/// </summary>
[SugarTable("usr_BankAccount")]
public class BankAccount
{
    /// <summary>
    /// 账户编号
    /// </summary>
    [SugarColumn(ColumnName = "AccountID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    [SugarColumn(ColumnName = "UserID")]
    public int UserID { get; set; }

    /// <summary>
    /// 银行类型
    /// </summary>
    [SugarColumn(ColumnName = "Type")]
    public int Type { get; set; }

    /// <summary>
    /// 提现银行名称
    /// </summary>
    [SugarColumn(ColumnName = "BankName")]
    public string BankName { get; set; }

    /// <summary>
    /// 提现账户名
    /// </summary>
    [SugarColumn(ColumnName = "Name")]
    public string Name { get; set; }

    /// <summary>
    /// 提现账户名(英文名)
    /// </summary>
    [SugarColumn(ColumnName = "NameLetters")]
    public string NameLetters { get; set; }

    /// <summary>
    /// 银行帐号
    /// </summary>
    [SugarColumn(ColumnName = "Account")]
    public string Account { get; set; }

    /// <summary>
    /// 银行分行
    /// </summary>
    [SugarColumn(ColumnName = "AccountDetail")]
    public string AccountDetail { get; set; }

    /// <summary>
    /// 添加时间
    /// </summary>
    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>
    /// 最后更新时间
    /// </summary>
    [SugarColumn(ColumnName = "UpdateAt")]
    public DateTime UpdateAt { get; set; }

    /// <summary>
    /// 0银行卡，1数字钱包 2电子钱包
    /// </summary>
    [SugarColumn(ColumnName = "IsVir")]
    public string IsVir { get; set; }

    /// <summary>
    /// 银行帐号图片，或者视频
    /// </summary>
    [SugarColumn(ColumnName = "BankInfo")]
    public string BankInfo { get; set; }

    /// <summary>
    /// 审核状态
    /// </summary>
    [SugarColumn(ColumnName = "Status")]
    public int Status { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnName = "Remark")]
    public string Remark { get; set; }

    /// <summary>
    /// 审核时间
    /// </summary>
    [SugarColumn(ColumnName = "CheckDateTime")]
    public DateTime? CheckDateTime { get; set; }

    /// <summary>
    /// 审核人
    /// </summary>
    [SugarColumn(ColumnName = "Checker")]
    public int? Checker { get; set; }

    [SugarColumn(ColumnName = "Country")]
    public string Country { get; set; }
}
