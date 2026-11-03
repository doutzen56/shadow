using Furion.SpecificationDocument;
using Lazy.Captcha.Core;
using NewLife.Reflection;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 系统登录授权服务 🧩
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "auth", Order = 500)]
public class AuthAppService : IDynamicApiController, ITransient
{
    private readonly UserManager _userManager;
    private readonly SqlSugarRepository<SysUser> _sysUserRep;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly MenuAppService _menuAppService;
    private readonly OnlineUserAppService _onlineUserAppService;
    private readonly ConfigAppService _configAppService;
    private readonly UserAppService _userAppService;
    private readonly TenantAppService _tenantAppService;
    private readonly LdapAppService _ldapAppService;
    private readonly ICaptcha _captcha;
    private readonly IEventPublisher _eventPublisher;
    private readonly ISimpXCache _cache;
    private readonly ICacheStateStore _state;
    private readonly AuthLoginService _authLogin;

    public AuthAppService(
        SqlSugarRepository<SysUser> sysUserRep,
        IHttpContextAccessor httpContextAccessor,
        OnlineUserAppService onlineUserAppService,
        ConfigAppService configAppService,
        LdapAppService ldapAppService,
        IEventPublisher eventPublisher,
        ISimpXCache cache,
        ICacheStateStore state,
        MenuAppService menuAppService,
        UserAppService userAppService,
        TenantAppService tenantAppService,
        UserManager userManager,
        ICaptcha captcha,
        AuthLoginService authLogin)
    {
        _captcha = captcha;
        _sysUserRep = sysUserRep;
        _userManager = userManager;
        _eventPublisher = eventPublisher;
        _userAppService = userAppService;
        _tenantAppService = tenantAppService;
        _menuAppService = menuAppService;
        _cache = cache;
        _state = state;
        _configAppService = configAppService;
        _httpContextAccessor = httpContextAccessor;
        _onlineUserAppService = onlineUserAppService;
        _ldapAppService = ldapAppService;
        _authLogin = authLogin;
    }

    /// <summary>
    /// 账号密码登录。
    /// </summary>
    /// <param name="input">账号、密码、验证码</param>
    /// <returns>登录令牌</returns>
    /// <remarks>用户名/密码：superadmin/123456</remarks>
    [AllowAnonymous]
    [DisplayName("账号密码登录")]
    public virtual Task<LoginOutput> Login([Required] LoginInput input) => _authLogin.Login(input);

    /// <summary>
    /// 账号密码登录。skipCaptcha 为 true 时不校验图形验证码，只给 Swagger 登录用，不对外做接口。
    /// </summary>
    /// <param name="input">账号、密码、验证码</param>
    /// <param name="skipCaptcha">为 true 时跳过图形验证码</param>
    /// <returns>登录令牌</returns>
    /// <exception cref="AppFriendlyException">账号、密码或验证码不对，或账号被停用</exception>
    [NonAction]
    public virtual Task<LoginOutput> Login([Required] LoginInput input, bool skipCaptcha)
        => _authLogin.Login(input, skipCaptcha);

    /// <summary>
    /// 获取登录租户和用户
    /// </summary>
    /// <param name="tenantId">租户 Id；租户站由域名解析得到</param>
    /// <param name="account">账号</param>
    /// <param name="phone">手机号</param>
    /// <returns>租户与用户</returns>
    [NonAction]
    public async Task<(SysTenant tenant, SysUser user)> GetLoginUserAndTenant(long? tenantId, string account = null, string phone = null)
    {
        // 账号是否存在
        var user = await _sysUserRep.AsQueryable().Includes(u => u.SysOrg).ClearFilter()
            .WhereIF(tenantId > 0, u => (u.AccountType == AccountTypeEnum.SuperAdmin || u.TenantId == tenantId))
            .WhereIF(!string.IsNullOrWhiteSpace(account), u => u.Account.Equals(account))
            .WhereIF(!string.IsNullOrWhiteSpace(phone), u => u.Phone.Equals(phone)).FirstAsync();
        _ = user ?? throw Oops.Oh(ErrorCodeEnum.D1000);

        // 租户是否存在或已禁用
        var tenant = await _sysUserRep.ChangeRepository<SqlSugarRepository<SysTenant>>().AsQueryable()
            .WhereIF(tenantId > 0, u => u.Id == tenantId).WhereIF(tenantId.ToLong() == 0, u => u.Id == user.TenantId).FirstAsync();
        if (tenant?.Status != StatusEnum.Enable) throw Oops.Oh(ErrorCodeEnum.Z1003);

        // 总后台账号代进某租户时，会话挂到所选租户
        if (tenantId > 0 && user.AccountType == AccountTypeEnum.SuperAdmin)
            user.TenantId = tenantId;

        return (tenant, user);
    }

    /// <summary>
    /// 验证用户密码
    /// </summary>
    /// <param name="password">前端传来的密码</param>
    /// <param name="account">登录账号，用来记错误次数</param>
    /// <param name="user">库里的用户</param>
    /// <exception cref="AppFriendlyException">密码不对</exception>
    private void VerifyPassword(string password, string account, SysUser user)
    {
        try
        {
            // 国密SM2解密（前端密码传输SM2加密后的）
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

    /// <summary>
    /// 读出这个账号已经输错密码多少次。没有记录就是 0。
    /// </summary>
    /// <param name="account">登录账号</param>
    /// <returns>错误次数</returns>
    private long GetPasswordErrorTimes(string account)
        => _state.Get<long>(AuthCacheKeys.PasswordError, account);

    /// <summary>
    /// 密码又错了一次；首次计数起 30 分钟后过期。
    /// </summary>
    /// <param name="account">登录账号</param>
    private void IncreasePasswordErrorTimes(string account)
        => _state.Increment(AuthCacheKeys.PasswordError, account, TimeSpan.FromMinutes(30));

    /// <summary>
    /// 登录成功或管理员解锁后，清掉密码错误次数。
    /// </summary>
    /// <param name="account">登录账号</param>
    private void ClearPasswordErrorTimes(string account)
        => _state.Remove(AuthCacheKeys.PasswordError, account);

    /// <summary>
    /// 验证锁屏密码 🔖
    /// </summary>
    /// <param name="password"></param>
    /// <returns></returns>
    [DisplayName("验证锁屏密码")]
    public virtual async Task<bool> UnLockScreen([Required, FromQuery] string password)
    {
        // 账号是否存在
        var user = await _sysUserRep.GetFirstAsync(u => u.Id == _userManager.UserId);
        _ = user ?? throw Oops.Oh(ErrorCodeEnum.D0009);

        // 是否开启域登录验证
        if (await _configAppService.GetConfigValue<bool>(ConfigConst.SysDomainLogin))
        {
            var userLdap = await _sysUserRep.ChangeRepository<SqlSugarRepository<SysUserLdap>>().GetFirstAsync(u => u.UserId == user.Id && u.TenantId == user.TenantId);
            if (userLdap == null)
            {
                VerifyPassword(password, user.Account, user);
            }
            else if (!await _ldapAppService.AuthAccount(user.TenantId!.Value, userLdap.Account, CryptogramUtil.Decrypt(password)))
            {
                IncreasePasswordErrorTimes(user.Account);
                throw Oops.Oh(ErrorCodeEnum.D1000);
            }
        }
        else
            VerifyPassword(password, user.Account, user);

        return true;
    }

    /// <summary>
    /// 手机号登录 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    // 入参带手机号和短信验证码，记请求日志会把它们明文写进去
    [SuppressMonitor]
    [AllowAnonymous]
    [DisplayName("手机号登录")]
    public virtual Task<LoginOutput> LoginPhone([Required] LoginPhoneInput input)
        => _authLogin.LoginPhone(input);

    /// <summary>
    /// 生成Token令牌 🔖
    /// </summary>
    /// <param name="user">登录用户</param>
    /// <param name="sysUserEventTypeEnum">记一笔什么登录事件</param>
    /// <param name="skipSingleLogin">为 true 时不踢掉这个用户已有的登录</param>
    /// <returns>访问令牌和刷新令牌</returns>
    [NonAction]
    internal virtual async Task<LoginOutput> CreateToken(SysUser user, SysUserEventTypeEnum sysUserEventTypeEnum = SysUserEventTypeEnum.Login, bool skipSingleLogin = false)
    {
        if (!skipSingleLogin && sysUserEventTypeEnum != SysUserEventTypeEnum.RefreshToken)
        {
            // 单用户登录
            await _onlineUserAppService.SingleLogin(user.Id);
        }

        // 生成Token令牌
        var tokenExpire = await _configAppService.GetTokenExpire();
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

        // 生成刷新Token令牌
        var refreshTokenExpire = await _configAppService.GetRefreshTokenExpire();
        var refreshToken = JWTEncryption.GenerateRefreshToken(accessToken, refreshTokenExpire);

        // 设置响应报文头
        _httpContextAccessor.HttpContext.SetTokensOfResponseHeaders(accessToken, refreshToken);

        // Swagger Knife4UI-AfterScript登录脚本
        // ke.global.setAllHeader('Authorization', 'Bearer ' + ke.response.headers['access-token']);

        // 更新用户登录信息
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

        // 发布系统用户操作事件
        await _eventPublisher.PublishAsync(sysUserEventTypeEnum, payload);
        return payload.Output;
    }

    /// <summary>
    /// 获取登录账号 🔖
    /// </summary>
    /// <returns></returns>
    [DisplayName("获取登录账号")]
    public virtual async Task<LoginUserOutput> GetUserInfo()
    {
        var user = await _sysUserRep.AsQueryable().ClearFilter().FirstAsync(u => u.Id == _userManager.UserId) ?? throw Oops.Oh(ErrorCodeEnum.D1011).StatusCode(401);
        // 获取机构
        var org = await _sysUserRep.ChangeRepository<SqlSugarRepository<SysOrg>>().GetFirstAsync(u => u.Id == user.OrgId);
        // 获取职位
        var pos = await _sysUserRep.ChangeRepository<SqlSugarRepository<SysPos>>().GetFirstAsync(u => u.Id == user.PosId);
        // 获取按钮集合
        var buttons = await _menuAppService.GetOwnBtnPermList();
        // 获取角色集合
        var roleIds = await _sysUserRep.ChangeRepository<SqlSugarRepository<SysUserRole>>().AsQueryable()
            .Where(u => u.UserId == user.Id).Select(u => u.RoleId).ToListAsync();
        // 获取水印文字（若系统水印为空则全局为空）
        var watermarkText = (await _sysUserRep.Context.Queryable<SysTenant>().FirstAsync(u => u.Id == user.TenantId))?.Watermark;
        if (!string.IsNullOrWhiteSpace(watermarkText)) watermarkText += $"-{user.RealName}";
        var loginUser = new LoginUserOutput
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
            WatermarkText = watermarkText,
            LangCode = user.LangCode,
        };

        //将登录信息中的当前租户id，更新为当前所切换到的租户
        long? currentTenantId = App.User.FindFirst(ClaimConst.TenantId)?.Value?.ToLong(0);
        loginUser.CurrentTenantId = currentTenantId > 0 ? currentTenantId : user.TenantId;

        return loginUser;
    }

    /// <summary>
    /// 获取刷新Token 🔖
    /// </summary>
    /// <param name="accessToken">旧的AccessToken</param>
    /// <returns>新的AccessToken和RefreshToken</returns>
    [DisplayName("获取刷新Token")]
    public virtual async Task<LoginOutput> GetRefreshToken([FromQuery] string accessToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) throw Oops.Oh(ErrorCodeEnum.D1016);

        if (string.IsNullOrWhiteSpace(accessToken)) throw Oops.Oh(ErrorCodeEnum.D1011);

        if (string.IsNullOrWhiteSpace(_userManager.Account)) throw Oops.Oh(ErrorCodeEnum.D1011);

        // 黑名单校验
        if (_state.Exists(AuthCacheKeys.TokenBlacklist, CacheIdHasher.Sha256(accessToken))) throw Oops.Oh(ErrorCodeEnum.D1011);

        // 解析Token
        var (isValid, tokenData, validationResult) = JWTEncryption.Validate(accessToken);
        if (!isValid) throw Oops.Oh(ErrorCodeEnum.D1016);

        // 获取用户Id
        var user = await _sysUserRep.AsQueryable().ClearFilter().FirstAsync(u => u.Id == _userManager.UserId) ?? throw Oops.Oh(ErrorCodeEnum.D1011).StatusCode(401);
        return await CreateToken(user, SysUserEventTypeEnum.RefreshToken);
    }

    /// <summary>
    /// 退出系统 🔖
    /// </summary>
    [DisplayName("退出系统")]
    public async Task Logout()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) throw Oops.Oh(ErrorCodeEnum.D1016);

        var token = httpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

        if (string.IsNullOrWhiteSpace(token))
            throw Oops.Oh(ErrorCodeEnum.D1011);

        if (string.IsNullOrWhiteSpace(_userManager.Account))
            throw Oops.Oh(ErrorCodeEnum.D1011);

        // 令牌黑名单只存摘要，不存原文；过期时间跟令牌剩余有效期一致，避免永久堆积。
        var tokenId = CacheIdHasher.Sha256(token);
        _state.Add(AuthCacheKeys.TokenBlacklist, tokenId, "1", GetAccessTokenRemaining(token));

        // 发布登出事件（用户退出）
        var user = await _sysUserRep.GetByIdAsync(_userManager.UserId);
        await _eventPublisher.PublishAsync(SysUserEventTypeEnum.LoginOut, new { Entity = user });

        // 清除 Swagger 登录信息
        httpContext.SignoutToSwagger();
    }

    /// <summary>
    /// 令牌还剩多久过期。黑名单定义本身不过期，退出时按这个时间补上，避免黑名单永久堆积。
    /// 解不出来或已经过期时按 1 分钟，避免写成永久键。
    /// </summary>
    /// <param name="token">访问令牌，不含 Bearer 前缀</param>
    /// <returns>剩余时间，至少 1 分钟</returns>
    private static TimeSpan GetAccessTokenRemaining(string token)
    {
        try
        {
            var jwt = JWTEncryption.SecurityReadJwtToken(token);
            var remain = jwt.ValidTo.ToUniversalTime() - DateTime.UtcNow;
            if (remain > TimeSpan.Zero)
                return remain;
        }
        catch (Exception ex)
        {
            Log.Warning($"退出时解析令牌有效期失败，黑名单按 1 分钟过期：{ex.Message}");
        }

        return TimeSpan.FromMinutes(1);
    }

    /// <summary>
    /// 获取验证码 🔖
    /// </summary>
    /// <returns></returns>
    [AllowAnonymous]
    [SuppressMonitor]
    [DisplayName("获取验证码")]
    public dynamic GetCaptcha() => _authLogin.GetCaptcha();

    /// <summary>
    /// 用户注册 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [UnitOfWork]
    [AllowAnonymous]
    [HttpPost, ApiDescriptionSettings(Description = "用户注册", DisableInherite = true)]
    public async Task UserRegistration(UserRegistrationInput input)
    {
        // 校验验证码
        if (!_captcha.Validate(input.CodeId.ToString(), input.Code)) throw Oops.Oh(ErrorCodeEnum.D0008);
        _captcha.Generate(input.CodeId.ToString());

        // 登录时隐藏租户，查找对应租户信息
        input.TenantId = input.TenantId <= 0 ? (await _tenantAppService.GetCurrentTenantSysInfo()).Id : input.TenantId;

        // 判断租户是否有效且启用注册功能
        var tenant = await _sysUserRep.Context.Queryable<SysTenant>().FirstAsync(u => u.Id == input.TenantId && u.Status == StatusEnum.Enable);
        if (tenant?.EnableReg != YesNoEnum.Y) throw Oops.Oh(ErrorCodeEnum.D1034);

        // 查找注册方案
        var wayId = input.WayId <= 0 ? tenant.RegWayId : input.WayId;
        var regWay = await _sysUserRep.Context.Queryable<SysUserRegWay>().FirstAsync(u => u.Id == wayId) ?? throw Oops.Oh(ErrorCodeEnum.D1035);

        var addUserInput = new AddUserInput
        {
            AccountType = regWay.AccountType,
            NickName = "注册用户-" + input.Account,
            OrgId = regWay.OrgId,
            PosId = regWay.PosId,
            TenantId = input.TenantId,
            RoleIdList = new List<long> { regWay.RoleId },
        };
        addUserInput.Copy(input);
        await _userAppService.RegisterUser(addUserInput);
    }

    /// <summary>
    /// Swagger登录检查 🔖
    /// </summary>
    /// <returns></returns>
    [AllowAnonymous]
    [HttpPost("/api/swagger/checkUrl"), NonUnify]
    [ApiDescriptionSettings(Description = "Swagger登录检查", DisableInherite = true)]
    public int SwaggerCheckUrl()
    {
        return _httpContextAccessor.HttpContext.User.Identity.IsAuthenticated ? 200 : 401;
    }

    /// <summary>
    /// Swagger登录提交 🔖
    /// </summary>
    /// <param name="auth"></param>
    /// <returns></returns>
    [AllowAnonymous]
    [HttpPost("/api/swagger/submitUrl"), NonUnify]
    [ApiDescriptionSettings(Description = "Swagger登录提交", DisableInherite = true)]
    public async Task<int> SwaggerSubmitUrl([FromForm] SpecificationAuth auth)
    {
        try
        {
            await Login(new LoginInput
            {
                Account = auth.UserName,
                Password = CryptogramUtil.SM2Encrypt(auth.Password)
            }, skipCaptcha: true);

            return 200;
        }
        catch (Exception)
        {
            return 401;
        }
    }
}