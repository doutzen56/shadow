// 由旧版游戏日志采集标记表映射过来；不继承 EntityBase*；不对业务表做 CodeFirst。
using SqlSugar;

namespace SimpX.Net.Data.Entity.Games;

/// <summary>
/// 游戏日志采集时间标记（表 game_Mark）。超过 DelayTime 分钟未更新则告警。
/// </summary>
[SugarTable("game_Mark")]
public class GameMark
{
    /// <summary>站点编号。</summary>
    [SugarColumn(ColumnName = "SiteID", IsPrimaryKey = true)]
    public int SiteID { get; set; }

    /// <summary>游戏类型（旧版 GameType 整型）。</summary>
    [SugarColumn(ColumnName = "Type", IsPrimaryKey = true)]
    public int Type { get; set; }

    /// <summary>标识类型。</summary>
    [SugarColumn(ColumnName = "Mark", IsPrimaryKey = true)]
    public byte Mark { get; set; }

    /// <summary>最近一次采集更新时间。</summary>
    [SugarColumn(ColumnName = "UpdateAt")]
    public DateTime UpdateAt { get; set; }

    /// <summary>延迟告警阈值（分钟）；0 表示不告警。</summary>
    [SugarColumn(ColumnName = "DelayTime")]
    public int DelayTime { get; set; }
}
