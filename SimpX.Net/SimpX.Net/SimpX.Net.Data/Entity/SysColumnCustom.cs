namespace SimpX.Net.Data;

/// <summary>
/// Vxe 表格列配置（按用户、按表格记住冻结/宽度/顺序/显隐）。
/// </summary>
[SugarTable(null, "Vxe表格列配置")]
[SysTable]
[SugarIndex("i_{table}_u", nameof(UserId), OrderByType.Asc)]
[SugarIndex("i_{table}_g", nameof(GridId), OrderByType.Asc)]
public class SysColumnCustom : EntityBaseTenantId
{
    /// <summary>
    /// 用户 Id
    /// </summary>
    [SugarColumn(ColumnDescription = "用户Id")]
    public long UserId { get; set; }

    /// <summary>
    /// 表格 Id（前端 vxe grid id）
    /// </summary>
    [SugarColumn(ColumnDescription = "表格Id", Length = 128)]
    [MaxLength(128)]
    public string GridId { get; set; }

    /// <summary>
    /// 冻结列状态
    /// </summary>
    [SugarColumn(ColumnDescription = "冻结列状态数据", ColumnDataType = StaticConfig.CodeFirst_BigString)]
    public string? FixedData { get; set; }

    /// <summary>
    /// 列宽状态
    /// </summary>
    [SugarColumn(ColumnDescription = "列宽状态数据", ColumnDataType = StaticConfig.CodeFirst_BigString)]
    public string? ResizableData { get; set; }

    /// <summary>
    /// 列顺序
    /// </summary>
    [SugarColumn(ColumnDescription = "列顺序数据", ColumnDataType = StaticConfig.CodeFirst_BigString)]
    public string? SortData { get; set; }

    /// <summary>
    /// 显示/隐藏列
    /// </summary>
    [SugarColumn(ColumnDescription = "显示/隐藏列状态数据", ColumnDataType = StaticConfig.CodeFirst_BigString)]
    public string? VisibleData { get; set; }
}
