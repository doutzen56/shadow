// 由旧版站点表映射过来；不继承 EntityBase*；不对业务表做 CodeFirst。
using SqlSugar;

namespace SimpX.Net.Data.Entity.Sites;

/// <summary>
/// 站点反馈意见
/// </summary>
[SugarTable("site_Feedback")]
public class SiteFeedback
{
    /// <summary>
    /// 编号
    /// </summary>
    [SugarColumn(ColumnName = "FeedID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    /// <summary>
    /// 反馈用户（为0是游客）
    /// </summary>
    [SugarColumn(ColumnName = "UserID")]
    public int UserID { get; set; }

    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>
    /// 留言内容
    /// </summary>
    [SugarColumn(ColumnName = "Content")]
    public string Content { get; set; }

    [SugarColumn(ColumnName = "IP")]
    public string IP { get; set; }

    /// <summary>
    /// 联系电话
    /// </summary>
    [SugarColumn(ColumnName = "Mobile")]
    public string Mobile { get; set; }

    /// <summary>
    /// 电子邮件
    /// </summary>
    [SugarColumn(ColumnName = "Email")]
    public string Email { get; set; }
}
