// 由旧版站点表映射过来；不继承 EntityBase*；不对业务表做 CodeFirst。
using SqlSugar;

namespace SimpX.Net.Data.Entity.Sites;

/// <summary>
/// 映射表 Site_SurveryUser
/// </summary>
[SugarTable("Site_SurveryUser")]
public class SiteSurveryUser
{
    /// <summary>
    /// 编号
    /// </summary>
    [SugarColumn(ColumnName = "ID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    /// <summary>
    /// 站点ID
    /// </summary>
    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    /// <summary>
    /// 用户ID
    /// </summary>
    [SugarColumn(ColumnName = "UserID")]
    public int UserID { get; set; }

    /// <summary>
    /// 是否隐藏
    /// </summary>
    [SugarColumn(ColumnName = "IsPay")]
    public bool IsPay { get; set; }

    /// <summary>
    /// 问卷ID
    /// </summary>
    [SugarColumn(ColumnName = "SurveryID")]
    public int SurveryID { get; set; }

    /// <summary>
    /// 问卷调查大难
    /// </summary>
    [SugarColumn(ColumnName = "Answer")]
    public string Answer { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }
}
