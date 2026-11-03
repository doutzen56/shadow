using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 活动参与日志审核。对齐旧 UserEventAgent.CheckEventLog：通过则按类型加款并写站内信。
/// </summary>
public interface IEventLogCheckService
{
    /// <summary>
    /// 审核单条活动日志。
    /// </summary>
    /// <param name="logId">活动日志编号</param>
    /// <param name="status">目标状态：成功=2 / 失败=1；不能为待处理</param>
    /// <param name="checkMessage">审核备注</param>
    /// <param name="reviewerId">审核人；自动审传 0</param>
    /// <param name="siteId">可选定站校验；空则用日志上的站点</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task CheckAsync(
        int logId,
        int status,
        string checkMessage = null,
        int reviewerId = 0,
        int? siteId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 自动审到期待处理日志。对齐旧 CheckEventLog()：Put/IsAuto 均为自动，且创建超过约 10 秒。
    /// </summary>
    /// <param name="maxCount">单轮最多条数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>成功审过条数</returns>
    Task<int> AutoCheckDueAsync(int maxCount = 100, CancellationToken cancellationToken = default);
}

/// <summary>
/// 活动日志审核与自动发放。对齐旧 CheckEventLog：成功写账变类型 200+活动类型（特权类 180+活动编号）。
/// </summary>
public sealed class EventLogCheckService : ITransient, IEventLogCheckService
{
    /// <summary>旧 CheckStatus.None。</summary>
    public const int StatusNone = 0;

    /// <summary>旧 CheckStatus.Faild。</summary>
    public const int StatusFailed = 1;

    /// <summary>旧 CheckStatus.Success。</summary>
    public const int StatusSuccess = 2;

    /// <summary>旧 EventLog.PutType.Automatic。</summary>
    public const int PutAutomatic = 1;

    /// <summary>旧 User.LockStatus.Activity。</summary>
    public const int LockActivity = 128;

    /// <summary>旧 Wallet.SINGLE。</summary>
    public const int WalletSingle = 1;

    /// <summary>旧 EventType.Privileges。</summary>
    public const int EventTypePrivileges = 18;

    /// <summary>普通活动账变类型基数：200 + EventType。</summary>
    public const int MoneyTypeEventBase = 200;

    /// <summary>特权活动账变类型基数：180 + EventID。</summary>
    public const int MoneyTypePrivilegeBase = 180;

    private readonly ISqlSugarClient _db;
    private readonly IMoneyLedger _money;
    private readonly IUserMessageNotifyService _messages;
    private readonly IUserPointService _points;

    /// <summary>
    /// 注入库、账本、站内信、积分。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="money">账本</param>
    /// <param name="messages">站内信；可空</param>
    /// <param name="points">积分；可空</param>
    public EventLogCheckService(
        IBizDbAccessor biz,
        IMoneyLedger money,
        IUserMessageNotifyService messages = null,
        IUserPointService points = null)
    {
        _db = biz.Db;
        _money = money;
        _messages = messages ?? NoOpUserMessageNotifyService.Instance;
        _points = points ?? NoOpUserPointService.Instance;
    }

    /// <inheritdoc />
    public async Task CheckAsync(
        int logId,
        int status,
        string checkMessage = null,
        int reviewerId = 0,
        int? siteId = null,
        CancellationToken cancellationToken = default)
    {
        if (status == StatusNone)
            throw new InvalidOperationException("#没有选择要进行的操作#");
        if (status != StatusSuccess && status != StatusFailed)
            throw new InvalidOperationException("#没有选择要进行的操作#");
        if (logId <= 0)
            throw new InvalidOperationException("#编号错误#");

        var rows = await _db.Queryable<EventLog>()
            .Where(l => l.ID == logId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var log = rows.FirstOrDefault()
                  ?? throw new InvalidOperationException("#编号错误#");

        if (siteId.HasValue && log.SiteID != siteId.Value)
            throw new InvalidOperationException("#编号错误#");
        if (log.Status != StatusNone)
            throw new InvalidOperationException("#该活动日志已审核#");

        var userRows = await _db.Queryable<User>()
            .Where(u => u.ID == log.UserID && u.SiteID == log.SiteID)
            .Select(u => new { u.Lock, u.Currency })
            .Take(1)
            .ToListAsync(cancellationToken);
        var user = userRows.FirstOrDefault()
                   ?? throw new InvalidOperationException("#编号错误#");
        if ((user.Lock & LockActivity) != 0)
            throw new InvalidOperationException("#禁止活动#");

        var siteWallet = (await _db.Queryable<Site>()
            .Where(s => s.ID == log.SiteID)
            .Select(s => s.Wallet)
            .Take(1)
            .ToListAsync(cancellationToken)).FirstOrDefault();
        var isSingle = siteWallet == WalletSingle;

        var reward = log.Reward;
        var flow = log.Flow;
        var currency = log.Currency;
        var coin = log.Coin;
        if ((user.Lock & UserPointService.LockPoint) != 0)
            coin = 0;

        if (status == StatusSuccess && reward > 0 && isSingle)
        {
            var rate = await GetExchangeRatePointAsync(log.SiteID, currency, user.Currency, cancellationToken);
            reward = Math.Round(reward * rate, 2, MidpointRounding.AwayFromZero);
            flow = Math.Round(flow * rate, 2, MidpointRounding.AwayFromZero);
            currency = user.Currency;
        }

        var now = DateTime.Now;
        var isAuto = log.IsAuto == 0 ? PutAutomatic : log.IsAuto;
        var affected = await _db.Updateable<EventLog>()
            .SetColumns(l => new EventLog
            {
                Status = status,
                ReleaseAt = now,
                CheckMessage = checkMessage ?? string.Empty,
                ReviewerID = reviewerId,
                IsAuto = isAuto,
                Reward = reward,
                Flow = flow,
                Currency = currency,
                Coin = coin
            })
            .Where(l => l.ID == logId && l.Status == StatusNone)
            .ExecuteCommandAsync(cancellationToken);
        if (affected == 0)
            throw new InvalidOperationException("#该活动日志已审核#");

        if (status == StatusSuccess && reward > 0)
        {
            var moneyType = ResolveMoneyType(log.Type, log.EventID);
            var flowRate = reward == 0 ? 0 : flow / reward;
            var description = ResolveMoneyDescription(log);
            try
            {
                await _money.AddMoneyLogAsync(
                    log.SiteID,
                    log.UserID,
                    reward,
                    moneyType,
                    log.ID.ToString(),
                    description,
                    currency,
                    cancellationToken,
                    flowRate: flowRate,
                    eventId: log.EventID);
            }
            catch
            {
                await _db.Updateable<EventLog>()
                    .SetColumns(l => new EventLog
                    {
                        Status = StatusNone,
                        ReleaseAt = DateTime.MinValue,
                        CheckMessage = string.Empty,
                        ReviewerID = 0,
                        Reward = log.Reward,
                        Flow = log.Flow,
                        Currency = log.Currency,
                        IsAuto = log.IsAuto,
                        Coin = log.Coin
                    })
                    .Where(l => l.ID == logId && l.Status == status)
                    .ExecuteCommandAsync(cancellationToken);
                throw;
            }
        }

        if (status == StatusSuccess && coin > 0)
        {
            try
            {
                await _points.AddPointAsync(
                    log.SiteID,
                    log.UserID,
                    coin,
                    UserPointService.PointTypeEvent,
                    string.Empty,
                    log.Type,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                Log.Warning($"活动积分发放失败 LogID={logId}：{ex.Message}");
            }
        }

        var title = await ResolveEventTitleAsync(log.EventID, cancellationToken);
        try
        {
            if (status == StatusSuccess)
            {
                var coinText = coin > 0 ? " #积分#:" + coin : string.Empty;
                if (reward > 0 || coin > 0)
                {
                    var amount = reward > 0 ? $"{TrimEndZeros(reward)}{currency}" : string.Empty;
                    var content = $"#活动奖励# #到帐#,{amount} {coinText}";
                    await _messages.SendNotifyAsync(log.SiteID, log.UserID, content, title, cancellationToken);
                }
            }
            else
            {
                var content = $"#活动奖励# #到帐##失败#,{checkMessage}";
                await _messages.SendNotifyAsync(log.SiteID, log.UserID, content, title, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            Log.Warning($"活动审核站内信失败 LogID={logId}：{ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<int> AutoCheckDueAsync(int maxCount = 100, CancellationToken cancellationToken = default)
    {
        if (maxCount <= 0)
            return 0;

        var cutoff = DateTime.Now.AddSeconds(-10);
        var ids = await _db.Queryable<EventLog>()
            .Where(l =>
                l.Status == StatusNone
                && l.Put == PutAutomatic
                && l.IsAuto == PutAutomatic
                && l.ExpectedAt < DateTime.Now
                && l.CreateAt < cutoff)
            .OrderBy(l => l.ID)
            .Take(maxCount)
            .Select(l => l.ID)
            .ToListAsync(cancellationToken);

        var ok = 0;
        foreach (var id in ids)
        {
            try
            {
                await CheckAsync(id, StatusSuccess, checkMessage: null, reviewerId: 0, siteId: null, cancellationToken);
                ok++;
            }
            catch (Exception ex)
            {
                Log.Warning($"活动自动审失败 LogID={id}：{ex.Message}");
            }
        }

        return ok;
    }

    /// <summary>
    /// 账变类型：特权用 180+活动编号，其余 200+活动类型。
    /// </summary>
    public static int ResolveMoneyType(int eventType, int eventId) =>
        eventType == EventTypePrivileges
            ? MoneyTypePrivilegeBase + eventId
            : MoneyTypeEventBase + eventType;

    private static string ResolveMoneyDescription(EventLog log)
    {
        if (!string.IsNullOrEmpty(log.ErrorMsg) && log.ErrorMsg.Contains("#宝箱#", StringComparison.Ordinal))
            return log.ErrorMsg.Replace("#完成每日任务#", string.Empty, StringComparison.Ordinal);
        return EventTypeLabel(log.Type);
    }

    private static string EventTypeLabel(int type) => type switch
    {
        9 => "#充值优惠#",
        19 => "#首充优惠#",
        20 => "#存款优惠(阶梯)#",
        16 => "#提款优惠#",
        _ => "#活动奖励#"
    };

    private async Task<string> ResolveEventTitleAsync(int eventId, CancellationToken cancellationToken)
    {
        var titles = await _db.Queryable<SiteEvent>()
            .Where(e => e.ID == eventId)
            .Select(e => e.Title)
            .Take(1)
            .ToListAsync(cancellationToken);
        var title = titles.FirstOrDefault();
        return string.IsNullOrWhiteSpace(title) ? "活动已经删除" : title;
    }

    private async Task<decimal> GetExchangeRatePointAsync(
        int siteId,
        int fromCurrency,
        int toCurrency,
        CancellationToken cancellationToken)
    {
        if (fromCurrency == toCurrency)
            return 1;
        var rows = await _db.Queryable<SiteExchangeRate>()
            .Where(r => r.SiteID == siteId && r.BaseCurrency == fromCurrency && r.ExchangeCurrency == toCurrency)
            .Take(1)
            .ToListAsync(cancellationToken);
        var point = rows.FirstOrDefault()?.Point ?? 0;
        return point > 0 ? point : 1;
    }

    private static string TrimEndZeros(decimal value)
    {
        var s = value.ToString("F2");
        if (s.Contains('.'))
            s = s.TrimEnd('0').TrimEnd('.');
        return s;
    }
}
