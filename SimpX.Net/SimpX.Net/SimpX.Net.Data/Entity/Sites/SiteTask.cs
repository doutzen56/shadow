// 由旧版站点表映射过来；不继承 EntityBase*；不对业务表做 CodeFirst。
using SqlSugar;

namespace SimpX.Net.Data.Entity.Sites;

/// <summary>
/// 站点自动任务
/// </summary>
[SugarTable("site_Task")]
public class SiteTask
{
    /// <summary>
    /// 任务编号
    /// </summary>
    [SugarColumn(ColumnName = "TaskID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    /// <summary>
    /// 所属平台
    /// </summary>
    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    /// <summary>
    /// 任务类型
    /// </summary>
    [SugarColumn(ColumnName = "Type")]
    public int Type { get; set; }

    /// <summary>
    /// 任务运行时间（以天为单位）
    /// </summary>
    [SugarColumn(ColumnName = "Time")]
    public int Time { get; set; }

    /// <summary>
    /// 是否开启
    /// </summary>
    [SugarColumn(ColumnName = "IsOpen")]
    public bool IsOpen { get; set; }

    /// <summary>
    /// 任务参数配置
    /// </summary>
    [SugarColumn(ColumnName = "Setting")]
    public string Setting { get; set; }
}
