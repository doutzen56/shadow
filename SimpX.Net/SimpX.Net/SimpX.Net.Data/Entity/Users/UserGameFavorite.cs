namespace SimpX.Net.Data.Entity.Users;

/// <summary>
/// 用户游戏收藏信息
/// </summary>
[SugarTable("usr_GameFavorite")]
public class UserGameFavorite
{
    /// <summary>
    /// 编号
    /// </summary>
    [SugarColumn(ColumnName = "ID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    [SugarColumn(ColumnName = "UserID")]
    public int UserID { get; set; }

    /// <summary>
    /// 站点游戏ID
    /// </summary>
    [SugarColumn(ColumnName = "GameID")]
    public int GameID { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>
    /// 标识是否是热门或最新，hot ,new
    /// </summary>
    [SugarColumn(ColumnName = "Category")]
    public string Category { get; set; }
}
