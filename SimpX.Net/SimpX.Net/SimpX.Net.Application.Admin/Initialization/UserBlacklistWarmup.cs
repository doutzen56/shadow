namespace SimpX.Net.Application.Admin;

/// <summary>
/// 启动时把已停用的用户写进用户黑名单。Redis 清空或第一次接上黑名单后，已停用账号不用等再次保存也能被拦住。
/// </summary>
public sealed class UserBlacklistWarmup : ICacheWarmup
{
    private readonly SqlSugarRepository<SysUser> _sysUserRep;
    private readonly ICacheStateStore _state;

    /// <summary>
    /// 注入用户仓储和状态缓存。
    /// </summary>
    /// <param name="sysUserRep">用户仓储</param>
    /// <param name="state">状态缓存</param>
    public UserBlacklistWarmup(SqlSugarRepository<SysUser> sysUserRep, ICacheStateStore state)
    {
        _sysUserRep = sysUserRep;
        _state = state;
    }

    /// <inheritdoc />
    public string CacheName => AuthCacheKeys.UserBlacklist;

    /// <inheritdoc />
    public bool RequiredOnStartup => true;

    /// <summary>
    /// 查出全部停用用户并写入黑名单。值只表示在名单里，不写姓名和手机号。
    /// 启动时没有登录用户，查询不会按租户裁剪，各租户的停用账号都会写入。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    public async Task WarmupAsync(CancellationToken cancellationToken = default)
    {
        // 先写再删一条探测键，确认状态缓存能写。停用名单为空时如果跳过写入，
        // Redis 挂了也会被当成预热成功，Admin 会带着坏缓存起来。
        const string probeId = "__warmup_probe__";
        _state.Add(AuthCacheKeys.UserBlacklist, probeId, "1");
        _state.Remove(AuthCacheKeys.UserBlacklist, probeId);

        var ids = await _sysUserRep.AsQueryable()
            .Where(u => u.Status == StatusEnum.Disable)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        foreach (var id in ids)
            _state.Add(AuthCacheKeys.UserBlacklist, id, "1");
    }
}
