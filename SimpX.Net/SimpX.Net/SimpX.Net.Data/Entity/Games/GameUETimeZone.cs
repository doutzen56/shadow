using SqlSugar;

namespace SimpX.Net.Data.Entity.Games;

/// <summary>游戏时区配置（game_UETimeZone）：按 Hour 筛 EST/UTC 需聚合的游戏类型名。</summary>
[SugarTable("game_UETimeZone")]
public class GameUETimeZone
{
    [SugarColumn(ColumnName = "ID", IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    [SugarColumn(ColumnName = "Type")]
    public int Type { get; set; }

    /// <summary>ES Type 字段用的游戏名。</summary>
    [SugarColumn(ColumnName = "GameName")]
    public string GameName { get; set; }

    /// <summary>EST=12 / UTC=8（对齐旧 SaveUserDateGameFromES）。</summary>
    [SugarColumn(ColumnName = "Hour")]
    public int Hour { get; set; }
}
