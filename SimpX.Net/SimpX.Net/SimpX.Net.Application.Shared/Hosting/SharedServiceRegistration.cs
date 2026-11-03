using Microsoft.Extensions.DependencyInjection;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// Shared 服务注册约定。
/// Furion 会把带 ITransient/IScoped 的类型扫进所有引用了 Shared 的宿主。
/// 依赖验证码、文件存储等「按宿主点选」能力的服务，禁止挂自动注入接口，改由本类显式注册。
/// </summary>
public static class SharedServiceRegistration
{
    /// <summary>
    /// 租户后台需要的 Shared 登录服务（依赖图形验证码）。
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddSharedAdminAuthServices(this IServiceCollection services)
    {
        services.AddTransient<AuthLoginService>();
        services.AddTransient<ILedgerSideEffectQueues, LedgerSideEffectQueues>();
        services.AddTransient<IMoneyLedgerCallbacks, MoneyLedgerCallbackService>();
        services.AddTransient<IMoneyLedger, MoneyLedgerService>();
        services.AddTransient<IRechargeGateway, RechargeGatewayRouter>();
        services.AddTransient<IUserMessageNotifyService, UserMessageNotifyService>();
        services.AddTransient<IRechargePromoGrantService, RechargePromoGrantService>();
        services.AddTransient<IEventLogCheckService, EventLogCheckService>();
        services.AddTransient<IUserPointService, UserPointService>();
        // Admin 宿主另注册真正的 SiteRealtimePublisher；此处不绑 NoOp，避免盖掉 Hub 实现
        // Stub 单独注入作回落；HttpJson 等进 IEnumerable<IGameVendorTransfer>
        services.AddTransient<StubGameVendorTransfer>();
        services.AddTransient<IGameVendorTransfer, HttpJsonGameVendorTransfer>();
        return services;
    }

    /// <summary>
    /// 总后台需要的 Shared 登录与租户平台服务（依赖验证码、文件存储）。
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddSharedSuperAdminServices(this IServiceCollection services)
    {
        services.AddTransient<AuthLoginService>();
        services.AddTransient<TenantPlatformService>();
        return services;
    }

    /// <summary>
    /// Channels 支付回调入账：只注册账本，不挂登录/验证码。
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddSharedChannelsMoneyServices(this IServiceCollection services)
    {
        services.AddTransient<ILedgerSideEffectQueues, LedgerSideEffectQueues>();
        services.AddTransient<IMoneyLedgerCallbacks, MoneyLedgerCallbackService>();
        services.AddTransient<IMoneyLedger, MoneyLedgerService>();
        services.AddTransient<IRechargeGateway, RechargeGatewayRouter>();
        services.AddTransient<IUserMessageNotifyService, UserMessageNotifyService>();
        services.AddTransient<ISiteRealtimePublisher, NoOpSiteRealtimePublisher>();
        services.AddTransient<IRechargePromoGrantService, RechargePromoGrantService>();
        services.AddTransient<IEventLogCheckService, EventLogCheckService>();
        services.AddTransient<IUserPointService, UserPointService>();
        services.AddTransient<StubGameVendorTransfer>();
        services.AddTransient<IGameVendorTransfer, HttpJsonGameVendorTransfer>();
        return services;
    }

    /// <summary>
    /// Schedule 消费旁路队列：账本队列 + 充值活动发奖 + 活动自动审 + 报表消费（不定站、无验证码）。
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddSharedScheduleQueueServices(this IServiceCollection services)
    {
        services.AddTransient<ILedgerSideEffectQueues, LedgerSideEffectQueues>();
        services.AddTransient<IRechargePromoGrantService, RechargePromoGrantService>();
        services.AddTransient<IUserMessageNotifyService, UserMessageNotifyService>();
        services.AddTransient<ISiteRealtimePublisher, NoOpSiteRealtimePublisher>();
        services.AddTransient<IEventLogCheckService, EventLogCheckService>();
        services.AddTransient<IUserPointService, UserPointService>();
        services.AddTransient<IMoneyLedgerCallbacks, MoneyLedgerCallbackService>();
        services.AddTransient<IMoneyLedger, MoneyLedgerService>();
        services.AddTransient<ITeamMoneyLogConsumer, TeamMoneyLogConsumer>();
        services.AddTransient<IProxyMoneyStsConsumer, ProxyMoneyStsConsumer>();
        services.AddTransient<IVipUpgradeService, VipUpgradeService>();
        services.AddTransient<IVipPeriodicGiftService, VipPeriodicGiftService>();
        services.AddTransient<IScheduledPointService, ScheduledPointService>();
        services.AddTransient<IAutoEventStatsService, AutoEventStatsService>();
        services.AddTransient<IDelotteryDrawService, DelotteryDrawService>();
        services.AddTransient<IUserActivityLockService, UserActivityLockService>();
        services.AddTransient<IUserMoneySyncService, UserMoneySyncService>();
        services.AddTransient<IWithdrawCheckService, WithdrawCheckService>();
        // 提现核对依赖网关路由（Furion 扫不到接口绑定）
        services.AddTransient<IWithdrawGateway, WithdrawGatewayRouter>();
        services.AddTransient<IUserGameDateSyncService, UserGameDateSyncService>();
        services.AddTransient<IAgentGameDateSyncService, AgentGameDateSyncService>();
        services.AddTransient<IDailyReportService, DailyReportService>();
        services.AddTransient<IUserActiveSyncService, UserActiveSyncService>();
        services.AddTransient<IUserExperSyncService, UserExperSyncService>();
        services.AddTransient<ISiteFileCleanupService, SiteFileCleanupService>();
        services.AddTransient<IUserAgentUpdateService, UserAgentUpdateService>();
        services.AddTransient<IGameMaintainResetService, GameMaintainResetService>();
        services.AddTransient<IGameLogDelayCheckService, GameLogDelayCheckService>();
        services.AddTransient<IEsIndexMaintenanceService, EsIndexMaintenanceService>();
        services.AddTransient<IBloomFilterMaintenanceService, BloomFilterMaintenanceService>();
        services.AddTransient<IRebateV2CutoverService, RebateV2CutoverService>();
        services.AddTransient<IRebateOrderV2SyncService, RebateOrderV2SyncService>();
        services.AddTransient<IGameEntranceQueue, GameEntranceQueue>();
        services.AddTransient<IGameEntranceSyncService, GameEntranceSyncService>();
        services.AddTransient<IGameMaintainPlanService, GameMaintainPlanService>();
        services.AddTransient<ITelegramMonitorService, TelegramMonitorService>();
        services.AddTransient<IJackpotAutoService, JackpotAutoService>();
        return services;
    }
}
