using Microsoft.Extensions.Logging;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 布隆过滤器维护：对齐旧 FilterClearJob → FilterCaching.RefreshAllFiltersStatsAsync。
/// SimpX 尚无布隆实现时占位空跑。
/// </summary>
public interface IBloomFilterMaintenanceService
{
    /// <summary>
    /// 刷新过滤器统计；无实现时写占位日志并返回说明。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>给人看的结果摘要</returns>
    Task<string> RefreshAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// 默认实现。Schedule 显式注册。
/// </summary>
public sealed class BloomFilterMaintenanceService : IBloomFilterMaintenanceService
{
    /// <summary>占位文案，对齐任务说明。</summary>
    public const string PlaceholderMessage = "无布隆过滤器实现，空跑对齐占位";

    private readonly ILogger<BloomFilterMaintenanceService> _logger;

    /// <summary>
    /// 注入日志。
    /// </summary>
    /// <param name="logger">日志</param>
    public BloomFilterMaintenanceService(ILogger<BloomFilterMaintenanceService> logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<string> RefreshAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _logger?.LogInformation("{Msg}", PlaceholderMessage);
        return Task.FromResult(PlaceholderMessage);
    }
}
