// 由旧版站点表映射过来；不继承 EntityBase*；不对业务表做 CodeFirst。
using SqlSugar;

namespace SimpX.Net.Data.Entity.Sites;

/// <summary>
/// 第三方授权登录的接口设定
/// </summary>
[SugarTable("site_Authorization")]
public class SiteAuthorization
{
    [SugarColumn(ColumnName = "SiteID", IsPrimaryKey = true)]
    public int SiteID { get; set; }

    /// <summary>
    /// 授权类型
    /// </summary>
    [SugarColumn(ColumnName = "Type", IsPrimaryKey = true)]
    public int Type { get; set; }

    /// <summary>
    /// 是否开启
    /// </summary>
    [SugarColumn(ColumnName = "IsOpen")]
    public bool IsOpen { get; set; }

    /// <summary>
    /// 第三方授权的参数设定
    /// </summary>
    [SugarColumn(ColumnName = "Setting")]
    public string Setting { get; set; }
}
