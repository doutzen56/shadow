// 由旧版站点表映射过来；不继承 EntityBase*；不对业务表做 CodeFirst。
using SqlSugar;

namespace SimpX.Net.Data.Entity.Sites;

/// <summary>
/// 平台管理员
/// </summary>
[SugarTable("site_Admin")]
public class SiteAdmin
{
    /// <summary>
    /// 站点ID
    /// </summary>
    [SugarColumn(ColumnName = "AdminID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    /// <summary>
    /// 管理员用户名
    /// </summary>
    [SugarColumn(ColumnName = "AdminName")]
    public string AdminName { get; set; }

    /// <summary>
    /// 昵称
    /// </summary>
    [SugarColumn(ColumnName = "NickName")]
    public string NickName { get; set; }

    /// <summary>
    /// 头像
    /// </summary>
    [SugarColumn(ColumnName = "Face")]
    public string Face { get; set; }

    /// <summary>
    /// 密码（MD5加密）
    /// </summary>
    [SugarColumn(ColumnName = "Password")]
    public string Password { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnName = "Status")]
    public int Status { get; set; }

    /// <summary>
    /// 所属管理员的权限分组（如果为0表示最高管理员）
    /// </summary>
    [SugarColumn(ColumnName = "GroupID")]
    public int GroupID { get; set; }

    /// <summary>
    /// 加款，上分上限
    /// </summary>
    [SugarColumn(ColumnName = "AuditUpperLimit")]
    public decimal AuditUpperLimit { get; set; }

    /// <summary>
    /// 上次登录的日期
    /// </summary>
    [SugarColumn(ColumnName = "LoginAt")]
    public DateTime LoginAt { get; set; }

    /// <summary>
    /// 上次登录的IP
    /// </summary>
    [SugarColumn(ColumnName = "LoginIP")]
    public string LoginIP { get; set; }

    /// <summary>
    /// 谷歌验证码密钥
    /// </summary>
    [SugarColumn(ColumnName = "SecretKey")]
    public Guid SecretKey { get; set; }

    /// <summary>
    /// 个性签名
    /// </summary>
    [SugarColumn(ColumnName = "Sign")]
    public string Sign { get; set; }

    /// <summary>
    /// 功能开放
    /// </summary>
    [SugarColumn(ColumnName = "Function")]
    public int Function { get; set; }

    /// <summary>
    /// 个性化提示声音配置
    /// </summary>
    [SugarColumn(ColumnName = "WarningSoundString")]
    public string WarningSoundString { get; set; }
}
