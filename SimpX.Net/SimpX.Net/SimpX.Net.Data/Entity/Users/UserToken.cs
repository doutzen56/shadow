namespace SimpX.Net.Data.Entity.Users;

/// <summary>
/// 用户的登录凭据
/// </summary>
[SugarTable("usr_Token")]
public class UserToken
{
    [SugarColumn(ColumnName = "SiteID", IsPrimaryKey = true)]
    public int SiteID { get; set; }

    /// <summary>
    /// 用户ID
    /// </summary>
    [SugarColumn(ColumnName = "UserID", IsPrimaryKey = true)]
    public int UserID { get; set; }

    /// <summary>
    /// 登录密钥
    /// </summary>
    [SugarColumn(ColumnName = "Token")]
    public Guid Token { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    [SugarColumn(ColumnName = "UpdateAt")]
    public DateTime UpdateAt { get; set; }

    /// <summary>
    /// 登录Key值类型（允许多点登录）
    /// </summary>
    [SugarColumn(ColumnName = "Type")]
    public int Type { get; set; }
}
