namespace SimpX.Net.Data.Entity.Users;

/// <summary>
/// 登录日志
/// </summary>
[SugarTable("usr_LoginLog")]
public class LoginLog
{
    [SugarColumn(ColumnName = "LogID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    [SugarColumn(ColumnName = "UserID")]
    public int UserID { get; set; }

    /// <summary>
    /// 登录时间
    /// </summary>
    [SugarColumn(ColumnName = "LoginAt")]
    public DateTime LoginAt { get; set; }

    [SugarColumn(ColumnName = "IP")]
    public string IP { get; set; }

    /// <summary>
    /// IP来源
    /// </summary>
    [SugarColumn(ColumnName = "IPAddress")]
    public string IPAddress { get; set; }

    /// <summary>
    /// 登录域名
    /// </summary>
    [SugarColumn(ColumnName = "LoginDomain")]
    public string LoginDomain { get; set; }

    /// <summary>
    /// 是否是最近一次登录
    /// </summary>
    [SugarColumn(ColumnName = "IsLast")]
    public bool IsLast { get; set; }

    /// <summary>
    /// 登录设备
    /// </summary>
    [SugarColumn(ColumnName = "Platform")]
    public int Platform { get; set; }

    /// <summary>
    /// 登录设备ID
    /// </summary>
    [SugarColumn(ColumnName = "DeviceID")]
    public string DeviceID { get; set; }
}
