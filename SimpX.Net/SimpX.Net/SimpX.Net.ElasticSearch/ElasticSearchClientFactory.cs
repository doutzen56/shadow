using Elastic.Clients.Elasticsearch;
using Elastic.Transport;

namespace SimpX.Net.ElasticSearch;

public class ElasticSearchClientFactory
{
    /// <summary>
    /// 按配置段创建客户端。段不存在或没启用时返回 null，不抛错。
    /// </summary>
    /// <param name="configPath">配置路径，例如 ElasticSearch:Business</param>
    /// <param name="keepClrPropertyNames">为 true 时字段名保持类属性原样，给注单这类已按原名写入的索引用</param>
    /// <param name="requestTimeout">单次请求超时；空则沿用客户端默认</param>
    /// <returns>客户端；没启用时为 null</returns>
    public static ElasticsearchClient TryCreate(string configPath, bool keepClrPropertyNames = false, TimeSpan? requestTimeout = null)
    {
        var options = App.GetConfig<ElasticSearchOptions>(configPath);
        if (options == null || !options.Enabled)
            return null;
        return CreateClient<ElasticSearchOptions>(configPath, keepClrPropertyNames, requestTimeout);
    }

    /// <summary>
    /// 创建客户端。配置段必须存在；没启用时返回 null。
    /// </summary>
    /// <typeparam name="TOptions">配置类型</typeparam>
    /// <param name="configPath">配置路径，例如 ElasticSearch:Logging</param>
    /// <param name="keepClrPropertyNames">为 true 时字段名保持类属性原样</param>
    /// <param name="requestTimeout">单次请求超时；空则沿用客户端默认</param>
    /// <returns>客户端；没启用时为 null</returns>
    /// <exception cref="AppFriendlyException">配置段不存在时抛出</exception>
    public static ElasticsearchClient CreateClient<TOptions>(
        string configPath,
        bool keepClrPropertyNames = false,
        TimeSpan? requestTimeout = null) where TOptions : ElasticSearchOptions, new()
    {
        // 从配置文件读取当前场景的配置
        var options = App.GetConfig<TOptions>(configPath);
        if (options == null)
            throw Oops.Oh($"未找到{configPath}配置项");

        if (!options.Enabled)
            return null;

        // 验证服务地址
        if (options.ServerUris == null || !options.ServerUris.Any())
            throw new ArgumentException($"ES 配置 {configPath} 未设置 ServerUris");

        // 构建连接池（支持集群）
        var uris = options.ServerUris.Select(uri => new Uri(uri)).ToList();
        var connectionPool = new StaticNodePool(uris);
        var connectionSettings = new ElasticsearchClientSettings(connectionPool)
            .DefaultIndex(options.DefaultIndex) // 设置默认索引
            .DisableDirectStreaming()  // 开启请求/响应日志，方便排查问题
            .OnRequestCompleted(response =>
            {
                if (response.HttpStatusCode == 401)
                {
                    Console.WriteLine("ES 请求被拒绝：未提供有效认证信息");
                }
            });

        // 配置认证
        ConfigureAuthentication(connectionSettings, options);

        // 配置 HTTPS 证书指纹
        if (!string.IsNullOrEmpty(options.Fingerprint))
            connectionSettings.CertificateFingerprint(options.Fingerprint);

        // 注单索引是按属性原名写入的（SiteID、SourceID）。默认会改成小写开头，对不上。
        if (keepClrPropertyNames)
            connectionSettings.DefaultFieldNameInferrer(static name => name);
        if (requestTimeout is TimeSpan timeout)
            connectionSettings.RequestTimeout(timeout);

        return new ElasticsearchClient(connectionSettings);
    }

    /// <summary>
    /// 配置认证（通用逻辑）
    /// </summary>
    private static void ConfigureAuthentication(ElasticsearchClientSettings settings, ElasticSearchOptions options)
    {
        switch (options.AuthType)
        {
            case ElasticSearchAuthTypeEnum.Basic:
                settings.Authentication(new BasicAuthentication(options.User, options.Password));
                break;

            case ElasticSearchAuthTypeEnum.ApiKey:
                settings.Authentication(new ApiKey(options.ApiKey));
                break;

            case ElasticSearchAuthTypeEnum.Base64ApiKey:
                settings.Authentication(new Base64ApiKey(options.Base64ApiKey));
                break;

            case ElasticSearchAuthTypeEnum.None:
                // 无需认证
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(options.AuthType));
        }
    }
}