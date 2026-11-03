namespace SimpX.Net.Data.Entity.Users;

/// <summary>
/// 代理绑定的域名
/// </summary>
[SugarTable("usr_Domain")]
public class UserDomain
{
    /// <summary>
    /// 域名编号
    /// </summary>
    [SugarColumn(ColumnName = "DomainID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    /// <summary>
    /// 域名属于的用户
    /// </summary>
    [SugarColumn(ColumnName = "UserID")]
    public int UserID { get; set; }

    /// <summary>
    /// 域名
    /// </summary>
    [SugarColumn(ColumnName = "DomainName")]
    public string Name { get; set; }

    /// <summary>
    /// 绑定的邀请码
    /// </summary>
    [SugarColumn(ColumnName = "InviteID")]
    public string InviteID { get; set; }

    /// <summary>
    /// 添加时间
    /// </summary>
    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>
    /// 代理域名自定义LOGO
    /// </summary>
    [SugarColumn(ColumnName = "Logo")]
    public string Logo { get; set; }

    /// <summary>
    /// 站点名字
    /// </summary>
    [SugarColumn(ColumnName = "SiteName")]
    public string SiteName { get; set; }

    /// <summary>
    /// 背景
    /// </summary>
    [SugarColumn(ColumnName = "DefaultBackground")]
    public string DefaultBackground { get; set; }

    /// <summary>
    /// 默认版型
    /// </summary>
    [SugarColumn(ColumnName = "DefaultTemplate")]
    public string DefaultTemplate { get; set; }
}
