using System.Text.Json;
using System.Text.Json.Serialization;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Search;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Microsoft.Extensions.Logging;
using SimpX.Net.Application.Shared;
using SimpX.Net.ElasticSearch;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 用业务检索客户端查游戏注单。共享层不引用客户端，所以实现放在租户后台。
/// </summary>
public sealed class GameOrderEsQuery : IGameOrderEsQuery
{
    private readonly ElasticSearchClientContainer _clients;
    private readonly ILogger<GameOrderEsQuery> _logger;

    /// <summary>
    /// 注入业务客户端容器。容器没注册时两个参数都可以空，查询会说明没配上。
    /// </summary>
    /// <param name="clients">客户端容器；没有业务客户端时也可空</param>
    /// <param name="logger">日志；可空</param>
    public GameOrderEsQuery(ElasticSearchClientContainer clients = null, ILogger<GameOrderEsQuery> logger = null)
    {
        _clients = clients;
        _logger = logger;
    }

    /// <summary>
    /// 按站点和分类分页查注单。
    /// </summary>
    /// <param name="siteId">站点</param>
    /// <param name="category">分类；-1 为全部</param>
    /// <param name="page">页码，从 1 开始</param>
    /// <param name="size">每页条数</param>
    /// <param name="userId">会员；小于等于 0 表示不限</param>
    /// <param name="start">开始时间，含这一刻</param>
    /// <param name="end">结束时间，不含这一刻</param>
    /// <param name="sourceId">第三方单号；空表示不限</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页</returns>
    public async Task<GameOrderEsPage> SearchAsync(
        int siteId,
        int category,
        int page,
        int size,
        int userId,
        DateTime? start,
        DateTime? end,
        string sourceId,
        CancellationToken cancellationToken = default,
        string currencyCode = null,
        string typeName = null,
        string playerName = null)
    {
        var client = BusinessClient();
        if (client == null)
            return NotConfiguredPage();

        page = Math.Max(1, page);
        size = Math.Clamp(size <= 0 ? 20 : size, 1, 100);
        var from = (page - 1) * size;
        // 默认窗口最多翻到一万条，再深会直接被集群拒绝。
        if (from >= 10000)
        {
            return new GameOrderEsPage
            {
                QueryFailed = true,
                Message = "#翻页太深，请缩小时间或加上会员、单号#"
            };
        }

        try
        {
            var response = await client.SearchAsync<GameOrderEsDocument>(
                BuildRequest(
                    GameOrderSummaryService.EsAdminLogIndexPattern(),
                    siteId,
                    userId,
                    start,
                    end,
                    sourceId,
                    from,
                    size,
                    currencyCode,
                    typeName,
                    playerName,
                    category),
                cancellationToken);
            return ReadPage(response, category);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning("注单明细查询失败 {Kind}", ex.GetType().Name);
            return new GameOrderEsPage
            {
                QueryFailed = true,
                Message = "#注单明细查询失败#"
            };
        }
    }

    /// <inheritdoc />
    public async Task<GameOrderEsPage> SearchRewardAsync(
        int siteId,
        int page,
        int size,
        int userId,
        DateTime? start,
        DateTime? end,
        string sourceId,
        string currencyCode = null,
        string typeName = null,
        string playerName = null,
        CancellationToken cancellationToken = default)
    {
        var client = BusinessClient();
        if (client == null)
            return NotConfiguredPage();

        page = Math.Max(1, page);
        size = Math.Clamp(size <= 0 ? 20 : size, 1, 100);
        var from = (page - 1) * size;
        if (from >= 10000)
        {
            return new GameOrderEsPage
            {
                QueryFailed = true,
                Message = "#翻页太深，请缩小时间或加上会员、单号#"
            };
        }

        try
        {
            var response = await client.SearchAsync<GameOrderEsDocument>(
                BuildRequest(
                    "rewardlog*",
                    siteId,
                    userId,
                    start,
                    end,
                    sourceId,
                    from,
                    size,
                    currencyCode,
                    typeName,
                    playerName,
                    category: -1,
                    dateField: "RewardAt"),
                cancellationToken);
            return ReadPage(response, -1);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning("打赏明细查询失败 {Kind}", ex.GetType().Name);
            return new GameOrderEsPage
            {
                QueryFailed = true,
                Message = "#打赏明细查询失败#"
            };
        }
    }

    /// <summary>
    /// 按第三方单号取一条。指定分类先查对应索引，没有再查综合索引。
    /// </summary>
    /// <param name="siteId">站点</param>
    /// <param name="sourceId">第三方单号</param>
    /// <param name="categoryHint">分类；空或 -1 只查综合索引</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>命中或说明</returns>
    public async Task<GameOrderEsHit> GetBySourceIdAsync(
        int siteId,
        string sourceId,
        int? categoryHint,
        CancellationToken cancellationToken = default)
    {
        var sid = (sourceId ?? string.Empty).Trim();
        if (sid.Length == 0)
        {
            return new GameOrderEsHit
            {
                Found = false,
                Message = "#请提供注单单号#"
            };
        }

        var client = BusinessClient();
        if (client == null)
            return NotConfiguredHit();

        var category = categoryHint is int value && value >= 0 ? value : -1;
        var pattern = GameOrderSummaryService.EsIndexPattern(category);
        try
        {
            var first = await SearchOneAsync(client, pattern, siteId, sid, category, cancellationToken);
            if (first.QueryFailed || first.Found)
                return first;

            var fallback = GameOrderSummaryService.EsIndexPattern(-1);
            if (!string.Equals(pattern, fallback, StringComparison.Ordinal))
                return await SearchOneAsync(client, fallback, siteId, sid, category, cancellationToken);
            return first;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning("注单明细按单号查询失败 {Kind}", ex.GetType().Name);
            return new GameOrderEsHit
            {
                Found = false,
                QueryFailed = true,
                SourceID = sid,
                Message = "#注单明细查询失败#"
            };
        }
    }

    /// <summary>查一条。索引里没有时 Found 为 false，且不算失败。</summary>
    private async Task<GameOrderEsHit> SearchOneAsync(
        ElasticsearchClient client,
        string pattern,
        int siteId,
        string sourceId,
        int category,
        CancellationToken cancellationToken)
    {
        var response = await client.SearchAsync<GameOrderEsDocument>(
            BuildRequest(pattern, siteId, 0, null, null, sourceId, 0, 1),
            cancellationToken);
        var page = ReadPage(response, category);
        if (page.QueryFailed)
        {
            return new GameOrderEsHit
            {
                Found = false,
                QueryFailed = true,
                SourceID = sourceId,
                Message = page.Message
            };
        }

        return page.Items.FirstOrDefault() ?? new GameOrderEsHit
        {
            Found = false,
            SourceID = sourceId
        };
    }

    /// <summary>
    /// 拼查询。币种和游戏类型在集群里是字母代码，入参是数字，对不上就不当条件，避免把单全滤掉。
    /// </summary>
    private static SearchRequest BuildRequest(
        string pattern,
        int siteId,
        int userId,
        DateTime? start,
        DateTime? end,
        string sourceId,
        int from,
        int size,
        string currencyCode = null,
        string typeName = null,
        string playerName = null,
        int category = -1,
        string dateField = null)
    {
        var filters = new List<Query>
        {
            new TermQuery(new Field("SiteID"), FieldValue.Long(siteId))
        };
        if (userId > 0)
            filters.Add(new TermQuery(new Field("UserID"), FieldValue.Long(userId)));
        if (category >= 0)
        {
            var catName = GameCategoryNameMap.Format(category);
            filters.Add(new TermQuery(new Field("Category"), FieldValue.String(catName)));
        }
        if (start.HasValue || end.HasValue)
        {
            if (!string.IsNullOrWhiteSpace(dateField))
            {
                var range = new DateRangeQuery(new Field(dateField));
                if (start.HasValue) range.Gte = start.Value;
                if (end.HasValue) range.Lt = end.Value;
                filters.Add(range);
            }
            else
            {
                // 旧站多数入口按 CreateAt；棋牌另有 StartAt。两边 should，避免漏单。
                var create = new DateRangeQuery(new Field("CreateAt"));
                var startAt = new DateRangeQuery(new Field("StartAt"));
                if (start.HasValue)
                {
                    create.Gte = start.Value;
                    startAt.Gte = start.Value;
                }
                if (end.HasValue)
                {
                    create.Lt = end.Value;
                    startAt.Lt = end.Value;
                }
                filters.Add(new BoolQuery
                {
                    MinimumShouldMatch = 1,
                    Should = new List<Query> { create, startAt }
                });
            }
        }

        var sid = (sourceId ?? string.Empty).Trim();
        if (sid.Length > 0)
        {
            // 有的索引单号是关键字，有的带 keyword 子字段，两边都试，命中一边即可。
            filters.Add(new BoolQuery
            {
                MinimumShouldMatch = 1,
                Should = new List<Query>
                {
                    new TermQuery(new Field("SourceID"), FieldValue.String(sid)),
                    new TermQuery(new Field("SourceID.keyword"), FieldValue.String(sid))
                }
            });
        }

        var cur = (currencyCode ?? string.Empty).Trim();
        if (cur.Length > 0)
        {
            filters.Add(new BoolQuery
            {
                MinimumShouldMatch = 1,
                Should = new List<Query>
                {
                    new TermQuery(new Field("Currency"), FieldValue.String(cur)),
                    new TermQuery(new Field("Currency.keyword"), FieldValue.String(cur))
                }
            });
        }

        var type = (typeName ?? string.Empty).Trim();
        if (type.Length > 0)
        {
            filters.Add(new BoolQuery
            {
                MinimumShouldMatch = 1,
                Should = new List<Query>
                {
                    new TermQuery(new Field("Type"), FieldValue.String(type)),
                    new TermQuery(new Field("Type.keyword"), FieldValue.String(type))
                }
            });
        }

        var player = (playerName ?? string.Empty).Trim();
        if (player.Length > 0)
        {
            filters.Add(new BoolQuery
            {
                MinimumShouldMatch = 1,
                Should = new List<Query>
                {
                    new TermQuery(new Field("PlayerName"), FieldValue.String(player)),
                    new TermQuery(new Field("PlayerName.keyword"), FieldValue.String(player))
                }
            });
        }

        return new SearchRequest(pattern)
        {
            From = from,
            Size = size,
            TrackTotalHits = new TrackHits(true),
            IgnoreUnavailable = true,
            AllowNoIndices = true,
            Query = new BoolQuery { Filter = filters },
            Sort = new List<SortOptions>
            {
                new FieldSort(new Field("CreateAt")) { Order = SortOrder.Desc }
            }
        };
    }

    /// <summary>把检索结果收成一页。失败只留简短说明，不把集群原文带回接口。</summary>
    private GameOrderEsPage ReadPage(SearchResponse<GameOrderEsDocument> response, int requestedCategory)
    {
        if (response == null || !response.IsValidResponse)
        {
            _logger?.LogWarning("注单明细查询未成功 {Status}", response?.ApiCallDetails?.HttpStatusCode);
            return new GameOrderEsPage
            {
                QueryFailed = true,
                Message = "#注单明细查询没有成功#"
            };
        }

        var items = new List<GameOrderEsHit>();
        if (response.Hits != null)
        {
            foreach (var hit in response.Hits)
            {
                if (hit?.Source == null)
                    continue;
                items.Add(MapHit(hit.Source, hit.Id, requestedCategory));
            }
        }

        var total = response.Total;
        if (total < 0)
            total = 0;
        if (total > int.MaxValue)
            total = int.MaxValue;
        return new GameOrderEsPage
        {
            Total = (int)total,
            Items = items
        };
    }

    /// <summary>取业务客户端。没注册时返回 null。</summary>
    private ElasticsearchClient BusinessClient()
    {
        if (_clients == null)
            return null;
        return _clients.TryGet(EsClientTypeEnum.Business, out var client) ? client : null;
    }

    /// <summary>没配业务检索时的分页。</summary>
    private static GameOrderEsPage NotConfiguredPage() => new()
    {
        NotConfigured = true,
        Message = "#没有配置注单明细检索#"
    };

    /// <summary>没配业务检索时的单条结果。</summary>
    private static GameOrderEsHit NotConfiguredHit() => new()
    {
        Found = false,
        NotConfigured = true,
        Message = "#没有配置注单明细检索#"
    };

    /// <summary>把一条文档收成接口用的命中。</summary>
    private static GameOrderEsHit MapHit(GameOrderEsDocument doc, string hitId, int requestedCategory)
    {
        var typeName = ReadText(doc.Type);
        var currencyCode = ReadText(doc.Currency);
        int.TryParse(typeName, out var typeNum);
        int.TryParse(currencyCode, out var currencyNum);
        return new GameOrderEsHit
        {
            Found = true,
            SourceID = doc.SourceID ?? string.Empty,
            EsID = string.IsNullOrEmpty(doc.EsID) ? (hitId ?? string.Empty) : doc.EsID,
            UserID = doc.UserID,
            UserName = doc.UserName ?? string.Empty,
            PlayerName = doc.PlayerName ?? string.Empty,
            BetMoney = doc.BetMoney,
            BetAmount = doc.BetAmount,
            Money = doc.Money,
            Type = typeNum,
            TypeName = typeName ?? string.Empty,
            Status = ReadText(doc.Status) ?? string.Empty,
            Currency = currencyNum,
            CurrencyCode = currencyCode ?? string.Empty,
            Game = doc.Game ?? string.Empty,
            CreateAt = NormalizeTime(doc.CreateAt),
            SettlementAt = NormalizeTime(doc.SettlementAt),
            Category = ResolveCategory(ReadText(doc.Category), ReadText(doc.SubCategory), requestedCategory)
        };
    }

    /// <summary>空日期和年份过小的占位时间不当成结算时间。</summary>
    private static DateTime? NormalizeTime(DateTime? value)
    {
        if (value == null || value.Value.Year < 1900)
            return null;
        return value;
    }

    /// <summary>读字符串或数字。没这个字段时返回 null。</summary>
    private static string ReadText(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.GetRawText(),
            _ => null
        };
    }

    /// <summary>分类名字对到后台用的数字。对不上就用这次请求的分类。</summary>
    private static int ResolveCategory(string category, string subCategory, int requestedCategory)
    {
        var parsed = ParseCategoryName(category);
        if (parsed >= 0)
            return parsed;
        parsed = ParseCategoryName(subCategory);
        if (parsed >= 0)
            return parsed;
        return requestedCategory;
    }

    /// <summary>已知分类名字。不认识返回 -1。</summary>
    private static int ParseCategoryName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return -1;
        return name.Trim().ToLowerInvariant() switch
        {
            "lottery" => GameOrderSummaryService.CategoryLottery,
            "esport" => GameOrderSummaryService.CategoryESport,
            "slot" => GameOrderSummaryService.CategorySlot,
            "live" => GameOrderSummaryService.CategoryLive,
            "chess" => GameOrderSummaryService.CategoryChess,
            "sport" => GameOrderSummaryService.CategorySport,
            "fish" => GameOrderSummaryService.CategoryFish,
            "animal" => GameOrderSummaryService.CategoryAnimal,
            "mini" or "minigame" => GameOrderSummaryService.CategoryMini,
            "fantasy" or "battle" => GameOrderSummaryService.CategoryFantasy,
            _ => -1
        };
    }

    /// <summary>注单索引里的一条文档。只取列表和详情要用的字段。</summary>
    private sealed class GameOrderEsDocument
    {
        /// <summary>站点。</summary>
        [JsonPropertyName("SiteID")]
        public int SiteID { get; set; }

        /// <summary>会员。</summary>
        [JsonPropertyName("UserID")]
        public int UserID { get; set; }

        /// <summary>用户名。</summary>
        [JsonPropertyName("UserName")]
        public string UserName { get; set; }

        /// <summary>玩家名。</summary>
        [JsonPropertyName("PlayerName")]
        public string PlayerName { get; set; }

        /// <summary>第三方单号。</summary>
        [JsonPropertyName("SourceID")]
        public string SourceID { get; set; }

        /// <summary>文档号。</summary>
        [JsonPropertyName("EsID")]
        public string EsID { get; set; }

        /// <summary>游戏名。</summary>
        [JsonPropertyName("Game")]
        public string Game { get; set; }

        /// <summary>投注。</summary>
        [JsonPropertyName("BetMoney")]
        public decimal BetMoney { get; set; }

        /// <summary>有效投注。</summary>
        [JsonPropertyName("BetAmount")]
        public decimal BetAmount { get; set; }

        /// <summary>盈亏。</summary>
        [JsonPropertyName("Money")]
        public decimal Money { get; set; }

        /// <summary>游戏类型，可能是文字或数字。</summary>
        [JsonPropertyName("Type")]
        public JsonElement Type { get; set; }

        /// <summary>状态，可能是文字或数字。</summary>
        [JsonPropertyName("Status")]
        public JsonElement Status { get; set; }

        /// <summary>币种，可能是文字或数字。</summary>
        [JsonPropertyName("Currency")]
        public JsonElement Currency { get; set; }

        /// <summary>分类名字。</summary>
        [JsonPropertyName("Category")]
        public JsonElement Category { get; set; }

        /// <summary>子分类名字。</summary>
        [JsonPropertyName("SubCategory")]
        public JsonElement SubCategory { get; set; }

        /// <summary>下注时间。</summary>
        [JsonPropertyName("CreateAt")]
        public DateTime? CreateAt { get; set; }

        /// <summary>结算时间。</summary>
        [JsonPropertyName("SettlementAt")]
        public DateTime? SettlementAt { get; set; }
    }
}
