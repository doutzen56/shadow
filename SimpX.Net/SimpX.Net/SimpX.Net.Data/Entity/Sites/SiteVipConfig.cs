// 由旧版站点表映射过来；不继承 EntityBase*；不对业务表做 CodeFirst。
using SqlSugar;

namespace SimpX.Net.Data.Entity.Sites;

/// <summary>
/// 站点VIP会员相关设置
/// </summary>
[SugarTable("site_VipConfig")]
public class SiteVipConfig
{
    /// <summary>
    /// 站点编号
    /// </summary>
    [SugarColumn(ColumnName = "SiteID", IsPrimaryKey = true)]
    public int ID { get; set; }

    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>
    /// VIP会员特权
    /// </summary>
    [SugarColumn(ColumnName = "VIPRightSetting")]
    public string VIPRightSetting { get; set; }

    /// <summary>
    /// VIP会员申请资格
    /// </summary>
    [SugarColumn(ColumnName = "VIPConditionSetting")]
    public string VIPConditionSetting { get; set; }

    /// <summary>
    /// 积分项设置
    /// </summary>
    [SugarColumn(ColumnName = "PointItemSetting")]
    public string PointItemSettingString { get; set; }

    /// <summary>
    /// 经验项设置
    /// </summary>
    [SugarColumn(ColumnName = "ExperItemSetting")]
    public string ExperItemSettingString { get; set; }

    /// <summary>
    /// 规则条款
    /// </summary>
    [SugarColumn(ColumnName = "RuleClause")]
    public string RuleClause { get; set; }

    /// <summary>
    /// 积分规则
    /// </summary>
    [SugarColumn(ColumnName = "PointRule")]
    public string PointRule { get; set; }

    /// <summary>
    /// 自助反水规则
    /// </summary>
    [SugarColumn(ColumnName = "AutoReturnRule")]
    public string AutoReturnRule { get; set; }

    /// <summary>
    /// VIP 设置关联币种
    /// </summary>
    [SugarColumn(ColumnName = "Currency")]
    public int Currency { get; set; }
}
