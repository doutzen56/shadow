namespace SimpX.Net.Application.Shared;

/// <summary>
/// 读系统参数配置（无 DynamicApi）。总后台与租户后台共用。
/// </summary>
public sealed class SysConfigReader : ITransient
{
    private readonly SqlSugarRepository<SysConfig> _sysConfigRep;
    private readonly ISimpXCache _cache;

    /// <summary>
    /// 注入配置仓储和缓存。
    /// </summary>
    /// <param name="sysConfigRep">配置仓储</param>
    /// <param name="cache">缓存</param>
    public SysConfigReader(SqlSugarRepository<SysConfig> sysConfigRep, ISimpXCache cache)
    {
        _sysConfigRep = sysConfigRep;
        _cache = cache;
    }

    /// <summary>
    /// 按编码读配置并转成指定类型。
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="code">配置编码</param>
    /// <returns>配置值；没有则返回默认值</returns>
    public async Task<T> GetConfigValue<T>(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return default;

        var value = _cache.Get<string>(ConfigCacheKeys.Config, code);
        if (string.IsNullOrEmpty(value))
        {
            value = (await _sysConfigRep.AsQueryable().FirstAsync(u => u.Code == code))?.Value;
            if (!string.IsNullOrEmpty(value))
                _cache.Add(ConfigCacheKeys.Config, code, value, TimeSpan.FromHours(1));
        }
        if (string.IsNullOrWhiteSpace(value)) return default;
        return (T)Convert.ChangeType(value, typeof(T));
    }

    /// <summary>
    /// 访问令牌有效期（分钟），没配时按 20。
    /// </summary>
    /// <returns>分钟数</returns>
    public async Task<int> GetTokenExpire()
    {
        var tokenExpireStr = await GetConfigValue<string>(ConfigConst.SysTokenExpire);
        _ = int.TryParse(tokenExpireStr, out var tokenExpire);
        return tokenExpire == 0 ? 20 : tokenExpire;
    }

    /// <summary>
    /// 刷新令牌有效期（分钟），没配时按 40。
    /// </summary>
    /// <returns>分钟数</returns>
    public async Task<int> GetRefreshTokenExpire()
    {
        var refreshTokenExpireStr = await GetConfigValue<string>(ConfigConst.SysRefreshTokenExpire);
        _ = int.TryParse(refreshTokenExpireStr, out var refreshTokenExpire);
        return refreshTokenExpire == 0 ? 40 : refreshTokenExpire;
    }
}
