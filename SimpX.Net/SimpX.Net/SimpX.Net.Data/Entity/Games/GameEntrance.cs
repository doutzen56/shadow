// 由旧版系统表映射过来；不继承 EntityBase*；不对业务表做 CodeFirst。
using SqlSugar;

namespace SimpX.Net.Data.Entity.Games;

/// <summary>
/// 系统游戏入口目录（表 game_Entrance）。Type 存旧版 GameType 整型。
/// </summary>
[SugarTable("game_Entrance")]
public class GameEntrance
{
    /// <summary>游戏编号。</summary>
    [SugarColumn(ColumnName = "GameID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    /// <summary>游戏类型（旧 GameType）。</summary>
    [SugarColumn(ColumnName = "Type")]
    public int Type { get; set; }

    /// <summary>游戏类别（旧 GameCategory）。</summary>
    [SugarColumn(ColumnName = "Category")]
    public int Category { get; set; }

    /// <summary>游戏名字。</summary>
    [SugarColumn(ColumnName = "GameName")]
    public string Name { get; set; }

    /// <summary>游戏代码。</summary>
    [SugarColumn(ColumnName = "GameCode")]
    public string Code { get; set; }

    /// <summary>排序（从大到小）。</summary>
    [SugarColumn(ColumnName = "Sort")]
    public short Sort { get; set; }

    /// <summary>封面图。</summary>
    [SugarColumn(ColumnName = "Cover")]
    public string Cover { get; set; }

    /// <summary>状态：0 关闭 / 1 开放。</summary>
    [SugarColumn(ColumnName = "Status")]
    public int Status { get; set; }

    /// <summary>自定义分类。</summary>
    [SugarColumn(ColumnName = "CateID")]
    public int CateID { get; set; }

    /// <summary>支持平台。</summary>
    [SugarColumn(ColumnName = "Platform")]
    public int Platform { get; set; }

    /// <summary>游戏标签。</summary>
    [SugarColumn(ColumnName = "GameTag")]
    public int GameTag { get; set; }
}
