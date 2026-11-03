namespace SimpX.Net.Data;

/// <summary>
/// 数据权限来源：当前登录用户能看哪些机构的数据。由管理后台实现（机构、角色数据在后台）。
/// 实现时要顺带把用户的机构列表和最大数据范围写进缓存（PermissionCacheKeys.UserDataScope），数据权限过滤器靠它判断“全部/仅本人”。
/// 宿主没注册实现时，数据权限过滤不生效（这些宿主本来就没有后台的机构和角色数据）。
/// </summary>
public interface IDataScopeProvider
{
    /// <summary>
    /// 获取当前用户可访问的机构 Id 集合。
    /// </summary>
    /// <returns>机构 Id 集合；空集合表示不按机构过滤</returns>
    Task<List<long>> GetCurrentUserOrgIdsAsync();
}
