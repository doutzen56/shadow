// 由旧版站点表映射过来；不继承 EntityBase*；不对业务表做 CodeFirst。
using SqlSugar;

namespace SimpX.Net.Data.Entity.Sites;

/// <summary>
/// 提现渠道
/// </summary>
[SugarTable("site_WithdrawChannel")]
public class WithdrawChannel
{
    /// <summary>
    /// 提现渠道ID
    /// </summary>
    [SugarColumn(ColumnName = "ChannelID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    [SugarColumn(ColumnName = "SysID")]
    public int SysID { get; set; }

    [SugarColumn(ColumnName = "Name")]
    public string Name { get; set; }

    [SugarColumn(ColumnName = "Setting")]
    public string Setting { get; set; }

    /// <summary>
    /// 查询接口设置
    /// </summary>
    [SugarColumn(ColumnName = "QuerySetting")]
    public string QuerySetting { get; set; }

    /// <summary>
    /// 是否开启
    /// </summary>
    [SugarColumn(ColumnName = "IsOpen")]
    public bool IsOpen { get; set; }

    /// <summary>
    /// 排序,从大到小
    /// </summary>
    [SugarColumn(ColumnName = "Sort")]
    public short Sort { get; set; }

    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }

    [SugarColumn(ColumnName = "IsDeleted")]
    public bool IsDeleted { get; set; }

    /// <summary>
    /// 成本费用(百分比）
    /// </summary>
    [SugarColumn(ColumnName = "Cost")]
    public decimal Cost { get; set; }

    /// <summary>
    /// 最低成本
    /// </summary>
    [SugarColumn(ColumnName = "MinCost")]
    public decimal MinCost { get; set; }

    /// <summary>
    /// 最高成本
    /// </summary>
    [SugarColumn(ColumnName = "MaxCost")]
    public decimal MaxCost { get; set; }

    /// <summary>
    /// 币种
    /// </summary>
    [SugarColumn(ColumnName = "Currency")]
    public int Currency { get; set; }
}
