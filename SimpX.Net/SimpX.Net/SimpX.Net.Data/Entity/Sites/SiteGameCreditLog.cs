// 由旧版站点表映射过来；不继承 EntityBase*；不对业务表做 CodeFirst。
using SqlSugar;

namespace SimpX.Net.Data.Entity.Sites;

/// <summary>
/// 授信额度日志
/// </summary>
[SugarTable("site_GameCreditLog")]
public class SiteGameCreditLog
{
    /// <summary>
    /// 日志ID
    /// </summary>
    [SugarColumn(ColumnName = "LogID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    /// <summary>
    /// 站点ID
    /// </summary>
    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    /// <summary>
    /// 账变金额
    /// </summary>
    [SugarColumn(ColumnName = "Money")]
    public decimal Money { get; set; }

    /// <summary>
    /// 当前授信额度
    /// </summary>
    [SugarColumn(ColumnName = "GameCredit")]
    public decimal GameCredit { get; set; }

    /// <summary>
    /// 创建日期
    /// </summary>
    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>
    /// 用户ID
    /// </summary>
    [SugarColumn(ColumnName = "UserID")]
    public int UserID { get; set; }

    /// <summary>
    /// 日志描述
    /// </summary>
    [SugarColumn(ColumnName = "LogDesc")]
    public string LogDesc { get; set; }

    /// <summary>
    /// 转账类型
    /// </summary>
    [SugarColumn(ColumnName = "TransferType")]
    public byte TransferType { get; set; }

    /// <summary>
    /// 游戏类型
    /// </summary>
    [SugarColumn(ColumnName = "GameType")]
    public byte GameType { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    [SugarColumn(ColumnName = "UserName")]
    public string UserName { get; set; }
}
