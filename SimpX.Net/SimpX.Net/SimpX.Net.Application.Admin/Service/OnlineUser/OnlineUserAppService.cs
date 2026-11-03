namespace SimpX.Net.Application.Admin;

/// <summary>
/// 系统在线用户服务 🧩
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "onlineUser", Order = 300)]
public class OnlineUserAppService : IDynamicApiController, ITransient
{
    private readonly UserManager _userManager;
    private readonly ConfigAppService _configAppService;
    private readonly IHubInvocationBus _hub;
    private readonly SqlSugarRepository<SysOnlineUser> _sysOnlineUerRep;

    /// <summary>
    /// 注入在线用户查询、系统配置和集线器推送。强制下线和站内消息在 Redis 时会发到持有该连接的实例。
    /// </summary>
    /// <param name="configAppService">系统配置</param>
    /// <param name="hub">集线器推送</param>
    /// <param name="sysOnlineUerRep">在线用户仓储</param>
    /// <param name="userManager">当前用户</param>
    public OnlineUserAppService(ConfigAppService configAppService,
        IHubInvocationBus hub,
        SqlSugarRepository<SysOnlineUser> sysOnlineUerRep,
        UserManager userManager)
    {
        _userManager = userManager;
        _configAppService = configAppService;
        _hub = hub;
        _sysOnlineUerRep = sysOnlineUerRep;
    }

    /// <summary>
    /// 获取在线用户分页列表 🔖
    /// </summary>
    /// <returns></returns>
    [DisplayName("获取在线用户分页列表")]
    public async Task<SqlSugarPagedList<SysOnlineUser>> Page(PageOnlineUserInput input)
    {
        return await _sysOnlineUerRep.AsQueryable()
            .WhereIF(_userManager.SuperAdmin && input.TenantId > 0, u => u.TenantId == input.TenantId)
            .WhereIF(!string.IsNullOrWhiteSpace(input.UserName), u => u.UserName.Contains(input.UserName))
            .WhereIF(!string.IsNullOrWhiteSpace(input.RealName), u => u.RealName.Contains(input.RealName))
            .ToPagedListAsync(input.Page, input.PageSize);
    }

    /// <summary>
    /// 强制下线 🔖
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    [NonValidation]
    [DisplayName("强制下线")]
    public async Task ForceOffline(SysOnlineUser user)
    {
        await _hub.SendClientAsync(typeof(OnlineUserHub), user.ConnectionId ?? "", nameof(IOnlineUserHub.ForceOffline), "强制下线");
        await _sysOnlineUerRep.DeleteAsync(user);
    }

    /// <summary>
    /// 发布站内消息
    /// </summary>
    /// <param name="notice"></param>
    /// <param name="userIds"></param>
    /// <returns></returns>
    [NonAction]
    public async Task PublicNotice(SysNotice notice, List<long> userIds)
    {
        var userList = await _sysOnlineUerRep.GetListAsync(u => userIds.Contains(u.UserId));
        if (userList.Count == 0) return;

        foreach (var item in userList)
        {
            await _hub.SendClientAsync(typeof(OnlineUserHub), item.ConnectionId ?? "", nameof(IOnlineUserHub.PublicNotice), notice);
        }
    }

    /// <summary>
    /// 单用户登录
    /// </summary>
    /// <returns></returns>
    [NonAction]
    public async Task SingleLogin(long userId)
    {
        if (await _configAppService.GetConfigValue<bool>(ConfigConst.SysSingleLogin))
        {
            var users = await _sysOnlineUerRep.GetListAsync(u => u.UserId == userId);
            foreach (var user in users)
            {
                await ForceOffline(user);
            }
        }
    }

    /// <summary>
    /// 通过用户ID踢掉在线用户
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    [NonAction]
    public async Task ForceOffline(long userId)
    {
        var users = await _sysOnlineUerRep.GetListAsync(u => u.UserId == userId);
        foreach (var user in users)
        {
            await ForceOffline(user);
        }
    }
}