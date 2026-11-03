using SqlSugar;

namespace SimpX.Net.Data.Entity.Systems;

/// <summary>
/// 总后台管理员（sys_Admin）。不对业务表做 CodeFirst。
/// </summary>
[SugarTable("sys_Admin")]
public class SystemAdmin
{
    /// <summary>管理员编号。</summary>
    [SugarColumn(ColumnName = "AdminID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    /// <summary>登录名。</summary>
    [SugarColumn(ColumnName = "AdminName")]
    public string AdminName { get; set; }

    /// <summary>密码（MD5 大写）。</summary>
    [SugarColumn(ColumnName = "Password")]
    public string Password { get; set; }

    /// <summary>创建时间。</summary>
    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>状态：0 正常，其它停用（跟旧版 AdminStatus 一样）。</summary>
    [SugarColumn(ColumnName = "Status")]
    public int Status { get; set; }

    /// <summary>上次登录时间。</summary>
    [SugarColumn(ColumnName = "LoginAt")]
    public DateTime LoginAt { get; set; }

    /// <summary>上次登录 IP。</summary>
    [SugarColumn(ColumnName = "LoginIP")]
    public string LoginIP { get; set; }

    /// <summary>权限串（逗号分隔 GUID）。</summary>
    [SugarColumn(ColumnName = "Permission")]
    public string Permission { get; set; }

    /// <summary>谷歌验证密钥；Empty 表示未启用。</summary>
    [SugarColumn(ColumnName = "SecretKey")]
    public Guid SecretKey { get; set; }

    /// <summary>昵称。</summary>
    [SugarColumn(ColumnName = "NickName")]
    public string NickName { get; set; }

    /// <summary>头像。</summary>
    [SugarColumn(ColumnName = "Face")]
    public string Face { get; set; }
}
