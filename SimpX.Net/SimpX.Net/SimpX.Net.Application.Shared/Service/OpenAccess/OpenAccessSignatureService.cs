namespace SimpX.Net.Application.Shared;

/// <summary>
/// 开放接口签名校验用到的查询与事件（无 DynamicApi）。
/// </summary>
public sealed class OpenAccessSignatureService : ITransient
{
    private readonly SqlSugarRepository<SysOpenAccess> _sysOpenAccessRep;
    private readonly ISimpXCache _cache;

    /// <summary>
    /// 注入开放接口仓储和缓存。
    /// </summary>
    /// <param name="sysOpenAccessRep">开放接口仓储</param>
    /// <param name="cache">缓存</param>
    public OpenAccessSignatureService(SqlSugarRepository<SysOpenAccess> sysOpenAccessRep, ISimpXCache cache)
    {
        _sysOpenAccessRep = sysOpenAccessRep;
        _cache = cache;
    }

    /// <summary>
    /// 按 AccessKey 取开放接口身份（带绑定用户）。
    /// </summary>
    /// <param name="accessKey">访问键</param>
    /// <returns>开放接口身份</returns>
    public async Task<SysOpenAccess> GetByKey(string accessKey)
    {
        return await _cache.GetOrAddAsync(TenantCacheKeys.OpenAccess, accessKey, async _ =>
        {
            return await _sysOpenAccessRep.AsQueryable()
                .Includes(u => u.BindUser)
                .Includes(u => u.BindUser, p => p.SysOrg)
                .FirstAsync(u => u.AccessKey == accessKey);
        }, TimeSpan.FromHours(1));
    }

    /// <summary>
    /// Signature 身份验证事件默认实现。
    /// </summary>
    /// <returns>签名认证事件</returns>
    public static SignatureAuthenticationEvent GetSignatureAuthenticationEventImpl()
    {
        return new SignatureAuthenticationEvent
        {
            OnGetAccessSecret = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<OpenAccessSignatureService>>();
                try
                {
                    var openAccessService = context.HttpContext.RequestServices.GetRequiredService<OpenAccessSignatureService>();
                    var openAccess = openAccessService.GetByKey(context.AccessKey).GetAwaiter().GetResult();
                    return Task.FromResult(openAccess == null ? "" : openAccess.AccessSecret);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "开放接口身份验证");
                    return Task.FromResult("");
                }
            },
            OnValidated = context =>
            {
                var openAccessService = context.HttpContext.RequestServices.GetRequiredService<OpenAccessSignatureService>();
                var openAccess = openAccessService.GetByKey(context.AccessKey).GetAwaiter().GetResult();
                var identity = ((ClaimsIdentity)context.Principal!.Identity!);

                identity.AddClaims(new[]
                {
                    new Claim(ClaimConst.UserId, openAccess.BindUserId + ""),
                    new Claim(ClaimConst.TenantId, openAccess.BindTenantId + ""),
                    new Claim(ClaimConst.Account, openAccess.BindUser.Account + ""),
                    new Claim(ClaimConst.RealName, openAccess.BindUser.RealName),
                    new Claim(ClaimConst.AccountType, ((int)openAccess.BindUser.AccountType).ToString()),
                    new Claim(ClaimConst.OrgId, openAccess.BindUser.OrgId + ""),
                    new Claim(ClaimConst.OrgName, openAccess.BindUser.SysOrg?.Name + ""),
                    new Claim(ClaimConst.OrgType, openAccess.BindUser.SysOrg?.Type + ""),
                });
                return Task.CompletedTask;
            }
        };
    }
}
