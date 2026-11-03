namespace SimpX.Net.Data.Entity.Reports;

/// <summary>
/// 代理按日统计（生成佣金时汇总直属下级；最小列）。
/// </summary>
[SugarTable("rpt_AgentStatistics")]
public class ReportAgentStatis
{
    /// <summary>业务日。</summary>
    [SugarColumn(ColumnName = "Date", IsPrimaryKey = true)]
    public DateTime Date { get; set; }

    /// <summary>会员/代理编号。</summary>
    [SugarColumn(ColumnName = "UserID", IsPrimaryKey = true)]
    public int UserID { get; set; }

    /// <summary>币种。</summary>
    [SugarColumn(ColumnName = "Currency", IsPrimaryKey = true)]
    public int Currency { get; set; }

    /// <summary>站点。</summary>
    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    /// <summary>游戏盈亏。</summary>
    [SugarColumn(ColumnName = "GameMoney")]
    public decimal GameMoney { get; set; }

    /// <summary>活动金额。</summary>
    [SugarColumn(ColumnName = "MoneyEvent")]
    public decimal MoneyEvent { get; set; }

    /// <summary>反水金额。</summary>
    [SugarColumn(ColumnName = "MoneyRebate")]
    public decimal MoneyRebate { get; set; }

    /// <summary>充值手续费。</summary>
    [SugarColumn(ColumnName = "RechargeFee")]
    public decimal RechargeFee { get; set; }

    /// <summary>提现手续费。</summary>
    [SugarColumn(ColumnName = "WithdrawFee")]
    public decimal WithdrawFee { get; set; }
}
