namespace SimpX.Net.Application.Shared;

/// <summary>
/// 支付渠道抽象。接入一家支付渠道就新增一个实现类并注册，业务调用方不用改。
/// </summary>
public interface IPaymentChannel
{
    /// <summary>渠道编码，全局唯一，下单时用它选择渠道。</summary>
    string ChannelCode { get; }

    /// <summary>
    /// 向渠道下单，返回前端调起支付要用的参数。
    /// </summary>
    /// <param name="order">本地支付单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>调起支付的参数</returns>
    Task<Dictionary<string, string>> CreateAsync(PaymentOrder order, CancellationToken cancellationToken = default);

    /// <summary>
    /// 解析并校验渠道回调，校验不通过时抛业务异常。
    /// </summary>
    /// <param name="rawBody">原始回调正文</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>回调解析结果</returns>
    Task<PaymentNotifyContext> ParseNotifyAsync(string rawBody, CancellationToken cancellationToken = default);
}
