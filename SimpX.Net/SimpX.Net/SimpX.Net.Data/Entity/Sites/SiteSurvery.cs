// 由旧版站点表映射过来；不继承 EntityBase*；不对业务表做 CodeFirst。
using SqlSugar;

namespace SimpX.Net.Data.Entity.Sites;

/// <summary>
/// 映射表 Site_Survery
/// </summary>
[SugarTable("Site_Survery")]
public class SiteSurvery
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
    /// 站点标题
    /// </summary>
    [SugarColumn(ColumnName = "Title")]
    public string Title { get; set; }

    /// <summary>
    /// 描述
    /// </summary>
    [SugarColumn(ColumnName = "Description")]
    public string Description { get; set; }

    /// <summary>
    /// 问卷内容
    /// </summary>
    [SugarColumn(ColumnName = "Questions")]
    public string Questions { get; set; }

    /// <summary>
    /// 问卷答案
    /// </summary>
    [SugarColumn(ColumnName = "Answer")]
    public string Answer { get; set; }

    /// <summary>
    /// 是否隐藏
    /// </summary>
    [SugarColumn(ColumnName = "IsOpen")]
    public bool IsOpen { get; set; }

    /// <summary>
    /// 问卷结束派发奖金
    /// </summary>
    [SugarColumn(ColumnName = "AutoPay")]
    public bool AutoPay { get; set; }

    /// <summary>
    /// 奖金金额
    /// </summary>
    [SugarColumn(ColumnName = "Money")]
    public decimal Money { get; set; }

    /// <summary>
    /// 流水
    /// </summary>
    [SugarColumn(ColumnName = "WashFlow")]
    public int WashFlow { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [SugarColumn(ColumnName = "StartAt")]
    public DateTime StartAt { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [SugarColumn(ColumnName = "ExpireAt")]
    public DateTime ExpireAt { get; set; }
}
