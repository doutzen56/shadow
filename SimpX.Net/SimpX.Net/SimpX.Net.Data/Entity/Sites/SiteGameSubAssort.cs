// 由旧版站点表映射过来；不继承 EntityBase*；不对业务表做 CodeFirst。
using SqlSugar;

namespace SimpX.Net.Data.Entity.Sites;

/// <summary>
/// 站点捕鱼视图
/// </summary>
[SugarTable("game_SubAssort")]
public class SiteGameSubAssort
{
    /// <summary>
    /// 所属站点（如果站点没设置则等于0）
    /// </summary>
    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    [SugarColumn(ColumnName = "GameID")]
    public int GameID { get; set; }

    [SugarColumn(ColumnName = "Type")]
    public int Type { get; set; }

    [SugarColumn(ColumnName = "GameName")]
    public string Name { get; set; }

    /// <summary>
    /// 游戏启动代码
    /// </summary>
    [SugarColumn(ColumnName = "GameCode")]
    public string Code { get; set; }

    [SugarColumn(ColumnName = "Sort")]
    public short Sort { get; set; }

    [SugarColumn(ColumnName = "Category")]
    public int Category { get; set; }

    [SugarColumn(ColumnName = "Cover")]
    public string Cover { get; set; }

    [SugarColumn(ColumnName = "CateID")]
    public int CateID { get; set; }

    [SugarColumn(ColumnName = "Platform")]
    public int Platform { get; set; }

    [SugarColumn(ColumnName = "Flag")]
    public int Flag { get; set; }
}
