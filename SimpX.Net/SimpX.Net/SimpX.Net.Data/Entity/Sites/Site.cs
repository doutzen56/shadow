// 由旧版站点表映射过来；不继承 EntityBase*；不对业务表做 CodeFirst。
using SqlSugar;

namespace SimpX.Net.Data.Entity.Sites;

/// <summary>
/// 站点配置
/// </summary>
[SugarTable("Site")]
public class Site
{
    /// <summary>
    /// 站点编号
    /// </summary>
    [SugarColumn(ColumnName = "SiteID", IsPrimaryKey = true)]
    public int ID { get; set; }

    /// <summary>
    /// 平台名称
    /// </summary>
    [SugarColumn(ColumnName = "SiteName")]
    public string Name { get; set; }

    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>
    /// 通知时间
    /// </summary>
    [SugarColumn(ColumnName = "NotifyAt")]
    public DateTime NotifyAt { get; set; }

    /// <summary>
    /// 站点状态
    /// </summary>
    [SugarColumn(ColumnName = "Status")]
    public int Status { get; set; }

    /// <summary>
    /// 主币种
    /// </summary>
    [SugarColumn(ColumnName = "Currency")]
    public int Currency { get; set; }

    /// <summary>
    /// 1单一钱包 0多币种钱包
    /// </summary>
    [SugarColumn(ColumnName = "Wallet")]
    public int Wallet { get; set; }

    /// <summary>
    /// 1可以筛选 0不可以筛选
    /// </summary>
    [SugarColumn(ColumnName = "IsCurrencySelect")]
    public bool IsCurrencySelect { get; set; }

    /// <summary>
    /// API接口的密钥
    /// </summary>
    [SugarColumn(ColumnName = "APIKey")]
    public Guid APIKey { get; set; }

    /// <summary>
    /// 参数配置
    /// </summary>
    [SugarColumn(ColumnName = "Setting")]
    public string SettingString { get; set; }

    /// <summary>
    /// APP的版本号,以及升级设定
    /// </summary>
    [SugarColumn(ColumnName = "APPSetting")]
    public string APPSettingString { get; set; }

    /// <summary>
    /// PC端站点的参数设定
    /// </summary>
    [SugarColumn(ColumnName = "PCSetting")]
    public string PCSettingString { get; set; }

    /// <summary>
    /// 第三方游戏授信额度
    /// </summary>
    [SugarColumn(ColumnName = "GameCredit")]
    public decimal GameCredit { get; set; }

    /// <summary>
    /// 预警提示额度
    /// </summary>
    [SugarColumn(ColumnName = "NotifyCredit")]
    public int NotifyCredit { get; set; }

    /// <summary>
    /// 管理员后台域名
    /// </summary>
    [SugarColumn(ColumnName = "AdminDomain")]
    public string AdminDomain { get; set; }

    /// <summary>
    /// 总后台首页配置设置
    /// </summary>
    [SugarColumn(ColumnName = "HomeTemplateName")]
    public string HomeTemplateName { get; set; }
}
