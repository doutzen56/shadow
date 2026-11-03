// 由旧版站点表映射过来；不继承 EntityBase*；不对业务表做 CodeFirst。
using SqlSugar;

namespace SimpX.Net.Data.Entity.Sites;

/// <summary>
/// 短信验证码
/// </summary>
[SugarTable("site_ValidCode")]
public class SiteValidCode
{
    /// <summary>
    /// 验证字段
    /// </summary>
    [SugarColumn(ColumnName = "CodeID", IsPrimaryKey = true)]
    public Guid ID { get; set; }

    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    /// <summary>
    /// 触发发送的用户（可能为0）
    /// </summary>
    [SugarColumn(ColumnName = "UserID")]
    public int UserID { get; set; }

    /// <summary>
    /// 验证类型  Register|Login|Mobile|Email
    /// </summary>
    [SugarColumn(ColumnName = "Type")]
    public int Type { get; set; }

    /// <summary>
    /// 手机号码
    /// </summary>
    [SugarColumn(ColumnName = "Mobile")]
    public string Mobile { get; set; }

    /// <summary>
    /// 发送到邮箱
    /// </summary>
    [SugarColumn(ColumnName = "Email")]
    public string Email { get; set; }

    [SugarColumn(ColumnName = "Code")]
    public string Code { get; set; }

    /// <summary>
    /// 验证码的创建时间
    /// </summary>
    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>
    /// 验证码的有效时间
    /// </summary>
    [SugarColumn(ColumnName = "ExpireAt")]
    public DateTime ExpireAt { get; set; }

    /// <summary>
    /// 是否已经被使用
    /// </summary>
    [SugarColumn(ColumnName = "IsUse")]
    public bool IsUse { get; set; }
}
