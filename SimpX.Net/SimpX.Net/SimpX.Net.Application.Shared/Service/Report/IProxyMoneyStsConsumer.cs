namespace SimpX.Net.Application.Shared;

/// <summary>
/// 消费 PROXY_MONEY_STS：代理上下分日统计。对齐旧 ProxyReportAgent.SaveProxyMoneySts。
/// </summary>
public interface IProxyMoneyStsConsumer
{
    /// <summary>
    /// 解析队列 JSON 并调 rpt_ProxyStatistics_UpdateMoney。
    /// </summary>
    /// <param name="payloadJson">队列载荷</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task ConsumeAsync(string payloadJson, CancellationToken cancellationToken = default);
}
