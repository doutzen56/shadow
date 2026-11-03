using System.Globalization;
using System.Text.Json;
using SimpX.Net.Data.Entity.Reports;
using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// AutoEvent 统计写待审活动日志 / 转盘次数。对齐旧 AutoEventLog 中全民代理、邀请有礼、洗码、邀请任务、转盘段。
/// </summary>
public interface IAutoEventStatsService
{
    /// <summary>
    /// 全民代理：按昨日下级投注写 EventLog（类型 UserInvite=10）。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <param name="now">当前时间；测试可注入</param>
    /// <returns>写入条数</returns>
    Task<int> RunUserInviteAsync(CancellationToken cancellationToken = default, DateTime? now = null);

    /// <summary>
    /// 邀请有礼：窗口内按被邀请人投注/充值给推荐人写 EventLog（类型 InviteReward=12；日/周/月）。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <param name="now">当前时间；测试可注入</param>
    /// <returns>写入条数</returns>
    Task<int> RunInviteRewardDailyAsync(CancellationToken cancellationToken = default, DateTime? now = null);

    /// <summary>
    /// 擂台赛：结算窗口内按周期投注排行写 EventLog（类型 Competition=7）。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <param name="now">当前时间；测试可注入</param>
    /// <returns>写入条数</returns>
    Task<int> RunCompetitionAsync(CancellationToken cancellationToken = default, DateTime? now = null);

    /// <summary>
    /// 洗码优惠：窗口内按昨日投注档位写 EventLog（类型 Washing=17）。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <param name="now">当前时间；测试可注入</param>
    /// <returns>写入条数</returns>
    Task<int> RunWashingAsync(CancellationToken cancellationToken = default, DateTime? now = null);

    /// <summary>
    /// 邀请任务：窗口内按被邀人投注/充值写 EventLog（类型 InviteTask=24；日/周/月）。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <param name="now">当前时间；测试可注入</param>
    /// <returns>写入条数</returns>
    Task<int> RunInviteTaskDailyAsync(CancellationToken cancellationToken = default, DateTime? now = null);

    /// <summary>
    /// 转盘次数：按昨日打码达标写 usr_DelotteryLog（类型 Bet）。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <param name="now">当前时间；测试可注入</param>
    /// <returns>写入条数</returns>
    Task<int> RunDelotteryBetGrantAsync(CancellationToken cancellationToken = default, DateTime? now = null);

    /// <summary>
    /// 转盘次数日清：把余额大于 0 的次数记一笔清零。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <param name="now">当前时间；测试可注入</param>
    /// <returns>写入条数</returns>
    Task<int> RunDelotteryCleanAsync(CancellationToken cancellationToken = default, DateTime? now = null);
}

/// <summary>
/// 全民代理 / 邀请有礼 / 洗码 / 邀请任务写待审 usr_EventLog；转盘次数写 usr_DelotteryLog。
/// 奖金发放仍走 EventLogCheck / AutoEventLogGrantJob。
/// </summary>
public sealed class AutoEventStatsService : ITransient, IAutoEventStatsService
{
    /// <summary>旧 EventType.Competition。</summary>
    public const int EventTypeCompetition = 7;

    /// <summary>旧 EventType.UserInvite。</summary>
    public const int EventTypeUserInvite = 10;

    /// <summary>旧 EventType.Delottery。</summary>
    public const int EventTypeDelottery = 11;

    /// <summary>旧 EventType.InviteReward。</summary>
    public const int EventTypeInviteReward = 12;

    /// <summary>旧 EventType.Washing。</summary>
    public const int EventTypeWashing = 17;

    /// <summary>旧 EventType.InviteTask。</summary>
    public const int EventTypeInviteTask = 24;

    /// <summary>旧 CheckStatus.None。</summary>
    public const int StatusNone = 0;

    /// <summary>旧 PutType.Automatic。</summary>
    public const int PutAutomatic = 1;

    /// <summary>旧 PutType.Manual。</summary>
    public const int PutManual = 0;

    /// <summary>旧 LockStatus.Point。</summary>
    public const int LockPoint = 64;

    /// <summary>旧 LockStatus.Activity。</summary>
    public const int LockActivity = 128;

    /// <summary>旧 PutT.Reward（奖金）。</summary>
    public const int PutTReward = 1;

    /// <summary>旧 UserType.ProxyMember / ProxyAgent。</summary>
    public const int UserTypeProxyMember = 2;

    /// <summary>旧 UserType.ProxyAgent。</summary>
    public const int UserTypeProxyAgent = 4;

    /// <summary>旧 UserType.Agent。</summary>
    public const int UserTypeAgent = 1;

    /// <summary>旧 Wallet.SINGLE。</summary>
    public const int WalletSingle = 1;

    /// <summary>旧 MoneyType.Recharge。</summary>
    public const int MoneyTypeRecharge = 1;

    /// <summary>旧 DelotteryCountItem.Bet。</summary>
    public const int DelotteryTypeBet = 5;

    /// <summary>旧 DelotteryCountItem.ClearZero。</summary>
    public const int DelotteryTypeClearZero = 10;

    /// <summary>邀请有礼日返窗口：12:00–12:10。</summary>
    public const int InviteRewardHour = 12;

    /// <summary>洗码 / 邀请任务窗口：17:30–17:39。</summary>
    public const int EveningStatsHour = 17;

    /// <summary>转盘打码发放窗口：01:00–01:09。</summary>
    public const int DelotteryGrantHour = 1;

    /// <summary>转盘清零窗口：23:55 起。</summary>
    public const int DelotteryCleanHour = 23;

    private readonly ISqlSugarClient _db;
    private readonly IUserActivityLockService _activityLock;

    /// <summary>
    /// 注入业务库与活动锁。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="activityLock">活动锁</param>
    public AutoEventStatsService(IBizDbAccessor biz, IUserActivityLockService activityLock = null)
    {
        _db = biz.Db;
        _activityLock = activityLock ?? new UserActivityLockService(biz);
    }

    /// <inheritdoc />
    public async Task<int> RunUserInviteAsync(CancellationToken cancellationToken = default, DateTime? now = null)
    {
        var clock = now ?? DateTime.Now;
        var date = clock.Date.AddDays(-1);
        var count = 0;
        var events = await LoadOpenEventsAsync(EventTypeUserInvite, clock, cancellationToken);

        foreach (var ev in events)
        {
            var setting = ParseUserInviteSetting(ev.SettingString);
            if (setting == null)
                continue;

            var site = await LoadSiteAsync(ev.SiteID, cancellationToken);
            if (site == null)
                continue;

            var betsByUser = await LoadBetByUserAsync(ev.SiteID, date, cancellationToken);
            if (betsByUser.Count == 0)
                continue;

            var siteUsers = await _db.Queryable<User>()
                .Where(u => u.SiteID == ev.SiteID)
                .Select(u => new { u.ID, u.InviteID, u.GroupID, u.Type, u.Lock, u.Currency, u.AgentID })
                .ToListAsync(cancellationToken);

            var inviteesOf = siteUsers
                .Where(u => u.InviteID > 0)
                .GroupBy(u => u.InviteID)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ID).ToList());

            foreach (var agent in siteUsers)
            {
                if (IsAgentSystem(agent.Type) || agent.AgentID == 0)
                    continue;
                if (await _activityLock.IsBlockedForActivityAsync(ev.SiteID, agent.ID, agent.Lock, cancellationToken))
                    continue;
                if (!GroupAllowed(setting.UserGroup, agent.GroupID))
                    continue;

                var level1 = inviteesOf.GetValueOrDefault(agent.ID) ?? [];
                if (level1.Count == 0)
                    continue;

                var money1 = level1.Sum(id => betsByUser.GetValueOrDefault(id));
                var level2 = level1.SelectMany(id => inviteesOf.GetValueOrDefault(id) ?? []).ToList();
                var money2 = level2.Sum(id => betsByUser.GetValueOrDefault(id));
                var level3 = level2.SelectMany(id => inviteesOf.GetValueOrDefault(id) ?? []).ToList();
                var money3 = level3.Sum(id => betsByUser.GetValueOrDefault(id));
                if (money1 + money2 + money3 <= 0)
                    continue;

                var reward = Math.Round(
                    money1 * setting.BetAmount1 + money2 * setting.BetAmount2 + money3 * setting.BetAmount3,
                    2,
                    MidpointRounding.AwayFromZero);
                if (reward <= 0)
                    continue;

                var currency = site.Wallet == WalletSingle ? agent.Currency : site.Currency;
                var sourceId = $"{date:yyyyMMdd}-{currency}";
                if (await EventLogExistsAsync(ev.SiteID, agent.ID, ev.ID, sourceId, cancellationToken))
                    continue;

                await InsertEventLogAsync(
                    ev,
                    agent.ID,
                    reward,
                    reward * setting.WashFlow,
                    sourceId,
                    currency,
                    setting.Put,
                    setting.IsAuto,
                    $"{date:yyyy-MM-dd}直属:{money1:n},二级:{money2:n},三级:{money3:n}",
                    cancellationToken);
                count++;
            }
        }

        return count;
    }

    /// <inheritdoc />
    public async Task<int> RunInviteRewardDailyAsync(CancellationToken cancellationToken = default, DateTime? now = null)
    {
        var clock = now ?? DateTime.Now;
        // 对齐旧窗口：12 点后 10 分钟内；测试注入时可强制进入。
        if (!now.HasValue && (clock.Hour != InviteRewardHour || clock.Minute >= 10))
            return 0;

        var count = 0;
        var events = await LoadOpenEventsAsync(EventTypeInviteReward, clock, cancellationToken);
        foreach (var ev in events)
        {
            var setting = ParseInviteRewardSetting(ev.SettingString);
            if (setting == null)
                continue;

            var metric = ResolveInviteMetric(setting.ReturnType, setting.ReturnTypeName);
            if (metric == InviteMetric.None)
                continue;
            if (!TryResolveInvitePeriod(
                    setting.ReturnPeriod,
                    setting.ReturnPeriodName,
                    clock,
                    force: now.HasValue,
                    out var rangeStart,
                    out var rangeEnd,
                    out var periodTag))
                continue;

            Dictionary<int, decimal> amountsByUser;
            if (metric == InviteMetric.Recharge)
                amountsByUser = await LoadRechargeRangeByUserAsync(ev.SiteID, rangeStart, rangeEnd, cancellationToken);
            else if (metric == InviteMetric.Loss || metric == InviteMetric.Win)
                amountsByUser = ToLossOrWinAmounts(
                    await LoadProfitRangeByUserAsync(ev.SiteID, rangeStart, rangeEnd, cancellationToken),
                    metric);
            else
                amountsByUser = await LoadBetRangeByUserAsync(ev.SiteID, rangeStart, rangeEnd, cancellationToken);
            if (amountsByUser.Count == 0)
                continue;

            var site = await LoadSiteAsync(ev.SiteID, cancellationToken);
            if (site == null)
                continue;

            foreach (var (inviteeId, amount) in amountsByUser)
            {
                if (amount <= 0)
                    continue;
                var invitees = await _db.Queryable<User>()
                    .Where(u => u.ID == inviteeId && u.SiteID == ev.SiteID)
                    .Take(1)
                    .ToListAsync(cancellationToken);
                var invitee = invitees.FirstOrDefault();
                if (invitee == null || invitee.InviteID <= 0)
                    continue;
                if (await _activityLock.IsBlockedForActivityAsync(ev.SiteID, invitee.ID, invitee.Lock, cancellationToken))
                    continue;

                var inviters = await _db.Queryable<User>()
                    .Where(u => u.ID == invitee.InviteID && u.SiteID == ev.SiteID)
                    .Take(1)
                    .ToListAsync(cancellationToken);
                var inviter = inviters.FirstOrDefault();
                if (inviter == null)
                    continue;
                if (await _activityLock.IsBlockedForActivityAsync(ev.SiteID, inviter.ID, inviter.Lock, cancellationToken))
                    continue;
                if (!GroupAllowed(setting.UserGroup, inviter.GroupID))
                    continue;

                var rateOrFixed = setting.ReturnValue1;
                var reward = setting.ReturnWay == 1 || string.Equals(setting.ReturnWayName, "Rate", StringComparison.OrdinalIgnoreCase)
                    ? Math.Round(amount * rateOrFixed, 2, MidpointRounding.AwayFromZero)
                    : rateOrFixed;
                if (reward <= 0)
                    continue;

                var sourceId = $"{metric.ToString().ToLowerInvariant()}-{inviteeId}-{periodTag}_{ev.ID}";
                if (sourceId.Length > 64)
                    sourceId = sourceId[..64];
                if (await EventLogExistsAsync(ev.SiteID, inviter.ID, ev.ID, sourceId, cancellationToken))
                    continue;

                var currency = site.Wallet == WalletSingle ? inviter.Currency : site.Currency;
                await InsertEventLogAsync(
                    ev,
                    inviter.ID,
                    reward,
                    reward * setting.WashFlow,
                    sourceId,
                    currency,
                    setting.Put,
                    setting.IsAuto,
                    $"邀请有礼{periodTag} 被邀:{invitee.UserName} 金额:{amount:n}",
                    cancellationToken);
                count++;
            }
        }

        return count;
    }

    /// <inheritdoc />
    public async Task<int> RunWashingAsync(CancellationToken cancellationToken = default, DateTime? now = null)
    {
        var clock = now ?? DateTime.Now;
        // 对齐旧窗口：17:30–17:39；注入时钟时跳过窗口限制。
        if (!now.HasValue && (clock.Hour != EveningStatsHour || clock.Minute <= 30 || clock.Minute >= 40))
            return 0;

        var date = clock.Date.AddDays(-1);
        var count = 0;
        var events = await LoadOpenEventsAsync(EventTypeWashing, clock, cancellationToken);
        foreach (var ev in events)
        {
            var setting = ParseWashingSetting(ev.SettingString);
            if (setting == null || setting.PutFirst == PutManual)
                continue;
            if (setting.Rules.Count == 0)
                continue;

            var site = await LoadSiteAsync(ev.SiteID, cancellationToken);
            if (site == null)
                continue;

            var games = await _db.Queryable<UserDateGame>()
                .Where(g => g.SiteID == ev.SiteID && g.Date == date)
                .ToListAsync(cancellationToken);
            if (games.Count == 0)
                continue;

            if (setting.GameKeys.Count > 0)
                games = games.Where(g => setting.GameKeys.Contains($"{g.Category}.{g.Type}")).ToList();
            if (games.Count == 0)
                continue;

            var betByUser = games.GroupBy(g => g.UserID).ToDictionary(g => g.Key, g => g.Sum(x => x.BetMoney));
            var rechargeByUser = await LoadRechargeByUserAsync(ev.SiteID, date, cancellationToken);
            var rechargeNeed = setting.Recharge;

            foreach (var (userId, bet) in betByUser)
            {
                if (bet <= 0)
                    continue;

                var users = await _db.Queryable<User>()
                    .Where(u => u.ID == userId && u.SiteID == ev.SiteID)
                    .Take(1)
                    .ToListAsync(cancellationToken);
                var user = users.FirstOrDefault();
                if (user == null || IsAgentSystem(user.Type))
                    continue;
                if (await _activityLock.IsBlockedForActivityAsync(ev.SiteID, userId, user.Lock, cancellationToken))
                    continue;
                if (!GroupAllowed(setting.UserGroup, user.GroupID))
                    continue;

                var charge = rechargeByUser.GetValueOrDefault(userId);
                if (charge < rechargeNeed)
                    continue;

                var matched = setting.Rules
                    .Where(r => r.Point <= bet)
                    .OrderByDescending(r => r.Point)
                    .FirstOrDefault();
                if (matched == null || (matched.Money <= 0 && matched.AwardPoint <= 0))
                    continue;

                var sourceId = $"{date:yyyyMMdd}-{userId}-{ev.ID}";
                if (sourceId.Length > 64)
                    sourceId = sourceId[..64];
                if (await EventLogExistsAsync(ev.SiteID, userId, ev.ID, sourceId, cancellationToken))
                    continue;

                var currency = site.Wallet == WalletSingle ? user.Currency : site.Currency;
                await InsertEventLogAsync(
                    ev,
                    userId,
                    matched.Money,
                    matched.Money * setting.WashFlow,
                    sourceId,
                    currency,
                    setting.PutFirst == 0 ? setting.Put : setting.PutFirst,
                    PutAutomatic,
                    $"洗码 昨日投注:{bet:n} 档位:{matched.Point:n}",
                    cancellationToken,
                    matched.AwardPoint);
                count++;
            }
        }

        return count;
    }

    /// <inheritdoc />
    public async Task<int> RunInviteTaskDailyAsync(CancellationToken cancellationToken = default, DateTime? now = null)
    {
        var clock = now ?? DateTime.Now;
        if (!now.HasValue && (clock.Hour != EveningStatsHour || clock.Minute <= 30 || clock.Minute >= 39))
            return 0;

        var count = 0;
        var events = await LoadOpenEventsAsync(EventTypeInviteTask, clock, cancellationToken);
        foreach (var ev in events)
        {
            var setting = ParseInviteTaskSetting(ev.SettingString);
            if (setting == null || setting.Items.Count == 0)
                continue;

            var metric = ResolveInviteMetric(setting.ReturnType, setting.ReturnTypeName);
            // FirstRecharge 枚举值是 0；JSON 字符串标签（如 VaildBet）时 ReadInt 也会回落成 0，不能单靠数字判断。
            var isFirstRecharge = IsFirstRechargeReturn(setting.ReturnType, setting.ReturnTypeName);
            if (metric == InviteMetric.None && isFirstRecharge)
                metric = InviteMetric.Recharge;
            if (metric == InviteMetric.None)
                continue;
            if (!TryResolveInvitePeriod(
                    setting.ReturnPeriod,
                    setting.ReturnPeriodName,
                    clock,
                    force: now.HasValue,
                    out var rangeStart,
                    out var rangeEnd,
                    out var periodTag))
                continue;

            var site = await LoadSiteAsync(ev.SiteID, cancellationToken);
            if (site == null)
                continue;

            Dictionary<int, decimal> amountsByUser;
            if (isFirstRecharge)
                amountsByUser = await LoadFirstRechargeRangeByUserAsync(ev.SiteID, rangeStart, rangeEnd, cancellationToken);
            else if (metric == InviteMetric.Recharge)
                amountsByUser = await LoadRechargeRangeByUserAsync(ev.SiteID, rangeStart, rangeEnd, cancellationToken);
            else if (metric == InviteMetric.Loss || metric == InviteMetric.Win)
                amountsByUser = ToLossOrWinAmounts(
                    await LoadProfitRangeByUserAsync(ev.SiteID, rangeStart, rangeEnd, cancellationToken),
                    metric);
            else
                amountsByUser = await LoadBetRangeByUserAsync(ev.SiteID, rangeStart, rangeEnd, cancellationToken);
            if (amountsByUser.Count == 0)
                continue;

            var activeUserIds = amountsByUser.Keys.ToList();
            var inviteeRows = await _db.Queryable<User>()
                .Where(u => u.SiteID == ev.SiteID && activeUserIds.Contains(u.ID) && u.InviteID > 0)
                .Select(u => new { u.ID, u.InviteID })
                .ToListAsync(cancellationToken);
            var inviterIds = inviteeRows.Select(x => x.InviteID).Distinct().ToList();
            if (inviterIds.Count == 0)
                continue;

            foreach (var inviterId in inviterIds)
            {
                var inviters = await _db.Queryable<User>()
                    .Where(u => u.ID == inviterId && u.SiteID == ev.SiteID)
                    .Take(1)
                    .ToListAsync(cancellationToken);
                var inviter = inviters.FirstOrDefault();
                if (inviter == null || inviter.Type == UserTypeAgent || IsAgentSystem(inviter.Type))
                    continue;
                if (await _activityLock.IsBlockedForActivityAsync(ev.SiteID, inviterId, inviter.Lock, cancellationToken))
                    continue;
                if (!GroupAllowed(setting.UserGroup, inviter.GroupID))
                    continue;

                var invitees = await _db.Queryable<User>()
                    .Where(u => u.InviteID == inviterId
                                && u.SiteID == ev.SiteID
                                && u.CreateAt >= rangeStart
                                && u.CreateAt < rangeEnd)
                    .Select(u => new { u.ID, u.UserName })
                    .ToListAsync(cancellationToken);
                if (invitees.Count == 0)
                    continue;

                var typeCode = metric == InviteMetric.Recharge ? 3 : 1;
                var sourceIdBase = $"{typeCode}{periodTag}_{ev.ID}";
                decimal money = 0;
                foreach (var item in setting.Items.OrderByDescending(i => i.ID))
                {
                    decimal sumReward = 0;
                    var valid = 0;
                    foreach (var invitee in invitees)
                    {
                        var amount = amountsByUser.GetValueOrDefault(invitee.ID);
                        if (amount < item.NeedMoney)
                            continue;
                        var piece = item.Money > 1
                            ? item.Money
                            : Math.Round(amount * item.Money, 2, MidpointRounding.AwayFromZero);
                        if (item.RewardUpLimit > 0 && piece > item.RewardUpLimit)
                            piece = item.RewardUpLimit;
                        sumReward += piece;
                        valid++;
                    }

                    if (sumReward > 0 && valid >= item.Member)
                    {
                        money = sumReward;
                        break;
                    }
                }

                if (money <= 0)
                    continue;

                var sourceId = $"{sourceIdBase}_{inviterId}";
                if (sourceId.Length > 64)
                    sourceId = sourceId[..64];
                if (await EventLogExistsAsync(ev.SiteID, inviterId, ev.ID, sourceId, cancellationToken))
                    continue;

                var currency = site.Wallet == WalletSingle ? inviter.Currency : site.Currency;
                await InsertEventLogAsync(
                    ev,
                    inviterId,
                    money,
                    money * setting.WashFlow,
                    sourceId,
                    currency,
                    setting.Put,
                    setting.IsAuto,
                    $"邀请任务{periodTag} 合计奖:{money:n}",
                    cancellationToken);
                count++;
            }
        }

        return count;
    }

    /// <inheritdoc />
    public async Task<int> RunDelotteryBetGrantAsync(CancellationToken cancellationToken = default, DateTime? now = null)
    {
        var clock = now ?? DateTime.Now;
        if (!now.HasValue && (clock.Hour != DelotteryGrantHour || clock.Minute < 1 || clock.Minute >= 10))
            return 0;

        var count = 0;
        var events = await LoadOpenEventsAsync(EventTypeDelottery, clock, cancellationToken);
        foreach (var ev in events)
        {
            var setting = ParseDelotterySetting(ev.SettingString);
            if (setting == null)
                continue;
            var betRules = setting.Rules
                .Where(r => r.ReturnType == 2
                            || string.Equals(r.ReturnTypeName, "Bet", StringComparison.OrdinalIgnoreCase))
                .Where(r => r.ReturnPeriod == 1
                            || string.Equals(r.ReturnPeriodName, "Day", StringComparison.OrdinalIgnoreCase)
                            || string.IsNullOrEmpty(r.ReturnPeriodName) && r.ReturnPeriod == 0)
                .ToList();
            // Day 枚举在 DelotteryPeriodRealTime 里是 1；若配置只写 Bet+Day 也收。
            if (betRules.Count == 0)
                betRules = setting.Rules
                    .Where(r => r.ReturnType == 2
                                || string.Equals(r.ReturnTypeName, "Bet", StringComparison.OrdinalIgnoreCase))
                    .ToList();
            if (betRules.Count == 0)
                continue;

            var rangeStart = clock.Date.AddDays(-1);
            var rangeEnd = clock.Date;
            var betsByUser = await LoadBetRangeByUserAsync(ev.SiteID, rangeStart, rangeEnd, cancellationToken);
            if (betsByUser.Count == 0)
                continue;

            foreach (var (userId, bet) in betsByUser)
            {
                if (bet <= 0)
                    continue;
                var users = await _db.Queryable<User>()
                    .Where(u => u.ID == userId && u.SiteID == ev.SiteID)
                    .Take(1)
                    .ToListAsync(cancellationToken);
                var user = users.FirstOrDefault();
                if (user == null || IsAgentSystem(user.Type))
                    continue;
                if (await _activityLock.IsBlockedForActivityAsync(ev.SiteID, userId, user.Lock, cancellationToken))
                    continue;
                if (!GroupAllowed(setting.UserGroup, user.GroupID))
                    continue;

                var matched = betRules
                    .Where(r => r.Threshold <= bet)
                    .OrderByDescending(r => r.MaxCount)
                    .ToList();
                if (matched.Count == 0)
                    continue;

                var grant = matched.Sum(r => r.MaxCount);
                if (grant <= 0)
                    continue;

                var content = $"bet-{rangeStart:yyyyMMdd}-{ev.ID}-{userId}";
                if (await DelotteryContentExistsAsync(ev.SiteID, userId, content, cancellationToken))
                    continue;

                var balance = await SumDelotteryBalanceAsync(ev.SiteID, userId, cancellationToken);
                var newBalance = balance + grant;
                await _db.Insertable(new UserDelotteryLog
                {
                    SiteID = ev.SiteID,
                    UserID = userId,
                    Point = grant,
                    Balance = newBalance,
                    Type = DelotteryTypeBet,
                    Content = content,
                    CreateAt = clock
                }).ExecuteCommandAsync(cancellationToken);
                count++;
            }
        }

        return count;
    }

    /// <inheritdoc />
    public async Task<int> RunDelotteryCleanAsync(CancellationToken cancellationToken = default, DateTime? now = null)
    {
        var clock = now ?? DateTime.Now;
        if (!now.HasValue && (clock.Hour != DelotteryCleanHour || clock.Minute < 55))
            return 0;

        var count = 0;
        var rows = await _db.Queryable<UserDelotteryLog>()
            .Select(l => new { l.SiteID, l.UserID, l.Point })
            .ToListAsync(cancellationToken);
        var groups = rows.GroupBy(r => new { r.SiteID, r.UserID });
        foreach (var g in groups)
        {
            var balance = g.Sum(x => x.Point);
            if (balance <= 0)
                continue;

            var content = $"clear-{clock:yyyyMMdd}-{g.Key.UserID}";
            if (await DelotteryContentExistsAsync(g.Key.SiteID, g.Key.UserID, content, cancellationToken))
                continue;

            await _db.Insertable(new UserDelotteryLog
            {
                SiteID = g.Key.SiteID,
                UserID = g.Key.UserID,
                Point = -balance,
                Balance = 0,
                Type = DelotteryTypeClearZero,
                Content = content,
                CreateAt = clock
            }).ExecuteCommandAsync(cancellationToken);
            count++;
        }

        return count;
    }

    /// <inheritdoc />
    public async Task<int> RunCompetitionAsync(CancellationToken cancellationToken = default, DateTime? now = null)
    {
        var clock = now ?? DateTime.Now;
        var count = 0;
        var events = await LoadOpenEventsAsync(EventTypeCompetition, clock, cancellationToken);
        foreach (var ev in events)
        {
            var setting = ParseCompetitionSetting(ev.SettingString);
            if (setting == null || setting.Items.Count == 0)
                continue;
            if (!TryResolveCompetitionWindow(setting, ev, clock, force: now.HasValue, out var rangeStart, out var rangeEnd, out var period))
                continue;

            var betsByUser = await LoadBetRangeByUserAsync(ev.SiteID, rangeStart, rangeEnd, cancellationToken);
            if (betsByUser.Count == 0)
                continue;

            var site = await LoadSiteAsync(ev.SiteID, cancellationToken);
            if (site == null)
                continue;

            var ranked = new List<(int UserId, decimal Bet)>();
            foreach (var (userId, bet) in betsByUser.OrderByDescending(x => x.Value))
            {
                if (bet < setting.StartWashFlow)
                    continue;
                var users = await _db.Queryable<User>()
                    .Where(u => u.ID == userId && u.SiteID == ev.SiteID)
                    .Take(1)
                    .ToListAsync(cancellationToken);
                var user = users.FirstOrDefault();
                if (user == null || IsAgentSystem(user.Type))
                    continue;
                if ((user.Lock & LockPoint) != 0)
                    continue;
                if (await _activityLock.IsBlockedForActivityAsync(ev.SiteID, userId, user.Lock, cancellationToken))
                    continue;
                if (!GroupAllowed(setting.UserGroup, user.GroupID))
                    continue;
                ranked.Add((userId, bet));
                if (ranked.Count >= setting.Items.Count)
                    break;
            }

            for (var i = 0; i < ranked.Count; i++)
            {
                var item = setting.Items[i];
                var userId = ranked[i].UserId;
                var sourceId = $"{ev.ID}-{period}-{item.ID}";
                if (sourceId.Length > 64)
                    sourceId = sourceId[..64];
                if (await EventLogExistsAsync(ev.SiteID, userId, ev.ID, sourceId, cancellationToken))
                    continue;

                var users = await _db.Queryable<User>()
                    .Where(u => u.ID == userId)
                    .Take(1)
                    .ToListAsync(cancellationToken);
                var user = users.FirstOrDefault();
                if (user == null)
                    continue;

                var currency = site.Wallet == WalletSingle ? user.Currency : site.Currency;
                var isReward = setting.PutT == PutTReward
                               || string.Equals(setting.PutTName, "Reward", StringComparison.OrdinalIgnoreCase);
                var reward = isReward ? item.Money : 0;
                var coin = isReward ? (short)0 : (short)item.AwardPoint;
                if (reward <= 0 && coin <= 0)
                    continue;

                await InsertEventLogAsync(
                    ev,
                    userId,
                    reward,
                    reward * setting.WashFlow,
                    sourceId,
                    currency,
                    setting.Put,
                    setting.IsAuto,
                    $"擂台赛第{i + 1}名 投注:{ranked[i].Bet:n} {item.Name}",
                    cancellationToken,
                    coin);
                count++;
            }
        }

        return count;
    }

    private async Task<List<SiteEvent>> LoadOpenEventsAsync(int type, DateTime clock, CancellationToken cancellationToken)
    {
        return await _db.Queryable<SiteEvent>()
            .Where(e => e.Type == type
                        && e.IsOpen
                        && e.StartAt <= clock
                        && e.ExpireAt >= clock)
            .ToListAsync(cancellationToken);
    }

    private async Task<Site> LoadSiteAsync(int siteId, CancellationToken cancellationToken)
    {
        var rows = await _db.Queryable<Site>()
            .Where(s => s.ID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        return rows.FirstOrDefault();
    }

    private async Task<Dictionary<int, decimal>> LoadBetByUserAsync(int siteId, DateTime date, CancellationToken cancellationToken)
    {
        var games = await _db.Queryable<UserDateGame>()
            .Where(g => g.SiteID == siteId && g.Date == date)
            .ToListAsync(cancellationToken);
        return games.GroupBy(g => g.UserID).ToDictionary(g => g.Key, g => g.Sum(x => x.BetMoney));
    }

    private async Task<Dictionary<int, decimal>> LoadBetRangeByUserAsync(
        int siteId,
        DateTime start,
        DateTime end,
        CancellationToken cancellationToken)
    {
        var games = await _db.Queryable<UserDateGame>()
            .Where(g => g.SiteID == siteId && g.Date >= start && g.Date < end)
            .ToListAsync(cancellationToken);
        return games.GroupBy(g => g.UserID).ToDictionary(g => g.Key, g => g.Sum(x => x.BetMoney));
    }

    /// <summary>
    /// 按日期区间汇总每人的游戏盈亏（UserDateGame.Money）。
    /// </summary>
    /// <param name="siteId">站点编号</param>
    /// <param name="start">区间起点（含）</param>
    /// <param name="end">区间终点（不含）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户编号 → 区间内 Money 合计</returns>
    private async Task<Dictionary<int, decimal>> LoadProfitRangeByUserAsync(
        int siteId,
        DateTime start,
        DateTime end,
        CancellationToken cancellationToken)
    {
        var games = await _db.Queryable<UserDateGame>()
            .Where(g => g.SiteID == siteId && g.Date >= start && g.Date < end)
            .ToListAsync(cancellationToken);
        return games.GroupBy(g => g.UserID).ToDictionary(g => g.Key, g => g.Sum(x => x.Money));
    }

    /// <summary>
    /// 把区间盈亏合计转成亏损或盈利金额：亏损取 max(0, -合计)，盈利取 max(0, 合计)；合计折算后为 0 的用户去掉。
    /// </summary>
    /// <param name="profitByUser">用户编号 → 区间 Money 合计</param>
    /// <param name="metric">Loss 或 Win</param>
    /// <returns>用户编号 → 用于发奖的正数金额</returns>
    private static Dictionary<int, decimal> ToLossOrWinAmounts(
        Dictionary<int, decimal> profitByUser,
        InviteMetric metric)
    {
        var result = new Dictionary<int, decimal>();
        foreach (var (userId, sum) in profitByUser)
        {
            var amount = metric == InviteMetric.Loss ? Math.Max(0m, -sum) : Math.Max(0m, sum);
            if (amount > 0)
                result[userId] = amount;
        }

        return result;
    }

    private async Task<bool> EventLogExistsAsync(
        int siteId,
        int userId,
        int eventId,
        string sourceId,
        CancellationToken cancellationToken) =>
        await _db.Queryable<EventLog>()
            .Where(l => l.SiteID == siteId && l.UserID == userId && l.EventID == eventId && l.SourceID == sourceId)
            .AnyAsync(cancellationToken);

    private async Task InsertEventLogAsync(
        SiteEvent ev,
        int userId,
        decimal reward,
        decimal flow,
        string sourceId,
        int currency,
        int put,
        int isAuto,
        string checkMessage,
        CancellationToken cancellationToken,
        short coin = 0)
    {
        await _db.Insertable(new EventLog
        {
            SiteID = ev.SiteID,
            UserID = userId,
            EventID = ev.ID,
            Type = ev.Type,
            CreateAt = DateTime.Now,
            Reward = reward,
            Coin = coin,
            Status = StatusNone,
            ErrorMsg = string.Empty,
            IP = string.Empty,
            IPValue = 0,
            Put = put,
            Flow = flow,
            ExpectedAt = DateTime.Now,
            ReleaseAt = DateTime.MinValue,
            SourceID = sourceId,
            CheckMessage = checkMessage ?? string.Empty,
            Currency = currency,
            IsAuto = isAuto,
            ReviewerID = 0
        }).ExecuteCommandAsync(cancellationToken);
    }

    private async Task<Dictionary<int, decimal>> LoadRechargeByUserAsync(
        int siteId,
        DateTime date,
        CancellationToken cancellationToken)
    {
        var rows = await _db.Queryable<UserDateMoney>()
            .Where(m => m.SiteID == siteId && m.Date == date && m.Type == MoneyTypeRecharge)
            .ToListAsync(cancellationToken);
        return rows.GroupBy(m => m.UserID).ToDictionary(g => g.Key, g => g.Sum(x => x.Money));
    }

    private async Task<Dictionary<int, decimal>> LoadRechargeRangeByUserAsync(
        int siteId,
        DateTime start,
        DateTime end,
        CancellationToken cancellationToken)
    {
        var rows = await _db.Queryable<UserDateMoney>()
            .Where(m => m.SiteID == siteId && m.Date >= start && m.Date < end && m.Type == MoneyTypeRecharge)
            .ToListAsync(cancellationToken);
        return rows.GroupBy(m => m.UserID).ToDictionary(g => g.Key, g => g.Sum(x => x.Money));
    }

    /// <summary>
    /// 区间内首充到账（Count=1 且已支付），对齐旧 GetUserFirstRechargeAmount 的订单条件。
    /// </summary>
    private async Task<Dictionary<int, decimal>> LoadFirstRechargeRangeByUserAsync(
        int siteId,
        DateTime start,
        DateTime end,
        CancellationToken cancellationToken)
    {
        var rows = await _db.Queryable<RechargeOrder>()
            .Where(o => o.SiteID == siteId
                        && o.IsPay
                        && o.Count == 1
                        && o.PayAt >= start
                        && o.PayAt < end)
            .ToListAsync(cancellationToken);
        return rows.GroupBy(o => o.UserID).ToDictionary(g => g.Key, g => g.Sum(x => x.Paid));
    }

    private enum InviteMetric
    {
        None = 0,
        VaildBet = 1,
        Recharge = 2,
        Loss = 3,
        Win = 4
    }

    /// <summary>
    /// 解析邀请统计口径。JSON 里 ReturnType 若是字符串，ReadInt 会回落成 0，所以先看名字，再看数字。
    /// </summary>
    /// <param name="returnType">解析后的数字返点类型</param>
    /// <param name="name">原始字符串返点类型</param>
    /// <returns>投注 / 充值 / 亏损 / 盈利；认不出则 None</returns>
    private static InviteMetric ResolveInviteMetric(int returnType, string name)
    {
        if (string.Equals(name, "Loss", StringComparison.OrdinalIgnoreCase))
            return InviteMetric.Loss;
        if (string.Equals(name, "Win", StringComparison.OrdinalIgnoreCase))
            return InviteMetric.Win;
        if (string.Equals(name, "VaildBet", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "Bet", StringComparison.OrdinalIgnoreCase))
            return InviteMetric.VaildBet;
        if (string.Equals(name, "Recharge", StringComparison.OrdinalIgnoreCase))
            return InviteMetric.Recharge;

        // 数字：1 投注，2 亏损，3 充值，4 盈利（与旧 InviteReturnType 对齐）
        if (returnType == 1)
            return InviteMetric.VaildBet;
        if (returnType == 2)
            return InviteMetric.Loss;
        if (returnType == 3)
            return InviteMetric.Recharge;
        if (returnType == 4)
            return InviteMetric.Win;
        return InviteMetric.None;
    }

    /// <summary>
    /// 是否按首充订单统计。旧 InviteReturnType.FirstRecharge=0；字符串标签优先，避免非数字字符串被 ReadInt 回落成 0。
    /// </summary>
    /// <param name="returnType">解析后的数字返点类型</param>
    /// <param name="name">原始字符串返点类型</param>
    /// <returns>是首充则 true</returns>
    private static bool IsFirstRechargeReturn(int returnType, string name)
    {
        if (string.Equals(name, "FirstRecharge", StringComparison.OrdinalIgnoreCase))
            return true;
        if (returnType != 0)
            return false;
        return string.IsNullOrEmpty(name) || name == "0";
    }

    /// <summary>
    /// 解析邀请日/周/月统计区间。周仅周一、月仅 1 号（注入时钟时可强制）。
    /// </summary>
    private static bool TryResolveInvitePeriod(
        int returnPeriod,
        string periodName,
        DateTime clock,
        bool force,
        out DateTime rangeStart,
        out DateTime rangeEnd,
        out string periodTag)
    {
        rangeStart = default;
        rangeEnd = default;
        periodTag = string.Empty;
        var isDay = returnPeriod == 1 || string.Equals(periodName, "Day", StringComparison.OrdinalIgnoreCase);
        var isWeek = returnPeriod == 2 || string.Equals(periodName, "Week", StringComparison.OrdinalIgnoreCase);
        var isMonth = returnPeriod == 3 || string.Equals(periodName, "Month", StringComparison.OrdinalIgnoreCase);

        if (isDay)
        {
            rangeStart = clock.Date.AddDays(-1);
            rangeEnd = clock.Date;
            periodTag = rangeStart.ToString("yyyyMMdd");
            return true;
        }

        if (isWeek)
        {
            if (!force && clock.DayOfWeek != DayOfWeek.Monday)
                return false;
            // 上周一 00:00 ~ 本周一 00:00
            var thisMonday = clock.Date.AddDays(-(int)clock.DayOfWeek + (int)DayOfWeek.Monday);
            if (clock.DayOfWeek == DayOfWeek.Sunday)
                thisMonday = clock.Date.AddDays(-6);
            rangeStart = thisMonday.AddDays(-7);
            rangeEnd = thisMonday;
            periodTag = $"{rangeStart:yyyy}W{System.Globalization.ISOWeek.GetWeekOfYear(rangeStart):000}";
            return true;
        }

        if (isMonth)
        {
            if (!force && clock.Day != 1)
                return false;
            var first = new DateTime(clock.Year, clock.Month, 1);
            rangeStart = first.AddMonths(-1);
            rangeEnd = first;
            periodTag = rangeStart.ToString("yyyyMM");
            return true;
        }

        return false;
    }

    /// <summary>
    /// 擂台赛结算窗口：日赛 23:55–23:59；周赛周日；月赛月末。注入时钟跳过窗口。
    /// </summary>
    private static bool TryResolveCompetitionWindow(
        CompetitionSetting setting,
        SiteEvent ev,
        DateTime clock,
        bool force,
        out DateTime rangeStart,
        out DateTime rangeEnd,
        out int period)
    {
        rangeStart = default;
        rangeEnd = default;
        period = 1;
        var isDay = setting.ReturnPeriod == 1
                    || string.Equals(setting.ReturnPeriodName, "Day", StringComparison.OrdinalIgnoreCase);
        var isWeek = setting.ReturnPeriod == 2
                     || string.Equals(setting.ReturnPeriodName, "Week", StringComparison.OrdinalIgnoreCase);
        var isMonth = setting.ReturnPeriod == 3
                      || string.Equals(setting.ReturnPeriodName, "Month", StringComparison.OrdinalIgnoreCase);
        var isNone = setting.ReturnPeriod == 0
                     || string.Equals(setting.ReturnPeriodName, "None", StringComparison.OrdinalIgnoreCase);

        if (isDay)
        {
            if (!force && (clock.Hour != 23 || clock.Minute < 55))
                return false;
            rangeStart = clock.Date;
            rangeEnd = clock.Date.AddDays(1);
            period = 1 + Math.Max(0, (int)(clock.Date - ev.StartAt.Date).TotalDays);
            return true;
        }

        if (isWeek)
        {
            if (!force && clock.DayOfWeek != DayOfWeek.Sunday)
                return false;
            if (!force && (clock.Hour != 23 || clock.Minute < 55))
                return false;
            rangeStart = clock.Date.AddDays(-6);
            rangeEnd = clock.Date.AddDays(1);
            period = 1 + Math.Max(0, (int)(clock.Date - ev.StartAt.Date).TotalDays / 7);
            return true;
        }

        if (isMonth)
        {
            var lastDay = DateTime.DaysInMonth(clock.Year, clock.Month);
            if (!force && clock.Day != lastDay)
                return false;
            if (!force && (clock.Hour != 23 || clock.Minute < 55))
                return false;
            rangeStart = new DateTime(clock.Year, clock.Month, 1);
            rangeEnd = rangeStart.AddMonths(1);
            period = 1 + Math.Max(0, (int)(clock.Date - ev.StartAt.Date).TotalDays / 30);
            return true;
        }

        if (isNone)
        {
            // 活动结束后次日 23:55 开奖（简化：ExpireAt 当天 23:55）
            if (!force && (clock.Date != ev.ExpireAt.Date || clock.Hour != 23 || clock.Minute < 55))
                return false;
            rangeStart = ev.StartAt.Date;
            rangeEnd = ev.ExpireAt.Date.AddDays(1);
            period = 1;
            return true;
        }

        return false;
    }

    private async Task<bool> DelotteryContentExistsAsync(
        int siteId,
        int userId,
        string content,
        CancellationToken cancellationToken) =>
        await _db.Queryable<UserDelotteryLog>()
            .Where(l => l.SiteID == siteId && l.UserID == userId && l.Content == content)
            .AnyAsync(cancellationToken);

    private async Task<decimal> SumDelotteryBalanceAsync(
        int siteId,
        int userId,
        CancellationToken cancellationToken)
    {
        var points = await _db.Queryable<UserDelotteryLog>()
            .Where(l => l.SiteID == siteId && l.UserID == userId)
            .Select(l => l.Point)
            .ToListAsync(cancellationToken);
        return points.Sum();
    }

    private static bool IsAgentSystem(int type) =>
        type is UserTypeProxyMember or UserTypeProxyAgent;

    private static bool GroupAllowed(int[] groups, int groupId)
    {
        if (groups == null || groups.Length == 0)
            return true;
        if (groups.Length == 1 && groups[0] == 0)
            return true;
        return groups.Contains(groupId);
    }

    /// <summary>
    /// 解析全民代理 Setting（JSON 或 query）。
    /// </summary>
    internal static UserInviteSetting ParseUserInviteSetting(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;
        try
        {
            if (raw.TrimStart().StartsWith('{'))
            {
                using var doc = JsonDocument.Parse(raw);
                var root = doc.RootElement;
                return new UserInviteSetting
                {
                    BetAmount1 = ReadDecimal(root, "BetAmount1"),
                    BetAmount2 = ReadDecimal(root, "BetAmount2"),
                    BetAmount3 = ReadDecimal(root, "BetAmount3"),
                    WashFlow = ReadDecimal(root, "WashFlow"),
                    Put = ReadInt(root, "Put", PutAutomatic),
                    IsAuto = ReadInt(root, "IsAuto", PutAutomatic),
                    UserGroup = ReadIntArray(root, "UserGroup")
                };
            }

            var map = SiteSettingQuery.Parse(raw);
            return new UserInviteSetting
            {
                BetAmount1 = ParseDec(SiteSettingQuery.Get(map, "BetAmount1")),
                BetAmount2 = ParseDec(SiteSettingQuery.Get(map, "BetAmount2")),
                BetAmount3 = ParseDec(SiteSettingQuery.Get(map, "BetAmount3")),
                WashFlow = ParseDec(SiteSettingQuery.Get(map, "WashFlow")),
                Put = int.TryParse(SiteSettingQuery.Get(map, "Put"), out var p) ? p : PutAutomatic,
                IsAuto = int.TryParse(SiteSettingQuery.Get(map, "IsAuto"), out var a) ? a : PutAutomatic,
                UserGroup = ParseIntList(SiteSettingQuery.Get(map, "UserGroup"))
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 解析邀请有礼 Setting。
    /// </summary>
    internal static InviteRewardSetting ParseInviteRewardSetting(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;
        try
        {
            if (!raw.TrimStart().StartsWith('{'))
                return null;
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            return new InviteRewardSetting
            {
                ReturnType = ReadInt(root, "ReturnType"),
                ReturnTypeName = ReadString(root, "ReturnType"),
                ReturnPeriod = ReadInt(root, "ReturnPeriod"),
                ReturnPeriodName = ReadString(root, "ReturnPeriod"),
                ReturnWay = ReadInt(root, "ReturnWay"),
                ReturnWayName = ReadString(root, "ReturnWay"),
                ReturnValue1 = ReadDecimal(root, "ReturnValue1"),
                WashFlow = ReadDecimal(root, "WashFlow"),
                Put = ReadInt(root, "Put", PutAutomatic),
                IsAuto = ReadInt(root, "IsAuto", PutAutomatic),
                UserGroup = ReadIntArray(root, "UserGroup")
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 解析洗码 Setting（含 DataSetting 档位）。
    /// </summary>
    internal static WashingSetting ParseWashingSetting(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || !raw.TrimStart().StartsWith('{'))
            return null;
        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            var putFirst = ReadInt(root, "PutFirst", PutAutomatic);
            var setting = new WashingSetting
            {
                PutFirst = putFirst,
                Put = ReadInt(root, "Put", putFirst),
                WashFlow = ReadDecimal(root, "WashFlow"),
                Recharge = ReadDecimal(root, "Recharge"),
                UserGroup = ReadIntArray(root, "UserGroup"),
                GameKeys = ParseGameKeys(ReadString(root, "SettingGame")),
                Rules = []
            };

            var dataRaw = ReadString(root, "DataSetting");
            if (string.IsNullOrWhiteSpace(dataRaw) && root.TryGetProperty("Rules", out _))
                dataRaw = raw;
            if (!string.IsNullOrWhiteSpace(dataRaw))
            {
                using var dataDoc = JsonDocument.Parse(dataRaw);
                var dataRoot = dataDoc.RootElement;
                if (dataRoot.TryGetProperty("Rules", out var rules) && rules.ValueKind == JsonValueKind.Array)
                {
                    foreach (var r in rules.EnumerateArray())
                    {
                        setting.Rules.Add(new WashingRule
                        {
                            ID = ReadInt(r, "ID"),
                            Money = ReadDecimal(r, "Money"),
                            Point = ReadDecimal(r, "Point"),
                            AwardPoint = (short)ReadInt(r, "AwardPoint")
                        });
                    }
                }
            }

            return setting;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 解析邀请任务 Setting（含 DataSetting 档位）。
    /// </summary>
    internal static InviteTaskSetting ParseInviteTaskSetting(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || !raw.TrimStart().StartsWith('{'))
            return null;
        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            var setting = new InviteTaskSetting
            {
                ReturnType = ReadInt(root, "ReturnType"),
                ReturnTypeName = ReadString(root, "ReturnType"),
                ReturnPeriod = ReadInt(root, "ReturnPeriod"),
                ReturnPeriodName = ReadString(root, "ReturnPeriod"),
                ReturnWay = ReadInt(root, "ReturnWay"),
                ReturnWayName = ReadString(root, "ReturnWay"),
                WashFlow = ReadDecimal(root, "WashFlow"),
                Put = ReadInt(root, "Put", PutAutomatic),
                IsAuto = ReadInt(root, "IsAuto", PutAutomatic),
                UserGroup = ReadIntArray(root, "UserGroup"),
                Items = []
            };

            var dataRaw = ReadString(root, "DataSetting");
            if (string.IsNullOrWhiteSpace(dataRaw) && root.TryGetProperty("Items", out _))
                dataRaw = raw;
            if (!string.IsNullOrWhiteSpace(dataRaw))
            {
                using var dataDoc = JsonDocument.Parse(dataRaw);
                var dataRoot = dataDoc.RootElement;
                if (dataRoot.TryGetProperty("Items", out var items) && items.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in items.EnumerateArray())
                    {
                        setting.Items.Add(new InviteTaskItem
                        {
                            ID = ReadInt(item, "ID"),
                            NeedMoney = ReadDecimal(item, "needMoney"),
                            Money = ReadDecimal(item, "Money"),
                            Member = ReadInt(item, "Member", 1),
                            RewardUpLimit = ReadDecimal(item, "RewardUpLimit")
                        });
                    }
                }
            }

            return setting;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 解析擂台赛 Setting。
    /// </summary>
    internal static CompetitionSetting ParseCompetitionSetting(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || !raw.TrimStart().StartsWith('{'))
            return null;
        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            var setting = new CompetitionSetting
            {
                ReturnPeriod = ReadInt(root, "ReturnPeriod"),
                ReturnPeriodName = ReadString(root, "ReturnPeriod"),
                WashFlow = ReadDecimal(root, "WashFlow"),
                StartWashFlow = ReadDecimal(root, "StartWashFlow"),
                Put = ReadInt(root, "Put", PutAutomatic),
                IsAuto = ReadInt(root, "IsAuto", PutAutomatic),
                PutT = ReadInt(root, "PutType", PutTReward),
                PutTName = ReadString(root, "PutType"),
                UserGroup = ReadIntArray(root, "UserGroup"),
                Items = []
            };

            var dataRaw = ReadString(root, "DataSetting");
            if (string.IsNullOrWhiteSpace(dataRaw) && root.TryGetProperty("Items", out _))
                dataRaw = raw;
            if (!string.IsNullOrWhiteSpace(dataRaw))
            {
                using var dataDoc = JsonDocument.Parse(dataRaw);
                var dataRoot = dataDoc.RootElement;
                if (dataRoot.TryGetProperty("Items", out var items) && items.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in items.EnumerateArray())
                    {
                        setting.Items.Add(new CompetitionItem
                        {
                            ID = ReadInt(item, "ID"),
                            Name = ReadString(item, "Name"),
                            Money = ReadDecimal(item, "Money"),
                            AwardPoint = ReadDecimal(item, "AwardPoint")
                        });
                    }
                }
            }

            return setting;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 解析转盘 Setting（含 DataSetting 规则）。
    /// </summary>
    internal static DelotterySetting ParseDelotterySetting(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || !raw.TrimStart().StartsWith('{'))
            return null;
        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            var setting = new DelotterySetting
            {
                UserGroup = ReadIntArray(root, "UserGroup"),
                Rules = []
            };

            var dataRaw = ReadString(root, "DataSetting");
            if (string.IsNullOrWhiteSpace(dataRaw) && root.TryGetProperty("Rules", out _))
                dataRaw = raw;
            if (!string.IsNullOrWhiteSpace(dataRaw))
            {
                using var dataDoc = JsonDocument.Parse(dataRaw);
                var dataRoot = dataDoc.RootElement;
                if (dataRoot.TryGetProperty("Rules", out var rules) && rules.ValueKind == JsonValueKind.Array)
                {
                    foreach (var r in rules.EnumerateArray())
                    {
                        setting.Rules.Add(new DelotteryRule
                        {
                            ID = ReadInt(r, "ID"),
                            MaxCount = ReadInt(r, "MaxCount"),
                            Threshold = ReadDecimal(r, "Recharge"),
                            ReturnType = ReadInt(r, "ReturnType"),
                            ReturnTypeName = ReadString(r, "ReturnType"),
                            ReturnPeriod = ReadInt(r, "ReturnPeriod"),
                            ReturnPeriodName = ReadString(r, "ReturnPeriod")
                        });
                    }
                }
            }

            return setting;
        }
        catch
        {
            return null;
        }
    }

    private static HashSet<string> ParseGameKeys(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return [];
        return raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static decimal ReadDecimal(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var p))
            return 0;
        return p.ValueKind switch
        {
            JsonValueKind.Number => p.GetDecimal(),
            JsonValueKind.String => ParseDec(p.GetString()),
            _ => 0
        };
    }

    private static int ReadInt(JsonElement root, string name, int fallback = 0)
    {
        if (!root.TryGetProperty(name, out var p))
            return fallback;
        if (p.ValueKind == JsonValueKind.Number && p.TryGetInt32(out var n))
            return n;
        if (p.ValueKind == JsonValueKind.String && int.TryParse(p.GetString(), out var s))
            return s;
        return fallback;
    }

    private static string ReadString(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var p))
            return string.Empty;
        return p.ValueKind == JsonValueKind.String ? (p.GetString() ?? string.Empty) : p.ToString();
    }

    private static int[] ReadIntArray(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var p))
            return [];
        if (p.ValueKind == JsonValueKind.Array)
            return p.EnumerateArray().Select(x => x.TryGetInt32(out var n) ? n : 0).ToArray();
        if (p.ValueKind == JsonValueKind.String)
            return ParseIntList(p.GetString());
        return [];
    }

    private static int[] ParseIntList(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return [];
        return raw.Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => int.TryParse(s, out var n) ? n : 0)
            .ToArray();
    }

    private static decimal ParseDec(string raw) =>
        decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var n) ? n : 0;

    /// <summary>全民代理配置。</summary>
    internal sealed class UserInviteSetting
    {
        /// <summary>直属比例。</summary>
        public decimal BetAmount1 { get; set; }

        /// <summary>二级比例。</summary>
        public decimal BetAmount2 { get; set; }

        /// <summary>三级比例。</summary>
        public decimal BetAmount3 { get; set; }

        /// <summary>流水倍率。</summary>
        public decimal WashFlow { get; set; }

        /// <summary>发放方式。</summary>
        public int Put { get; set; }

        /// <summary>是否自动审。</summary>
        public int IsAuto { get; set; }

        /// <summary>会员分组。</summary>
        public int[] UserGroup { get; set; }
    }

    /// <summary>邀请有礼配置。</summary>
    internal sealed class InviteRewardSetting
    {
        /// <summary>返点类型整型。</summary>
        public int ReturnType { get; set; }

        /// <summary>返点类型名。</summary>
        public string ReturnTypeName { get; set; }

        /// <summary>周期整型。</summary>
        public int ReturnPeriod { get; set; }

        /// <summary>周期名。</summary>
        public string ReturnPeriodName { get; set; }

        /// <summary>模式整型。</summary>
        public int ReturnWay { get; set; }

        /// <summary>模式名。</summary>
        public string ReturnWayName { get; set; }

        /// <summary>推荐人返点值。</summary>
        public decimal ReturnValue1 { get; set; }

        /// <summary>流水倍率。</summary>
        public decimal WashFlow { get; set; }

        /// <summary>发放方式。</summary>
        public int Put { get; set; }

        /// <summary>是否自动审。</summary>
        public int IsAuto { get; set; }

        /// <summary>会员分组。</summary>
        public int[] UserGroup { get; set; }
    }

    /// <summary>洗码配置。</summary>
    internal sealed class WashingSetting
    {
        /// <summary>自动/手动（PutFirst）。</summary>
        public int PutFirst { get; set; }

        /// <summary>发放方式。</summary>
        public int Put { get; set; }

        /// <summary>流水倍率。</summary>
        public decimal WashFlow { get; set; }

        /// <summary>昨日充值门槛。</summary>
        public decimal Recharge { get; set; }

        /// <summary>会员分组。</summary>
        public int[] UserGroup { get; set; }

        /// <summary>限定游戏键 Category.Type。</summary>
        public HashSet<string> GameKeys { get; set; }

        /// <summary>投注档位。</summary>
        public List<WashingRule> Rules { get; set; }
    }

    /// <summary>洗码档位。</summary>
    internal sealed class WashingRule
    {
        /// <summary>档位编号。</summary>
        public int ID { get; set; }

        /// <summary>奖励金额。</summary>
        public decimal Money { get; set; }

        /// <summary>所需投注。</summary>
        public decimal Point { get; set; }

        /// <summary>奖励积分。</summary>
        public short AwardPoint { get; set; }
    }

    /// <summary>邀请任务配置。</summary>
    internal sealed class InviteTaskSetting
    {
        /// <summary>返点类型整型。</summary>
        public int ReturnType { get; set; }

        /// <summary>返点类型名。</summary>
        public string ReturnTypeName { get; set; }

        /// <summary>周期整型。</summary>
        public int ReturnPeriod { get; set; }

        /// <summary>周期名。</summary>
        public string ReturnPeriodName { get; set; }

        /// <summary>模式整型。</summary>
        public int ReturnWay { get; set; }

        /// <summary>模式名。</summary>
        public string ReturnWayName { get; set; }

        /// <summary>流水倍率。</summary>
        public decimal WashFlow { get; set; }

        /// <summary>发放方式。</summary>
        public int Put { get; set; }

        /// <summary>是否自动审。</summary>
        public int IsAuto { get; set; }

        /// <summary>会员分组。</summary>
        public int[] UserGroup { get; set; }

        /// <summary>档位表。</summary>
        public List<InviteTaskItem> Items { get; set; }
    }

    /// <summary>邀请任务档位。</summary>
    internal sealed class InviteTaskItem
    {
        /// <summary>档位编号。</summary>
        public int ID { get; set; }

        /// <summary>下级所需投注。</summary>
        public decimal NeedMoney { get; set; }

        /// <summary>奖励（&gt;1 固定，否则比例）。</summary>
        public decimal Money { get; set; }

        /// <summary>最少达标人数。</summary>
        public int Member { get; set; }

        /// <summary>单人奖励上限。</summary>
        public decimal RewardUpLimit { get; set; }
    }

    /// <summary>擂台赛配置。</summary>
    internal sealed class CompetitionSetting
    {
        /// <summary>周期整型。</summary>
        public int ReturnPeriod { get; set; }

        /// <summary>周期名。</summary>
        public string ReturnPeriodName { get; set; }

        /// <summary>流水倍率。</summary>
        public decimal WashFlow { get; set; }

        /// <summary>上榜起点流水。</summary>
        public decimal StartWashFlow { get; set; }

        /// <summary>发放方式。</summary>
        public int Put { get; set; }

        /// <summary>是否自动审。</summary>
        public int IsAuto { get; set; }

        /// <summary>奖金/积分模式。</summary>
        public int PutT { get; set; }

        /// <summary>奖金/积分模式名。</summary>
        public string PutTName { get; set; }

        /// <summary>会员分组。</summary>
        public int[] UserGroup { get; set; }

        /// <summary>名次奖品（按名次顺序）。</summary>
        public List<CompetitionItem> Items { get; set; }
    }

    /// <summary>擂台赛名次奖。</summary>
    internal sealed class CompetitionItem
    {
        /// <summary>奖品编号。</summary>
        public int ID { get; set; }

        /// <summary>名称。</summary>
        public string Name { get; set; }

        /// <summary>奖金。</summary>
        public decimal Money { get; set; }

        /// <summary>积分。</summary>
        public decimal AwardPoint { get; set; }
    }

    /// <summary>转盘配置。</summary>
    internal sealed class DelotterySetting
    {
        /// <summary>会员分组。</summary>
        public int[] UserGroup { get; set; }

        /// <summary>次数规则。</summary>
        public List<DelotteryRule> Rules { get; set; }
    }

    /// <summary>转盘次数规则。</summary>
    internal sealed class DelotteryRule
    {
        /// <summary>规则编号。</summary>
        public int ID { get; set; }

        /// <summary>发放次数。</summary>
        public int MaxCount { get; set; }

        /// <summary>门槛金额（配置字段名 Recharge）。</summary>
        public decimal Threshold { get; set; }

        /// <summary>条件类型整型。</summary>
        public int ReturnType { get; set; }

        /// <summary>条件类型名。</summary>
        public string ReturnTypeName { get; set; }

        /// <summary>周期整型。</summary>
        public int ReturnPeriod { get; set; }

        /// <summary>周期名。</summary>
        public string ReturnPeriodName { get; set; }
    }
}
