namespace SimpX.Net.Application.Shared;

/// <summary>
/// 提现订单分页筛选（只读）。
/// </summary>
public sealed class WithdrawOrderPageInput
{
    /// <summary>页码。</summary>
    public int Page { get; set; } = 1;

    /// <summary>页大小。</summary>
    public int PageSize { get; set; } = 20;

    /// <summary>订单号。</summary>
    public int? OrderID { get; set; }

    /// <summary>网关单号 Contains。</summary>
    public string SystemID { get; set; }

    /// <summary>用户名。</summary>
    public string User { get; set; }

    /// <summary>上级代理用户名。</summary>
    public string Agent { get; set; }

    /// <summary>状态；空=全部。</summary>
    public int? Status { get; set; }

    /// <summary>创建起。</summary>
    public DateTime? StartAt { get; set; }

    /// <summary>创建止。</summary>
    public DateTime? EndAt { get; set; }

    /// <summary>时间字段：CreateAt（默认）/ PayAt。</summary>
    public string DateType { get; set; }

    /// <summary>渠道；空=全部。</summary>
    public int? ChannelID { get; set; }

    /// <summary>币种；空=全部。</summary>
    public int? Currency { get; set; }

    /// <summary>出款源币种（FromCurrency）；空=全部。</summary>
    public int? FromCurrency { get; set; }

    /// <summary>金额下限。</summary>
    public decimal? MoneyFrom { get; set; }

    /// <summary>金额上限。</summary>
    public decimal? MoneyTo { get; set; }

    /// <summary>审核时长下限（分钟，CreateAt→AuditAt）。</summary>
    public int? StartAudit { get; set; }

    /// <summary>审核时长上限（分钟）。</summary>
    public int? EndAudit { get; set; }

    /// <summary>出款时长下限（分钟，AuditAt|CreateAt→PayAt）。</summary>
    public int? StartCash { get; set; }

    /// <summary>出款时长上限（分钟）。</summary>
    public int? EndCash { get; set; }
}

/// <summary>
/// 提现订单行。
/// </summary>
public class WithdrawOrderListItemDto
{
    /// <summary>订单号。</summary>
    public int OrderID { get; set; }

    /// <summary>会员。</summary>
    public int UserID { get; set; }

    /// <summary>用户名。</summary>
    public string UserName { get; set; }

    /// <summary>金额。</summary>
    public decimal Money { get; set; }

    /// <summary>实付。</summary>
    public decimal Paid { get; set; }

    /// <summary>手续费。</summary>
    public decimal Fee { get; set; }

    /// <summary>行政费。</summary>
    public decimal Cost { get; set; }

    /// <summary>状态。</summary>
    public int Status { get; set; }

    /// <summary>状态名（None/Wait/Finish/Faild/InPayment）。</summary>
    public string StatusValue { get; set; }

    /// <summary>渠道。</summary>
    public int ChannelID { get; set; }

    /// <summary>付款渠道名。</summary>
    public string Channel { get; set; }

    /// <summary>创建时间。</summary>
    public DateTime CreateAt { get; set; }

    /// <summary>到账时间。</summary>
    public DateTime PayAt { get; set; }

    /// <summary>币种。</summary>
    public int Currency { get; set; }

    /// <summary>币种代码。</summary>
    public string CurrencyCode { get; set; }

    /// <summary>户名（脱敏后仍整串返回；展示层自行掩码）。</summary>
    public string AccountName { get; set; }

    /// <summary>账号尾号提示用完整字段；日志勿明文落。</summary>
    public string AccountNumber { get; set; }

    /// <summary>提款信息展示（户名+账号）。</summary>
    public string BankInfo { get; set; }

    /// <summary>上级用户名。</summary>
    public string AgentName { get; set; }

    /// <summary>所在分组名。</summary>
    public string Group { get; set; }

    /// <summary>审核人。</summary>
    public int AdminID { get; set; }

    /// <summary>审核人显示名。</summary>
    public string Admin { get; set; }

    /// <summary>出款人。</summary>
    public int CashierID { get; set; }

    /// <summary>出款人显示名。</summary>
    public string Cashier { get; set; }

    /// <summary>第三方单号。</summary>
    public string SystemID { get; set; }

    /// <summary>审核时间。</summary>
    public DateTime AuditAt { get; set; }

    /// <summary>审核时长 H:mm:ss。</summary>
    public string AuditTime { get; set; }

    /// <summary>出款时长 H:mm:ss。</summary>
    public string DealTime { get; set; }

    /// <summary>前端备注。</summary>
    public string Description { get; set; }

    /// <summary>后端备注。</summary>
    public string DescriptionA { get; set; }

    /// <summary>实际到帐展示。</summary>
    public string RealPaid { get; set; }

    /// <summary>内部标注。</summary>
    public string Mark { get; set; }
}

/// <summary>
/// 提现订单分页。
/// </summary>
public sealed class WithdrawOrderPageOutput
{
    /// <summary>总条数。</summary>
    public int Total { get; set; }

    /// <summary>行。</summary>
    public List<WithdrawOrderListItemDto> Items { get; set; } = [];
}

/// <summary>
/// 提现订单详情（精简）。对齐旧 orderinfo 主字段。
/// </summary>
public sealed class WithdrawOrderInfoDto : WithdrawOrderListItemDto
{
    /// <summary>系统代付编号（渠道 SysID）。</summary>
    public int SysID { get; set; }

    /// <summary>开户行。</summary>
    public int Bank { get; set; }
}

/// <summary>
/// 提现订单操作日志行。对齐旧 orderlog。
/// </summary>
public sealed class WithdrawOrderLogItemDto
{
    /// <summary>日志号。</summary>
    public int ID { get; set; }

    /// <summary>订单号。</summary>
    public int OrderID { get; set; }

    /// <summary>操作后状态。</summary>
    public int Status { get; set; }

    /// <summary>操作时间。</summary>
    public DateTime CreateAt { get; set; }

    /// <summary>操作人。</summary>
    public int AdminID { get; set; }
}
