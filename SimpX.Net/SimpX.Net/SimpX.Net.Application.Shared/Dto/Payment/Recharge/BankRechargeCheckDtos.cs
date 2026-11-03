namespace SimpX.Net.Application.Shared;

/// <summary>
/// 入款审核列表筛选。对齐旧 recharge/checklist。
/// </summary>
public sealed class BankRechargeCheckListInput
{
    /// <summary>状态；空=全部。</summary>
    public string Status { get; set; }

    /// <summary>用户名。</summary>
    public string User { get; set; }

    /// <summary>上级代理用户名（直属会员 AgentID）。</summary>
    public string Agent { get; set; }

    /// <summary>订单号。</summary>
    public string OrderID { get; set; }

    /// <summary>通道。</summary>
    public int PayID { get; set; }

    /// <summary>币种；0/null=全部。</summary>
    public int? Currency { get; set; }

    /// <summary>创建起。</summary>
    public DateTime? StartAt { get; set; }

    /// <summary>创建止。</summary>
    public DateTime? EndAt { get; set; }
}

/// <summary>
/// 入款审核行。
/// </summary>
public sealed class BankRechargeCheckListItemDto
{
    /// <summary>编号。</summary>
    public int ID { get; set; }

    /// <summary>充值订单号。</summary>
    public string OrderID { get; set; }

    /// <summary>通道。</summary>
    public int PayID { get; set; }

    /// <summary>会员。</summary>
    public int UserID { get; set; }

    /// <summary>用户名。</summary>
    public string UserName { get; set; }

    /// <summary>提交金额。</summary>
    public decimal Money { get; set; }

    /// <summary>实付。</summary>
    public decimal Paid { get; set; }

    /// <summary>状态。</summary>
    public int Status { get; set; }

    /// <summary>币种。</summary>
    public int Currency { get; set; }

    /// <summary>创建时间。</summary>
    public DateTime CreateAt { get; set; }

    /// <summary>审核时间。</summary>
    public DateTime CheckAt { get; set; }

    /// <summary>转账名。</summary>
    public string Name { get; set; }

    /// <summary>备注。</summary>
    public string Description { get; set; }

    /// <summary>通道名（对齐旧 PayName）。</summary>
    public string PayName { get; set; }

    /// <summary>审核管理员名（对齐旧 Admin）。</summary>
    public string AdminName { get; set; }

    /// <summary>支付单号（对齐旧 PayOrderID）。</summary>
    public string PayOrderID { get; set; }

    /// <summary>支付备注（对齐旧 PayRemark；库无独立列时为空）。</summary>
    public string PayRemark { get; set; }

    /// <summary>提现次数（对齐旧 withdrawCount）。</summary>
    public int WithdrawCount { get; set; }
}

/// <summary>
/// 入款审核分页（对齐旧 ShowResult）。
/// </summary>
public sealed class BankRechargeCheckPageOutput
{
    /// <summary>总条数。</summary>
    public int RecordCount { get; set; }

    /// <summary>当前页。</summary>
    public List<BankRechargeCheckListItemDto> Items { get; set; }
}

/// <summary>
/// 入款审核操作。
/// </summary>
public sealed class BankRechargeCheckInput
{
    /// <summary>编号。</summary>
    public int ID { get; set; }

    /// <summary>1 拒绝 / 2 通过。</summary>
    public int Status { get; set; }

    /// <summary>实付（通过时；0 则用订单 Money）。</summary>
    public decimal Paid { get; set; }

    /// <summary>网关单号。</summary>
    public string SystemID { get; set; }

    /// <summary>前端备注。</summary>
    public string Description { get; set; }

    /// <summary>后端备注。</summary>
    public string DescriptionA { get; set; }
}
