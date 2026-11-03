using Furion.HttpRemote;

namespace SimpX.Net.Contracts.Jobs;

/// <summary>
/// Job 调 Admin 系统维护内部接口的声明式客户端。Job 只负责“几点调”，清理什么、怎么清由 Admin 决定。
/// </summary>
[HttpClientName(JobManagementRoutes.AdminService)]
public interface IAdminMaintenanceApi : IHttpDeclarative
{
    /// <summary>
    /// 清理超过保留天数的访问、操作、差异日志。
    /// </summary>
    /// <returns>本次实际使用的保留天数</returns>
    [Post("api/internal/maintenance/cleanExpiredLogs")]
    Task<int> CleanExpiredLogs();
}
