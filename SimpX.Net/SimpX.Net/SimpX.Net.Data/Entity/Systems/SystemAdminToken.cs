using SqlSugar;

namespace SimpX.Net.Data.Entity.Systems;

/// <summary>
/// 总后台登录凭据（sys_AdminToken）。不对业务表做 CodeFirst。
/// </summary>
[SugarTable("sys_AdminToken")]
public class SystemAdminToken
{
    /// <summary>管理员编号。</summary>
    [SugarColumn(ColumnName = "AdminID", IsPrimaryKey = true)]
    public int AdminID { get; set; }

    /// <summary>登录令牌。</summary>
    [SugarColumn(ColumnName = "Token")]
    public Guid Token { get; set; }

    /// <summary>更新时间。</summary>
    [SugarColumn(ColumnName = "UpdateAt")]
    public DateTime UpdateAt { get; set; }
}
