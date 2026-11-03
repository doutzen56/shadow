namespace SimpX.Net.Data;

/// <summary>
/// 后台用户与第三方账号的绑定。同一提供方下 OpenId 唯一。
/// </summary>
[SugarTable(null, "系统用户第三方账号绑定表")]
[SysTable]
[SugarIndex("index_{table}_PO", nameof(Provider), OrderByType.Asc, nameof(OpenId), OrderByType.Asc, true)]
[SugarIndex("index_{table}_U", nameof(UserId), OrderByType.Asc)]
public class SysUserExternalLogin : EntityBase
{
    /// <summary>
    /// 后台用户 Id。
    /// </summary>
    [SugarColumn(ColumnDescription = "用户Id")]
    public long UserId { get; set; }

    /// <summary>
    /// 后台用户。
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [Navigate(NavigateType.OneToOne, nameof(UserId))]
    public SysUser SysUser { get; set; }

    /// <summary>
    /// 提供方名称。
    /// </summary>
    [SugarColumn(ColumnDescription = "提供方", Length = 32)]
    [Required, MaxLength(32)]
    public string Provider { get; set; }

    /// <summary>
    /// 第三方用户唯一标识。
    /// </summary>
    [SugarColumn(ColumnDescription = "第三方用户标识", Length = 128)]
    [Required, MaxLength(128)]
    public string OpenId { get; set; }

    /// <summary>
    /// 第三方昵称。
    /// </summary>
    [SugarColumn(ColumnDescription = "昵称", Length = 64, IsNullable = true)]
    [MaxLength(64)]
    public string? NickName { get; set; }

    /// <summary>
    /// 第三方头像。
    /// </summary>
    [SugarColumn(ColumnDescription = "头像", Length = 512, IsNullable = true)]
    [MaxLength(512)]
    public string? Avatar { get; set; }
}
