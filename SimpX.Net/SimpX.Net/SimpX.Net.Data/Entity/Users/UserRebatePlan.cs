namespace SimpX.Net.Data.Entity.Users;

/// <summary>
/// 返水计划
/// </summary>
[SugarTable("usr_RebatePlan")]
public class UserRebatePlan
{
    /// <summary>
    /// 返水计划ID
    /// </summary>
    [SugarColumn(ColumnName = "PlanID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    /// <summary>
    /// 返水计划的名字
    /// </summary>
    [SugarColumn(ColumnName = "PlanName")]
    public string Name { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }

    [SugarColumn(ColumnName = "AdminID")]
    public int AdminID { get; set; }

    /// <summary>
    /// 总共发放的资金（统计）
    /// </summary>
    [SugarColumn(ColumnName = "Money")]
    public decimal Money { get; set; }

    /// <summary>
    /// 实际发放的资金
    /// </summary>
    [SugarColumn(ColumnName = "Paid")]
    public decimal Paid { get; set; }

    /// <summary>
    /// 成功的记录数量
    /// </summary>
    [SugarColumn(ColumnName = "Success")]
    public int Success { get; set; }

    /// <summary>
    /// 失败的数量
    /// </summary>
    [SugarColumn(ColumnName = "Faild")]
    public int Faild { get; set; }

    /// <summary>
    /// 总共发放的数量
    /// </summary>
    [SugarColumn(ColumnName = "Total")]
    public int Total { get; set; }

    /// <summary>
    /// 返水周期（开始时间）
    /// </summary>
    [SugarColumn(ColumnName = "StartAt")]
    public DateTime StartAt { get; set; }

    /// <summary>
    /// 返水周期（结束时间）
    /// </summary>
    [SugarColumn(ColumnName = "EndAt")]
    public DateTime EndAt { get; set; }

    /// <summary>
    /// 打码量要求
    /// </summary>
    [SugarColumn(ColumnName = "FlowRate")]
    public decimal FlowRate { get; set; }

    /// <summary>
    /// 币种
    /// </summary>
    [SugarColumn(ColumnName = "Currency")]
    public int Currency { get; set; }

    /// <summary>
    /// 币种
    /// </summary>
    [SugarColumn(ColumnName = "FromCurrency")]
    public int FromCurrency { get; set; }
}
