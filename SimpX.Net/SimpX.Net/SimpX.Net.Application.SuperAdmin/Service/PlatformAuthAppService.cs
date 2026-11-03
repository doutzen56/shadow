namespace SimpX.Net.Application.SuperAdmin;

/// <summary>
/// 总后台登录与验证码、当前用户。对外走 /api/platform/auth，实现在 Shared 的 AuthLoginService。
/// </summary>
[ApiDescriptionSettings(Module = "platform", Name = "auth", Order = 20)]
public class PlatformAuthAppService : IDynamicApiController, ITransient
{
    private readonly AuthLoginService _authLogin;
    private readonly SqlSugarRepository<SysUser> _sysUserRep;
    private readonly SqlSugarRepository<SysMenu> _sysMenuRep;
    private readonly UserManager _userManager;

    /// <summary>
    /// 注入登录服务、用户/菜单仓储与当前用户。
    /// </summary>
    /// <param name="authLogin">共享登录服务</param>
    /// <param name="sysUserRep">用户仓储</param>
    /// <param name="sysMenuRep">菜单仓储</param>
    /// <param name="userManager">当前登录用户</param>
    public PlatformAuthAppService(
        AuthLoginService authLogin,
        SqlSugarRepository<SysUser> sysUserRep,
        SqlSugarRepository<SysMenu> sysMenuRep,
        UserManager userManager)
    {
        _authLogin = authLogin;
        _sysUserRep = sysUserRep;
        _sysMenuRep = sysMenuRep;
        _userManager = userManager;
    }

    /// <summary>
    /// 获取图形验证码。
    /// </summary>
    /// <returns>验证码 Id、图片、过期秒数</returns>
    [AllowAnonymous]
    [HttpGet]
    [DisplayName("获取验证码")]
    public dynamic GetCaptcha()
    {
        EnsurePlatformHost();
        return _authLogin.GetCaptcha();
    }

    /// <summary>
    /// 总后台账号密码登录。
    /// </summary>
    /// <param name="input">账号、密码、验证码</param>
    /// <returns>登录令牌</returns>
    [AllowAnonymous]
    [HttpPost]
    [DisplayName("账号密码登录")]
    public Task<LoginOutput> Login([Required] LoginInput input)
    {
        EnsurePlatformHost();
        return _authLogin.Login(input);
    }

    /// <summary>
    /// 当前登录账号资料（给 Vue 壳装用户信息）。
    /// </summary>
    /// <returns>用户信息</returns>
    [DisplayName("获取登录账号")]
    public async Task<PlatformLoginUserOutput> GetUserInfo()
    {
        EnsurePlatformHost();
        var user = await _sysUserRep.AsQueryable().ClearFilter()
            .FirstAsync(u => u.Id == _userManager.UserId)
            ?? throw Oops.Oh(ErrorCodeEnum.D1011).StatusCode(401);

        var org = await _sysUserRep.ChangeRepository<SqlSugarRepository<SysOrg>>()
            .GetFirstAsync(u => u.Id == user.OrgId);
        var pos = await _sysUserRep.ChangeRepository<SqlSugarRepository<SysPos>>()
            .GetFirstAsync(u => u.Id == user.PosId);
        var roleIds = await _sysUserRep.ChangeRepository<SqlSugarRepository<SysUserRole>>()
            .AsQueryable()
            .Where(u => u.UserId == user.Id)
            .Select(u => u.RoleId)
            .ToListAsync();

        // 超管：启用状态下的全部按钮权限码；否则先回空，后续再按角色收窄。
        var buttons = _userManager.SuperAdmin
            ? await _sysMenuRep.AsQueryable().ClearFilter()
                .Where(u => u.Type == MenuTypeEnum.Btn && u.Status == StatusEnum.Enable && u.Permission != null && u.Permission != "")
                .Select(u => u.Permission)
                .ToListAsync()
            : [];

        return new PlatformLoginUserOutput
        {
            Id = user.Id,
            Account = user.Account,
            RealName = user.RealName,
            Phone = user.Phone,
            IdCardNum = user.IdCardNum,
            Email = user.Email,
            AccountType = user.AccountType,
            Avatar = user.Avatar,
            Address = user.Address,
            Signature = user.Signature,
            OrgId = user.OrgId,
            OrgName = org?.Name,
            OrgType = org?.Type,
            PosName = pos?.Name,
            Buttons = buttons,
            RoleIds = roleIds,
            TenantId = user.TenantId,
            CurrentTenantId = user.TenantId,
            LangCode = user.LangCode,
            WatermarkText = string.Empty,
        };
    }

    /// <summary>
    /// 退出登录（前端清令牌即可；接口占位避免 404）。
    /// </summary>
    [HttpPost]
    [DisplayName("退出登录")]
    public void Logout()
    {
        EnsurePlatformHost();
    }

    /// <summary>
    /// 只允许在总后台进程调用本接口。
    /// </summary>
    private static void EnsurePlatformHost()
    {
        if (!HostTenantMode.IsPlatformHost)
            throw Oops.Oh(ErrorCodeEnum.SA001);
    }
}
