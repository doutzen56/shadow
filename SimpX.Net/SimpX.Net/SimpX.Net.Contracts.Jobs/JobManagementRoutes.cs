namespace SimpX.Net.Contracts.Jobs;

/// <summary>
/// 作业管理的路由约定：总后台对外是 /api/platform/job/*，调度宿主内部是 /api/internal/job/*，两边动作名一致。
/// </summary>
public static class JobManagementRoutes
{
    /// <summary>
    /// Swagger 分组名。
    /// </summary>
    public const string Group = "Default";

    /// <summary>
    /// 资源名（路由里模块后面那一段）。
    /// </summary>
    public const string Resource = "job";

    /// <summary>
    /// 调度宿主内部接口的模块名。
    /// </summary>
    public const string InternalModule = "internal";

    /// <summary>
    /// InternalCall:Services 里调度宿主的键名，同时是命名 HttpClient 的名字。
    /// </summary>
    public const string ScheduleService = "Schedule";

    /// <summary>
    /// InternalCall:Services 里 Admin 宿主的键名，同时是命名 HttpClient 的名字。
    /// </summary>
    public const string AdminService = "Admin";
}
