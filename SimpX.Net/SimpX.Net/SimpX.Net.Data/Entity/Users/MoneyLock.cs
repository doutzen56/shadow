namespace SimpX.Net.Data.Entity.Users;

/// <summary>
/// 资金锁定记录
/// </summary>
[SugarTable("usr_MoneyLock")]
public class MoneyLock
{
    /// <summary>
    /// 锁定编号
    /// </summary>
    [SugarColumn(ColumnName = "LockID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    [SugarColumn(ColumnName = "UserID")]
    public int UserID { get; set; }

    /// <summary>
    /// 锁定类型
    /// </summary>
    [SugarColumn(ColumnName = "Type")]
    public int Type { get; set; }

    /// <summary>
    /// 锁定对象的来源
    /// </summary>
    [SugarColumn(ColumnName = "SourceID")]
    public int SourceID { get; set; }

    /// <summary>
    /// 要锁定的金额
    /// </summary>
    [SugarColumn(ColumnName = "Money")]
    public decimal Money { get; set; }

    /// <summary>
    /// 是否锁定
    /// </summary>
    [SugarColumn(ColumnName = "IsLock")]
    public bool IsLock { get; set; }

    /// <summary>
    /// 锁定时间
    /// </summary>
    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>
    /// 解锁时间
    /// </summary>
    [SugarColumn(ColumnName = "UnLockAt")]
    public DateTime UnLockAt { get; set; }

    /// <summary>
    /// 锁定备注
    /// </summary>
    [SugarColumn(ColumnName = "LockDesc")]
    public string LockDesc { get; set; }

    /// <summary>
    /// 解锁备注
    /// </summary>
    [SugarColumn(ColumnName = "UnLockDesc")]
    public string UnLockDesc { get; set; }

    /// <summary>
    /// 币种
    /// </summary>
    [SugarColumn(ColumnName = "Currency")]
    public int Currency { get; set; }
}
