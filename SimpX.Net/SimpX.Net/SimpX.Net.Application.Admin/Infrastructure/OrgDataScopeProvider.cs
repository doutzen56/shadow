namespace SimpX.Net.Application.Admin;

/// <summary>
/// 按后台机构和角色计算数据权限。<see cref="OrgAppService.GetUserOrgIdList()"/> 会把机构列表和最大数据范围写进同一条缓存，过滤器靠它判断“全部/仅本人”。
/// </summary>
public sealed class OrgDataScopeProvider : IDataScopeProvider, IScoped
{
    private readonly OrgAppService _orgAppService;

    /// <summary>
    /// 注入机构服务。
    /// </summary>
    /// <param name="orgAppService">机构服务</param>
    public OrgDataScopeProvider(OrgAppService orgAppService)
    {
        _orgAppService = orgAppService;
    }

    /// <summary>
    /// 获取当前用户可访问的机构 Id 集合。
    /// </summary>
    /// <returns>机构 Id 集合</returns>
    public Task<List<long>> GetCurrentUserOrgIdsAsync() => _orgAppService.GetUserOrgIdList();
}
