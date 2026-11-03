namespace SimpX.Net.Data.Entity.Users;

[SugarTable("usr_BankAccountImport")]
public class UserBankImport
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
    /// 0银行卡，1数字钱包 2电子钱包
    /// </summary>
    [SugarColumn(ColumnName = "IsVir")]
    public string IsVir { get; set; }

    /// <summary>
    /// 银行帐号图片，或者视频
    /// </summary>
    [SugarColumn(ColumnName = "BankInfo")]
    public string BankInfo { get; set; }

    [SugarColumn(ColumnName = "BankAddress")]
    public string BankAddress { get; set; }

    [SugarColumn(ColumnName = "BranchCode")]
    public string BranchCode { get; set; }

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
