// 由旧版站点表映射过来；不继承 EntityBase*；不对业务表做 CodeFirst。
using SqlSugar;

namespace SimpX.Net.Data.Entity.Sites;

/// <summary>
/// 站点的返水设定
/// </summary>
[SugarTable("site_RebateSetting")]
public class RebateSetting
{
    /// <summary>
    /// 返水设定编号
    /// </summary>
    [SugarColumn(ColumnName = "RebateID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    /// <summary>
    /// 用逗号隔开的分组ID
    /// </summary>
    [SugarColumn(ColumnName = "Groups")]
    public string Groups { get; set; }

    /// <summary>
    /// 用逗号隔开的VIP等级
    /// </summary>
    [SugarColumn(ColumnName = "Levels")]
    public string Levels { get; set; }

    /// <summary>
    /// 有效投注额度
    /// </summary>
    [SugarColumn(ColumnName = "BetMoney")]
    public decimal BetMoney { get; set; }

    /// <summary>
    /// 游戏的返点
    /// </summary>
    [SugarColumn(ColumnName = "Setting")]
    public string SettingString { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>
    /// 最高发放金额
    /// </summary>
    [SugarColumn(ColumnName = "MaxMoney")]
    public decimal MaxMoney { get; set; }

    /// <summary>
    /// 自动发放
    /// </summary>
    [SugarColumn(ColumnName = "IsAuto")]
    public bool IsAuto { get; set; }

    /// <summary>
    /// 返水规则名称
    /// </summary>
    [SugarColumn(ColumnName = "Name")]
    public string Name { get; set; }

    /// <summary>
    /// 返水类型
    /// </summary>
    [SugarColumn(ColumnName = "Type")]
    public string Type { get; set; }

    /// <summary>
    /// 规则创建人ID
    /// </summary>
    [SugarColumn(ColumnName = "CreatorID")]
    public int CreatorID { get; set; }

    /// <summary>
    /// 最低发放金额
    /// </summary>
    [SugarColumn(ColumnName = "MinMoney")]
    public decimal MinMoney { get; set; }

    /// <summary>
    /// 规则创建人
    /// </summary>
    [SugarColumn(ColumnName = "CreatorName")]
    public string CreatorName { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    [SugarColumn(ColumnName = "Enabled")]
    public bool Enabled { get; set; }
}
