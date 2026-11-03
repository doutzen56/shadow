// 由旧版站点表映射过来；不继承 EntityBase*；不对业务表做 CodeFirst。
using SqlSugar;

namespace SimpX.Net.Data.Entity.Sites;

/// <summary>
/// 站点的资金日志（按照SiteID做分组）
/// </summary>
[SugarTable("site_MoneyLog")]
public class SiteMoneyLog
{
    /// <summary>
    /// 流水编号,与 usr_MoneyLog 一致
    /// </summary>
    [SugarColumn(ColumnName = "LogID", IsPrimaryKey = true)]
    public int ID { get; set; }

    /// <summary>
    /// 站点
    /// </summary>
    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    /// <summary>
    /// 用户
    /// </summary>
    [SugarColumn(ColumnName = "UserID")]
    public int UserID { get; set; }

    /// <summary>
    /// 帐变金额
    /// </summary>
    [SugarColumn(ColumnName = "Money")]
    public decimal Money { get; set; }

    /// <summary>
    /// 当前的锁定金额
    /// </summary>
    [SugarColumn(ColumnName = "LockMoney")]
    public decimal LockMoney { get; set; }

    /// <summary>
    /// 帐变之后的金额（含锁定金额）
    /// </summary>
    [SugarColumn(ColumnName = "Balance")]
    public decimal Balance { get; set; }

    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>
    /// 资金类型
    /// </summary>
    [SugarColumn(ColumnName = "Type")]
    public int Type { get; set; }

    /// <summary>
    /// 来源
    /// </summary>
    [SugarColumn(ColumnName = "SourceID")]
    public string SourceID { get; set; }

    [SugarColumn(ColumnName = "IP")]
    public string IP { get; set; }

    /// <summary>
    /// 帐变备注
    /// </summary>
    [SugarColumn(ColumnName = "LogDesc")]
    public string Description { get; set; }

    /// <summary>
    /// 标记该记录属于哪个表
    /// </summary>
    [SugarColumn(ColumnName = "TableID")]
    public int TableID { get; set; }

    /// <summary>
    /// 子类型
    /// </summary>
    [SugarColumn(ColumnName = "SubType")]
    public byte SubType { get; set; }

    /// <summary>
    /// 币种
    /// </summary>
    [SugarColumn(ColumnName = "Currency")]
    public int Currency { get; set; }
}
