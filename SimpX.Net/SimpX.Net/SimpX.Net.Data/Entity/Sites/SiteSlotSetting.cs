// 由旧版站点表映射过来；不继承 EntityBase*；不对业务表做 CodeFirst。
using SqlSugar;

namespace SimpX.Net.Data.Entity.Sites;

/// <summary>
/// 站点的电子游戏设置
/// </summary>
[SugarTable("site_SlotSetting")]
public class SiteSlotSetting
{
    /// <summary>
    /// 所属站点
    /// </summary>
    [SugarColumn(ColumnName = "SiteID", IsPrimaryKey = true)]
    public int SiteID { get; set; }

    /// <summary>
    /// 游戏ID
    /// </summary>
    [SugarColumn(ColumnName = "GameID", IsPrimaryKey = true)]
    public int GameID { get; set; }

    /// <summary>
    /// 排序值（从大到小）
    /// </summary>
    [SugarColumn(ColumnName = "Sort")]
    public short Sort { get; set; }

    /// <summary>
    /// 开启状态
    /// </summary>
    [SugarColumn(ColumnName = "Status")]
    public int Status { get; set; }

    /// <summary>
    /// 老虎机的标识（热门/新）
    /// </summary>
    [SugarColumn(ColumnName = "Flag")]
    public int Flag { get; set; }
}
