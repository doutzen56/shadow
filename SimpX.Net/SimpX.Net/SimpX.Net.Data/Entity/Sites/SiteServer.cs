// 由旧版站点表映射过来；不继承 EntityBase*；不对业务表做 CodeFirst。
using SqlSugar;

namespace SimpX.Net.Data.Entity.Sites;

/// <summary>
/// 平台的节点
/// </summary>
[SugarTable("site_Server")]
public class SiteServer
{
    /// <summary>
    /// 服务器编号
    /// </summary>
    [SugarColumn(ColumnName = "ServerID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    /// <summary>
    /// 节点名称
    /// </summary>
    [SugarColumn(ColumnName = "ServerName")]
    public string Name { get; set; }

    /// <summary>
    /// 节点备注,总管理员可见
    /// </summary>
    [SugarColumn(ColumnName = "ServerDesc")]
    public string Description { get; set; }

    /// <summary>
    /// IPV4地址
    /// </summary>
    [SugarColumn(ColumnName = "IP")]
    public string IP { get; set; }

    /// <summary>
    /// 节点的在线请求时间
    /// </summary>
    [SugarColumn(ColumnName = "OnlineAt")]
    public DateTime OnlineAt { get; set; }

    /// <summary>
    /// 别名记录（SiteID+4位随机）
    /// </summary>
    [SugarColumn(ColumnName = "CName")]
    public string CName { get; set; }

    /// <summary>
    /// A记录要指向的IP,默认等于服务IP
    /// </summary>
    [SugarColumn(ColumnName = "AIP")]
    public string AIP { get; set; }
}
