using NewLife.Reflection;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 平台参数配置服务 🧩
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "config", Order = 440)]
public class ConfigAppService : IDynamicApiController, ITransient
{
    private static readonly TenantAppService TenantAppService = App.GetService<TenantAppService>();
    private readonly SqlSugarRepository<SysConfig> _sysConfigRep;
    private readonly SqlSugarRepository<SysTenant> _sysTenantRep;
    private readonly ISimpXCache _cache;
    private readonly UserManager _userManager;

    public ConfigAppService(
        SqlSugarRepository<SysTenant> sysTenantRep,
        SqlSugarRepository<SysConfig> sysConfigRep,
        ISimpXCache cache,
        UserManager userManager)
    {
        _cache = cache;
        _sysConfigRep = sysConfigRep;
        _sysTenantRep = sysTenantRep;
        _userManager = userManager;
    }

    /// <summary>
    /// 获取参数配置分页列表 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("获取参数配置分页列表")]
    public async Task<SqlSugarPagedList<SysConfig>> Page(PageConfigInput input)
    {
        return await _sysConfigRep.AsQueryable()
            .WhereIF(!string.IsNullOrWhiteSpace(input.Name?.Trim()), u => u.Name.Contains(input.Name))
            .WhereIF(!string.IsNullOrWhiteSpace(input.Code?.Trim()), u => u.Code.Contains(input.Code))
            .WhereIF(!string.IsNullOrWhiteSpace(input.GroupCode?.Trim()), u => u.GroupCode.Equals(input.GroupCode))
            .OrderBuilder(input)
            .ToPagedListAsync(input.Page, input.PageSize);
    }

    /// <summary>
    /// 获取参数配置列表 🔖
    /// </summary>
    /// <returns></returns>
    [DisplayName("获取参数配置列表")]
    public async Task<List<SysConfig>> List(PageConfigInput input)
    {
        return await _sysConfigRep.AsQueryable()
            .WhereIF(!string.IsNullOrWhiteSpace(input.GroupCode?.Trim()), u => u.GroupCode.Equals(input.GroupCode))
            .ToListAsync();
    }

    /// <summary>
    /// 增加参数配置 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("增加参数配置")]
    public async Task AddConfig(AddConfigInput input)
    {
        if (input.SysFlag == YesNoEnum.Y && !_userManager.SuperAdmin) throw Oops.Oh(ErrorCodeEnum.D3010);

        var isExist = await _sysConfigRep.IsAnyAsync(u => u.Name == input.Name || u.Code == input.Code);
        if (isExist) throw Oops.Oh(ErrorCodeEnum.D9000);

        await _sysConfigRep.InsertAsync(input.Adapt<SysConfig>());
    }

    /// <summary>
    /// 更新参数配置 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("更新参数配置")]
    public async Task UpdateConfig(UpdateConfigInput input)
    {
        if (input.SysFlag == YesNoEnum.Y && !_userManager.SuperAdmin) throw Oops.Oh(ErrorCodeEnum.D3010);

        var isExist = await _sysConfigRep.IsAnyAsync(u => (u.Name == input.Name || u.Code == input.Code) && u.Id != input.Id);
        if (isExist) throw Oops.Oh(ErrorCodeEnum.D9000);

        var config = input.Adapt<SysConfig>();
        await _sysConfigRep.AsUpdateable(config).IgnoreColumns(true).ExecuteCommandAsync();

        Remove(config);
    }

    /// <summary>
    /// 删除参数配置 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("删除参数配置")]
    public async Task DeleteConfig(DeleteConfigInput input)
    {
        var config = await _sysConfigRep.GetFirstAsync(u => u.Id == input.Id);

        // 禁止删除系统参数
        if (config.SysFlag == YesNoEnum.Y)
        { throw Oops.Oh(ErrorCodeEnum.D9001); }
        else
        { await _sysConfigRep.DeleteAsync(config); }

        Remove(config);
    }

    /// <summary>
    /// 批量删除参数配置 🔖
    /// </summary>
    /// <param name="ids"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "BatchDelete"), HttpPost]
    [DisplayName("批量删除参数配置")]
    public async Task BatchDeleteConfig(List<long> ids)
    {
        foreach (var id in ids)
        {
            var config = await _sysConfigRep.GetFirstAsync(u => u.Id == id);

            // 禁止删除系统参数
            if (config.SysFlag == YesNoEnum.Y) continue;

            await _sysConfigRep.DeleteAsync(config);

            Remove(config);
        }
    }

    /// <summary>
    /// 获取参数配置详情 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("获取参数配置详情")]
    public async Task<SysConfig> GetDetail([FromQuery] ConfigInput input)
    {
        return await _sysConfigRep.GetFirstAsync(u => u.Id == input.Id);
    }

    /// <summary>
    /// 获取参数配置值
    /// </summary>
    /// <param name="code"></param>
    /// <returns></returns>
    [NonAction]
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
    /// 根据Code获取配置参数值 🔖
    /// </summary>
    /// <param name="code"></param>
    /// <returns></returns>
    [DisplayName("根据Code获取配置参数值")]
    public async Task<string> GetConfigValueByCode(string code)
    {
        return await GetConfigValueByCode<string>(code);
    }

    /// <summary>
    /// 获取配置参数值
    /// </summary>
    /// <param name="code"></param>
    /// <returns></returns>
    [NonAction]
    public async Task<T> GetConfigValueByCode<T>(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return default;

        var value = _cache.Get<string>(ConfigCacheKeys.Config, code);
        if (string.IsNullOrEmpty(value))
        {
            value = (await _sysConfigRep.CopyNew().GetFirstAsync(u => u.Code == code))?.Value;
            if (!string.IsNullOrEmpty(value))
                _cache.Add(ConfigCacheKeys.Config, code, value, TimeSpan.FromHours(1));
        }
        if (string.IsNullOrWhiteSpace(value)) return default;
        return (T)Convert.ChangeType(value, typeof(T));
    }

    /// <summary>
    /// 更新参数配置值
    /// </summary>
    /// <param name="code"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    [NonAction]
    public async Task UpdateConfigValue(string code, string value)
    {
        var config = await _sysConfigRep.AsQueryable().FirstAsync(u => u.Code == code);
        if (config == null) return;

        config.Value = value;
        await _sysConfigRep.AsUpdateable(config).ExecuteCommandAsync();

        Remove(config);
    }

    /// <summary>
    /// 获取分组列表 🔖
    /// </summary>
    /// <returns></returns>
    [DisplayName("获取分组列表")]
    public async Task<List<string>> GetGroupList()
    {
        return await _sysConfigRep.AsQueryable()
            .GroupBy(u => u.GroupCode)
            .Select(u => u.GroupCode).ToListAsync();
    }

    /// <summary>
    /// 获取 Token 过期时间
    /// </summary>
    /// <returns></returns>
    [NonAction]
    public async Task<int> GetTokenExpire()
    {
        var tokenExpireStr = await GetConfigValue<string>(ConfigConst.SysTokenExpire);
        _ = int.TryParse(tokenExpireStr, out var tokenExpire);
        return tokenExpire == 0 ? 20 : tokenExpire;
    }

    /// <summary>
    /// 获取 RefreshToken 过期时间
    /// </summary>
    /// <returns></returns>
    [NonAction]
    public async Task<int> GetRefreshTokenExpire()
    {
        var refreshTokenExpireStr = await GetConfigValue<string>(ConfigConst.SysRefreshTokenExpire);
        _ = int.TryParse(refreshTokenExpireStr, out var refreshTokenExpire);
        return refreshTokenExpire == 0 ? 40 : refreshTokenExpire;
    }

    /// <summary>
    /// 批量更新参数配置值
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "BatchUpdate"), HttpPost]
    [DisplayName("批量更新参数配置值")]
    public async Task BatchUpdateConfig(List<BatchConfigInput> input)
    {
        foreach (var config in input)
        {
            var info = await _sysConfigRep.AsQueryable().FirstAsync(c => c.Code == config.Code);
            if (info == null || info.SysFlag == YesNoEnum.Y) continue;

            await _sysConfigRep.AsUpdateable().SetColumns(u => u.Value == config.Value).Where(u => u.Code == config.Code).ExecuteCommandAsync();
            Remove(info);
        }
    }

    /// <summary>
    /// 获取系统信息 🔖
    /// </summary>
    /// <returns></returns>
    [SuppressMonitor]
    [AllowAnonymous]
    [DisplayName("获取系统信息")]
    public async Task<dynamic> GetSysInfo()
    {
        var tenant = await TenantAppService.GetCurrentTenantSysInfo();
        var wayList = await _sysConfigRep.Context.Queryable<SysUserRegWay>()
            .Where(u => u.TenantId == tenant.Id)
            .Select(u => new { Label = u.Name, Value = u.Id })
            .ToListAsync();

        var captcha = await GetConfigValue<bool>(ConfigConst.SysCaptcha);
        var secondVer = await GetConfigValue<bool>(ConfigConst.SysSecondVer);
        var hideTenantForLogin = await GetConfigValue<bool>(ConfigConst.SysHideTenantLogin);
        return new
        {
            tenant.Logo,
            tenant.Title,
            tenant.ViceTitle,
            tenant.ViceDesc,
            tenant.Watermark,
            tenant.Copyright,
            tenant.Icp,
            tenant.IcpUrl,
            tenant.RegWayId,
            tenant.EnableReg,
            SecondVer = secondVer ? YesNoEnum.Y : YesNoEnum.N,
            Captcha = captcha ? YesNoEnum.Y : YesNoEnum.N,
            HideTenantForLogin = hideTenantForLogin,
            WayList = wayList
        };
    }

    /// <summary>
    /// 保存系统信息 🔖
    /// </summary>
    /// <returns></returns>
    [UnitOfWork]
    [DisplayName("保存系统信息")]
    public async Task SaveSysInfo(InfoSaveInput input)
    {
        var tenant = await _sysTenantRep.GetFirstAsync(u => u.Id == _userManager.TenantId) ?? throw Oops.Oh(ErrorCodeEnum.D1002);
        if (!string.IsNullOrEmpty(input.LogoBase64)) await TenantAppService.SetLogoUrl(tenant, input.LogoBase64, input.LogoFileName);
        // await UpdateConfigValue(ConfigConst.SysCaptcha, (input.Captcha == YesNoEnum.Y).ToString());
        // await UpdateConfigValue(ConfigConst.SysSecondVer, (input.SecondVer == YesNoEnum.Y).ToString());

        tenant.Copy(input);
        tenant.RegWayId = input.EnableReg == YesNoEnum.Y ? input.RegWayId : null;
        await _sysTenantRep.Context.Updateable(tenant).ExecuteCommandAsync();
    }

    /// <summary>
    /// 删掉这个配置编码对应的缓存。只删配置值这一条。
    /// </summary>
    /// <param name="config">刚改过的配置</param>
    private void Remove(SysConfig config)
    {
        _cache.Remove(ConfigCacheKeys.Config, config.Code);
    }
}