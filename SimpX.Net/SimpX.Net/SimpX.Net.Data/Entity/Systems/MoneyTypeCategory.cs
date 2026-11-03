using SqlSugar;

namespace SimpX.Net.Data.Entity.Systems;

/// <summary>
/// 资金类型与分类对应（sys_MoneyTypeCategory）。
/// </summary>
[SugarTable("sys_MoneyTypeCategory")]
public class MoneyTypeCategory
{
    /// <summary>资金类型。</summary>
    [SugarColumn(ColumnName = "Type", IsPrimaryKey = true)]
    public int Type { get; set; }

    /// <summary>分类。</summary>
    [SugarColumn(ColumnName = "Category", IsPrimaryKey = true)]
    public int Category { get; set; }

    /// <summary>同一资金类型下的分类序号（0=主分类）。</summary>
    [SugarColumn(ColumnName = "Index")]
    public int Index { get; set; }
}
