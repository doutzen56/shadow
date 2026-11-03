namespace SimpX.Net.Data;

/// <summary>
/// 会员。与后台 SysUser 完全独立，不共用账号表；按租户隔离。
/// </summary>
[SugarTable("member", "会员")]
[SugarIndex("index_{table}_A", nameof(Account), OrderByType.Asc, true)]
public class Member : EntityBaseTenantDel
{
    /// <summary>登录账号。</summary>
    [SugarColumn(ColumnDescription = "账号", Length = 64)]
    public string Account { get; set; }

    /// <summary>密码密文。SM2 加密后较长，预留 512，避免换库时被截断。</summary>
    [SugarColumn(ColumnDescription = "密码", Length = 512)]
    public string Password { get; set; }

    /// <summary>昵称。</summary>
    [SugarColumn(ColumnDescription = "昵称", Length = 64, IsNullable = true)]
    public string NickName { get; set; }

    /// <summary>手机号。</summary>
    [SugarColumn(ColumnDescription = "手机号", Length = 32, IsNullable = true)]
    public string Phone { get; set; }

    /// <summary>状态。</summary>
    [SugarColumn(ColumnDescription = "状态")]
    public StatusEnum Status { get; set; } = StatusEnum.Enable;
}
