namespace SimpX.Net.Data.Entity.Users;

/// <summary>
/// 会员分组
/// </summary>
[SugarTable("usr_Group")]
public class UserGroup
{
    /// <summary>
    /// 分组ID
    /// </summary>
    [SugarColumn(ColumnName = "GroupID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    [SugarColumn(ColumnName = "SiteID")]
    public int SiteID { get; set; }

    /// <summary>
    /// 分组名字
    /// </summary>
    [SugarColumn(ColumnName = "GroupName")]
    public string Name { get; set; }

    /// <summary>
    /// 该分组可以看到的充值方式
    /// </summary>
    [SugarColumn(ColumnName = "Payment")]
    public string Payment { get; set; }

    /// <summary>
    /// 是否是默认分组
    /// </summary>
    [SugarColumn(ColumnName = "IsDefault")]
    public bool IsDefault { get; set; }

    /// <summary>
    /// 分组创建时间
    /// </summary>
    [SugarColumn(ColumnName = "CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>
    /// 分组的参数配置
    /// </summary>
    [SugarColumn(ColumnName = "Withdraw")]
    public string WithdrawString { get; set; }

    /// <summary>
    /// 组内用户数量
    /// </summary>
    [SugarColumn(ColumnName = "Member")]
    public int Member { get; set; }

    /// <summary>
    /// 是否支持自动升级
    /// </summary>
    [SugarColumn(ColumnName = "IsUpdate")]
    public bool IsUpdate { get; set; }

    /// <summary>
    /// 升级到该组需要满足的条件
    /// </summary>
    [SugarColumn(ColumnName = "Condition")]
    public string ConditionString { get; set; }

    /// <summary>
    /// 排序值
    /// </summary>
    [SugarColumn(ColumnName = "Sort")]
    public short Sort { get; set; }

    /// <summary>
    /// 货币
    /// </summary>
    [SugarColumn(ColumnName = "SettingInfo")]
    public string SettingInfo { get; set; }

    /// <summary>
    /// 是否夜间模式
    /// </summary>
    [SugarColumn(ColumnName = "IsNight")]
    public bool IsNight { get; set; }
}
