using Elastic.Clients.Elasticsearch;

namespace SimpX.Net.ElasticSearch;

/// <summary>
/// ElasticSearch 客户端注册。只有需要写 ES 或读 ES 的应用再引用本包并调用。
/// </summary>
public static class ElasticSearchSetup
{
    /// <summary>
    /// 注册检索客户端。
    /// 审计日志仍只看 Logging:ElasticSearch（一般在 Logging.json），启用时才注册 <see cref="ElasticsearchClient"/> 单例。
    /// 注单等业务数据看 ElasticSearch.json 的 ElasticSearch:Business，没有再退到同文件的 ElasticSearch:Logging，放进 <see cref="ElasticSearchClientContainer"/>，不拿去顶替日志单例。
    /// </summary>
    /// <param name="services">服务集合</param>
    public static void AddElasticSearch(this IServiceCollection services)
    {
        var clients = new Dictionary<EsClientTypeEnum, ElasticsearchClient>();

        // 审计写入器按类型取 ElasticsearchClient。这段没开就不注册，避免把操作日志写进注单集群。
        var logging = ElasticSearchClientFactory.TryCreate("Logging:ElasticSearch");
        if (logging != null)
        {
            clients[EsClientTypeEnum.Logging] = logging;
            services.AddSingleton(logging);
        }

        // 注单字段是原名，业务客户端单独保持属性名，并限制单次等待，避免后台页面一直挂着。
        var business = ElasticSearchClientFactory.TryCreate(
                "ElasticSearch:Business",
                keepClrPropertyNames: true,
                requestTimeout: TimeSpan.FromSeconds(20))
            ?? ElasticSearchClientFactory.TryCreate(
                "ElasticSearch:Logging",
                keepClrPropertyNames: true,
                requestTimeout: TimeSpan.FromSeconds(20));
        if (business != null)
            clients[EsClientTypeEnum.Business] = business;

        if (clients.Count > 0)
            services.AddSingleton(new ElasticSearchClientContainer(clients));
    }
}