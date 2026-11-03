using System.Globalization;
using Elastic.Transport;
using Microsoft.Extensions.Logging;
using SimpX.Net.ElasticSearch;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// ES 月度索引维护：对齐旧 EsIndexManager 月滚动的轻量版。
/// 只检查 gamelogv2 / rebateorderv2 当月索引是否存在；不做完整 ILM。
/// </summary>
public interface IEsIndexMaintenanceService
{
    /// <summary>
    /// 记录本轮计划，并对业务 ES 做索引存在检查；ES 未配置时空跑。
    /// </summary>
    /// <param name="now">业务本地时钟；空则取当前</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>给人看的结果摘要</returns>
    Task<string> RunAsync(DateTime? now = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// 默认实现。Schedule 显式注册；容器可缺。
/// </summary>
public sealed class EsIndexMaintenanceService : IEsIndexMaintenanceService
{
    /// <summary>旧 GameLogV2 写别名根。</summary>
    public const string GameLogAlias = "gamelogv2";

    /// <summary>旧 RebateOrderV2 写别名根。</summary>
    public const string RebateOrderAlias = "rebateorderv2";

    private static readonly string[] AliasRoots = [GameLogAlias, RebateOrderAlias];

    private readonly ElasticSearchClientContainer _clients;
    private readonly ILogger<EsIndexMaintenanceService> _logger;

    /// <summary>
    /// 注入业务 ES 容器与日志；容器可缺（未配置 ES 时空跑）。
    /// </summary>
    /// <param name="clients">ES 客户端容器</param>
    /// <param name="logger">日志</param>
    public EsIndexMaintenanceService(
        ElasticSearchClientContainer clients = null,
        ILogger<EsIndexMaintenanceService> logger = null)
    {
        _clients = clients;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> RunAsync(DateTime? now = null, CancellationToken cancellationToken = default)
    {
        var clock = now ?? DateTime.Now;
        var monthLabel = clock.ToString("yyyyMM", CultureInfo.InvariantCulture);
        var plan = string.Join(", ", AliasRoots.Select(a => GetMonthlyIndexName(a, clock)));

        if (_clients == null || !_clients.TryGet(EsClientTypeEnum.Business, out var client) || client == null)
        {
            var skip = $"ES 月度维护空跑（业务 ES 未配置）计划={plan}";
            _logger?.LogInformation("{Msg}", skip);
            return skip;
        }

        var parts = new List<string>(AliasRoots.Length);
        foreach (var alias in AliasRoots)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var indexName = GetMonthlyIndexName(alias, clock);
            try
            {
                var path = indexName;
                var response = await client.Transport.RequestAsync<StringResponse>(
                    Elastic.Transport.HttpMethod.HEAD,
                    path,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                var exists = response != null
                    && response.ApiCallDetails?.HttpStatusCode is >= 200 and < 300;
                var line = exists
                    ? $"月份索引已存在: {indexName}"
                    : $"月份索引不存在（本轮仅检查，不自动建）: {indexName}";
                parts.Add(line);
                _logger?.LogInformation("{Line}", line);
            }
            catch (Exception ex)
            {
                var line = $"月份索引检查失败: {indexName} ({ex.Message})";
                parts.Add(line);
                _logger?.LogWarning(ex, "ES 月度索引检查失败 index={Index}", indexName);
            }
        }

        var msg = $"ES 月度维护完成 month={monthLabel}; {string.Join("; ", parts)}";
        _logger?.LogInformation("{Msg}", msg);
        return msg;
    }

    /// <summary>
    /// 月索引名：{alias}-{yyyyMM}-000001，对齐旧 EsIndexManager.GetMonthlyIndexName 的当月形态。
    /// </summary>
    /// <param name="aliasName">别名根</param>
    /// <param name="time">时间</param>
    /// <returns>物理索引名</returns>
    public static string GetMonthlyIndexName(string aliasName, DateTime time) =>
        $"{aliasName}-{time:yyyyMM}-000001";
}
