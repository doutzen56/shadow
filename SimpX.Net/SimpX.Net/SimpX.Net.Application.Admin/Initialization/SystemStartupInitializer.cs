using Microsoft.Extensions.Hosting;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// Admin 启动后的系统初始化：必须成功的缓存预热、清空在线用户、缓存租户、同步枚举字典。
/// 这些原来挂在“启动时跑一次”的定时任务上；调度挪到 Job 后，要由 Admin 自己在启动时完成，
/// 否则租户缓存（内存缓存时）只会出现在 Job 进程里。
/// 标记为启动必须成功的预热，失败重试一次后仍失败，就让 Admin 起不来。
/// </summary>
public sealed class SystemStartupInitializer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SystemStartupInitializer> _logger;

    /// <summary>
    /// 注入作用域工厂和日志。
    /// </summary>
    /// <param name="scopeFactory">作用域工厂</param>
    /// <param name="logger">日志</param>
    public SystemStartupInitializer(IServiceScopeFactory scopeFactory, ILogger<SystemStartupInitializer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// 先跑必须成功的缓存预热。这里抛异常时宿主还没对外服务，Admin 会起不来。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await WarmupAsync(cancellationToken);
        await base.StartAsync(cancellationToken);
    }

    /// <summary>
    /// 按顺序执行各项初始化；单项失败只记日志，不影响其它项和进程启动。
    /// </summary>
    /// <param name="stoppingToken">取消令牌</param>
    /// <returns>异步任务</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var sp = scope.ServiceProvider;

        await RunStepAsync("清理在线用户", () => sp.GetRequiredService<ISqlSugarClient>().CopyNew().Deleteable<SysOnlineUser>().ExecuteCommandAsync(stoppingToken));
        await RunStepAsync("清理废弃菜单与行政区域残留", async () =>
        {
            var removed = await ObsoletePlatformMenuCleaner.CleanAsync(
                sp.GetRequiredService<ISqlSugarClient>().CopyNew(), stoppingToken);
            if (removed > 0)
                _logger.LogInformation("已清理废弃菜单 {Count} 条（含行政区域表/配置清理）", removed);
        });
        await RunStepAsync("缓存租户", () => sp.GetRequiredService<TenantAppService>().CacheTenant());
        await RunStepAsync("同步枚举字典", () => sp.GetRequiredService<EnumDictionarySynchronizer>().SyncAsync(stoppingToken));
    }

    /// <summary>
    /// 跑全部缓存预热。必须成功的那几项失败后重试一次，还失败就抛出，让 Admin 起不来。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    private async Task WarmupAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var warmups = scope.ServiceProvider.GetServices<ICacheWarmup>();
        foreach (var warmup in warmups)
            await RunWarmupAsync(warmup, cancellationToken);
    }

    /// <summary>
    /// 跑一项预热。取消时直接抛出，不再重试。
    /// </summary>
    /// <param name="warmup">预热项</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    private async Task RunWarmupAsync(ICacheWarmup warmup, CancellationToken cancellationToken)
    {
        Exception last = null;
        for (var attempt = 1; attempt <= 2; attempt++)
        {
            try
            {
                await warmup.WarmupAsync(cancellationToken);
                _logger.LogInformation("缓存预热完成：{Cache}", warmup.CacheName);
                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                last = ex;
                if (attempt == 1)
                    _logger.LogWarning(ex, "缓存预热失败，再试一次：{Cache}", warmup.CacheName);
            }
        }

        _logger.LogError(last, "缓存预热失败：{Cache}", warmup.CacheName);
        if (warmup.RequiredOnStartup)
            throw new InvalidOperationException($"启动前必须完成的缓存预热失败：{warmup.CacheName}", last);
    }

    /// <summary>
    /// 执行单个初始化步骤并记录结果。
    /// </summary>
    /// <param name="name">步骤名</param>
    /// <param name="step">步骤内容</param>
    /// <returns>异步任务</returns>
    private async Task RunStepAsync(string name, Func<Task> step)
    {
        try
        {
            await step();
            _logger.LogInformation("系统启动初始化：{Step} 完成", name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "系统启动初始化：{Step} 失败", name);
        }
    }
}
