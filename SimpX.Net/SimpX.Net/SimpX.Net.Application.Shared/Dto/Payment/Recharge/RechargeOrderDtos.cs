
namespace SimpX.Net.Application.Shared;

/// <summary>
/// 充值订单列表筛选（只读）。对齐旧 recharge/orderlist 主条件。
/// </summary>
public sealed class RechargeOrderPageInput
{
    /// <summary>页码，从 1 起。</summary>
    public int Page { get; set; } = 1;

    /// <summary>页大小。</summary>
    public int PageSize { get; set; } = 20;

    /// <summary>订单号。</summary>
    public string OrderID { get; set; }

    /// <summary>关键字：订单号/网关单号 Contains（对齐旧 KeyWord）。</summary>
    public string KeyWord { get; set; }

    /// <summary>用户名。</summary>
    public string User { get; set; }

    /// <summary>通道 PayID；0=全部。</summary>
    public int PayID { get; set; }

    /// <summary>是否已到账；空=全部。</summary>
    public bool? IsPay { get; set; }

    /// <summary>创建起。</summary>
    public DateTime? StartAt { get; set; }

    /// <summary>创建止。</summary>
    public DateTime? EndAt { get; set; }

    /// <summary>时间字段：CreateAt（默认）/ PayAt。</summary>
    public string DateType { get; set; }

    /// <summary>币种；空=全部。</summary>
    public int? Currency { get; set; }

    /// <summary>入款源币种（FromCurrency）；空=全部。</summary>
    public int? FromCurrency { get; set; }

    /// <summary>金额下限。</summary>
    public decimal? MoneyFrom { get; set; }

    /// <summary>金额上限。</summary>
    public decimal? MoneyTo { get; set; }

    /// <summary>上级代理用户名（旧 Agent）。</summary>
    public string Agent { get; set; }

    /// <summary>是否代客充值；空=全部。</summary>
    public bool? IsProxyRecharge { get; set; }
}

/// <summary>
/// 充值订单列表行。
/// </summary>
public sealed class RechargeOrderListItemDto
{
    /// <summary>订单号。</summary>
    public string OrderID { get; set; }

    /// <summary>会员编号。</summary>
    public int UserID { get; set; }

    /// <summary>用户名。</summary>
    public string UserName { get; set; }

    /// <summary>金额。</summary>
    public decimal Money { get; set; }

    /// <summary>实付。</summary>
    public decimal Paid { get; set; }

    /// <summary>手续费。</summary>
    public decimal Fee { get; set; }

    /// <summary>通道编号。</summary>
    public int PayID { get; set; }

    /// <summary>是否到账。</summary>
    public bool IsPay { get; set; }

    /// <summary>创建时间。</summary>
    public DateTime CreateAt { get; set; }

    /// <summary>到账时间。</summary>
    public DateTime PayAt { get; set; }

    /// <summary>币种。</summary>
    public int Currency { get; set; }

    /// <summary>备注。</summary>
    public string Remark { get; set; }

    /// <summary>网关单号。</summary>
    public string SystemID { get; set; }

    /// <summary>行政费。</summary>
    public decimal Cost { get; set; }

    /// <summary>第几次充值（到账后）。</summary>
    public int Count { get; set; }

    /// <summary>是否代客充值。</summary>
    public bool IsProxyRecharge { get; set; }

    /// <summary>上级用户名。</summary>
    public string AgentName { get; set; }

    /// <summary>审核人显示名（SiteAdmin.AdminName）。</summary>
    public string Admin { get; set; }

    /// <summary>处理时长（到账后 CreateAt→PayAt，形如 H:mm:ss；未到账为空）。</summary>
    public string DealTime { get; set; }

    /// <summary>支付渠道名（SitePayment.Name）。</summary>
    public string Payment { get; set; }

    /// <summary>第三方/支付分组名。</summary>
    public string ThirdParty { get; set; }

    /// <summary>实际充值展示（旧 RealPaid）。</summary>
    public string RealPaid { get; set; }
}

/// <summary>
/// 充值订单分页结果。
/// </summary>
public sealed class RechargeOrderPageOutput
{
    /// <summary>总条数。</summary>
    public int Total { get; set; }

    /// <summary>行。</summary>
    public List<RechargeOrderListItemDto> Items { get; set; } = [];
}
/// <summary>
/// 充值订单确认到账入参。对齐旧 recharge/checkorder：ID、SystemID、Paid、Fee、Cost。
/// </summary>
public sealed class RechargeOrderCheckInput
{
    /// <summary>订单号（旧 QF ID）。</summary>
    public string ID { get; set; }

    /// <summary>网关单号（旧 SystemID）。</summary>
    public string SystemID { get; set; }

    /// <summary>实际到账金额（旧 Paid）。</summary>
    public decimal Paid { get; set; }

    /// <summary>手续费（旧 Fee）。</summary>
    public decimal Fee { get; set; }

    /// <summary>行政费用（旧 Cost）。</summary>
    public decimal Cost { get; set; }

    /// <summary>付款账户；可空。</summary>
    public string PayAccount { get; set; }

    /// <summary>附加备注；可空。</summary>
    public string Description { get; set; }
}
/// <summary>
/// 拒绝/删除充值订单入参。对齐旧 deleteorder 的 ID。
/// </summary>
public sealed class RechargeOrderDeleteInput
{
    /// <summary>订单号。</summary>
    public string ID { get; set; }
}

/// <summary>
/// 充值建单并提交网关入参。
/// </summary>
public sealed class RechargeOrderCreateInput
{
    /// <summary>会员编号。</summary>
    public int UserID { get; set; }

    /// <summary>站点通道编号。</summary>
    public int PayID { get; set; }

    /// <summary>充值金额。</summary>
    public decimal Money { get; set; }

    /// <summary>活动编号（可空）。</summary>
    public string ActiveID { get; set; }

    /// <summary>备注（可空）。</summary>
    public string Remark { get; set; }

    /// <summary>付款账户信息（可空）。</summary>
    public string PayAccount { get; set; }

    /// <summary>付款人姓名（可空，交给网关）。</summary>
    public string AccountName { get; set; }

    /// <summary>是否代客充值。</summary>
    public bool IsProxyRecharge { get; set; }

    /// <summary>客户端 IP；代客充值可传 0.0.0.0。</summary>
    public string ClientIP { get; set; }
}

/// <summary>
/// 充值建单并提交网关出参。
/// </summary>
public sealed class RechargeOrderCreateOutput
{
    /// <summary>订单号。</summary>
    public string OrderID { get; set; }

    /// <summary>支付跳转地址（可空）。</summary>
    public string PayUrl { get; set; }

    /// <summary>支付原文/表单（可空）。</summary>
    public string PayContent { get; set; }

    /// <summary>网关单号（可空）。</summary>
    public string SystemId { get; set; }
}
