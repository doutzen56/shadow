namespace SimpX.Net.Data.Entity.Systems;

/// <summary>
/// 系统积分/经验项配置（表 sys_PointExper）。通常只有一行。
/// </summary>
[SugarTable("sys_PointExper")]
public class SystemPointExper
{
    /// <summary>编号。</summary>
    [SugarColumn(ColumnName = "SettingID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    /// <summary>积分项设置原文（XML/JSON）。</summary>
    [SugarColumn(ColumnName = "PointItemSetting", ColumnDataType = "nvarchar(max)")]
    public string PointItemSetting { get; set; }

    /// <summary>经验项设置原文。</summary>
    [SugarColumn(ColumnName = "ExperItemSetting", ColumnDataType = "nvarchar(max)")]
    public string ExperItemSetting { get; set; }
}
