namespace SimpX.Net.Data.Entity.Users;

/// <summary>
/// 自助反水 V2 Job1 全局同步水位（单行，ID 固定为 1；LastDownloadTime 存 UTC）。
/// </summary>
[SugarTable("usr_RebateV2SyncMark")]
public class UserRebateV2SyncMark
{
    /// <summary>
    /// 固定为 1 的单行主键。
    /// </summary>
    [SugarColumn(ColumnName = "ID", IsPrimaryKey = true)]
    public int ID { get; set; }

    /// <summary>
    /// Job1 已处理到的 LastDownloadTime（UTC 墙上钟）。
    /// </summary>
    [SugarColumn(ColumnName = "LastDownloadTime")]
    public DateTime LastDownloadTime { get; set; }

    /// <summary>
    /// 最近更新时间（业务本地时钟）。
    /// </summary>
    [SugarColumn(ColumnName = "UpdateAt")]
    public DateTime UpdateAt { get; set; }
}
