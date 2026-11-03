using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.SignalR;

namespace SimpX.Net.Core;

/// <summary>
/// 集线器推送。Redis 时用 NewLife 的发布订阅把调用发到每个实例，各实例再调本机的集线器。
/// 发布方自己也推本机连接，并忽略自己发出的广播，避免同一条消息推两次。
/// </summary>
public sealed class HubInvocationBus : IHubInvocationBus, IHostedService, IDisposable
{
    private const string ScopeAll = "All";
    private const string ScopeClient = "Client";
    private const string ScopeClients = "Clients";
    private const string ScopeAllExcept = "AllExcept";
    private const string ScopeGroup = "Group";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };

    private readonly ICacheProvider _cacheProvider;
    private readonly CacheOptions _cacheOptions;
    private readonly IServiceProvider _services;
    private readonly ILogger<HubInvocationBus> _logger;
    private readonly string _instanceId = Guid.NewGuid().ToString("N");
    private readonly CancellationTokenSource _stop = new();

    private Redis _subscribeClient;
    private Task _subscribeLoop;
    private int _disposed;

    /// <summary>
    /// 注入缓存、配置和日志。集线器上下文在真正推送时再取，避免启动顺序拧在一起。
    /// </summary>
    /// <param name="cacheProvider">内存或 Redis</param>
    /// <param name="cacheOptions">缓存配置，用来判断是不是 Redis，以及频道名前缀</param>
    /// <param name="services">根服务，用来取本机集线器</param>
    /// <param name="logger">日志</param>
    public HubInvocationBus(
        ICacheProvider cacheProvider,
        IOptions<CacheOptions> cacheOptions,
        IServiceProvider services,
        ILogger<HubInvocationBus> logger)
    {
        _cacheProvider = cacheProvider;
        _cacheOptions = cacheOptions.Value;
        _services = services;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task SendAllAsync(Type hubType, string method, object argument, CancellationToken cancellationToken = default)
        => DeliverAsync(Create(hubType, ScopeAll, null, method, argument), cancellationToken);

    /// <inheritdoc />
    public Task SendClientAsync(Type hubType, string connectionId, string method, object argument, CancellationToken cancellationToken = default)
        => DeliverAsync(Create(hubType, ScopeClient, new[] { connectionId ?? string.Empty }, method, argument), cancellationToken);

    /// <inheritdoc />
    public Task SendClientsAsync(Type hubType, IEnumerable<string> connectionIds, string method, object argument, CancellationToken cancellationToken = default)
    {
        var ids = (connectionIds ?? Array.Empty<string>()).Where(u => !string.IsNullOrEmpty(u)).ToArray();
        if (ids.Length == 0) return Task.CompletedTask;
        return DeliverAsync(Create(hubType, ScopeClients, ids, method, argument), cancellationToken);
    }

    /// <inheritdoc />
    public Task SendAllExceptAsync(Type hubType, IEnumerable<string> excludedConnectionIds, string method, object argument, CancellationToken cancellationToken = default)
    {
        var ids = (excludedConnectionIds ?? Array.Empty<string>()).ToArray();
        return DeliverAsync(Create(hubType, ScopeAllExcept, ids, method, argument), cancellationToken);
    }

    /// <inheritdoc />
    public Task SendGroupAsync(Type hubType, string groupName, string method, object argument, CancellationToken cancellationToken = default)
        => DeliverAsync(Create(hubType, ScopeGroup, new[] { groupName ?? string.Empty }, method, argument), cancellationToken);

    /// <inheritdoc />
    public Task AddToGroupAsync(Type hubType, string connectionId, string groupName, CancellationToken cancellationToken = default)
    {
        var groups = GetGroups(hubType);
        return groups.AddToGroupAsync(connectionId, groupName, cancellationToken);
    }

    /// <summary>
    /// Redis 时另开一条连接订阅集线器频道。这条连接会一直占着，不能拿来做普通缓存命令。
    /// </summary>
    /// <param name="cancellationToken">宿主启动取消令牌。订阅循环用自己的令牌，避免启动结束后被取消。</param>
    /// <returns>异步任务</returns>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!UseRedis())
            return Task.CompletedTask;

        if (_cacheProvider.Cache is not FullRedis redis)
            throw new InvalidOperationException("缓存配置是 Redis，但当前缓存不是 Redis 客户端，集线器消息无法跨实例转发。");

        _subscribeClient = redis.CreateSub(redis.Db);
        var subscriber = new PubSub(_subscribeClient, ChannelName());
        _subscribeLoop = Task.Run(() => subscriber.SubscribeAsync(OnMessage, _stop.Token), CancellationToken.None);
        _subscribeLoop.ContinueWith(task =>
        {
            if (task.IsFaulted && !_stop.IsCancellationRequested)
                _logger.LogError(task.Exception, "集线器订阅已退出，本机收不到其他实例的推送");
        }, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 停掉订阅。订阅线程最多再等 3 秒，避免卡住进程退出。
    /// </summary>
    /// <param name="cancellationToken">宿主停止取消令牌</param>
    /// <returns>异步任务</returns>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (!_stop.IsCancellationRequested)
            _stop.Cancel();

        if (_subscribeLoop != null)
        {
            var finished = await Task.WhenAny(_subscribeLoop, Task.Delay(TimeSpan.FromSeconds(3), cancellationToken));
            if (finished != _subscribeLoop)
                _logger.LogWarning("集线器订阅没有在 3 秒内退出");
        }

        DisposeSubscribeClient();
    }

    /// <summary>
    /// 释放订阅连接。
    /// </summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        if (!_stop.IsCancellationRequested)
            _stop.Cancel();
        _stop.Dispose();
        DisposeSubscribeClient();
    }

    /// <summary>
    /// 先发给其他实例，再推本机。发布失败时本机也不推，调用方能知道这次没推成功。
    /// </summary>
    /// <param name="message">调用内容</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    /// <exception cref="InvalidOperationException">配置了 Redis 但当前缓存不是 Redis 客户端</exception>
    private async Task DeliverAsync(HubInvocationMessage message, CancellationToken cancellationToken)
    {
        if (UseRedis())
        {
            var full = _cacheProvider.Cache as FullRedis
                ?? throw new InvalidOperationException("缓存配置是 Redis，但当前缓存不是 Redis 客户端，跨实例推送发不出去。");
            message.Origin = _instanceId;
            var delivered = new PubSub(full, ChannelName()).Publish(JsonSerializer.Serialize(message, JsonOptions));
            if (delivered <= 0)
                _logger.LogWarning("集线器消息已发出，但当前没有其他实例在订阅。方法 {Method}", message.Method);
        }

        await DispatchAsync(message, cancellationToken);
    }

    /// <summary>
    /// 收到其他实例的消息后，在本机集线器上再调一次。自己发出的消息直接丢掉。
    /// </summary>
    /// <param name="channel">频道</param>
    /// <param name="json">消息正文</param>
    private void OnMessage(string channel, string json)
    {
        try
        {
            var message = JsonSerializer.Deserialize<HubInvocationMessage>(json, JsonOptions);
            if (message == null || message.Origin == _instanceId)
                return;

            DispatchAsync(message, CancellationToken.None).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "本机执行集线器消息失败");
        }
    }

    /// <summary>
    /// 按范围调用本机集线器。本进程没有这个集线器时跳过，调度进程会落到这里。
    /// </summary>
    /// <param name="message">调用内容</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    private async Task DispatchAsync(HubInvocationMessage message, CancellationToken cancellationToken)
    {
        var hubType = Type.GetType(message.HubTypeName, throwOnError: false);
        if (hubType == null)
        {
            _logger.LogDebug("本进程没有集线器 {Hub}，跳过", message.HubTypeName);
            return;
        }

        var contextType = typeof(IHubContext<>).MakeGenericType(hubType);
        var context = _services.GetService(contextType);
        if (context == null)
        {
            _logger.LogDebug("本进程没有注册集线器 {Hub}，跳过", hubType.FullName);
            return;
        }

        var clients = (IHubClients)contextType.GetProperty(nameof(IHubContext<Hub>.Clients))!.GetValue(context)!;
        var proxy = message.Scope switch
        {
            ScopeAll => clients.All,
            ScopeClient => clients.Client(message.Targets?.FirstOrDefault() ?? string.Empty),
            ScopeClients => clients.Clients(message.Targets ?? Array.Empty<string>()),
            ScopeAllExcept => clients.AllExcept(message.Targets ?? Array.Empty<string>()),
            ScopeGroup => clients.Group(message.Targets?.FirstOrDefault() ?? string.Empty),
            _ => throw new InvalidOperationException($"不认识的集线器推送范围：{message.Scope}")
        };

        var args = DeserializeArgument(message);
        await proxy.SendCoreAsync(message.Method, args, cancellationToken);
    }

    /// <summary>
    /// 取本机分组管理。连接只存在于接受它的那台机器上。
    /// </summary>
    /// <param name="hubType">集线器类型</param>
    /// <returns>分组管理</returns>
    private IGroupManager GetGroups(Type hubType)
    {
        var contextType = typeof(IHubContext<>).MakeGenericType(hubType);
        var context = _services.GetRequiredService(contextType);
        return (IGroupManager)contextType.GetProperty(nameof(IHubContext<Hub>.Groups))!.GetValue(context)!;
    }

    /// <summary>
    /// 把参数从 JSON 还原成原来的类型，再交给集线器序列化给前端。
    /// </summary>
    /// <param name="message">调用内容</param>
    /// <returns>参数数组；没有参数时是空数组</returns>
    private static object[] DeserializeArgument(HubInvocationMessage message)
    {
        if (string.IsNullOrEmpty(message.ArgumentTypeName))
            return Array.Empty<object>();

        var argumentType = Type.GetType(message.ArgumentTypeName, throwOnError: true);
        var argument = JsonSerializer.Deserialize(message.ArgumentJson, argumentType!, JsonOptions);
        return new[] { argument };
    }

    /// <summary>
    /// 组装一条可广播的调用。
    /// </summary>
    /// <param name="hubType">集线器类型</param>
    /// <param name="scope">推送范围</param>
    /// <param name="targets">连接或分组</param>
    /// <param name="method">客户端方法名</param>
    /// <param name="argument">参数</param>
    /// <returns>调用内容</returns>
    private static HubInvocationMessage Create(Type hubType, string scope, string[] targets, string method, object argument)
    {
        return new HubInvocationMessage
        {
            HubTypeName = hubType.AssemblyQualifiedName,
            Scope = scope,
            Targets = targets,
            Method = method,
            ArgumentTypeName = argument?.GetType().AssemblyQualifiedName,
            ArgumentJson = argument == null ? null : JsonSerializer.Serialize(argument, argument.GetType(), JsonOptions)
        };
    }

    /// <summary>
    /// 当前是不是 Redis 缓存。
    /// </summary>
    /// <returns>是 Redis 返回 true</returns>
    private bool UseRedis()
        => string.Equals(_cacheOptions.CacheType, CacheTypeEnum.Redis.ToString(), StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// 发布订阅频道。带上缓存前缀，避免多个系统共用一个 Redis 时串消息。
    /// </summary>
    /// <returns>频道名</returns>
    private string ChannelName() => $"{_cacheOptions.Prefix}signalr:hub";

    /// <summary>
    /// 关掉订阅专用连接。可以重复调用。
    /// </summary>
    private void DisposeSubscribeClient()
    {
        var client = Interlocked.Exchange(ref _subscribeClient, null);
        client?.TryDispose();
    }

    /// <summary>
    /// 跨实例传递的一次集线器调用。
    /// </summary>
    private sealed class HubInvocationMessage
    {
        /// <summary>集线器类型的程序集限定名。</summary>
        public string HubTypeName { get; set; }

        /// <summary>推送范围：All、Client、Clients、AllExcept、Group。</summary>
        public string Scope { get; set; }

        /// <summary>连接编号或分组名。</summary>
        public string[] Targets { get; set; }

        /// <summary>客户端方法名。</summary>
        public string Method { get; set; }

        /// <summary>参数类型的程序集限定名。没有参数时为空。</summary>
        public string ArgumentTypeName { get; set; }

        /// <summary>参数的 JSON。</summary>
        public string ArgumentJson { get; set; }

        /// <summary>发出这条消息的实例。收到自己的消息时不再推一次。</summary>
        public string Origin { get; set; }
    }
}
