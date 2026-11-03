namespace SimpX.Net.Data.Entity.Users;

/// <summary>
/// 第三方授权帐号
/// </summary>
[SugarTable("usr_Authorization")]
public class UserAuthorization
{
    [SugarColumn(ColumnName = "UserID", IsPrimaryKey = true)]
    public int UserID { get; set; }

    [SugarColumn(ColumnName = "SiteID", IsPrimaryKey = true)]
    public int SiteID { get; set; }

    /// <summary>
    /// 授权类型
    /// </summary>
    [SugarColumn(ColumnName = "Type", IsPrimaryKey = true)]
    public int Type { get; set; }

    /// <summary>
    /// 第三方平台的唯一ID
    /// </summary>
    [SugarColumn(ColumnName = "OpenID", IsPrimaryKey = true)]
    public string OpenID { get; set; }

    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>
    /// 原始数据
    /// </summary>
    [SugarColumn(ColumnName = "RawData")]
    public string RawData { get; set; }
}
