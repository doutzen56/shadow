using System.Globalization;
using System.Text;
using System.Text.Json;
using Elastic.Transport;
using SimpX.Net.Application.Shared;
using SimpX.Net.ElasticSearch;

namespace SimpX.Net.Application.Schedule;

/// <summary>
/// 从 gamelogv2* 按 Money 取站点 top-N。对齐旧 JackpotAgent.GetJackpotListByES 的「有数据」判定。
/// </summary>
public sealed class JackpotEsTopQuery : IJackpotEsTopQuery
{
    private readonly ElasticSearchClientContainer _clients;
    private readonly ILogger<JackpotEsTopQuery> _logger;

    /// <summary>
    /// 注入业务 ES 容器；未注册时可空。
    /// </summary>
    /// <param name="clients">客户端容器</param>
    /// <param name="logger">日志</param>
    public JackpotEsTopQuery(
        ElasticSearchClientContainer clients = null,
        ILogger<JackpotEsTopQuery> logger = null)
    {
        _clients = clients;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<JackpotEsTopResult> QueryTopAsync(
        int siteId,
        int hours,
        int topRank,
        CancellationToken cancellationToken = default)
    {
        if (_clients == null || !_clients.TryGet(EsClientTypeEnum.Business, out var client) || client == null)
        {
            return new JackpotEsTopResult
            {
                NotConfigured = true,
                Message = "业务 ES 未配置"
            };
        }

        hours = Math.Clamp(hours <= 0 ? 1 : hours, 1, 24 * 30);
        topRank = Math.Clamp(topRank <= 0 ? 5 : topRank, 1, 50);
        var end = DateTime.Now;
        var start = end.AddHours(-hours);
        var index = GameOrderSummaryService.EsAdminLogIndexPattern();

        try
        {
            var body = BuildBody(siteId, start, end, topRank);
            var path = $"{index}/_search";
            var response = await client.Transport.RequestAsync<StringResponse>(
                Elastic.Transport.HttpMethod.POST,
                path,
                PostData.String(body),
                cancellationToken);

            if (response == null || response.ApiCallDetails?.HttpStatusCode is < 200 or >= 300)
            {
                _logger?.LogWarning(
                    "爆奖 ES top 查询未成功 site={SiteId} status={Status}",
                    siteId,
                    response?.ApiCallDetails?.HttpStatusCode);
                return new JackpotEsTopResult
                {
                    QueryFailed = true,
                    Message = "爆奖 ES 查询失败，需要游戏拉取"
                };
            }

            using var doc = JsonDocument.Parse(response.Body ?? "{}");
            var hits = 0;
            if (doc.RootElement.TryGetProperty("hits", out var hitsNode)
                && hitsNode.TryGetProperty("hits", out var arr)
                && arr.ValueKind == JsonValueKind.Array)
            {
                hits = arr.GetArrayLength();
            }

            return new JackpotEsTopResult
            {
                HitCount = hits,
                Message = hits > 0 ? $"命中 {hits}" : "无命中，需要游戏拉取 / 注单进 ES"
            };
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "爆奖 ES top 查询异常 site={SiteId}", siteId);
            return new JackpotEsTopResult
            {
                QueryFailed = true,
                Message = "爆奖 ES 查询异常，需要游戏拉取"
            };
        }
    }

    /// <summary>构造按 SettlementAt / Money 的检索体。</summary>
    private static string BuildBody(int siteId, DateTime start, DateTime end, int size)
    {
        var sb = new StringBuilder(512);
        sb.Append('{');
        sb.Append("\"size\":").Append(size).Append(',');
        sb.Append("\"sort\":[{\"Money\":{\"order\":\"desc\"}}],");
        sb.Append("\"query\":{\"bool\":{\"filter\":[");
        sb.Append("{\"term\":{\"SiteID\":").Append(siteId).Append("}},");
        sb.Append("{\"range\":{\"SettlementAt\":{");
        sb.Append("\"gte\":\"").Append(start.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)).Append("\",");
        sb.Append("\"lt\":\"").Append(end.AddSeconds(1).ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)).Append("\"");
        sb.Append("}}}]}}");
        sb.Append('}');
        return sb.ToString();
    }
}
