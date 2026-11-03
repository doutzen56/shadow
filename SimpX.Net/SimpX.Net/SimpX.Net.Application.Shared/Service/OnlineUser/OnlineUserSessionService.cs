namespace SimpX.Net.Application.Shared;

/// <summary>
/// 登录会话：单点登录踢人（无 DynamicApi）。总后台进程只清在线记录；租户后台完整踢人仍走 OnlineUserAppService。
/// </summary>
public sealed class OnlineUserSessionService : ITransient
{
    private readonly SysConfigReader _configReader;
    private readonly SqlSugarRepository<SysOnlineUser> _sysOnlineUserRep;

    /// <summary>
    /// 注入配置和在线用户仓储。
    /// </summary>
    /// <param name="configReader">配置读取器</param>
    /// <param name="sysOnlineUserRep">在线用户仓储</param>
    public OnlineUserSessionService(
        SysConfigReader configReader,
        SqlSugarRepository<SysOnlineUser> sysOnlineUserRep)
    {
        _configReader = configReader;
        _sysOnlineUserRep = sysOnlineUserRep;
    }

    /// <summary>
    /// 开启单点登录时，清掉该用户已有在线记录。
    /// </summary>
    /// <param name="userId">用户 Id</param>
    public async Task SingleLogin(long userId)
    {
        if (!await _configReader.GetConfigValue<bool>(ConfigConst.SysSingleLogin))
            return;

        await _sysOnlineUserRep.DeleteAsync(u => u.UserId == userId);
    }
}
