namespace SimpX.Net.Data.Entity.Users;

/// <summary>
/// 自助反水 V2 会员领取标记（按 LastDownloadTime 滚动，索引不回写发放状态）。
/// </summary>
[SugarTable("usr_RebateClaimMark")]
public class UserRebateClaimMark
{
    /// <summary>
    /// 自增主键。
    /// </summary>
    [SugarColumn(ColumnName = "ID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    /// <summary>
    /// 站点编号。
    /// </summary>
    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    /// <summary>
    /// 用户编号。
    /// </summary>
    [SugarColumn(ColumnName = "UserID")]
    public int UserID { get; set; }

    /// <summary>
    /// 已领取到的 LastDownloadTime 标记（严格大于此时间的明细才可领）。
    /// </summary>
    [SugarColumn(ColumnName = "LastDownloadTime")]
    public DateTime LastDownloadTime { get; set; }

    /// <summary>
    /// 乐观锁版本号。
    /// </summary>
    [SugarColumn(ColumnName = "Version")]
    public int Version { get; set; }

    /// <summary>
    /// 最近更新时间。
    /// </summary>
    [SugarColumn(ColumnName = "UpdateAt")]
    public DateTime UpdateAt { get; set; }
}
