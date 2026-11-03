// 由旧版站点表映射过来；不继承 EntityBase*；不对业务表做 CodeFirst。
using SqlSugar;

namespace SimpX.Net.Data.Entity.Sites;

/// <summary>
/// 代付回调日志
/// </summary>
[SugarTable("site_WithdrawCallbackLog")]
public class SiteWithdrawCallbackLog
{
    [SugarColumn(ColumnName = "LogID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    /// <summary>
    /// 所属站点
    /// </summary>
    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    /// <summary>
    /// 渠道ID
    /// </summary>
    [SugarColumn(ColumnName = "ChannelID")]
    public int ChannelID { get; set; }

    /// <summary>
    /// 通知时间
    /// </summary>
    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>
    /// 通知内容
    /// </summary>
    [SugarColumn(ColumnName = "Content")]
    public string Content { get; set; }

    [SugarColumn(ColumnName = "Method")]
    public string Method { get; set; }

    /// <summary>
    /// 用户标识
    /// </summary>
    [SugarColumn(ColumnName = "UserAgent")]
    public string UserAgent { get; set; }
}
