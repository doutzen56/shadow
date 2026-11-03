namespace SimpX.Net.Application.Shared;

/// <summary>
/// 创建支付单请求。业务方只传金额、标题、业务单号，不关心渠道细节。
/// </summary>
public class PaymentCreateRequest
{
    /// <summary>业务侧订单号，用于幂等和对账。</summary>
    public string BusinessOrderNo { get; set; }

    /// <summary>支付金额，单位元。</summary>
    public decimal Amount { get; set; }

    /// <summary>商品或账单标题。</summary>
    public string Subject { get; set; }

    /// <summary>渠道编码；不传时用演示渠道 demo。</summary>
    public string ChannelCode { get; set; } = "demo";

    /// <summary>附加数据，回调时原样带回；也可直接传站/会员字段，服务端会写入 Attach。</summary>
    public string Attach { get; set; }

    /// <summary>站点（回调入账用；与 Attach JSON 二选一）。</summary>
    public int SiteId { get; set; }

    /// <summary>会员（回调入账用）。</summary>
    public int UserId { get; set; }

    /// <summary>币种；默认 1。</summary>
    public int Currency { get; set; } = 1;

    /// <summary>入账资金类型；默认 1=充值。</summary>
    public int MoneyType { get; set; } = 1;
}

/// <summary>
/// 创建支付单结果。
/// </summary>
public class PaymentCreateResult
{
    /// <summary>支付中心生成的支付单号。</summary>
    public string PaymentOrderNo { get; set; }

    /// <summary>渠道编码。</summary>
    public string ChannelCode { get; set; }

    /// <summary>当前状态。</summary>
    public PaymentStatus Status { get; set; }

    /// <summary>调起支付所需参数。</summary>
    public Dictionary<string, string> PayParams { get; set; } = new();
}

/// <summary>
/// 渠道回调上下文。
/// </summary>
public class PaymentNotifyContext
{
    /// <summary>渠道编码。</summary>
    public string ChannelCode { get; set; }

    /// <summary>支付中心单号。</summary>
    public string PaymentOrderNo { get; set; }

    /// <summary>渠道侧交易号。</summary>
    public string ChannelTradeNo { get; set; }

    /// <summary>解析后的目标状态。</summary>
    public PaymentStatus Status { get; set; }

    /// <summary>原始回调正文，便于排障，勿写密钥。</summary>
    public string RawBody { get; set; }
}
