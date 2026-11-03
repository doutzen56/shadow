using SimpX.Net.Data.Entity.Risk;
using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 后台待办角标。对齐旧 NotifyAgent.RefrsehAdminNotify + /admin/site/notify/*。
/// </summary>
public sealed class SiteAdminNotifyService : ITransient
{
    /// <summary>旧 CheckStatus.None。</summary>
    public const int CheckNone = 0;

    /// <summary>旧 EventLog.PutType.Automatic → IsAuto。</summary>
    public const int EventPutAutomatic = 1;

    /// <summary>旧 AgentApply.ApplyStatus.None。</summary>
    public const int AgentApplyNone = 0;

    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;
    private readonly ISiteRealtimePublisher _realtime;

    /// <summary>注入。</summary>
    public SiteAdminNotifyService(
        IBizDbAccessor biz,
        ICurrentSiteAccessor site,
        ISiteRealtimePublisher realtime = null)
    {
        _db = biz.Db;
        _site = site;
        _realtime = realtime ?? new NoOpSiteRealtimePublisher();
    }

    /// <summary>统计当前站角标（全量字段）。</summary>
    public async Task<AdminNotifyCounts> CountAsync(CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var today = DateTime.Now.Date;
        var counts = new AdminNotifyCounts
        {
            Withdraw = await _db.Queryable<WithdrawOrder>()
                .Where(t => t.SiteID == siteId && t.Status == WithdrawOrderOperateService.StatusNone)
                .CountAsync(cancellationToken),
            WithdrawExecutor = await _db.Queryable<WithdrawOrder>()
                .Where(t => t.SiteID == siteId && t.Status == WithdrawOrderOperateService.StatusWait)
                .CountAsync(cancellationToken),
            Transfer = await _db.Queryable<BankRechargeOrder>()
                .Where(t => t.SiteID == siteId && t.Status == CheckNone)
                .CountAsync(cancellationToken),
            Event = await _db.Queryable<EventLog>()
                .Where(t => t.SiteID == siteId && t.Status == CheckNone && t.IsAuto == EventPutAutomatic)
                .CountAsync(cancellationToken),
            Commission = await _db.Queryable<UserCommissionLog>()
                .Where(t => t.SiteID == siteId && t.Status == CheckNone)
                .CountAsync(cancellationToken),
            Rebate = await _db.Queryable<UserRebateLog>()
                .Where(t => t.SiteID == siteId && t.Status == CheckNone)
                .CountAsync(cancellationToken),
            AgentApply = await _db.Queryable<AgentApply>()
                .Where(t => t.SiteID == siteId && t.Status == AgentApplyNone)
                .CountAsync(cancellationToken),
            MoneyReview = await _db.Queryable<MoneyReview>()
                .Where(t => t.SiteID == siteId && t.Status == CheckNone)
                .CountAsync(cancellationToken),
            Exchange = await _db.Queryable<CurrencyExchangeOrder>()
                .Where(t => t.SiteID == siteId && t.Status == CheckNone)
                .CountAsync(cancellationToken),
            Kyc = await _db.Queryable<UserIdCard>()
                .Where(t => t.SiteID == siteId && t.Status == CheckNone)
                .CountAsync(cancellationToken),
            Bank = await _db.Queryable<BankAccount>()
                .Where(t => t.SiteID == siteId && t.Status == CheckNone)
                .CountAsync(cancellationToken),
            Risk = await _db.Queryable<RiskUserDanger>()
                .Where(t => t.SiteID == siteId && t.Date == today)
                .CountAsync(cancellationToken)
        };
        return counts;
    }

    /// <summary>
    /// 对齐旧 notify/get：按权限过滤后返回 total+data（仅 &gt;0）。
    /// 本波先返回全量角标；权限过滤留给前端/后续 OldApiPermission 细拆。
    /// </summary>
    public async Task<OldApiResult> GetOldApiAsync(CancellationToken cancellationToken = default)
    {
        var counts = await CountAsync(cancellationToken);
        var data = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        void Add(string key, int value)
        {
            if (value > 0) data[key] = value;
        }

        Add("Withdraw", counts.Withdraw);
        Add("WithdrawExecutor", counts.WithdrawExecutor);
        Add("Transfer", counts.Transfer);
        Add("Event", counts.Event);
        Add("Rebate", counts.Rebate);
        Add("Commission", counts.Commission);
        Add("AgentApply", counts.AgentApply);
        Add("MoneyReview", counts.MoneyReview);
        Add("Exchange", counts.Exchange);
        Add("Kyc", counts.Kyc);
        Add("Bank", counts.Bank);
        Add("Risk", counts.Risk);

        if (data.Count == 0)
            return OldApiResult.Ok(string.Empty);

        return OldApiResult.Ok(string.Empty, new { total = data.Values.Sum(), data });
    }

    /// <summary>对齐旧 notify/update：重算并经 SignalR 推送。</summary>
    public async Task<OldApiResult> UpdateOldApiAsync(CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var counts = await CountAsync(cancellationToken);
        await _realtime.PublishAdminNotifyAsync(siteId, counts, cancellationToken);
        return OldApiResult.Ok("#刷新成功#");
    }

    /// <summary>对齐旧 notify/withdraw。</summary>
    public async Task<OldApiResult> WithdrawOldApiAsync(CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var count = await _db.Queryable<WithdrawOrder>()
            .Where(t => t.SiteID == siteId && t.Status == WithdrawOrderOperateService.StatusNone)
            .CountAsync(cancellationToken);
        return OldApiResult.Ok(string.Empty, new { Count = count });
    }

    /// <summary>对齐旧 notify/events。</summary>
    public async Task<OldApiResult> EventsOldApiAsync(CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var count = await _db.Queryable<EventLog>()
            .Where(t => t.SiteID == siteId && t.Status == CheckNone)
            .CountAsync(cancellationToken);
        return OldApiResult.Ok(string.Empty, new { Count = count });
    }
}
