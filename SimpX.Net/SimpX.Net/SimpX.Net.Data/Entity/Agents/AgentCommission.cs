namespace SimpX.Net.Data.Entity.Agents;

/// <summary>
/// 代理佣金方案。对齐旧 agent_Commission。
/// </summary>
[SugarTable("agent_Commission")]
public class AgentCommission
{
    /// <summary>方案编号。</summary>
    [SugarColumn(ColumnName = "CommissionID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    /// <summary>站点。</summary>
    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    /// <summary>方案名称。</summary>
    [SugarColumn(ColumnName = "CommissionName")]
    public string Name { get; set; }

    /// <summary>优惠/活动承担比例。</summary>
    [SugarColumn(ColumnName = "Award")]
    public decimal Award { get; set; }

    /// <summary>反水承担比例。</summary>
    [SugarColumn(ColumnName = "Return")]
    public decimal Return { get; set; }

    /// <summary>成本承担。</summary>
    [SugarColumn(ColumnName = "Cost")]
    public decimal Cost { get; set; }

    /// <summary>有效会员门槛投注额。</summary>
    [SugarColumn(ColumnName = "MemberMoney")]
    public decimal MemberMoney { get; set; }

    /// <summary>
    /// 阶梯或比例配置原文（库列 Setting）。
    /// </summary>
    [SugarColumn(ColumnName = "Setting")]
    public string SettingString { get; set; }

    /// <summary>是否开放。</summary>
    [SugarColumn(ColumnName = "IsOpen")]
    public bool IsOpen { get; set; }

    /// <summary>是否契约方案。</summary>
    [SugarColumn(ColumnName = "IsContract")]
    public bool IsContract { get; set; }

    /// <summary>绑定代理数（展示用）。</summary>
    [SugarColumn(ColumnName = "AgentCount")]
    public int AgentCount { get; set; }

    /// <summary>最大比例。</summary>
    [SugarColumn(ColumnName = "MaxRate")]
    public decimal MaxRate { get; set; }

    /// <summary>创建时间。</summary>
    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }
}
