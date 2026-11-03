// 由旧版系统表映射过来；不对业务表做 CodeFirst。
using SqlSugar;

namespace SimpX.Net.Data.Entity.Systems;

/// <summary>
/// 系统层代付接口（表 sys_Withdraw）。
/// </summary>
[SugarTable("sys_Withdraw")]
public class SystemWithdraw
{
    /// <summary>编号。</summary>
    [SugarColumn(ColumnName = "SettingID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    /// <summary>名称。</summary>
    [SugarColumn(ColumnName = "Name")]
    public string Name { get; set; }

    /// <summary>是否开放。</summary>
    [SugarColumn(ColumnName = "IsOpen")]
    public bool IsOpen { get; set; }

    /// <summary>字段配置。</summary>
    [SugarColumn(ColumnName = "Fields")]
    public string FieldString { get; set; }

    /// <summary>网银配置。</summary>
    [SugarColumn(ColumnName = "Bank")]
    public string BankString { get; set; }

    /// <summary>签名配置。</summary>
    [SugarColumn(ColumnName = "Sign")]
    public string SignString { get; set; }

    /// <summary>提交配置。</summary>
    [SugarColumn(ColumnName = "Submit")]
    public string SubmitString { get; set; }

    /// <summary>回调配置。</summary>
    [SugarColumn(ColumnName = "Callback")]
    public string CallbackString { get; set; }
}
