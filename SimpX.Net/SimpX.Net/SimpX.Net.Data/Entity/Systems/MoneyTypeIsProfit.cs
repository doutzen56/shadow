using SqlSugar;

namespace SimpX.Net.Data.Entity.Systems;

/// <summary>
/// 资金类型是否计入盈利（sys_MoneyTypeIsProfit）。
/// </summary>
[SugarTable("sys_MoneyTypeIsProfit")]
public class MoneyTypeIsProfit
{
    /// <summary>资金类型。</summary>
    [SugarColumn(ColumnName = "Type", IsPrimaryKey = true)]
    public int Type { get; set; }

    /// <summary>是否计入盈利。</summary>
    [SugarColumn(ColumnName = "IsProfit")]
    public bool IsProfit { get; set; }
}
