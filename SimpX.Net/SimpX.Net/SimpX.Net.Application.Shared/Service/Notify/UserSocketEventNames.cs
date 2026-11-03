namespace SimpX.Net.Application.Shared;

/// <summary>
/// 会员端即时事件名。对齐旧 <c>SocketType</c> 枚举 ToString（载荷里 Event 字段）。
/// 通道改 SignalR，内容字段保持一致。
/// </summary>
public static class UserSocketEventNames
{
    /// <summary>KYC 审核。</summary>
    public const string KycMessage = "KycMessage";

    /// <summary>银行卡审核。</summary>
    public const string BankMessage = "BankMessage";

    /// <summary>存款到账。</summary>
    public const string DepositMessage = "DepositMessage";

    /// <summary>取款结果。</summary>
    public const string WithdrawMessage = "WithdrawMessage";

    /// <summary>活动到账。</summary>
    public const string ActivityMessage = "ActivityMessage";

    /// <summary>首存。</summary>
    public const string FirstRecharge = "FirstRecharge";

    /// <summary>任务完成。</summary>
    public const string TaskAchievedMessage = "TaskAchievedMessage";
}
