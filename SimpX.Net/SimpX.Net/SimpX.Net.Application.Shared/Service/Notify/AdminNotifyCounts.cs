namespace SimpX.Net.Application.Shared;

/// <summary>
/// 后台待办角标。字段名对齐旧 <c>AdminNotify</c> / <c>/admin/site/notify/get</c>。
/// </summary>
public sealed class AdminNotifyCounts
{
    /// <summary>待审核提现。</summary>
    public int Withdraw { get; set; }

    /// <summary>待出款提现。</summary>
    public int WithdrawExecutor { get; set; }

    /// <summary>入款审核。</summary>
    public int Transfer { get; set; }

    /// <summary>活动发放。</summary>
    public int Event { get; set; }

    /// <summary>佣金。</summary>
    public int Commission { get; set; }

    /// <summary>返水。</summary>
    public int Rebate { get; set; }

    /// <summary>代理申请。</summary>
    public int AgentApply { get; set; }

    /// <summary>加减款审核。</summary>
    public int MoneyReview { get; set; }

    /// <summary>兑币审核。</summary>
    public int Exchange { get; set; }

    /// <summary>证件审核。</summary>
    public int Kyc { get; set; }

    /// <summary>银行审核。</summary>
    public int Bank { get; set; }

    /// <summary>当日风险会员。</summary>
    public int Risk { get; set; }

    /// <summary>合计。</summary>
    public int Total =>
        Withdraw + WithdrawExecutor + Transfer + Event + Commission + Rebate
        + AgentApply + MoneyReview + Exchange + Kyc + Bank + Risk;
}
