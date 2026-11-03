namespace SimpX.Net.Data.Entity.Users;

/// <summary>
/// 返水发放日志
/// </summary>
[SugarTable("usr_RebateLog")]
public class UserRebateLog
{
    /// <summary>
    /// 返水编号
    /// </summary>
    [SugarColumn(ColumnName = "LogID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    /// <summary>
    /// 所属返水计划
    /// </summary>
    [SugarColumn(ColumnName = "PlanID")]
    public int PlanID { get; set; }

    [SugarColumn(ColumnName = "UserID")]
    public int UserID { get; set; }

    /// <summary>
    /// 返水统计时间段（开始时间，业务本地时钟；库列为 datetime2，自助返水含时分秒）
    /// </summary>
    [SugarColumn(ColumnName = "StartAt")]
    public DateTime StartAt { get; set; }

    /// <summary>
    /// 返水统计时间段（结束时间，业务本地时钟；库列为 datetime2，自助返水含时分秒）
    /// </summary>
    [SugarColumn(ColumnName = "EndAt")]
    public DateTime EndAt { get; set; }

    /// <summary>
    /// 返水总金额
    /// </summary>
    [SugarColumn(ColumnName = "Money")]
    public decimal Money { get; set; }

    /// <summary>
    /// 返水记录创建时间
    /// </summary>
    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>
    /// 审核时间
    /// </summary>
    [SugarColumn(ColumnName = "CheckAt")]
    public DateTime CheckAt { get; set; }

    /// <summary>
    /// 返水的详情
    /// </summary>
    [SugarColumn(ColumnName = "Detail")]
    public string Detail { get; set; }

    /// <summary>
    /// 返水的类型
    /// </summary>
    [SugarColumn(ColumnName = "Type")]
    public string Type { get; set; }

    /// <summary>
    /// 返水状态（是否已经发放、拒绝）
    /// </summary>
    [SugarColumn(ColumnName = "Status")]
    public int Status { get; set; }

    /// <summary>
    /// 返水失败原因
    /// </summary>
    [SugarColumn(ColumnName = "FaildMsg")]
    public string FaildMsg { get; set; }

    /// <summary>
    /// 返水设定方案ID
    /// </summary>
    [SugarColumn(ColumnName = "RebateID")]
    public int RebateID { get; set; }

    /// <summary>
    /// 币种
    /// </summary>
    [SugarColumn(ColumnName = "Currency")]
    public int Currency { get; set; }

    /// <summary>
    /// 返水排序ID
    /// </summary>
    [SugarColumn(ColumnName = "SortID")]
    public int SortID { get; set; }
}
