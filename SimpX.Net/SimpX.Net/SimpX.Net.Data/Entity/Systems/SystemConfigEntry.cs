using SqlSugar;

namespace SimpX.Net.Data.Entity.Systems;

/// <summary>
/// 全局配置键值（表 sys_Config）。对齐旧 Name/Value。
/// </summary>
[SugarTable("sys_Config")]
public class SystemConfigEntry
{
    /// <summary>配置名（主键）。</summary>
    [SugarColumn(ColumnName = "Name", IsPrimaryKey = true, Length = 64)]
    public string Name { get; set; }

    /// <summary>配置值。</summary>
    [SugarColumn(ColumnName = "Value", ColumnDataType = "nvarchar(max)")]
    public string Value { get; set; }
}
