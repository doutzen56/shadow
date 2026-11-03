// 由旧版站点表映射过来；不继承 EntityBase*；不对业务表做 CodeFirst。
using SqlSugar;

namespace SimpX.Net.Data.Entity.Games;

/// <summary>
/// 站点厂商 API / 游戏参数（表 game_Setting）。Game 存旧版 GameType 整型。
/// </summary>
[SugarTable("game_Setting")]
public class GameSetting
{
    /// <summary>站点编号。</summary>
    [SugarColumn(ColumnName = "SiteID", IsPrimaryKey = true)]
    public int SiteID { get; set; }

    /// <summary>游戏类型（旧版 GameType 枚举值）。</summary>
    [SugarColumn(ColumnName = "Game", IsPrimaryKey = true)]
    public int Game { get; set; }

    /// <summary>自定义名称。</summary>
    [SugarColumn(ColumnName = "Name")]
    public string Name { get; set; }

    /// <summary>是否开启。</summary>
    [SugarColumn(ColumnName = "IsOpen")]
    public bool IsOpen { get; set; }

    /// <summary>剩余分数。</summary>
    [SugarColumn(ColumnName = "Credit")]
    public decimal Credit { get; set; }

    /// <summary>买分条件。</summary>
    [SugarColumn(ColumnName = "Rate")]
    public decimal Rate { get; set; }

    /// <summary>是否月结。</summary>
    [SugarColumn(ColumnName = "IsMonthly")]
    public bool IsMonthly { get; set; }

    /// <summary>是否自有接口。</summary>
    [SugarColumn(ColumnName = "IsSelf")]
    public bool IsSelf { get; set; }

    /// <summary>参数 JSON。</summary>
    [SugarColumn(ColumnName = "Setting")]
    public string SettingString { get; set; }

    /// <summary>是否免转。</summary>
    [SugarColumn(ColumnName = "IsFree")]
    public bool IsFree { get; set; }

    /// <summary>排序。</summary>
    [SugarColumn(ColumnName = "Sort")]
    public short Sort { get; set; }

    /// <summary>代理平台费。</summary>
    [SugarColumn(ColumnName = "AgentRate")]
    public decimal AgentRate { get; set; }

    /// <summary>是否计流水。</summary>
    [SugarColumn(ColumnName = "IsWater")]
    public bool IsWater { get; set; }

    /// <summary>标签。</summary>
    [SugarColumn(ColumnName = "Label")]
    public string Label { get; set; }

    /// <summary>背景图。</summary>
    [SugarColumn(ColumnName = "Backdrop")]
    public string Backdrop { get; set; }

    /// <summary>Logo。</summary>
    [SugarColumn(ColumnName = "Logo")]
    public string Logo { get; set; }

    /// <summary>是否推荐背景。</summary>
    [SugarColumn(ColumnName = "IsBackdrop")]
    public bool IsBackdrop { get; set; }

    /// <summary>详细点位。</summary>
    [SugarColumn(ColumnName = "DetailRate")]
    public string DetailRate { get; set; }

    /// <summary>是否下架。</summary>
    [SugarColumn(ColumnName = "IsDisable")]
    public bool IsDisable { get; set; }

    /// <summary>最低转账。</summary>
    [SugarColumn(ColumnName = "MinTransfer")]
    public int MinTransfer { get; set; }

    /// <summary>最高转账。</summary>
    [SugarColumn(ColumnName = "MaxTransfer")]
    public int MaxTransfer { get; set; }

    /// <summary>分线状态 0停1正常2维护3部分。</summary>
    [SugarColumn(ColumnName = "Status")]
    public int Status { get; set; }

    /// <summary>维护分类。</summary>
    [SugarColumn(ColumnName = "Maintenance")]
    public string Maintenance { get; set; }
}
