// 由旧版系统表映射过来；不继承 EntityBase*；不对业务表做 CodeFirst。
using SqlSugar;

namespace SimpX.Net.Data.Entity.Systems;

/// <summary>
/// 系统支付分组（表 sys_PaymentGroup）。回调白名单在此。
/// </summary>
[SugarTable("sys_PaymentGroup")]
public class SystemPaymentGroup
{
    /// <summary>分组编号。</summary>
    [SugarColumn(ColumnName = "GroupID", IsPrimaryKey = true, IsIdentity = true)]
    public int ID { get; set; }

    /// <summary>分组名。</summary>
    [SugarColumn(ColumnName = "GroupName")]
    public string Name { get; set; }

    /// <summary>回调白名单 IP，逗号或竖线分隔。</summary>
    [SugarColumn(ColumnName = "CallbackIP")]
    public string CallbackIP { get; set; }

    /// <summary>是否启用（库字段原样）。</summary>
    [SugarColumn(ColumnName = "IsEnable")]
    public string IsEnable { get; set; }
}
