// 由旧版站点表映射过来；不继承 EntityBase*；不对业务表做 CodeFirst。
using SqlSugar;

namespace SimpX.Net.Data.Entity.Sites;

/// <summary>
/// 管理员的权限分组
/// </summary>
[SugarTable("site_AdminGroup")]
public class SiteAdminGroup
{
    /// <summary>
    /// 分组编号
    /// </summary>
    [SugarColumn(ColumnName = "GroupID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    /// <summary>
    /// 分组名称
    /// </summary>
    [SugarColumn(ColumnName = "GroupName")]
    public string Name { get; set; }

    /// <summary>
    /// 分组权限
    /// </summary>
    [SugarColumn(ColumnName = "Permission")]
    public string PermissionString { get; set; }

    /// <summary>
    /// 分组内管理员的数量
    /// </summary>
    [SugarColumn(ColumnName = "Member")]
    public int Member { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }
}
