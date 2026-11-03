using Elastic.Clients.Elasticsearch;

namespace SimpX.Net.ElasticSearch;

/// <summary>
/// ES客户端容器
/// </summary>
public class ElasticSearchClientContainer
{
    private readonly Dictionary<EsClientTypeEnum, ElasticsearchClient> _clients;

    /// <summary>
    /// 初始化容器（通过字典注入所有客户端）
    /// </summary>
    public ElasticSearchClientContainer(Dictionary<EsClientTypeEnum, ElasticsearchClient> clients)
    {
        _clients = clients ?? throw new ArgumentNullException(nameof(clients));
    }

    /// <summary>
    /// 日志专用客户端
    /// </summary>
    public ElasticsearchClient Logging => GetClient(EsClientTypeEnum.Logging);

    /// <summary>
    /// 业务数据同步客户端
    /// </summary>
    public ElasticsearchClient Business => GetClient(EsClientTypeEnum.Business);

    /// <summary>
    /// 按类型取客户端。没注册时返回 false，不抛错。
    /// </summary>
    /// <param name="type">客户端类型</param>
    /// <param name="client">取到的客户端；没有时为 null</param>
    /// <returns>是否已注册</returns>
    public bool TryGet(EsClientTypeEnum type, out ElasticsearchClient client)
    {
        if (_clients != null && _clients.TryGetValue(type, out client))
            return true;
        client = null;
        return false;
    }

    /// <summary>
    /// 根据类型获取客户端（内部校验，避免未注册的类型）
    /// </summary>
    private ElasticsearchClient GetClient(EsClientTypeEnum type)
    {
        if (_clients.TryGetValue(type, out var client))
        {
            return client;
        }
        throw new KeyNotFoundException($"未注册的ES客户端类型：{type}，请检查注册配置");
    }
}