namespace SimpX.Net.Application.Shared;

/// <summary>
/// 账号密码登录与验证码（无 DynamicApi）。总后台与租户后台共用实现。
/// 不挂 ITransient：Channels/Schedule 等无验证码宿主也会扫到 Shared，自动注册会因缺 ICaptcha 起不来。
/// 由 <see cref="SharedServiceRegistration"/> 在 Admin/SuperAdmin 显式注册。
/// </summary>
public sealed class AuthLoginService
{
    private readonly SqlSugarRepository<SysUser> _sysUserRep;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly OnlineUserSessionService _onlineUserSession;
    private readonly SysConfigReader _configReader;
    private readonly ICaptcha _captcha;
    private readonly IEventPublisher _eventPublisher;
    private readonly ISimpXCache _cache;
    private readonly ICacheStateStore _state;
    private readonly ICurrentTenantAccessor _currentTenant;

    /// <summary>
    /// 注入登录所需仓储、配置、验证码、缓存和当前租户。
    /// </summary>
    public AuthLoginService(
        SqlSugarRepository<SysUser> sysUserRep,
        IHttpContextAccessor httpContextAccessor,
        OnlineUserSessionService onlineUserSession,
        SysConfigReader configReader,
        IEventPublisher eventPublisher,
        ISimpXCache cache,
        ICacheStateStore state,
        ICaptcha captcha,
        ICurrentTenantAccessor currentTenant)
    {
        _sysUserRep = sysUserRep;
        _httpContextAccessor = httpContextAccessor;
        _onlineUserSession = onlineUserSession;
        _configReader = configReader;
        _eventPublisher = eventPublisher;
        _cache = cache;
        _state = state;
        _captcha = captcha;
        _currentTenant = currentTenant;
    }

    /// <summary>
    /// 获取图形验证码。
    /// </summary>
    /// <returns>验证码 Id、图片、过期秒数</returns>
    public dynamic GetCaptcha()
    {
        var codeId = YitIdHelper.NextId().ToString();
        var captcha = _captcha.Generate(codeId);
        var expirySeconds = App.GetOptions<CaptchaOptions>()?.ExpirySeconds ?? 60;
        return new { Id = codeId, Img = captcha.Base64, ExpirySeconds = expirySeconds };
    }

    /// <summary>
    /// 账号密码登录。
    /// </summary>
    /// <param name="input">账号、密码、验证码</param>
    /// <param name="skipCaptcha">为 true 时跳过图形验证码</param>
    /// <returns>登录令牌</returns>
    public async Task<LoginOutput> Login(LoginInput input, bool skipCaptcha = false)
    {
        var passwordErrorTimes = GetPasswordErrorTimes(input.Account);
        var passwordMaxErrorTimes = await _configReader.GetConfigValue<int>(ConfigConst.SysPasswordMaxErrorTimes);
        if (passwordMaxErrorTimes < 1) passwordMaxErrorTimes = 5;
        if (passwordErrorTimes > passwordMaxErrorTimes) throw Oops.Oh(ErrorCodeEnum.D1027);

        if (!skipCaptcha && await _configReader.GetConfigValue<bool>(ConfigConst.SysCaptcha)
            && !_captcha.Validate(input.CodeId.ToString(), input.Code))
            throw Oops.Oh(ErrorCodeEnum.D0008);

        var tenantId = ResolveLoginTenantId(input.TenantId);
        var (tenant, user) = await GetLoginUserAndTenant(tenantId, account: input.Account);
        EnsureAccountAllowedOnThisHost(user);

        if (user.Status == StatusEnum.Disable) throw Oops.Oh(ErrorCodeEnum.D1017);

        if (await _configReader.GetConfigValue<bool>(ConfigConst.SysDomainLogin))
        {
            var userLdap = await _sysUserRep.ChangeRepository<SqlSugarRepository<SysUserLdap>>()
                .GetFirstAsync(u => u.UserId == user.Id && u.TenantId == tenant.Id);
            if (userLdap == null)
            {
                VerifyPassword(input.Password, input.Account, user);
            }
            else
            {
                // Admin 宿主会注册域账号校验；总后台未注册时有域绑定就直接失败
                var ldap = App.GetService<ILdapAccountAuthenticator>();
                if (ldap == null || !await ldap.AuthAccount(tenant.Id, userLdap.Account, CryptogramUtil.Decrypt(input.Password)))
                {
                    IncreasePasswordErrorTimes(input.Account);
                    throw Oops.Oh(ErrorCodeEnum.D1000);
                }
            }
        }
        else
            VerifyPassword(input.Password, input.Account, user);
        ClearPasswordErrorTimes(input.Account);
        _cache.Remove(PermissionCacheKeys.UserButton, user.Id);
        return await CreateToken(user);
    }

    /// <summary>
    /// 手机号登录。先校验短信验证码，再按当前进程的租户规则签发令牌。
    /// </summary>
    /// <param name="input">手机号、验证码、租户 Id（总后台用）</param>
    /// <returns>登录令牌</returns>
    public async Task<LoginOutput> LoginPhone(LoginPhoneInput input)
    {
        var sms = App.GetService<ISmsCodeVerifier>();
        if (sms == null)
            throw Oops.Oh("当前宿主未开通短信验证码校验，无法手机号登录");
        sms.VerifyPhoneCode(input.Phone, input.Code);

        var tenantId = ResolveLoginTenantId(input.TenantId);
        var (_, user) = await GetLoginUserAndTenant(tenantId, phone: input.Phone);
        EnsureAccountAllowedOnThisHost(user);

        if (user.Status == StatusEnum.Disable) throw Oops.Oh(ErrorCodeEnum.D1017);

        return await CreateToken(user);
    }

    /// <summary>
    /// 取登录用的租户 Id。
    /// </summary>
    private long? ResolveLoginTenantId(long? inputTenantId)
    {
        if (HostTenantMode.ResolveByHost)
            return _currentTenant.RequireTenantId();
        return inputTenantId;
    }

    /// <summary>
    /// 账号类型必须匹配当前宿主。
    /// </summary>
    private static void EnsureAccountAllowedOnThisHost(SysUser user)
    {
        if (HostTenantMode.ResolveByHost && user.AccountType == AccountTypeEnum.SuperAdmin)
            throw Oops.Oh(ErrorCodeEnum.Z1006);
        if (HostTenantMode.IsPlatformHost && user.AccountType != AccountTypeEnum.SuperAdmin)
            throw Oops.Oh(ErrorCodeEnum.SA001);
    }

    /// <summary>
    /// 获取登录租户和用户。
    /// </summary>
    public async Task<(SysTenant tenant, SysUser user)> GetLoginUserAndTenant(long? tenantId, string account = null, string phone = null)
    {
        var user = await _sysUserRep.AsQueryable().Includes(u => u.SysOrg).ClearFilter()
            .WhereIF(tenantId > 0, u => (u.AccountType == AccountTypeEnum.SuperAdmin || u.TenantId == tenantId))
            .WhereIF(!string.IsNullOrWhiteSpace(account), u => u.Account.Equals(account))
            .WhereIF(!string.IsNullOrWhiteSpace(phone), u => u.Phone.Equals(phone)).FirstAsync();
        _ = user ?? throw Oops.Oh(ErrorCodeEnum.D1000);

        var tenant = await _sysUserRep.ChangeRepository<SqlSugarRepository<SysTenant>>().AsQueryable()
            .WhereIF(tenantId > 0, u => u.Id == tenantId)
            .WhereIF(tenantId.ToLong() == 0, u => u.Id == user.TenantId).FirstAsync();
        if (tenant?.Status != StatusEnum.Enable) throw Oops.Oh(ErrorCodeEnum.Z1003);

        if (tenantId > 0 && user.AccountType == AccountTypeEnum.SuperAdmin)
            user.TenantId = tenantId;

        return (tenant, user);
    }

    /// <summary>
    /// 验证用户密码。
    /// </summary>
    private void VerifyPassword(string password, string account, SysUser user)
    {
        try
        {
            password = CryptogramUtil.SM2Decrypt(password);
            if (CryptogramUtil.CryptoType == CryptogramEnum.MD5.ToString())
            {
                if (user.Password.Equals(MD5Encryption.Encrypt(password))) return;
            }
            else
            {
                if (CryptogramUtil.Decrypt(user.Password).Equals(password)) return;
            }
        }
        catch (Exception ex)
        {
            Log.Error("用户密码验证异常：", ex);
        }

        IncreasePasswordErrorTimes(account);
        throw Oops.Oh(ErrorCodeEnum.D1000);
    }

    private long GetPasswordErrorTimes(string account)
        => _state.Get<long>(AuthCacheKeys.PasswordError, account);

    private void IncreasePasswordErrorTimes(string account)
        => _state.Increment(AuthCacheKeys.PasswordError, account, TimeSpan.FromMinutes(30));

    private void ClearPasswordErrorTimes(string account)
        => _state.Remove(AuthCacheKeys.PasswordError, account);

    /// <summary>
    /// 生成访问令牌与刷新令牌。
    /// </summary>
    public async Task<LoginOutput> CreateToken(SysUser user, SysUserEventTypeEnum sysUserEventTypeEnum = SysUserEventTypeEnum.Login, bool skipSingleLogin = false)
    {
        if (!skipSingleLogin && sysUserEventTypeEnum != SysUserEventTypeEnum.RefreshToken)
            await _onlineUserSession.SingleLogin(user.Id);

        var tokenExpire = await _configReader.GetTokenExpire();
        var accessToken = JWTEncryption.Encrypt(new Dictionary<string, object>
        {
            { ClaimConst.UserId, user.Id },
            { ClaimConst.TenantId, user.TenantId },
            { ClaimConst.Account, user.Account },
            { ClaimConst.RealName, user.RealName },
            { ClaimConst.AccountType, user.AccountType },
            { ClaimConst.OrgId, user.OrgId },
            { ClaimConst.OrgName, user.SysOrg?.Name },
            { ClaimConst.OrgType, user.SysOrg?.Type },
            { ClaimConst.LangCode, user.LangCode }
        }, tokenExpire);

        var refreshTokenExpire = await _configReader.GetRefreshTokenExpire();
        var refreshToken = JWTEncryption.GenerateRefreshToken(accessToken, refreshTokenExpire);
        _httpContextAccessor.HttpContext.SetTokensOfResponseHeaders(accessToken, refreshToken);

        user.LastLoginIp = _httpContextAccessor.HttpContext.GetRemoteIpAddressToIPv4(true);
        (user.LastLoginAddress, double? longitude, double? latitude) = CommonUtil.GetIpAddress(user.LastLoginIp);
        user.LastLoginTime = DateTime.Now;
        user.LastLoginDevice = CommonUtil.GetClientDeviceInfo(_httpContextAccessor.HttpContext?.Request?.Headers?.UserAgent);
        await _sysUserRep.AsUpdateable(user).UpdateColumns(u => new
        {
            u.LastLoginIp,
            u.LastLoginAddress,
            u.LastLoginTime,
            u.LastLoginDevice,
        }).ExecuteCommandAsync();

        var payload = new
        {
            Entity = user,
            Output = new LoginOutput
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                Homepage = user.Homepage
            }
        };
        await _eventPublisher.PublishAsync(sysUserEventTypeEnum, payload);
        return payload.Output;
    }
}
