namespace SimpX.Net.Data;

/// <summary>
/// 支付单状态。渠道实现只回写这些通用状态，不绑定具体支付厂商。
/// </summary>
public enum PaymentStatus
{
    /// <summary>已创建，等待用户付款。</summary>
    Created = 0,

    /// <summary>支付成功。</summary>
    Paid = 1,

    /// <summary>已关闭或取消。</summary>
    Closed = 2,

    /// <summary>已退款（全额或部分，细节由渠道扩展字段说明）。</summary>
    Refunded = 3,

    /// <summary>支付失败。</summary>
    Failed = 4
}
