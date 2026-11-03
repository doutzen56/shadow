// 由旧版站点表映射过来；不继承 EntityBase*；不对业务表做 CodeFirst。
using SqlSugar;

namespace SimpX.Net.Data.Entity.Sites;

/// <summary>
/// 任务运行日志
/// </summary>
[SugarTable("site_TaskLog")]
public class SiteTaskLog
{
    /// <summary>
    /// 编号
    /// </summary>
    [SugarColumn(ColumnName = "LogID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    [SugarColumn(ColumnName = "TaskID")]
    public int TaskID { get; set; }

    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    /// <summary>
    /// 任务日期
    /// </summary>
    [SugarColumn(ColumnName = "Date")]
    public DateTime Date { get; set; }

    /// <summary>
    /// 任务的执行时间
    /// </summary>
    [SugarColumn(ColumnName = "RunAt")]
    public DateTime RunAt { get; set; }

    /// <summary>
    /// 任务运行状态
    /// </summary>
    [SugarColumn(ColumnName = "Status")]
    public int Status { get; set; }

    /// <summary>
    /// 任务数量
    /// </summary>
    [SugarColumn(ColumnName = "Total")]
    public int Total { get; set; }
}
