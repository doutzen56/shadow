// 由旧版系统表映射过来；不继承 EntityBase*；不对业务表做 CodeFirst。
using SqlSugar;

namespace SimpX.Net.Data.Entity.Systems;

/// <summary>
/// 系统层充值接口（表 sys_Payment）。
/// </summary>
[SugarTable("sys_Payment")]
public class SystemPayment
{
    /// <summary>编号。</summary>
    [SugarColumn(ColumnName = "SettingID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    /// <summary>分组。</summary>
    [SugarColumn(ColumnName = "GroupID")]
    public int GroupID { get; set; }

    /// <summary>名称。</summary>
    [SugarColumn(ColumnName = "Name")]
    public string Name { get; set; }

    /// <summary>是否开放。</summary>
    [SugarColumn(ColumnName = "IsOpen")]
    public bool IsOpen { get; set; }

    /// <summary>字段配置串。</summary>
    [SugarColumn(ColumnName = "Fields")]
    public string FieldString { get; set; }

    /// <summary>网银配置串。</summary>
    [SugarColumn(ColumnName = "Bank")]
    public string BankString { get; set; }

    /// <summary>签名配置串。</summary>
    [SugarColumn(ColumnName = "Sign")]
    public string SignString { get; set; }

    /// <summary>提交配置串。</summary>
    [SugarColumn(ColumnName = "Submit")]
    public string SubmitString { get; set; }

    /// <summary>回调配置串。</summary>
    [SugarColumn(ColumnName = "Callback")]
    public string CallbackString { get; set; }

    /// <summary>特殊支付类型整型。</summary>
    [SugarColumn(ColumnName = "SpecialType")]
    public int SpecialType { get; set; }
}
