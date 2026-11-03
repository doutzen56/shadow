using System.Globalization;
using Elastic.Transport;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SimpX.Net.Data.Entity.Users;
using SimpX.Net.ElasticSearch;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 自助反水 V2 Job1：对齐旧 RebateOrderV2SyncJob / SyncRebateOrderV2FromGameLog 的骨架。
/// 只做水位推进与 ES 可用性检查，不入账、不算赔付。
/// </summary>
public interface IRebateOrderV2SyncService
{
    /// <summary>
    /// 在运行时总开关打开时，尝试从 ES GameLog 同步到 RebateOrderV2；
    /// 缺 ES / 缺水位表时记录跳过并尽量推进水位说明。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>给人看的结果摘要</returns>
    Task<string> SyncAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// 默认实现。Schedule 显式注册；不做发放。
/// </summary>
public sealed class RebateOrderV2SyncService : IRebateOrderV2SyncService
{
    private readonly ISqlSugarClient _db;
    private readonly ElasticSearchClientContainer _clients;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RebateOrderV2SyncService> _logger;

    /// <summary>
    /// 注入业务库、ES 容器、配置与日志。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="clients">ES 容器（可缺）</param>
    /// <param name="configuration">配置</param>
    /// <param name="logger">日志</param>
    public RebateOrderV2SyncService(
        IBizDbAccessor biz,
        ElasticSearchClientContainer clients = null,
        IConfiguration configuration = null,
        ILogger<RebateOrderV2SyncService> logger = null)
    {
        _db = biz.Db;
        _clients = clients;
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> SyncAsync(CancellationToken cancellationToken = default)
    {
        if (!RebateV2CutoverService.IsRuntimeEnabled(_configuration))
        {
            const string gated = "AutoReturnV2RuntimeEnabled=false，跳过 RebateOrderV2 同步";
            _logger?.LogDebug("{Msg}", gated);
            return gated;
        }

        cancellationToken.ThrowIfCancellationRequested();

        DateTime? markUtc = null;
        try
        {
            var rows = await _db.Queryable<UserRebateV2SyncMark>()
                .Where(t => t.ID == 1)
                .Take(1)
                .ToListAsync(cancellationToken);
            markUtc = rows.FirstOrDefault()?.LastDownloadTime;
        }
        catch (Exception ex)
        {
            var skipTable = $"跳过同步：usr_RebateV2SyncMark 不可用（{ex.Message}）";
            _logger?.LogWarning(ex, "{Msg}", skipTable);
            return skipTable;
        }

        if (_clients == null || !_clients.TryGet(EsClientTypeEnum.Business, out var client) || client == null)
        {
            var skipEs = markUtc.HasValue
                ? $"跳过同步：业务 ES 未配置；当前水位 LastDownloadTime={FormatMark(markUtc.Value)}"
                : "跳过同步：业务 ES 未配置且无水位行";
            _logger?.LogInformation("{Msg}", skipEs);
            return skipEs;
        }

        // 轻量探测：gamelogv2* 与 rebateorderv2 当月索引是否可达；完整注单→返水计算尚未迁入，禁止编造入账。
        var monthIndex = EsIndexMaintenanceService.GetMonthlyIndexName(
            EsIndexMaintenanceService.RebateOrderAlias,
            DateTime.Now);
        var gamePattern = EsIndexMaintenanceService.GameLogAlias + "*";
        var gameOk = await IndexHeadOkAsync(client, gamePattern, cancellationToken);
        var rebateOk = await IndexHeadOkAsync(client, monthIndex, cancellationToken);

        if (!gameOk)
        {
            var skipGame = $"跳过同步：GameLog 索引不可用 pattern={gamePattern}；水位={FormatMark(markUtc)}";
            _logger?.LogInformation("{Msg}", skipGame);
            return skipGame;
        }

        if (!rebateOk)
        {
            var skipRebate =
                $"部分跳过：rebateorderv2 当月索引不可用 index={monthIndex}；水位={FormatMark(markUtc)}（不入账）";
            _logger?.LogInformation("{Msg}", skipRebate);
            return skipRebate;
        }

        // 实体/计算链路未完整迁入：只记计划与水位，不写返水明细、不入账。
        var msg =
            $"RebateOrderV2 同步占位完成（未入账）：GameLog 可达，rebate 月索引存在；水位={FormatMark(markUtc)}";
        _logger?.LogInformation("{Msg}", msg);
        return msg;
    }

    private static async Task<bool> IndexHeadOkAsync(
        Elastic.Clients.Elasticsearch.ElasticsearchClient client,
        string indexOrPattern,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await client.Transport.RequestAsync<StringResponse>(
                Elastic.Transport.HttpMethod.HEAD,
                indexOrPattern,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            var code = response?.ApiCallDetails?.HttpStatusCode ?? 0;
            return code is >= 200 and < 300;
        }
        catch
        {
            return false;
        }
    }

    private static string FormatMark(DateTime? mark) =>
        mark.HasValue
            ? mark.Value.ToString("o", CultureInfo.InvariantCulture)
            : "无";
}
