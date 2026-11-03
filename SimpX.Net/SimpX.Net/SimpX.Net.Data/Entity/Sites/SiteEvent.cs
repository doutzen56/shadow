// 由旧版站点表映射过来；不继承 EntityBase*；不对业务表做 CodeFirst。
using SqlSugar;

namespace SimpX.Net.Data.Entity.Sites;

/// <summary>
/// 活动设置
/// </summary>
[SugarTable("site_Event")]
public class SiteEvent
{
    /// <summary>
    /// 活动编号
    /// </summary>
    [SugarColumn(ColumnName = "EventID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    /// <summary>
    /// 活动标题
    /// </summary>
    [SugarColumn(ColumnName = "Title")]
    public string Title { get; set; }

    /// <summary>
    /// 活动副标题
    /// </summary>
    [SugarColumn(ColumnName = "SubTitle")]
    public string SubTitle { get; set; }

    /// <summary>
    /// 标签
    /// </summary>
    [SugarColumn(ColumnName = "Tag")]
    public string Tag { get; set; }

    /// <summary>
    /// 封面图
    /// </summary>
    [SugarColumn(ColumnName = "Cover")]
    public string Cover { get; set; }

    /// <summary>
    /// 活动所属分类
    /// </summary>
    [SugarColumn(ColumnName = "ColID")]
    public int ColID { get; set; }

    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>
    /// 有效日期 开始时间
    /// </summary>
    [SugarColumn(ColumnName = "StartAt")]
    public DateTime StartAt { get; set; }

    /// <summary>
    /// 有效日期 结束时间
    /// </summary>
    [SugarColumn(ColumnName = "ExpireAt")]
    public DateTime ExpireAt { get; set; }

    /// <summary>
    /// 摘要说明
    /// </summary>
    [SugarColumn(ColumnName = "Description")]
    public string Description { get; set; }

    /// <summary>
    /// 活动内容
    /// </summary>
    [SugarColumn(ColumnName = "Content")]
    public string Content { get; set; }

    /// <summary>
    /// 参数人数
    /// </summary>
    [SugarColumn(ColumnName = "Member")]
    public int Member { get; set; }

    /// <summary>
    /// 活动模板参数设定
    /// </summary>
    [SugarColumn(ColumnName = "Setting")]
    public string SettingString { get; set; }

    /// <summary>
    /// 模板类型
    /// </summary>
    [SugarColumn(ColumnName = "Type")]
    public int Type { get; set; }

    /// <summary>
    /// 是否显示
    /// </summary>
    [SugarColumn(ColumnName = "IsVisible")]
    public bool IsVisible { get; set; }

    /// <summary>
    /// 是否开启
    /// </summary>
    [SugarColumn(ColumnName = "IsOpen")]
    public bool IsOpen { get; set; }

    /// <summary>
    /// 是否开启登录前弹窗
    /// </summary>
    [SugarColumn(ColumnName = "IsLoginBeforeOpen")]
    public bool IsLoginBeforeOpen { get; set; }

    /// <summary>
    /// 是否开启登录后弹窗
    /// </summary>
    [SugarColumn(ColumnName = "IsLoginAfterOpen")]
    public bool IsLoginAfterOpen { get; set; }

    /// <summary>
    /// 是否开启快捷入口
    /// </summary>
    [SugarColumn(ColumnName = "IsQuickEntrance")]
    public bool IsQuickEntrance { get; set; }

    /// <summary>
    /// 是否显示详情
    /// </summary>
    [SugarColumn(ColumnName = "IsDetail")]
    public bool IsDetail { get; set; }

    /// <summary>
    /// 是否新手任务
    /// </summary>
    [SugarColumn(ColumnName = "IsNewerTask")]
    public bool IsNewerTask { get; set; }

    /// <summary>
    /// 跳转类型
    /// </summary>
    [SugarColumn(ColumnName = "JumpType")]
    public byte JumpType { get; set; }

    /// <summary>
    /// 活动状态
    /// </summary>
    [SugarColumn(ColumnName = "Status")]
    public int Status { get; set; }

    /// <summary>
    /// 自定义视图页面
    /// </summary>
    [SugarColumn(ColumnName = "Views")]
    public string Views { get; set; }

    /// <summary>
    /// 皮肤代码（与样式配合使用）
    /// </summary>
    [SugarColumn(ColumnName = "Skin")]
    public string Skin { get; set; }

    /// <summary>
    /// 要求置顶（从大到小）
    /// </summary>
    [SugarColumn(ColumnName = "Sort")]
    public short Sort { get; set; }

    /// <summary>
    /// 活动发放的币种
    /// </summary>
    [SugarColumn(ColumnName = "Currency")]
    public int Currency { get; set; }

    /// <summary>
    /// 活动支持的币种
    /// </summary>
    [SugarColumn(ColumnName = "SupportCurrency")]
    public string SupportCurrency { get; set; }

    [SugarColumn(ColumnName = "FilterGame")]
    public string FilterGame { get; set; }

    /// <summary>
    /// 排除转入
    /// </summary>
    [SugarColumn(ColumnName = "FilterPlatform")]
    public string FilterPlatform { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnName = "Demo")]
    public string Demo { get; set; }

    /// <summary>
    /// 模板背景
    /// </summary>
    [SugarColumn(ColumnName = "Background")]
    public string Background { get; set; }
}
