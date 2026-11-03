using SqlSugar;

namespace SimpX.Net.Data.Entity.Games;

/// <summary>
/// 游戏标签（表 game_Tag）。对齐旧 BW.Common.Games.GameTag。
/// </summary>
[SugarTable("game_Tag")]
public class GameTag
{
    /// <summary>标签编号。</summary>
    [SugarColumn(ColumnName = "TagID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    /// <summary>标签名（可存多语 JSON）。</summary>
    [SugarColumn(ColumnName = "TagName")]
    public string TagName { get; set; }

    /// <summary>排序，从小到大。</summary>
    [SugarColumn(ColumnName = "Sort")]
    public int Sort { get; set; }

    /// <summary>状态：1 开 / 0 关（旧 OpenStatus）。</summary>
    [SugarColumn(ColumnName = "Status")]
    public int Status { get; set; }

    /// <summary>创建时间。</summary>
    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }
}
