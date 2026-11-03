// 由旧版站点表映射过来；不继承 EntityBase*；不对业务表做 CodeFirst。
using SqlSugar;

namespace SimpX.Net.Data.Entity.Sites;

/// <summary>
/// 站点自定义配置的菜单项目（视图）
/// </summary>
[SugarTable("site_PageItem")]
public class SitePageItem
{
    [SugarColumn(ColumnName = "PageID")]
    public int PageID { get; set; }

    /// <summary>
    /// 所属站点
    /// </summary>
    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    /// <summary>
    /// 所属控件
    /// </summary>
    [SugarColumn(ColumnName = "ControlID")]
    public int ControlID { get; set; }

    /// <summary>
    /// 菜单ID
    /// </summary>
    [SugarColumn(ColumnName = "ItemID")]
    public int ItemID { get; set; }

    /// <summary>
    /// 菜单配件的名字
    /// </summary>
    [SugarColumn(ColumnName = "Name")]
    public string Name { get; set; }

    /// <summary>
    /// 是否隐藏
    /// </summary>
    [SugarColumn(ColumnName = "IsHide")]
    public bool IsHide { get; set; }

    /// <summary>
    /// 是否使用自定义配置
    /// </summary>
    [SugarColumn(ColumnName = "IsSelf")]
    public byte IsSelf { get; set; }

    /// <summary>
    /// 自定义排序值
    /// </summary>
    [SugarColumn(ColumnName = "Sort")]
    public short Sort { get; set; }

    /// <summary>
    /// 是否热门
    /// </summary>
    [SugarColumn(ColumnName = "IsHot")]
    public byte IsHot { get; set; }

    /// <summary>
    /// 是否最新
    /// </summary>
    [SugarColumn(ColumnName = "IsNew")]
    public bool IsNew { get; set; }

    /// <summary>
    /// 转账备注
    /// </summary>
    [SugarColumn(ColumnName = "TransferRemark")]
    public string TransferRemark { get; set; }

    /// <summary>
    /// 自定义的配置
    /// </summary>
    [SugarColumn(ColumnName = "Setting")]
    public string SettingString { get; set; }
}
