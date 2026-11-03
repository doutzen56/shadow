using System.Globalization;
using SimpX.Net.Data.Entity.Reports;
using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// VIP 周/月/生日礼金。对齐旧 UpdateUserVIPJob + AddPointJob 中的发放段。
/// </summary>
public sealed class VipPeriodicGiftService : ITransient, IVipPeriodicGiftService
{
    /// <summary>旧 Privilege.EveryMonth。</summary>
    public const int PrivilegeEveryMonth = 3;

    /// <summary>旧 Privilege.Birthday。</summary>
    public const int PrivilegeBirthday = 4;

    /// <summary>旧 Privilege.EveryWeek。</summary>
    public const int PrivilegeEveryWeek = 8;

    /// <summary>旧 MoneyType：180 + Privilege。</summary>
    public const int MoneyTypePrivilegeBase = 180;

    /// <summary>旧 MoneyType.Recharge。</summary>
    public const int MoneyTypeRecharge = 1;

    /// <summary>旧 MoneyType.RechargeDue（周/月门槛统计也算进充值）。</summary>
    public const int MoneyTypeRechargeDue = 9;

    /// <summary>旧 LockStatus.Activity。</summary>
    public const int LockActivity = 128;

    /// <summary>旧 SiteStauts.Stop。</summary>
    public const int SiteStatusStop = 1;

    /// <summary>旧 Wallet.SINGLE。</summary>
    public const int WalletSingle = 1;

    /// <summary>旧 UserType.Member。</summary>
    public const int UserTypeMember = 0;

    /// <summary>旧 UserType.Agent。</summary>
    public const int UserTypeAgent = 1;

    /// <summary>旧 UserType.ProxyMember。</summary>
    public const int UserTypeProxyMember = 2;

    /// <summary>旧 UserType.ProxyAgent。</summary>
    public const int UserTypeProxyAgent = 4;

    /// <summary>无效生日哨兵。</summary>
    public static readonly DateTime InvalidBirthday = new(1900, 1, 1);

    /// <summary>周序号锚点（对齐旧 GetWeekOfMYear）。</summary>
    public static readonly DateTime WeekEpoch = new(2024, 4, 22);

    private readonly ISqlSugarClient _db;
    private readonly IMoneyLedger _money;
    private readonly IUserMessageNotifyService _messages;
    private readonly IUserActivityLockService _activityLock;

    /// <summary>
    /// 注入业务库、账本、站内信与活动锁。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="money">账本</param>
    /// <param name="messages">站内信；可空</param>
    /// <param name="activityLock">活动锁；可空</param>
    public VipPeriodicGiftService(
        IBizDbAccessor biz,
        IMoneyLedger money,
        IUserMessageNotifyService messages = null,
        IUserActivityLockService activityLock = null)
    {
        _db = biz.Db;
        _money = money;
        _messages = messages ?? NoOpUserMessageNotifyService.Instance;
        _activityLock = activityLock ?? new UserActivityLockService(biz);
    }

    /// <inheritdoc />
    public async Task<int> GrantEveryWeekAsync(CancellationToken cancellationToken = default, DateTime? now = null)
    {
        var clock = now ?? DateTime.Now;
        var count = 0;
        var sites = await LoadActiveVipSitesAsync(cancellationToken);
        foreach (var site in sites)
        {
            var config = await LoadVipConfigAsync(site.ID, cancellationToken);
            if (config == null)
                continue;
            var privilege = VipUpgradeService.ParsePrivilegeItems(config.VIPRightSetting ?? string.Empty)
                .FirstOrDefault(p => p.Id == PrivilegeEveryWeek);
            if (privilege == null || !privilege.Enabled)
                continue;

            var setting = ParseScheduleSetting(privilege.SettingRaw);
            if ((int)clock.DayOfWeek != setting.Day || setting.Hour is <= 0 or >= 24)
                continue;
            if (!IsWithinHourWindow(clock, clock.Year, clock.Month, clock.Day, setting.Hour))
                continue;

            var weekStart = FirstDayOfWeek(clock.Date);
            var rangeStart = weekStart.AddDays(-7);
            var rangeEnd = weekStart;
            var userIds = await LoadWeekCandidateUserIdsAsync(site.ID, rangeStart, rangeEnd, cancellationToken);
            foreach (var userId in userIds)
            {
                if (await TrySendWeekGiftAsync(site, userId, privilege, clock, cancellationToken))
                    count++;
            }
        }

        return count;
    }

    /// <inheritdoc />
    public async Task<int> GrantEveryMonthAsync(CancellationToken cancellationToken = default, DateTime? now = null)
    {
        var clock = now ?? DateTime.Now;
        var count = 0;
        var sites = await LoadActiveVipSitesAsync(cancellationToken);
        var monthStart = FirstDayOfMonth(clock);
        var rangeStart = monthStart.AddMonths(-1);
        var rangeEnd = monthStart;
        var candidates = await LoadMonthCandidatePairsAsync(rangeStart, rangeEnd, cancellationToken);

        foreach (var site in sites)
        {
            var config = await LoadVipConfigAsync(site.ID, cancellationToken);
            if (config == null)
                continue;
            var privilege = VipUpgradeService.ParsePrivilegeItems(config.VIPRightSetting ?? string.Empty)
                .FirstOrDefault(p => p.Id == PrivilegeEveryMonth);
            if (privilege == null || !privilege.Enabled)
                continue;

            var setting = ParseScheduleSetting(privilege.SettingRaw);
            if (setting.Day is <= 0 or > 28 || setting.Hour is <= 0 or >= 24)
                continue;
            if (!IsWithinHourWindow(clock, clock.Year, clock.Month, setting.Day, setting.Hour))
                continue;
            if (setting.RechargeOrderCount <= 0)
                continue;

            foreach (var (siteId, userId) in candidates.Where(p => p.SiteId == site.ID))
            {
                if (await TrySendMonthGiftAsync(site, userId, privilege, setting.RechargeOrderCount, clock, cancellationToken))
                    count++;
            }
        }

        return count;
    }

    /// <inheritdoc />
    public async Task<int> GrantBirthdayAsync(CancellationToken cancellationToken = default, DateTime? now = null)
    {
        var clock = now ?? DateTime.Now;
        var count = 0;
        var sites = await LoadActiveVipSitesAsync(cancellationToken);
        var users = await _db.Queryable<User>()
            .Where(u => u.Birthday.Month == clock.Month
                        && u.Birthday.Day == clock.Day
                        && (u.Type == UserTypeMember || u.Type == UserTypeAgent))
            .ToListAsync(cancellationToken);

        foreach (var site in sites)
        {
            var config = await LoadVipConfigAsync(site.ID, cancellationToken);
            if (config == null)
                continue;
            var privilege = VipUpgradeService.ParsePrivilegeItems(config.VIPRightSetting ?? string.Empty)
                .FirstOrDefault(p => p.Id == PrivilegeBirthday);
            if (privilege == null || !privilege.Enabled)
                continue;

            foreach (var user in users.Where(u => u.SiteID == site.ID))
            {
                if (await _activityLock.IsBlockedForActivityAsync(site.ID, user.ID, user.Lock, cancellationToken))
                    continue;
                if (user.Birthday.Date == InvalidBirthday)
                    continue;
                if (IsAgentSystem(user.Type))
                    continue;
                if (await TrySendBirthdayGiftAsync(site, user, privilege, clock, cancellationToken))
                    count++;
            }
        }

        return count;
    }

    private async Task<bool> TrySendWeekGiftAsync(
        Site site,
        int userId,
        VipUpgradeService.VipPrivilegeRule privilege,
        DateTime clock,
        CancellationToken cancellationToken)
    {
        var user = await LoadUserAsync(userId, cancellationToken);
        if (user == null || user.SiteID != site.ID || IsAgentSystem(user.Type))
            return false;
        if (await _activityLock.IsBlockedForActivityAsync(site.ID, userId, user.Lock, cancellationToken))
            return false;

        var mark = clock.ToString("yyyyMM", CultureInfo.InvariantCulture) + GetWeekOfMYear(clock);
        if (await HasGiftAsync(userId, PrivilegeEveryWeek, mark, cancellationToken))
            return false;

        privilege.AmountsByLevel.TryGetValue(user.Level, out var raw);
        if (!TryParseTripleCondition(raw, user.Currency, out var gift, out var needRecharge, out var needBet))
            return false;

        var weekStart = FirstDayOfWeek(clock.Date);
        var rangeStart = weekStart.AddDays(-7);
        var moneyR = await SumPeriodRechargeAsync(userId, rangeStart, weekStart, cancellationToken);
        var moneyB = await SumPeriodBetAsync(userId, rangeStart, weekStart, cancellationToken);
        if (moneyR < needRecharge || moneyB < needBet || gift <= 0)
            return false;

        return await PayGiftAsync(
            site,
            user,
            PrivilegeEveryWeek,
            gift,
            privilege.Ratio,
            mark,
            "VIP 每周礼金",
            $"尊敬的 VIP 用户，您当前等级为 VIP{user.Level}，您的每周礼金 {Math.Round(gift, 2)} 已派发至您的账户中心。",
            $"VIP每周礼金 {Math.Round(gift, 2)}",
            cancellationToken);
    }

    private async Task<bool> TrySendMonthGiftAsync(
        Site site,
        int userId,
        VipUpgradeService.VipPrivilegeRule privilege,
        int requireOrderCount,
        DateTime clock,
        CancellationToken cancellationToken)
    {
        var user = await LoadUserAsync(userId, cancellationToken);
        if (user == null || user.SiteID != site.ID || IsAgentSystem(user.Type))
            return false;
        if (await _activityLock.IsBlockedForActivityAsync(site.ID, userId, user.Lock, cancellationToken))
            return false;

        var mark = clock.ToString("yyyyMM", CultureInfo.InvariantCulture);
        if (await HasGiftAsync(userId, PrivilegeEveryMonth, mark, cancellationToken))
            return false;

        privilege.AmountsByLevel.TryGetValue(user.Level, out var raw);
        if (!TryParseTripleCondition(raw, user.Currency, out var gift, out var needRecharge, out var needBet))
            return false;

        var monthStart = FirstDayOfMonth(clock);
        var rangeStart = monthStart.AddMonths(-1);
        var moneyR = await SumPeriodRechargeAsync(userId, rangeStart, monthStart, cancellationToken);
        var moneyB = await SumPeriodBetAsync(userId, rangeStart, monthStart, cancellationToken);
        var orderCount = await SumPeriodOrderCountAsync(userId, rangeStart, monthStart, cancellationToken);
        // 旧月礼：充值/流水用「>」；充值笔数用 >=。
        if (moneyR <= needRecharge || moneyB <= needBet || orderCount < requireOrderCount || gift <= 0)
            return false;

        return await PayGiftAsync(
            site,
            user,
            PrivilegeEveryMonth,
            gift,
            privilege.Ratio,
            mark,
            "VIP每月红包",
            $"尊敬的 VIP 用户，您当前等级为 VIP{user.Level}，您的每月红包 {Math.Round(gift, 2)} 已派发至您的账户中心。",
            $"VIP每月红包 {Math.Round(gift, 2)}",
            cancellationToken);
    }

    private async Task<bool> TrySendBirthdayGiftAsync(
        Site site,
        User user,
        VipUpgradeService.VipPrivilegeRule privilege,
        DateTime clock,
        CancellationToken cancellationToken)
    {
        var mark = clock.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        if (await HasGiftAsync(user.ID, PrivilegeBirthday, mark, cancellationToken))
            return false;

        privilege.AmountsByLevel.TryGetValue(user.Level, out var raw);
        var gift = VipUpgradeService.ParseCurrencyTagged((raw ?? "0").Split(',')[0], user.Currency);
        if (gift <= 0)
            return false;

        return await PayGiftAsync(
            site,
            user,
            PrivilegeBirthday,
            gift,
            privilege.Ratio,
            mark,
            "VIP 生日礼金",
            $"尊敬的 VIP 用户，您当前等级为 VIP{user.Level}，您的生日礼金 {Math.Round(gift, 2)} 已派发至您的账户中心。",
            $"VIP生日礼金 {Math.Round(gift, 2)}",
            cancellationToken);
    }

    private async Task<bool> PayGiftAsync(
        Site site,
        User user,
        int privilegeId,
        decimal gift,
        decimal flowRate,
        string mark,
        string title,
        string content,
        string description,
        CancellationToken cancellationToken)
    {
        var currency = site.Wallet == WalletSingle ? user.Currency : site.Currency;
        var sourceId = $"VIP{privilegeId}-{user.ID}-{mark}";
        if (sourceId.Length > 20)
            sourceId = sourceId[..20];

        await _money.AddMoneyLogAsync(
            user.SiteID,
            user.ID,
            gift,
            MoneyTypePrivilegeBase + privilegeId,
            sourceId,
            description,
            currency,
            cancellationToken,
            flowRate: flowRate,
            minFlow: 0,
            eventId: privilegeId);

        await _db.Insertable(new UserVipGiftLog
        {
            SiteID = user.SiteID,
            UserID = user.ID,
            Money = gift,
            Type = privilegeId,
            Content = description,
            CreateAt = DateTime.Now,
            Mark = mark
        }).ExecuteCommandAsync(cancellationToken);

        await _messages.SendNotifyAsync(user.SiteID, user.ID, content, title, cancellationToken);
        return true;
    }

    private async Task<List<Site>> LoadActiveVipSitesAsync(CancellationToken cancellationToken)
    {
        var sites = await _db.Queryable<Site>()
            .Where(s => s.Status != SiteStatusStop)
            .ToListAsync(cancellationToken);
        return sites
            .Where(s => SiteSettingQuery.GetBool(SiteSettingQuery.Parse(s.SettingString ?? string.Empty), "IsEnabledVIP"))
            .ToList();
    }

    private async Task<SiteVipConfig> LoadVipConfigAsync(int siteId, CancellationToken cancellationToken)
    {
        var rows = await _db.Queryable<SiteVipConfig>()
            .Where(c => c.ID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        return rows.FirstOrDefault();
    }

    private async Task<User> LoadUserAsync(int userId, CancellationToken cancellationToken)
    {
        var rows = await _db.Queryable<User>()
            .Where(u => u.ID == userId)
            .Take(1)
            .ToListAsync(cancellationToken);
        return rows.FirstOrDefault();
    }

    private async Task<bool> HasGiftAsync(int userId, int privilegeId, string mark, CancellationToken cancellationToken) =>
        await _db.Queryable<UserVipGiftLog>()
            .Where(l => l.UserID == userId && l.Type == privilegeId && l.Mark == mark)
            .AnyAsync(cancellationToken);

    private async Task<List<int>> LoadWeekCandidateUserIdsAsync(
        int siteId,
        DateTime startAt,
        DateTime endAt,
        CancellationToken cancellationToken)
    {
        var fromOrders = await _db.Queryable<RechargeOrder>()
            .Where(o => o.SiteID == siteId && o.IsPay && o.PayAt > startAt && o.PayAt < endAt)
            .Select(o => o.UserID)
            .Distinct()
            .ToListAsync(cancellationToken);
        var fromGame = await _db.Queryable<UserDateGame>()
            .Where(g => g.SiteID == siteId && g.Date >= startAt && g.Date < endAt)
            .Select(g => g.UserID)
            .Distinct()
            .ToListAsync(cancellationToken);
        return fromOrders.Concat(fromGame).Distinct().ToList();
    }

    private async Task<List<(int SiteId, int UserId)>> LoadMonthCandidatePairsAsync(
        DateTime startAt,
        DateTime endAt,
        CancellationToken cancellationToken)
    {
        var rows = await _db.Queryable<UserDateMoney>()
            .Where(m => m.Date >= startAt
                        && m.Date <= endAt
                        && (m.Type == MoneyTypeRecharge || m.Type == MoneyTypeRechargeDue))
            .Select(m => new { m.SiteID, m.UserID })
            .Distinct()
            .ToListAsync(cancellationToken);
        return rows.Select(r => (r.SiteID, r.UserID)).ToList();
    }

    private async Task<decimal> SumPeriodRechargeAsync(
        int userId,
        DateTime startAt,
        DateTime endAt,
        CancellationToken cancellationToken)
    {
        return await _db.Queryable<UserDateMoney>()
            .Where(m => m.UserID == userId
                        && m.Date >= startAt
                        && m.Date < endAt
                        && (m.Type == MoneyTypeRecharge || m.Type == MoneyTypeRechargeDue))
            .SumAsync(m => m.Money);
    }

    private async Task<decimal> SumPeriodBetAsync(
        int userId,
        DateTime startAt,
        DateTime endAt,
        CancellationToken cancellationToken)
    {
        return await _db.Queryable<UserDateGame>()
            .Where(g => g.UserID == userId && g.Date >= startAt && g.Date < endAt)
            .SumAsync(g => g.BetMoney);
    }

    private async Task<int> SumPeriodOrderCountAsync(
        int userId,
        DateTime startAt,
        DateTime endAt,
        CancellationToken cancellationToken)
    {
        // 对齐旧 GetLastMonthRechargeOrderCount：上月全部资金日汇总 Count 之和。
        var sum = await _db.Queryable<UserDateMoney>()
            .Where(m => m.UserID == userId && m.Date >= startAt && m.Date < endAt)
            .SumAsync(m => m.Count);
        return sum;
    }

    /// <summary>
    /// 解析特权内嵌 Day/Hour/RechargeOrderCount（querystring）。
    /// </summary>
    internal static ScheduleSetting ParseScheduleSetting(string raw)
    {
        var map = SiteSettingQuery.Parse(raw ?? string.Empty);
        return new ScheduleSetting
        {
            Day = ParseInt(SiteSettingQuery.Get(map, "Day")),
            Hour = ParseInt(SiteSettingQuery.Get(map, "Hour")),
            RechargeOrderCount = ParseInt(SiteSettingQuery.Get(map, "RechargeOrderCount"))
        };
    }

    /// <summary>
    /// 解析 L{n}=礼金|充值门槛|流水门槛。
    /// </summary>
    internal static bool TryParseTripleCondition(
        string raw,
        int currency,
        out decimal gift,
        out decimal needRecharge,
        out decimal needBet)
    {
        gift = 0;
        needRecharge = 0;
        needBet = 0;
        if (string.IsNullOrWhiteSpace(raw))
            return false;
        var text = raw.Contains("%7B", StringComparison.OrdinalIgnoreCase)
            ? Uri.UnescapeDataString(raw)
            : raw;
        var parts = text.Split('|');
        if (parts.Length != 3)
            return false;
        gift = VipUpgradeService.ParseCurrencyTagged(parts[0], currency);
        needRecharge = VipUpgradeService.ParseCurrencyTagged(parts[1], currency);
        needBet = VipUpgradeService.ParseCurrencyTagged(parts[2], currency);
        return true;
    }

    /// <summary>
    /// 对齐旧 GetWeekOfMYear（自 2024-04-22 起的整周序号）。
    /// </summary>
    /// <param name="dt">日期</param>
    /// <returns>周序号</returns>
    public static int GetWeekOfMYear(DateTime dt) =>
        (int)(dt.Date - WeekEpoch.Date).TotalDays / 7;

    /// <summary>
    /// 本周第一天（周一；周日回退到上周一）。
    /// </summary>
    /// <param name="date">任意日</param>
    /// <returns>周一 0 点</returns>
    public static DateTime FirstDayOfWeek(DateTime date)
    {
        var dt = date.Date;
        return dt.DayOfWeek == DayOfWeek.Sunday
            ? dt.AddDays(-6)
            : dt.AddDays(1 - (int)dt.DayOfWeek);
    }

    /// <summary>
    /// 本月第一天。
    /// </summary>
    /// <param name="dt">任意日</param>
    /// <returns>月初</returns>
    public static DateTime FirstDayOfMonth(DateTime dt) =>
        new(dt.Year, dt.Month, 1);

    /// <summary>
    /// 是否在发放整点起一小时内。
    /// </summary>
    public static bool IsWithinHourWindow(DateTime now, int year, int month, int day, int hour)
    {
        var open = new DateTime(year, month, day, hour, 0, 0);
        return open <= now && (now - open).TotalSeconds < 3600;
    }

    private static bool IsAgentSystem(int type) =>
        type is UserTypeProxyMember or UserTypeProxyAgent;

    private static int ParseInt(string raw) =>
        int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : 0;

    /// <summary>周/月发放时间与门槛。</summary>
    internal sealed class ScheduleSetting
    {
        /// <summary>日或星期（WeekDay 0-6 / 月日 1-28）。</summary>
        public int Day { get; set; }

        /// <summary>整点小时。</summary>
        public int Hour { get; set; }

        /// <summary>上月充值笔数下限（月礼）。</summary>
        public int RechargeOrderCount { get; set; }
    }
}
