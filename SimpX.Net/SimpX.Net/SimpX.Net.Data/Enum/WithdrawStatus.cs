namespace SimpX.Net.Data;

/// <summary>
/// 提现订单状态。对齐旧 WithdrawOrder.WithdrawStatus（含历史拼写 Faild）。
/// </summary>
public enum WithdrawStatus
{
    /// <summary>待审核。</summary>
    None = 0,

    /// <summary>已审待出款。</summary>
    Wait = 1,

    /// <summary>已完成。</summary>
    Finish = 2,

    /// <summary>失败（旧站拼写 Faild）。</summary>
    Faild = 3,

    /// <summary>出款中。</summary>
    InPayment = 4,
}
