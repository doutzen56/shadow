namespace SimpX.Net.Data;

/// <summary>
/// 充提/资金审核状态。对齐旧站 CheckStatus（None/Failed/Success/Processing）。
/// </summary>
public enum MoneyCheckStatus
{
    /// <summary>未处理。</summary>
    None = 0,

    /// <summary>失败（旧拼写 Faild 解析时映射到此）。</summary>
    Failed = 1,

    /// <summary>成功。</summary>
    Success = 2,

    /// <summary>处理中。</summary>
    Processing = 3,
}
