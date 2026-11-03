namespace SimpX.Net.Data.Entity.Agents;

/// <summary>
/// 代理资料（与会员 UserID 一一对应）。
/// </summary>
[SugarTable("agent_User")]
public class AgentUser
{
    /// <summary>
    /// 代理状态。旧枚举：None=0 待审、Normal=1 正常、Locked=2 锁定。
    /// </summary>
    public enum AgentStatus : byte
    {
        /// <summary>待审核。</summary>
        None = 0,

        /// <summary>正常。</summary>
        Normal = 1,

        /// <summary>锁定，无代理权限。</summary>
        Locked = 2
    }

    /// <summary>代理编号，与 Users.UserID 一致。</summary>
    [SugarColumn(ColumnName = "AgentID", IsPrimaryKey = true)]
    public int ID { get; set; }

    /// <summary>站点。</summary>
    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    /// <summary>上级代理。</summary>
    [SugarColumn(ColumnName = "ParentID")]
    public int ParentID { get; set; }

    /// <summary>成为代理的时间。</summary>
    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>代理状态。</summary>
    [SugarColumn(ColumnName = "Status")]
    public AgentStatus Status { get; set; }

    /// <summary>签约的佣金方案编号。</summary>
    [SugarColumn(ColumnName = "CommissionID")]
    public int CommissionID { get; set; }

    /// <summary>返点方案编号。</summary>
    [SugarColumn(ColumnName = "ReturnID")]
    public int ReturnID { get; set; }

    /// <summary>权限开关位。</summary>
    [SugarColumn(ColumnName = "Permission")]
    public long Permission { get; set; }
}
