// 由旧版系统表映射过来；不继承 EntityBase*；不对业务表做 CodeFirst。
using SqlSugar;

namespace SimpX.Net.Data.Entity.Games;

/// <summary>
/// 系统层游戏配置（表 sys_GameSetting）。Type 存旧版 GameType 整型。
/// </summary>
[SugarTable("sys_GameSetting")]
public class SystemGameSetting
{
    /// <summary>游戏类型。</summary>
    [SugarColumn(ColumnName = "Type", IsPrimaryKey = true)]
    public int Type { get; set; }

    /// <summary>系统名称。</summary>
    [SugarColumn(ColumnName = "GameName")]
    public string Name { get; set; }

    /// <summary>参数。</summary>
    [SugarColumn(ColumnName = "Setting")]
    public string SettingString { get; set; }

    /// <summary>全局状态 0停1正常2维护。</summary>
    [SugarColumn(ColumnName = "Status")]
    public int Status { get; set; }

    /// <summary>默认条件。</summary>
    [SugarColumn(ColumnName = "DefaultRate")]
    public decimal DefaultRate { get; set; }

    /// <summary>支持币种。</summary>
    [SugarColumn(ColumnName = "Currencys")]
    public string Currencys { get; set; }

    /// <summary>支持语言。</summary>
    [SugarColumn(ColumnName = "Languages")]
    public string Languages { get; set; }

    /// <summary>支持分类。</summary>
    [SugarColumn(ColumnName = "GameCategorys")]
    public string GameCategorys { get; set; }

    /// <summary>维护分类。</summary>
    [SugarColumn(ColumnName = "Maintenance")]
    public string Maintenance { get; set; }

    /// <summary>支持内嵌。</summary>
    [SugarColumn(ColumnName = "Embedded")]
    public bool Embedded { get; set; }

    /// <summary>维护计划 JSON（临时/每周/每月窗口）。</summary>
    [SugarColumn(ColumnName = "MaintainPlan")]
    public string MaintainPlan { get; set; }
}
