namespace SimpX.Net.Data.Entity.Users;

[SugarTable("usr_import")]
public class UserImport
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
    /// 上级代理
    /// </summary>
    [SugarColumn(ColumnName = "AgentName")]
    public string AgentName { get; set; }

    /// <summary>
    /// 密码,明码
    /// </summary>
    [SugarColumn(ColumnName = "Password")]
    public string Password { get; set; }

    /// <summary>
    /// 电话
    /// </summary>
    [SugarColumn(ColumnName = "Phone")]
    public string Mobile { get; set; }

    /// <summary>
    /// 币种
    /// </summary>
    [SugarColumn(ColumnName = "Currency")]
    public int Currency { get; set; }

    /// <summary>
    /// 余额
    /// </summary>
    [SugarColumn(ColumnName = "Balance")]
    public decimal Balance { get; set; }

    /// <summary>
    /// 导入状态0，未导入，1导入成功，2导入失败
    /// </summary>
    [SugarColumn(ColumnName = "Status")]
    public byte Status { get; set; }

    /// <summary>
    /// 时间
    /// </summary>
    [SugarColumn(ColumnName = "CreateTime")]
    public DateTime CreateTime { get; set; }

    /// <summary>
    /// 提现账户名
    /// </summary>
    [SugarColumn(ColumnName = "AccountName")]
    public string AccountName { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnName = "Remark")]
    public string Remark { get; set; }
}
