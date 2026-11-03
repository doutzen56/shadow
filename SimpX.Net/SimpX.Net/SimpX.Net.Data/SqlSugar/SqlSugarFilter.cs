using Microsoft.Extensions.Caching.Memory;

namespace SimpX.Net.Data;

/// <summary>
/// SqlSugar 数据权限和自定义过滤器。机构范围走统一数据缓存，过滤表达式放进程内内存，避免把表达式塞进 Redis。
/// </summary>
public static class SqlSugarFilter
{
    /// <summary>
    /// 过滤表达式缓存。按版本号区分，机构大范围变更时把版本加一，旧表达式自然失效。
    /// </summary>
    private static readonly Microsoft.Extensions.Caching.Memory.MemoryCache ExpressionCache = new(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions());

    /// <summary>
    /// 表达式缓存版本。整组作废时加一。
    /// </summary>
    private static int _expressionVersion;

    /// <summary>
    /// 在独立作用域里取当前用户可访问的机构；宿主没注册 <see cref="IDataScopeProvider"/> 时返回空集合（不按机构过滤）。
    /// </summary>
    /// <returns>机构 Id 集合</returns>
    private static List<long> LoadCurrentUserOrgIds()
    {
        var orgIds = new List<long>();
        Scoped.Create((factory, scope) =>
        {
            var provider = scope.ServiceProvider.GetService<IDataScopeProvider>();
            if (provider != null)
                orgIds = provider.GetCurrentUserOrgIdsAsync().GetAwaiter().GetResult() ?? new List<long>();
        });
        return orgIds;
    }

    /// <summary>
    /// 删掉这个用户的数据范围、按钮权限，以及他的过滤表达式。
    /// </summary>
    /// <param name="userId">用户 Id</param>
    /// <param name="dbConfigId">数据库配置 Id</param>
    public static void DeleteUserOrgCache(long userId, string dbConfigId)
    {
        var cache = App.GetRequiredService<ISimpXCache>();
        cache.Remove(PermissionCacheKeys.UserDataScope, userId);
        cache.Remove(PermissionCacheKeys.UserButton, userId);
        ExpressionCache.Remove(ExpressionKey("custom", dbConfigId, userId.ToString()));
        ExpressionCache.Remove(ExpressionKey("dataScope", dbConfigId, userId.ToString()));
    }

    /// <summary>
    /// 机构结构变了，没法再按用户逐个挑，整组数据范围缓存和过滤表达式一起作废。
    /// </summary>
    public static void DeleteAllUserDataScopeCache()
    {
        App.GetRequiredService<ISimpXCache>().RemoveAll(PermissionCacheKeys.UserDataScope);
        Interlocked.Increment(ref _expressionVersion);
    }

    /// <summary>
    /// 删除这个用户的自定义过滤器表达式。
    /// </summary>
    /// <param name="userId">用户 Id</param>
    /// <param name="dbConfigId">数据库配置 Id</param>
    public static void DeleteCustomCache(long userId, string dbConfigId)
    {
        ExpressionCache.Remove(ExpressionKey("custom", dbConfigId, userId.ToString()));
    }

    /// <summary>
    /// 配置用户机构集合过滤器。
    /// </summary>
    /// <param name="db">当前库</param>
    public static void SetOrgEntityFilter(SqlSugarScopeProvider db)
    {
        var maxDataScope = SetDataScopeFilter(db);
        if (maxDataScope is 0 or (int)DataScopeEnum.Self or (int)DataScopeEnum.All) return;

        var orgIds = LoadCurrentUserOrgIds();
        if (orgIds.Count == 0) return;

        db.QueryFilter.AddTableFilter<IOrgIdFilter>(o => SqlFunc.ContainsArray(orgIds, o.OrgId));
    }

    /// <summary>
    /// 配置“仅本人数据”过滤器，并返回这个用户的最大数据范围。
    /// </summary>
    /// <param name="db">当前库</param>
    /// <returns>最大数据范围；还没算出来时是 0</returns>
    private static int SetDataScopeFilter(SqlSugarScopeProvider db)
    {
        var maxDataScope = (int)DataScopeEnum.All;

        long.TryParse(App.HttpContext?.User.FindFirst(ClaimConst.UserId)?.Value, out var userId);
        if (userId <= 0) return maxDataScope;

        var cache = App.GetRequiredService<ISimpXCache>();
        maxDataScope = ReadMaxDataScope(cache, userId);
        if (maxDataScope == 0)
        {
            LoadCurrentUserOrgIds();
            maxDataScope = ReadMaxDataScope(cache, userId);
        }
        if (maxDataScope != (int)DataScopeEnum.Self) return maxDataScope;

        var cacheKey = ExpressionKey("dataScope", db.CurrentConnectionConfig.ConfigId?.ToString(), userId.ToString());
        var dataScopeFilter = ExpressionCache.Get<ConcurrentDictionary<Type, LambdaExpression>>(cacheKey);
        if (dataScopeFilter == null)
        {
            var entityTypes = App.EffectiveTypes.Where(u => !u.IsInterface && !u.IsAbstract && u.IsClass
                && (u.IsSubclassOf(typeof(EntityBaseOrg)) || u.IsSubclassOf(typeof(EntityBaseOrgDel))));
            if (!entityTypes.Any()) return maxDataScope;

            dataScopeFilter = new ConcurrentDictionary<Type, LambdaExpression>();
            foreach (var entityType in entityTypes)
            {
                var tAtt = entityType.GetCustomAttribute<TenantAttribute>();
                if (tAtt != null && db.CurrentConnectionConfig.ConfigId.ToString() != tAtt.configId.ToString())
                    continue;

                var lambda = entityType.GetConditionExpression<OwnerUserAttribute>(new List<long> { userId });
                db.QueryFilter.AddTableFilter(entityType, lambda);
                dataScopeFilter.TryAdd(entityType, lambda);
            }
            ExpressionCache.Set(cacheKey, dataScopeFilter);
        }
        else
        {
            foreach (var filter in dataScopeFilter)
                db.QueryFilter.AddTableFilter(filter.Key, filter.Value);
        }
        return maxDataScope;
    }

    /// <summary>
    /// 配置自定义过滤器。
    /// </summary>
    /// <param name="db">当前库</param>
    public static void SetCustomEntityFilter(SqlSugarScopeProvider db)
    {
        var userId = App.User?.FindFirst(ClaimConst.UserId)?.Value;
        var cacheKey = ExpressionKey("custom", db.CurrentConnectionConfig.ConfigId?.ToString(), userId);
        var tableFilterItemList = ExpressionCache.Get<List<TableFilterItem<object>>>(cacheKey);
        if (tableFilterItemList == null)
        {
            var entityFilterTypes = App.EffectiveTypes.Where(u => !u.IsInterface && !u.IsAbstract && u.IsClass
                && u.GetInterfaces().Any(i => i.HasImplementedRawGeneric(typeof(IEntityFilter))));
            if (!entityFilterTypes.Any()) return;

            var tableFilterItems = new List<TableFilterItem<object>>();
            foreach (var entityFilter in entityFilterTypes)
            {
                var instance = Activator.CreateInstance(entityFilter);
                var entityFilterMethod = entityFilter.GetMethod("AddEntityFilter");
                var entityFilters = ((IList)entityFilterMethod?.Invoke(instance, null))?.Cast<object>();
                if (entityFilters == null) continue;

                foreach (var u in entityFilters)
                {
                    var tableFilterItem = (TableFilterItem<object>)u;
                    var entityType = tableFilterItem.GetType().GetProperty("type", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tableFilterItem, null) as Type;
                    var tAtt = entityType.GetCustomAttribute<TenantAttribute>();
                    if ((tAtt != null && db.CurrentConnectionConfig.ConfigId.ToString() != tAtt.configId.ToString()) ||
                        (tAtt == null && db.CurrentConnectionConfig.ConfigId.ToString() != SqlSugarConst.MainConfigId))
                        continue;

                    tableFilterItems.Add(tableFilterItem);
                    db.QueryFilter.Add(tableFilterItem);
                }
            }
            ExpressionCache.Set(cacheKey, tableFilterItems);
        }
        else
        {
            tableFilterItemList.ForEach(u =>
            {
                db.QueryFilter.Add(u);
            });
        }
    }

    /// <summary>
    /// 读用户最大数据范围。缓存还没有时返回 0。
    /// </summary>
    /// <param name="cache">数据缓存</param>
    /// <param name="userId">用户 Id</param>
    /// <returns>最大数据范围</returns>
    private static int ReadMaxDataScope(ISimpXCache cache, long userId)
    {
        return cache.Get<UserDataScopeCache>(PermissionCacheKeys.UserDataScope, userId)?.MaxDataScope ?? 0;
    }

    /// <summary>
    /// 拼过滤表达式的内存缓存键。版本号变了，旧键就不再被读到。
    /// </summary>
    /// <param name="kind">表达式种类</param>
    /// <param name="dbConfigId">数据库配置 Id</param>
    /// <param name="userId">用户 Id</param>
    /// <returns>缓存键</returns>
    private static string ExpressionKey(string kind, string dbConfigId, string userId)
    {
        return $"{Volatile.Read(ref _expressionVersion)}:{kind}:{dbConfigId}:{userId}";
    }
}

/// <summary>
/// 自定义实体过滤器接口
/// </summary>
public interface IEntityFilter
{
    /// <summary>
    /// 实体过滤器
    /// </summary>
    /// <returns>要加到查询上的条件</returns>
    IEnumerable<TableFilterItem<object>> AddEntityFilter();
}
