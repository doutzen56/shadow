using System.Text.RegularExpressions;
using System.Web;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 缓存管理：后台查看缓存定义，并按类别清理。业务读写不要走这里。
/// 清空只删数据类缓存，不会调用整库清空，避免把事件队列、锁和黑名单一起抹掉。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "cache", Order = 400, Description = "缓存管理")]
public class CacheAppService : IDynamicApiController, ITransient
{
    private readonly ICacheProvider _cacheProvider;
    private readonly CacheOptions _cacheOptions;
    private readonly CacheRegistry _registry;
    private readonly CacheKeyBuilder _keys;
    private readonly UserManager _userManager;
    private readonly ILogger<CacheAppService> _logger;

    /// <summary>
    /// 注入底层缓存、缓存定义和当前用户。
    /// </summary>
    /// <param name="cacheProvider">内存或 Redis</param>
    /// <param name="cacheOptions">缓存配置，用来去掉全局前缀</param>
    /// <param name="registry">缓存定义</param>
    /// <param name="keys">按定义拼键</param>
    /// <param name="userManager">当前用户</param>
    /// <param name="logger">日志。清空状态缓存时记一笔，方便事后追查</param>
    public CacheAppService(
        ICacheProvider cacheProvider,
        IOptions<CacheOptions> cacheOptions,
        CacheRegistry registry,
        CacheKeyBuilder keys,
        UserManager userManager,
        ILogger<CacheAppService> logger)
    {
        _cacheProvider = cacheProvider;
        _cacheOptions = cacheOptions.Value;
        _registry = registry;
        _keys = keys;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// 获取数据类缓存的键名。状态类（锁、黑名单、验证码）不在这里列出。
    /// </summary>
    /// <returns>键名集合（不含全局前缀）</returns>
    [DisplayName("获取缓存键名集合")]
    public List<string> GetKeyList() => ListDataKeys(null);

    /// <summary>
    /// 根据键名前缀获取数据类缓存的键名。
    /// </summary>
    /// <param name="prefixKey">键名前缀</param>
    /// <returns>键名集合</returns>
    [DisplayName("根据键名前缀获取缓存键名集合")]
    public List<string> GetKeysByPrefixKey(string prefixKey) => ListDataKeys(prefixKey);

    /// <summary>
    /// 列出已注册的缓存定义。状态类的 Caution 为 true，清空前要单独确认。
    /// </summary>
    /// <returns>缓存定义</returns>
    [DisplayName("获取缓存定义列表")]
    public List<CacheDefinitionOutput> ListDefinitions()
    {
        return _registry.All
            .OrderBy(u => u.RegistryKey, StringComparer.Ordinal)
            .Select(u => new CacheDefinitionOutput
            {
                Module = u.Module,
                Name = u.Name,
                RegistryKey = u.RegistryKey,
                Kind = u.Kind?.ToString() ?? "未使用",
                ExpirationSeconds = null, // 过期时间在业务调用处指定，定义里不再写死
                Caution = u.Kind == CacheKind.State
            })
            .ToList();
    }

    /// <summary>
    /// 获取缓存值。
    /// </summary>
    /// <param name="key">键名</param>
    /// <returns>缓存值</returns>
    [DisplayName("获取缓存值")]
    public object GetValue(string key) => ReadValue(key);

    /// <summary>
    /// 删除一条数据类缓存。状态类键请改用清空状态缓存。
    /// </summary>
    /// <param name="key">键名</param>
    /// <returns>删除的条数</returns>
    /// <exception cref="AppFriendlyException">键属于状态类或未登记</exception>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("删除缓存")]
    public int Remove(string key)
    {
        var full = FullKey(DecodeKey(key));
        if (!IsDataKey(full))
            throw Oops.Oh("这是状态缓存或未登记的键，不能在这里删除。状态缓存请用清空状态缓存，并确认影响。");

        return _cacheProvider.Cache.Remove(full);
    }

    /// <summary>
    /// 按前缀删除数据类缓存。匹配不到数据类键时不删任何东西。
    /// </summary>
    /// <param name="prefixKey">键名前缀</param>
    /// <returns>删除的条数</returns>
    [ApiDescriptionSettings(Name = "DeleteByPreKey"), HttpPost]
    [DisplayName("根据键名前缀删除缓存")]
    public int RemoveByPrefixKey(string prefixKey)
    {
        var keys = ListDataKeys(prefixKey).Select(FullKey).ToArray();
        return keys.Length == 0 ? 0 : _cacheProvider.Cache.Remove(keys);
    }

    /// <summary>
    /// 清空数据类缓存。超级管理员清全部数据类键，其他人只清掉以自己用户编号结尾的数据类键。
    /// 不调用整库清空，事件队列和状态缓存会留下来。
    /// </summary>
    [DisplayName("清空所有缓存")]
    [ApiDescriptionSettings(Name = "Clear"), HttpPost]
    public void Clear()
    {
        if (_userManager.SuperAdmin)
        {
            RemoveDataKeys();
            return;
        }

        var userId = _userManager.UserId.ToString();
        var keys = ListDataKeys(null).Where(u => u.EndsWith(userId, StringComparison.Ordinal)).Select(FullKey).ToArray();
        if (keys.Length > 0)
            _cacheProvider.Cache.Remove(keys);
    }

    /// <summary>
    /// 清空全部数据类缓存。总后台超管或租户站系统管理员可以调用。
    /// </summary>
    /// <returns>删除的条数</returns>
    /// <exception cref="AppFriendlyException">权限不够</exception>
    [DisplayName("清空数据缓存")]
    [ApiDescriptionSettings(Name = "ClearData"), HttpPost]
    public int ClearDataCaches()
    {
        EnsureCanClearAllCaches("只有总后台超管或系统管理员可以清空全部数据缓存");
        return RemoveDataKeys();
    }

    /// <summary>
    /// 按缓存名清空这一类数据缓存。状态类名称会拒绝，改走清空状态缓存。
    /// </summary>
    /// <param name="name">缓存名，或「模块:名称」</param>
    /// <returns>删除的条数</returns>
    /// <exception cref="AppFriendlyException">权限不够，或这个名字是状态类</exception>
    [DisplayName("按缓存名清空数据缓存")]
    [ApiDescriptionSettings(Name = "ClearByName"), HttpPost]
    public int ClearByCacheName(string name)
    {
        EnsureCanClearAllCaches("只有总后台超管或系统管理员可以按缓存名清空");
        var definition = _registry.GetRequired(name);
        if (definition.Kind == CacheKind.State)
            throw Oops.Oh("这个缓存是状态类。请改用清空状态缓存，并确认锁、黑名单、验证码会被清掉。");

        return RemoveDefinitionKeys(definition);
    }

    /// <summary>
    /// 清空某一类状态缓存。会让对应的锁、黑名单、验证码或在线用户记录立刻失效。
    /// 总后台超管或租户站系统管理员可以调用。
    /// </summary>
    /// <param name="name">缓存名，或「模块:名称」</param>
    /// <returns>删除的条数</returns>
    /// <exception cref="AppFriendlyException">权限不够，或这个名字不是状态类</exception>
    [DisplayName("清空状态缓存")]
    [ApiDescriptionSettings(Name = "ClearState"), HttpPost]
    public int ClearStateCache(string name)
    {
        EnsureCanClearAllCaches("只有总后台超管或系统管理员可以清空状态缓存");
        var definition = _registry.GetRequired(name);
        if (definition.Kind == CacheKind.Data)
            throw Oops.Oh("这个缓存不是状态类，请用按缓存名清空数据缓存。");

        _logger.LogWarning("管理员 {UserId} 清空状态缓存 {Name}。锁、黑名单、验证码、在线用户里对应该缓存的内容会立刻失效。",
            _userManager.UserId, definition.RegistryKey);
        return RemoveDefinitionKeys(definition);
    }

    /// <summary>
    /// 整表清空要总后台超管或租户站系统管理员；普通用户只能清自己后缀的键。
    /// </summary>
    /// <param name="message">提示</param>
    /// <exception cref="AppFriendlyException">权限不够</exception>
    private void EnsureCanClearAllCaches(string message)
    {
        if (!_userManager.SuperAdmin && !_userManager.SysAdmin)
            throw Oops.Oh(message);
    }

    /// <summary>
    /// 删掉全部数据类键。
    /// </summary>
    /// <returns>删除的条数</returns>
    private int RemoveDataKeys()
    {
        var keys = ListDataKeys(null).Select(FullKey).ToArray();
        return keys.Length == 0 ? 0 : _cacheProvider.Cache.Remove(keys);
    }

    /// <summary>
    /// 删掉某一个定义下的全部键。
    /// </summary>
    /// <param name="definition">缓存定义</param>
    /// <returns>删除的条数</returns>
    private int RemoveDefinitionKeys(CacheDefinition definition)
    {
        var keys = SearchFullKeys(_keys.BuildNamePrefix(definition));
        return keys.Length == 0 ? 0 : _cacheProvider.Cache.Remove(keys);
    }

    /// <summary>
    /// 列出数据类键名，不含全局前缀。prefix 为空时列出全部数据类键。
    /// </summary>
    /// <param name="prefix">业务前缀，可空</param>
    /// <returns>键名</returns>
    private List<string> ListDataKeys(string prefix)
    {
        var global = Prefix();
        var result = new List<string>();
        // 未使用过的名字 Kind 为空，按数据类前缀扫，避免后台列表空白；已标成状态的跳过
        foreach (var definition in _registry.All.Where(u => u.Kind != CacheKind.State))
        {
            foreach (var full in SearchFullKeys(_keys.BuildNamePrefix(definition)))
            {
                var business = full.Length >= global.Length ? full[global.Length..] : full;
                if (!string.IsNullOrEmpty(prefix) && !business.StartsWith(prefix, StringComparison.Ordinal))
                    continue;
                result.Add(business);
            }
        }

        return result.Distinct(StringComparer.Ordinal).OrderBy(u => u, StringComparer.Ordinal).ToList();
    }

    /// <summary>
    /// 完整键是不是某个数据类定义打头的。
    /// </summary>
    /// <param name="fullKey">带全局前缀的键</param>
    /// <returns>是数据类键返回 true</returns>
    private bool IsDataKey(string fullKey)
    {
        foreach (var definition in _registry.All.Where(u => u.Kind != CacheKind.State))
        {
            if (fullKey.StartsWith(_keys.BuildNamePrefix(definition), StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    /// <summary>
    /// 按完整前缀扫描键。内存缓存用本进程的键，Redis 用搜索。
    /// </summary>
    /// <param name="fullPrefix">带全局前缀的扫描前缀</param>
    /// <returns>完整键名</returns>
    private string[] SearchFullKeys(string fullPrefix)
    {
        if (_cacheProvider.Cache == Cache.Default)
        {
            return _cacheProvider.Cache.Keys
                .Where(u => u.StartsWith(fullPrefix, StringComparison.Ordinal))
                .ToArray();
        }

        if (_cacheProvider.Cache is FullRedis redis)
            return redis.Search($"{fullPrefix}*", 0, int.MaxValue).ToArray();

        return Array.Empty<string>();
    }

    /// <summary>
    /// 读一条原始值，给后台页面看。Redis 下按类型取出。
    /// </summary>
    /// <param name="key">键名，可以是编码过的</param>
    /// <returns>缓存值</returns>
    private object ReadValue(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;

        key = DecodeKey(key);
        var fullKey = FullKey(key);
        if (_cacheProvider.Cache == Cache.Default)
            return _cacheProvider.Cache.Get<object>(fullKey);

        if (_cacheProvider.Cache is FullRedis redisCache)
        {
            if (!redisCache.ContainsKey(fullKey))
                return null;
            try
            {
                var keyType = redisCache.TYPE(fullKey)?.ToLower();
                switch (keyType)
                {
                    case "string":
                        return redisCache.Get<string>(fullKey);
                    case "list":
                        return redisCache.GetList<string>(fullKey)?.ToList();
                    case "hash":
                        return redisCache.GetDictionary<string>(fullKey)?.ToDictionary(k => k.Key, v => v.Value);
                    case "set":
                        return redisCache.GetSet<string>(fullKey)?.ToArray();
                    case "zset":
                        return redisCache.GetSortedSet<string>(fullKey)?.Range(0, -1)?.ToList();
                    case "none":
                        return null;
                    default:
                        return new Dictionary<string, object>
                        {
                            { "key", key },
                            { "type", keyType ?? "unknown" },
                            { "message", "无法使用标准方式获取此类型数据" }
                        };
                }
            }
            catch (Exception ex)
            {
                return new Dictionary<string, object>
                {
                    { "key", key },
                    { "error", ex.Message },
                    { "type", "exception" }
                };
            }
        }

        return _cacheProvider.Cache.Get<object>(fullKey);
    }

    /// <summary>
    /// 键名如果做了 URL 编码，先解出来。
    /// </summary>
    /// <param name="key">键名</param>
    /// <returns>解码后的键名</returns>
    private static string DecodeKey(string key)
    {
        if (!string.IsNullOrEmpty(key) && Regex.IsMatch(key, @"%[0-9a-fA-F]{2}"))
            return HttpUtility.UrlDecode(key);
        return key;
    }

    /// <summary>
    /// 全局前缀。没配时是空字符串。
    /// </summary>
    /// <returns>前缀</returns>
    private string Prefix() => _cacheOptions.Prefix ?? string.Empty;

    /// <summary>
    /// 补上全局前缀。
    /// </summary>
    /// <param name="key">不含前缀的键名</param>
    /// <returns>完整键名</returns>
    private string FullKey(string key) => $"{Prefix()}{key}";
}
