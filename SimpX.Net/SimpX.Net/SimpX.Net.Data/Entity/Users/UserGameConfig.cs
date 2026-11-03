namespace SimpX.Net.Data.Entity.Users;

/// <summary>
/// 用戶的遊戲參數前置配置
/// </summary>
[SugarTable("usr_GameConfig")]
public class UserGameConfig
{
    [SugarColumn(ColumnName = "UserID", IsPrimaryKey = true)]
    public int UserID { get; set; }

    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    /// <summary>
    /// 彩票點位（如果彩票遊戲支持的話）
    /// </summary>
    [SugarColumn(ColumnName = "Lottery")]
    public int Lottery { get; set; }
}
