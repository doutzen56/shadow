// 由旧版站点表映射过来；不继承 EntityBase*；不对业务表做 CodeFirst。
using SqlSugar;

namespace SimpX.Net.Data.Entity.Sites;

/// <summary>
/// 充值渠道的分类
/// </summary>
[SugarTable("site_PaymentCategory")]
public class SitePaymentCategory
{
    /// <summary>
    /// 分类编号
    /// </summary>
    [SugarColumn(ColumnName = "CateID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    [SugarColumn(ColumnName = "CateType")]
    public int Type { get; set; }

    /// <summary>
    /// 分类名称
    /// </summary>
    [SugarColumn(ColumnName = "CateName")]
    public string Name { get; set; }

    /// <summary>
    /// 排序值
    /// </summary>
    [SugarColumn(ColumnName = "Sort")]
    public short Sort { get; set; }

    /// <summary>
    /// 封面图
    /// </summary>
    [SugarColumn(ColumnName = "Cover")]
    public string Cover { get; set; }

    /// <summary>
    /// 前端是否显示推荐
    /// </summary>
    [SugarColumn(ColumnName = "IsRecommend")]
    public bool IsRecommend { get; set; }

    /// <summary>
    /// 当前分类下可用的充值渠道数量
    /// </summary>
    [SugarColumn(ColumnName = "Count")]
    public int Count { get; set; }

    /// <summary>
    /// 支付说明
    /// </summary>
    [SugarColumn(ColumnName = "Description")]
    public string Description { get; set; }
}
