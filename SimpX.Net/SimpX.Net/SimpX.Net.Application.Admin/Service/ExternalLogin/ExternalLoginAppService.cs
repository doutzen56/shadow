using System.Security.Cryptography;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 第三方登录（路由 /api/system/externalLogin/*）：发起授权、处理回调、绑定或自动创建后台用户，最后带令牌跳回前端登录页。
/// 具体平台由 <see cref="IExternalLoginProvider"/> 实现，本服务不关心是哪家。
/// </summary>
[AllowAnonymous]
[ApiDescriptionSettings("Default", Module = "system", Name = "externalLogin", Order = 498, Description = "第三方登录")]
public class ExternalLoginAppService : IDynamicApiController, ITransient
{
    /// <summary>根组织，自动创建的用户挂在这里。</summary>
    private const long DefaultOrgId = 1300000000101;

    /// <summary>“仅本人数据”角色，自动创建的用户默认只能看自己的数据。</summary>
    private const long SelfDataRoleId = 1300000000104;

    private readonly IEnumerable<IExternalLoginProvider> _providers;
    private readonly IOptionsMonitor<ExternalLoginOptions> _options;
    private readonly ICacheStateStore _state;
    private readonly SqlSugarRepository<SysUserExternalLogin> _bindingRep;
    private readonly UserAppService _userAppService;
    private readonly AuthAppService _authAppService;

    /// <summary>
    /// 注入已注册的提供方、配置、缓存、绑定仓储，以及建用户、签令牌用的后台服务。
    /// </summary>
    /// <param name="providers">已注册的第三方登录提供方</param>
    /// <param name="options">第三方登录配置</param>
    /// <param name="state">状态缓存，防伪 state 用一次就删</param>
    /// <param name="bindingRep">第三方账号绑定仓储</param>
    /// <param name="userAppService">用户服务</param>
    /// <param name="authAppService">登录服务</param>
    public ExternalLoginAppService(IEnumerable<IExternalLoginProvider> providers, IOptionsMonitor<ExternalLoginOptions> options,
        ICacheStateStore state, SqlSugarRepository<SysUserExternalLogin> bindingRep, UserAppService userAppService, AuthAppService authAppService)
    {
        _providers = providers;
        _options = options;
        _state = state;
        _bindingRep = bindingRep;
        _userAppService = userAppService;
        _authAppService = authAppService;
    }

    /// <summary>
    /// 发起第三方登录：校验提供方和跳回地址，生成一次性 state，跳到第三方授权页。
    /// </summary>
    /// <param name="provider">提供方名称</param>
    /// <param name="redirectUrl">登录完成后跳回的前端地址</param>
    /// <returns>跳转结果</returns>
    /// <exception cref="AppFriendlyException">提供方未注册或跳回地址不在白名单</exception>
    [HttpGet]
    [DisplayName("发起第三方登录")]
    public async Task<IActionResult> SignIn([FromQuery] string provider, [FromQuery] string redirectUrl)
    {
        var loginProvider = ResolveProvider(provider);
        EnsureRedirectAllowed(redirectUrl);

        var state = Guid.NewGuid().ToString("N");
        _state.Add(AuthCacheKeys.ExternalLogin, state, new ExternalLoginState { Provider = loginProvider.Name, RedirectUrl = redirectUrl }, TimeSpan.FromMinutes(10));

        var authorizeUrl = await loginProvider.BuildAuthorizeUrlAsync(BuildCallbackUrl(loginProvider.Name), state);
        return new RedirectResult(authorizeUrl);
    }

    /// <summary>
    /// 第三方授权回调：核对 state（只能用一次），换取用户信息，找到或创建绑定的后台用户，带访问令牌跳回前端登录页。
    /// </summary>
    /// <param name="provider">提供方名称</param>
    /// <param name="code">授权码</param>
    /// <param name="state">发起时生成的防伪串</param>
    /// <returns>跳转结果</returns>
    /// <exception cref="AppFriendlyException">state 无效或过期、提供方不一致、账号被禁用</exception>
    [HttpGet]
    [DisplayName("第三方登录回调")]
    public async Task<IActionResult> Callback([FromQuery] string provider, [FromQuery] string code, [FromQuery] string state)
    {
        var loginProvider = ResolveProvider(provider);
        if (!_state.TryTake(AuthCacheKeys.ExternalLogin, state ?? string.Empty, out ExternalLoginState saved)
            || saved == null
            || !string.Equals(saved.Provider, loginProvider.Name, StringComparison.OrdinalIgnoreCase))
            throw Oops.Oh("登录请求已失效，请重新发起");

        var userInfo = await loginProvider.GetUserInfoAsync(code, BuildCallbackUrl(loginProvider.Name));
        if (string.IsNullOrWhiteSpace(userInfo?.OpenId))
            throw Oops.Oh("第三方未返回用户标识");

        var user = await FindOrCreateUserAsync(loginProvider.Name, userInfo);
        if (user.Status == StatusEnum.Disable) throw Oops.Oh(ErrorCodeEnum.D1017);

        var token = await _authAppService.CreateToken(user);
        return new RedirectResult($"{saved.RedirectUrl.TrimEnd('/')}/#/login?token={Uri.EscapeDataString(token.AccessToken)}");
    }

    /// <summary>
    /// 按提供方 + OpenId 找绑定的后台用户；第一次登录时自动建用户并绑定。
    /// 自动建的用户用随机密码，只能走第三方登录，避免被人用默认密码登录。
    /// </summary>
    /// <param name="provider">提供方名称</param>
    /// <param name="userInfo">第三方用户信息</param>
    /// <returns>后台用户</returns>
    private async Task<SysUser> FindOrCreateUserAsync(string provider, ExternalUserInfo userInfo)
    {
        var binding = await _bindingRep.AsQueryable().ClearFilter().Includes(u => u.SysUser)
            .FirstAsync(u => u.Provider == provider && u.OpenId == userInfo.OpenId);
        if (binding?.SysUser != null) return binding.SysUser;

        var nickName = string.IsNullOrWhiteSpace(userInfo.NickName) ? userInfo.OpenId : userInfo.NickName;
        var userId = await _userAppService.AddUser(new AddUserInput
        {
            TenantId = SqlSugarConst.DefaultTenantId,
            Account = BuildAccount(provider, userInfo.OpenId),
            RealName = nickName,
            NickName = nickName,
            Email = userInfo.Email,
            Avatar = userInfo.Avatar,
            Password = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)),
            OrgId = DefaultOrgId,
            RoleIdList = new List<long> { SelfDataRoleId }
        });

        await _bindingRep.InsertAsync(new SysUserExternalLogin
        {
            UserId = userId,
            Provider = provider,
            OpenId = userInfo.OpenId,
            NickName = userInfo.NickName,
            Avatar = userInfo.Avatar
        });

        return (await _bindingRep.AsQueryable().ClearFilter().Includes(u => u.SysUser)
            .FirstAsync(u => u.Provider == provider && u.OpenId == userInfo.OpenId)).SysUser;
    }

    /// <summary>
    /// 按名称取已注册的提供方。
    /// </summary>
    /// <param name="provider">提供方名称</param>
    /// <returns>提供方</returns>
    /// <exception cref="AppFriendlyException">未注册</exception>
    private IExternalLoginProvider ResolveProvider(string provider)
    {
        return _providers.FirstOrDefault(p => string.Equals(p.Name, provider, StringComparison.OrdinalIgnoreCase))
               ?? throw Oops.Oh("不支持的第三方登录方式");
    }

    /// <summary>
    /// 跳回地址要在白名单里；没配白名单时只允许站内相对路径，防止被拿来跳到钓鱼站。
    /// </summary>
    /// <param name="redirectUrl">跳回地址</param>
    /// <exception cref="AppFriendlyException">不允许的地址</exception>
    private void EnsureRedirectAllowed(string redirectUrl)
    {
        if (string.IsNullOrWhiteSpace(redirectUrl)) throw Oops.Oh("缺少跳回地址");

        var allowed = _options.CurrentValue.AllowedRedirectUrls ?? new List<string>();
        var ok = allowed.Count == 0
            ? redirectUrl.StartsWith('/') && !redirectUrl.StartsWith("//", StringComparison.Ordinal)
            : allowed.Any(prefix => !string.IsNullOrWhiteSpace(prefix) && redirectUrl.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        if (!ok) throw Oops.Oh("不允许的跳回地址");
    }

    /// <summary>
    /// 本系统的回调地址：当前请求的站点根 + 回调接口路由。
    /// </summary>
    /// <param name="provider">提供方名称</param>
    /// <returns>回调地址</returns>
    private static string BuildCallbackUrl(string provider)
    {
        var request = App.HttpContext.Request;
        return $"{request.Scheme}://{request.Host}{request.PathBase}/api/system/externalLogin/callback?provider={Uri.EscapeDataString(provider)}";
    }

    /// <summary>
    /// 自动建用户的账号名：提供方_OpenId，超长时截断。
    /// </summary>
    /// <param name="provider">提供方名称</param>
    /// <param name="openId">第三方用户标识</param>
    /// <returns>账号名</returns>
    private static string BuildAccount(string provider, string openId)
    {
        var account = $"{provider}_{openId}";
        return account.Length <= 32 ? account : account[..32];
    }

    /// <summary>
    /// 发起登录时暂存的信息，回调时取回。
    /// </summary>
    private sealed class ExternalLoginState
    {
        /// <summary>提供方名称。</summary>
        public string Provider { get; set; }

        /// <summary>跳回地址。</summary>
        public string RedirectUrl { get; set; }
    }
}
