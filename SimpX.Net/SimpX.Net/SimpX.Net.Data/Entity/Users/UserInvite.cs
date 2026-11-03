namespace SimpX.Net.Data.Entity.Users;

/// <summary>
/// 邀请码
/// </summary>
[SugarTable("usr_Invite")]
public class UserInvite
{
    /// <summary>
    /// 邀请码
    /// </summary>
    [SugarColumn(ColumnName = "InviteID", IsPrimaryKey = true)]
    public string ID { get; set; }

    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    [SugarColumn(ColumnName = "UserID")]
    public int UserID { get; set; }

    /// <summary>
    /// 该邀请码的注册数量
    /// </summary>
    [SugarColumn(ColumnName = "Member")]
    public int Member { get; set; }

    /// <summary>
    /// 是否开启
    /// </summary>
    [SugarColumn(ColumnName = "IsOpen")]
    public bool IsOpen { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>
    /// 邀請設置
    /// </summary>
    [SugarColumn(ColumnName = "Setting")]
    public string SettingString { get; set; }
}
