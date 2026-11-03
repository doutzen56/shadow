// 由旧版站点表映射过来；不继承 EntityBase*；不对业务表做 CodeFirst。
using SqlSugar;

namespace SimpX.Net.Data.Entity.Sites;

/// <summary>
/// 站点游戏列表
/// </summary>
[SugarTable("site_GameList")]
public class SiteGameList
{
    [SugarColumn(ColumnName = "ID", IsIdentity = true)]
    public int ID { get; set; }

    [SugarColumn(ColumnName = "SiteID", IsPrimaryKey = true)]
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
    /// 游戏Name
    /// </summary>
    [SugarColumn(ColumnName = "Name")]
    public string Name { get; set; }

    [SugarColumn(ColumnName = "Type", IsPrimaryKey = true)]
    public int Type { get; set; }

    [SugarColumn(ColumnName = "Category")]
    public int Category { get; set; }

    /// <summary>
    /// 游戏代码值
    /// </summary>
    [SugarColumn(ColumnName = "Code", IsPrimaryKey = true)]
    public string Code { get; set; }

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
    /// 推荐是否开启
    /// </summary>
    [SugarColumn(ColumnName = "RecommendEnable")]
    public bool RecommendEnable { get; set; }

    /// <summary>
    /// 图标
    /// </summary>
    [SugarColumn(ColumnName = "ShowImg")]
    public string ShowImg { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    [SugarColumn(ColumnName = "Sort")]
    public int Sort { get; set; }

    /// <summary>
    /// 标签
    /// </summary>
    [SugarColumn(ColumnName = "Tags")]
    public string Tags { get; set; }

    /// <summary>
    /// 推荐值
    /// </summary>
    [SugarColumn(ColumnName = "Recommend")]
    public int Recommend { get; set; }

    /// <summary>
    /// 是否显示奖池
    /// </summary>
    [SugarColumn(ColumnName = "IsJack")]
    public bool IsJack { get; set; }

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

    /// <summary>
    /// 游戏标签，top，new
    /// </summary>
    [SugarColumn(ColumnName = "GameTag")]
    public int GameTag { get; set; }

    /// <summary>
    /// 最新是否开启
    /// </summary>
    [SugarColumn(ColumnName = "NewEnable")]
    public bool NewEnable { get; set; }

    /// <summary>
    /// 最新值
    /// </summary>
    [SugarColumn(ColumnName = "New")]
    public int New { get; set; }
}
