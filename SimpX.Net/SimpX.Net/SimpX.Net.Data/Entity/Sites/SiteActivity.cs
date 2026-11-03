// 由旧版站点表映射过来；不继承 EntityBase*；不对业务表做 CodeFirst。
using SqlSugar;

namespace SimpX.Net.Data.Entity.Sites;

/// <summary>
/// 站点活动设置
/// </summary>
[SugarTable("site_Activity")]
public class SiteActivity
{
    /// <summary>
    /// 活动管理
    /// </summary>
    [SugarColumn(ColumnName = "ActivityID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    /// <summary>
    /// 活动类型
    /// </summary>
    [SugarColumn(ColumnName = "Type")]
    public int Type { get; set; }

    [SugarColumn(ColumnName = "Title")]
    public string Title { get; set; }

    [SugarColumn(ColumnName = "Content")]
    public string Content { get; set; }

    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>
    /// 活动开始时间
    /// </summary>
    [SugarColumn(ColumnName = "StartAt")]
    public DateTime StartAt { get; set; }

    /// <summary>
    /// 活动结束时间
    /// </summary>
    [SugarColumn(ColumnName = "EndAt")]
    public DateTime EndAt { get; set; }

    /// <summary>
    /// 封面图
    /// </summary>
    [SugarColumn(ColumnName = "Cover")]
    public string Cover { get; set; }

    /// <summary>
    /// 活动的参数设定
    /// </summary>
    [SugarColumn(ColumnName = "Setting")]
    public string SettingString { get; set; }
}
