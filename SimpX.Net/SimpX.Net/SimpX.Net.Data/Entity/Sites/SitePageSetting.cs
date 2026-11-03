// 由旧版站点表映射过来；不继承 EntityBase*；不对业务表做 CodeFirst。
using SqlSugar;

namespace SimpX.Net.Data.Entity.Sites;

/// <summary>
/// 站点的页面配置
/// </summary>
[SugarTable("site_PageSetting")]
public class SitePageSetting
{
    /// <summary>
    /// 页面配置编号
    /// </summary>
    [SugarColumn(ColumnName = "PageID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    /// <summary>
    /// 所属站点
    /// </summary>
    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    /// <summary>
    /// 关联的系统控件编号
    /// </summary>
    [SugarColumn(ColumnName = "ControlID")]
    public int ControlID { get; set; }

    /// <summary>
    /// 系统菜单编号
    /// </summary>
    [SugarColumn(ColumnName = "ItemID")]
    public int ItemID { get; set; }

    /// <summary>
    /// 自定义排序
    /// </summary>
    [SugarColumn(ColumnName = "Sort")]
    public short Sort { get; set; }

    /// <summary>
    /// 是否隐藏
    /// </summary>
    [SugarColumn(ColumnName = "IsHide")]
    public bool IsHide { get; set; }

    /// <summary>
    /// 0:默认系统，1：自定义，2：终端类型
    /// </summary>
    [SugarColumn(ColumnName = "IsSelf")]
    public byte IsSelf { get; set; }

    /// <summary>
    /// 自定义的参数配置
    /// </summary>
    [SugarColumn(ColumnName = "Setting")]
    public string SettingString { get; set; }

    /// <summary>
    /// 语言类型
    /// </summary>
    [SugarColumn(ColumnName = "Language")]
    public int Language { get; set; }

    /// <summary>
    /// 是否热门
    /// </summary>
    [SugarColumn(ColumnName = "IsHot")]
    public byte IsHot { get; set; }

    /// <summary>
    /// 是否新游戏
    /// </summary>
    [SugarColumn(ColumnName = "IsNew")]
    public bool IsNew { get; set; }

    /// <summary>
    /// 转账备注
    /// </summary>
    [SugarColumn(ColumnName = "TransferRemark")]
    public string TransferRemark { get; set; }
}
