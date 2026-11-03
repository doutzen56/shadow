namespace SimpX.Net.Data.Entity.Agents;

/// <summary>
/// 代理返点方案。对齐旧 agent_Return。
/// </summary>
[SugarTable("agent_Return")]
public class AgentReturn
{
    /// <summary>方案编号。</summary>
    [SugarColumn(ColumnName = "ReturnID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    /// <summary>站点。</summary>
    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    /// <summary>方案名称。</summary>
    [SugarColumn(ColumnName = "ReturnName")]
    public string Name { get; set; }

    /// <summary>各游戏返点策略原文。</summary>
    [SugarColumn(ColumnName = "Setting")]
    public string SettingString { get; set; }

    /// <summary>创建时间。</summary>
    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>是否开启。</summary>
    [SugarColumn(ColumnName = "IsOpen")]
    public bool IsOpen { get; set; }
}
