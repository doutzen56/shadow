// 由旧版系统表映射过来；不对业务表做 CodeFirst。
using SqlSugar;

namespace SimpX.Net.Data.Entity.Systems;

/// <summary>
/// 提现银行卡黑名单（表 sys_BankAccountBlackList）。
/// </summary>
[SugarTable("sys_BankAccountBlackList")]
public class SystemBankAccountBlackList
{
    /// <summary>编号。</summary>
    [SugarColumn(ColumnName = "BlackIP", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    /// <summary>户名。</summary>
    [SugarColumn(ColumnName = "AccountName")]
    public string AccountName { get; set; }

    /// <summary>卡号。</summary>
    [SugarColumn(ColumnName = "AccountNumber")]
    public string AccountNumber { get; set; }

    /// <summary>备注。</summary>
    [SugarColumn(ColumnName = "Remark")]
    public string Remark { get; set; }

    /// <summary>创建时间。</summary>
    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }
}
