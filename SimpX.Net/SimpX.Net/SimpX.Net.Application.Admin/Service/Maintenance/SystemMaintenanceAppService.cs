namespace SimpX.Net.Application.Admin;

/// <summary>
/// 系统维护内部接口（路由 /api/internal/maintenance/*）。只接受内部签名调用，由调度宿主按计划触发。
/// </summary>
[Authorize(AuthenticationSchemes = InternalCallDefaults.AuthenticationScheme)]
[ApiDescriptionSettings("Default", Module = "internal", Name = "maintenance", Order = 900, Description = "系统维护（内部）")]
public class SystemMaintenanceAppService : IDynamicApiController, ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ConfigAppService _configAppService;

    /// <summary>
    /// 注入数据库和系统配置服务。
    /// </summary>
    /// <param name="db">数据库</param>
    /// <param name="configAppService">系统配置服务</param>
    public SystemMaintenanceAppService(ISqlSugarClient db, ConfigAppService configAppService)
    {
        _db = db;
        _configAppService = configAppService;
    }

    /// <summary>
    /// 清理超过保留天数（系统配置 sys_log_retention_days）的访问、操作、差异日志。
    /// </summary>
    /// <returns>本次使用的保留天数</returns>
    [HttpPost]
    [DisplayName("清理过期日志")]
    public async Task<int> CleanExpiredLogs()
    {
        var daysAgo = await _configAppService.GetConfigValue<int>(ConfigConst.SysLogRetentionDays);
        var deadline = DateTime.Now.AddDays(-daysAgo);
        var db = _db.CopyNew();
        await db.Deleteable<SysLogVis>().Where(u => u.CreateTime < deadline).ExecuteCommandAsync();
        await db.Deleteable<SysLogOp>().Where(u => u.CreateTime < deadline).ExecuteCommandAsync();
        await db.Deleteable<SysLogDiff>().Where(u => u.CreateTime < deadline).ExecuteCommandAsync();
        return daysAgo;
    }
}
