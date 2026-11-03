using System.Globalization;
using System.Text;
using System.Text.Json;
using Elastic.Transport;
using Microsoft.Extensions.Logging;
using SimpX.Net.Application.Shared;
using SimpX.Net.ElasticSearch;

namespace SimpX.Net.Application.Schedule;

/// <summary>
/// ES 复合聚合用户日结。对齐旧 SaveUserDateGameFromES / Battle（gamelogv2* / fantasylog）。
/// </summary>
public sealed class UserGameDateEsAggregate : IUserGameDateEsAggregate
{
    private const string AggName = "group_by_all";
    private const int PageSize = 5000;

    /// <summary>旧 BattleLog 索引名。</summary>
    public const string BattleIndex = "fantasylog";

    private readonly ElasticSearchClientContainer _clients;
    private readonly ILogger<UserGameDateEsAggregate> _logger;

    public UserGameDateEsAggregate(
        ElasticSearchClientContainer clients = null,
        ILogger<UserGameDateEsAggregate> logger = null)
    {
        _clients = clients;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<UserGameDateEsAggregateResult> AggregateAsync(
        DateTime startBj,
        DateTime endBj,
        IReadOnlyCollection<string> typeNames = null,
        CancellationToken cancellationToken = default) =>
        AggregateCoreAsync(
            GameOrderSummaryService.EsAdminLogIndexPattern(),
            startBj,
            endBj,
            typeNames,
            includeCategorySource: true,
            cancellationToken);

    /// <inheritdoc />
    public async Task<UserGameDateEsAggregateResult> AggregateBattleAsync(
        DateTime startBj,
        DateTime endBj,
        CancellationToken cancellationToken = default)
    {
        var result = await AggregateCoreAsync(
            BattleIndex,
            startBj,
            endBj,
            typeNames: null,
            includeCategorySource: false,
            cancellationToken);
        if (result.Buckets != null)
        {
            foreach (var b in result.Buckets)
                b.Category = GameOrderSummaryService.CategoryFantasy;
        }

        return result;
    }

    private async Task<UserGameDateEsAggregateResult> AggregateCoreAsync(
        string indexPattern,
        DateTime startBj,
        DateTime endBj,
        IReadOnlyCollection<string> typeNames,
        bool includeCategorySource,
        CancellationToken cancellationToken)
    {
        if (_clients == null || !_clients.TryGet(EsClientTypeEnum.Business, out var client) || client == null)
        {
            return new UserGameDateEsAggregateResult
            {
                NotConfigured = true,
                Message = "业务 ES 未配置"
            };
        }

        try
        {
            var all = new List<UserGameDateEsBucket>();
            JsonElement? after = null;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var body = BuildBody(startBj, endBj, after, typeNames, includeCategorySource);
                var path = $"{indexPattern}/_search";
                var response = await client.Transport.RequestAsync<StringResponse>(
                    Elastic.Transport.HttpMethod.POST,
                    path,
                    PostData.String(body),
                    cancellationToken);

                if (response == null || response.ApiCallDetails?.HttpStatusCode is < 200 or >= 300)
                {
                    _logger?.LogWarning(
                        "用户日结 ES 聚合未成功 index={Index} status={Status}",
                        indexPattern,
                        response?.ApiCallDetails?.HttpStatusCode);
                    return new UserGameDateEsAggregateResult
                    {
                        QueryFailed = true,
                        Message = "#用户日结聚合失败#"
                    };
                }

                using var doc = JsonDocument.Parse(response.Body ?? "{}");
                if (!doc.RootElement.TryGetProperty("aggregations", out var aggs)
                    || !aggs.TryGetProperty(AggName, out var composite))
                    break;

                if (!composite.TryGetProperty("buckets", out var buckets)
                    || buckets.ValueKind != JsonValueKind.Array
                    || buckets.GetArrayLength() == 0)
                    break;

                var count = 0;
                foreach (var bucket in buckets.EnumerateArray())
                {
                    all.Add(MapBucket(bucket, includeCategorySource));
                    count++;
                }

                if (count < PageSize
                    || !composite.TryGetProperty("after_key", out var afterKey)
                    || afterKey.ValueKind != JsonValueKind.Object)
                    break;

                after = afterKey.Clone();
            }

            return new UserGameDateEsAggregateResult { Buckets = all };
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "用户日结 ES 聚合异常 {Kind}", ex.GetType().Name);
            return new UserGameDateEsAggregateResult
            {
                QueryFailed = true,
                Message = "#用户日结聚合异常#"
            };
        }
    }

    private static string BuildBody(
        DateTime startBj,
        DateTime endBj,
        JsonElement? after,
        IReadOnlyCollection<string> typeNames,
        bool includeCategorySource)
    {
        var gte = startBj.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
        var lt = endBj.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
        var filters = new StringBuilder();
        filters.Append("{\"range\":{\"SettlementAt\":{\"gte\":\"")
            .Append(gte).Append("\",\"lt\":\"").Append(lt).Append("\"}}}");
        if (typeNames != null && typeNames.Count > 0)
        {
            filters.Append(",{\"terms\":{\"Type\":[");
            var first = true;
            foreach (var name in typeNames)
            {
                if (string.IsNullOrWhiteSpace(name))
                    continue;
                if (!first) filters.Append(',');
                first = false;
                filters.Append('"').Append(EscapeJson(name.Trim())).Append('"');
            }

            filters.Append("]}}");
        }

        var afterJson = after.HasValue ? $",\"after\":{after.Value.GetRawText()}" : string.Empty;
        var sources = includeCategorySource
            ? "{\"user_id\":{\"terms\":{\"field\":\"UserID\"}}},"
              + "{\"site_id\":{\"terms\":{\"field\":\"SiteID\"}}},"
              + "{\"category\":{\"terms\":{\"field\":\"Category\"}}},"
              + "{\"type\":{\"terms\":{\"field\":\"Type\"}}},"
              + "{\"currency\":{\"terms\":{\"field\":\"Currency\"}}}"
            : "{\"user_id\":{\"terms\":{\"field\":\"UserID\"}}},"
              + "{\"site_id\":{\"terms\":{\"field\":\"SiteID\"}}},"
              + "{\"type\":{\"terms\":{\"field\":\"Type\"}}},"
              + "{\"currency\":{\"terms\":{\"field\":\"Currency\"}}}";

        return
            "{\"size\":0,\"query\":{\"bool\":{\"filter\":[" + filters + "]}}"
            + ",\"aggs\":{\"" + AggName + "\":{\"composite\":{\"size\":" + PageSize
            + ",\"sources\":[" + sources + "]" + afterJson
            + "},\"aggs\":{"
            + "\"total_bet_money\":{\"sum\":{\"field\":\"BetMoney\"}},"
            + "\"total_bet_amount\":{\"sum\":{\"field\":\"BetAmount\"}},"
            + "\"total_money\":{\"sum\":{\"field\":\"Money\"}},"
            + "\"total_jackpot\":{\"sum\":{\"field\":\"JackPot\"}},"
            + "\"total_jackpot_count\":{\"sum\":{\"field\":\"JackPotCount\"}}"
            + "}}}}";
    }

    private static string EscapeJson(string s) =>
        s.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);

    private static UserGameDateEsBucket MapBucket(JsonElement bucket, bool hasCategory)
    {
        var key = bucket.GetProperty("key");
        return new UserGameDateEsBucket
        {
            UserID = ReadInt(key, "user_id"),
            SiteID = ReadInt(key, "site_id"),
            Category = hasCategory ? ReadCategory(key, "category") : GameOrderSummaryService.CategoryFantasy,
            Type = ReadType(key, "type"),
            Currency = ReadInt(key, "currency"),
            BetMoney = ReadSum(bucket, "total_bet_money"),
            BetAmount = ReadSum(bucket, "total_bet_amount"),
            Money = ReadSum(bucket, "total_money"),
            JackPot = ReadSum(bucket, "total_jackpot"),
            JackPotCount = ReadSum(bucket, "total_jackpot_count"),
            OrderCount = bucket.TryGetProperty("doc_count", out var dc) ? dc.GetInt32() : 0
        };
    }

    private static decimal ReadSum(JsonElement bucket, string name)
    {
        if (!bucket.TryGetProperty(name, out var node))
            return 0m;
        if (node.TryGetProperty("value", out var value) && value.ValueKind == JsonValueKind.Number)
            return value.GetDecimal();
        return 0m;
    }

    private static int ReadInt(JsonElement key, string name)
    {
        if (!key.TryGetProperty(name, out var node))
            return 0;
        return ParseInt(node);
    }

    private static int ReadCategory(JsonElement key, string name)
    {
        if (!key.TryGetProperty(name, out var node))
            return 0;
        var text = NodeText(node);
        if (GameCategoryNameMap.TryParse(text, out var cat) && cat >= 0)
            return cat;
        return ParseInt(node);
    }

    private static int ReadType(JsonElement key, string name)
    {
        if (!key.TryGetProperty(name, out var node))
            return 0;
        var text = NodeText(node);
        if (GameTypeNameMap.TryParse(text, out var type) && type >= 0)
            return type;
        if (GameTypeMeta.TryParse(text, out type) && type >= 0)
            return type;
        return ParseInt(node);
    }

    private static string NodeText(JsonElement node) =>
        node.ValueKind switch
        {
            JsonValueKind.String => node.GetString() ?? string.Empty,
            JsonValueKind.Number => node.GetRawText(),
            JsonValueKind.True => "1",
            JsonValueKind.False => "0",
            _ => node.ToString()
        };

    private static int ParseInt(JsonElement node)
    {
        if (node.ValueKind == JsonValueKind.Number && node.TryGetInt32(out var n))
            return n;
        var text = NodeText(node);
        if (int.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out n))
            return n;
        if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
            return (int)d;
        return 0;
    }
}
