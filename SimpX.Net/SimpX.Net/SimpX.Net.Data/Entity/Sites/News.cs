// 由旧版站点表映射过来；不继承 EntityBase*；不对业务表做 CodeFirst。
using SqlSugar;

namespace SimpX.Net.Data.Entity.Sites;

/// <summary>
/// 新闻公告
/// </summary>
[SugarTable("site_News")]
public class News
{
    /// <summary>
    /// 新闻编号
    /// </summary>
    [SugarColumn(ColumnName = "NewsID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    /// <summary>
    /// 所属分类
    /// </summary>
    [SugarColumn(ColumnName = "ColID")]
    public int ColID { get; set; }

    /// <summary>
    /// 封面图
    /// </summary>
    [SugarColumn(ColumnName = "Cover")]
    public string Cover { get; set; }

    [SugarColumn(ColumnName = "Title")]
    public string Title { get; set; }

    [SugarColumn(ColumnName = "Content")]
    public string Content { get; set; }

    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>
    /// 是否登录后弹出
    /// </summary>
    [SugarColumn(ColumnName = "IsTipAfterLogin")]
    public bool IsTipAfterLogin { get; set; }

    /// <summary>
    /// 是否登录前弹出
    /// </summary>
    [SugarColumn(ColumnName = "IsTipBeforeLogin")]
    public bool IsTipBeforeLogin { get; set; }

    [SugarColumn(ColumnName = "Sort")]
    public int Sort { get; set; }

    [SugarColumn(ColumnName = "StartAt")]
    public DateTime StartAt { get; set; }

    [SugarColumn(ColumnName = "EndAt")]
    public DateTime EndAt { get; set; }
}
