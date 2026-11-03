// 由旧版站点表映射过来；不继承 EntityBase*；不对业务表做 CodeFirst。
using SqlSugar;

namespace SimpX.Net.Data.Entity.Sites;

/// <summary>
/// 映射表 site_GameCate
/// </summary>
[SugarTable("site_GameCate")]
public class SiteGameCate
{
    [SugarColumn(ColumnName = "ID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    /// <summary>
    /// 游戏名称
    /// </summary>
    [SugarColumn(ColumnName = "GameName")]
    public string GameName { get; set; }

    /// <summary>
    /// 游戏编码
    /// </summary>
    [SugarColumn(ColumnName = "GameCode")]
    public string GameCode { get; set; }

    /// <summary>
    /// 游戏Id 枚举byte值
    /// </summary>
    [SugarColumn(ColumnName = "GameID")]
    public int GameID { get; set; }

    /// <summary>
    /// 游戏GameParentID 枚举byte值
    /// </summary>
    [SugarColumn(ColumnName = "GameParentID")]
    public int GameParentID { get; set; }

    /// <summary>
    /// 是否开启
    /// </summary>
    [SugarColumn(ColumnName = "IsEnable")]
    public bool IsEnable { get; set; }

    /// <summary>
    /// Pc是否开启
    /// </summary>
    [SugarColumn(ColumnName = "PcEnable")]
    public bool PcEnable { get; set; }

    /// <summary>
    /// App是否开启
    /// </summary>
    [SugarColumn(ColumnName = "AppEnable")]
    public bool AppEnable { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    [SugarColumn(ColumnName = "Sort")]
    public int Sort { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnName = "Remark")]
    public string Remark { get; set; }

    /// <summary>
    /// 修改时间
    /// </summary>
    [SugarColumn(ColumnName = "UpdateAt")]
    public DateTime UpdateAt { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }
}
