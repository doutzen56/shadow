using System.Globalization;
using System.Xml.Linq;
using SimpX.Net.Data.Entity.Reports;
using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 生日/投注定时积分。读 site_VipConfig.PointItemSetting。
/// </summary>
public sealed class ScheduledPointService : ITransient, IScheduledPointService
{
    /// <summary>旧 PointItem.Birthday。</summary>
    public const int PointTypeBirthday = 10;

    /// <summary>旧 PointItem.Betting。</summary>
    public const int PointTypeBetting = 13;

    /// <summary>旧 ItemStatus.Available。</summary>
    public const int StatusAvailable = 1;

    /// <summary>旧 CalculateType.Fixed。</summary>
    public const int CalcFixed = 0;

    /// <summary>旧 CalculateType.Rate。</summary>
    public const int CalcRate = 1;

    /// <summary>旧 ExceMethod.Once。</summary>
    public const int MethodOnce = 0;

    /// <summary>旧 ExceMethod.OnceADay。</summary>
    public const int MethodOnceADay = 1;

    /// <summary>旧 ExceMethod.Each。</summary>
    public const int MethodEach = 2;

    /// <summary>旧 LockStatus.Point。</summary>
    public const int LockPoint = 64;

    /// <summary>旧 LockStatus.Activity。</summary>
    public const int LockActivity = 128;

    /// <summary>旧 SiteStauts.Stop。</summary>
    public const int SiteStatusStop = 1;

    private readonly ISqlSugarClient _db;
    private readonly IUserPointService _points;

    /// <summary>
    /// 注入业务库与积分服务。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="points">积分</param>
    public ScheduledPointService(IBizDbAccessor biz, IUserPointService points)
    {
        _db = biz.Db;
        _points = points;
    }

    /// <inheritdoc />
    public async Task<int> GrantBirthdayPointsAsync(CancellationToken cancellationToken = default, DateTime? now = null)
    {
        var clock = now ?? DateTime.Now;
        var count = 0;
        var sites = await _db.Queryable<Site>()
            .Where(s => s.Status != SiteStatusStop)
            .Select(s => s.ID)
            .ToListAsync(cancellationToken);

        foreach (var siteId in sites)
        {
            var item = await LoadPointItemAsync(siteId, "Birthday", cancellationToken);
            if (item == null || item.Status != StatusAvailable)
                continue;

            var users = await _db.Queryable<User>()
                .Where(u => u.SiteID == siteId
                            && u.Birthday.Month == clock.Month
                            && u.Birthday.Day == clock.Day)
                .ToListAsync(cancellationToken);

            foreach (var user in users)
            {
                if ((user.Lock & LockActivity) != 0 || (user.Lock & LockPoint) != 0)
                    continue;
                if (!await TryGrantAsync(siteId, user.ID, PointTypeBirthday, item, rawMoney: 1m, clock, cancellationToken))
                    continue;
                count++;
            }
        }

        return count;
    }

    /// <inheritdoc />
    public async Task<int> GrantBettingPointsAsync(CancellationToken cancellationToken = default, DateTime? now = null)
    {
        var clock = now ?? DateTime.Now;
        // 对齐旧 AddPointJob：18 点后跑昨日；测试可直接注入业务日所在「现在」。
        var bizDate = clock.Hour >= 18 ? clock.Date.AddDays(-1) : clock.Date.AddDays(-1);
        if (now.HasValue)
            bizDate = clock.Date.AddDays(-1);

        var count = 0;
        var sites = await _db.Queryable<Site>()
            .Where(s => s.Status != SiteStatusStop)
            .Select(s => s.ID)
            .ToListAsync(cancellationToken);

        foreach (var siteId in sites)
        {
            var item = await LoadPointItemAsync(siteId, "Betting", cancellationToken);
            if (item == null || item.Status != StatusAvailable)
                continue;

            var games = await _db.Queryable<UserDateGame>()
                .Where(g => g.SiteID == siteId && g.Date == bizDate)
                .ToListAsync(cancellationToken);
            var rows = games
                .GroupBy(g => g.UserID)
                .Select(g => new { UserID = g.Key, Bet = g.Sum(x => x.BetMoney) })
                .ToList();

            foreach (var row in rows)
            {
                var users = await _db.Queryable<User>()
                    .Where(u => u.ID == row.UserID && u.SiteID == siteId)
                    .Take(1)
                    .ToListAsync(cancellationToken);
                var user = users.FirstOrDefault();
                if (user == null)
                    continue;
                if ((user.Lock & LockActivity) != 0 || (user.Lock & LockPoint) != 0)
                    continue;
                if (!await TryGrantAsync(siteId, user.ID, PointTypeBetting, item, row.Bet, clock, cancellationToken))
                    continue;
                count++;
            }
        }

        return count;
    }

    private async Task<bool> TryGrantAsync(
        int siteId,
        int userId,
        int pointType,
        PointItemRule item,
        decimal rawMoney,
        DateTime clock,
        CancellationToken cancellationToken)
    {
        if (item.Method == MethodOnce && await ExistsLogAsync(siteId, userId, pointType, null, cancellationToken))
            return false;
        if (item.Method == MethodOnceADay && await ExistsLogAsync(siteId, userId, pointType, clock.Date, cancellationToken))
            return false;

        var point = item.CalcType == CalcFixed ? item.Value : rawMoney;
        if (item.CalcType == CalcRate)
            point = rawMoney * item.Value;
        if (point == 0)
            return false;

        await _points.AddPointAsync(
            siteId,
            userId,
            point,
            pointType,
            string.IsNullOrWhiteSpace(item.Description) ? item.Name : item.Description,
            eventType: 0,
            cancellationToken,
            createAt: clock);
        return true;
    }

    private async Task<bool> ExistsLogAsync(
        int siteId,
        int userId,
        int pointType,
        DateTime? day,
        CancellationToken cancellationToken)
    {
        var q = _db.Queryable<UserPointLog>()
            .Where(l => l.SiteID == siteId && l.UserID == userId && l.Type == pointType);
        if (day.HasValue)
            q = q.Where(l => l.CreateAt >= day.Value && l.CreateAt < day.Value.AddDays(1));
        return await q.AnyAsync(cancellationToken);
    }

    private async Task<PointItemRule> LoadPointItemAsync(int siteId, string name, CancellationToken cancellationToken)
    {
        var configs = await _db.Queryable<SiteVipConfig>()
            .Where(c => c.ID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var xml = configs.FirstOrDefault()?.PointItemSettingString;
        return ParsePointItems(xml).FirstOrDefault(i =>
            string.Equals(i.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 解析 PointItemSetting XML。
    /// </summary>
    internal static List<PointItemRule> ParsePointItems(string xml)
    {
        var list = new List<PointItemRule>();
        if (string.IsNullOrWhiteSpace(xml))
            return list;
        try
        {
            var root = XDocument.Parse(xml);
            foreach (var item in root.Descendants("item"))
            {
                list.Add(new PointItemRule
                {
                    Name = (string)item.Attribute("Name") ?? string.Empty,
                    Description = (string)item.Attribute("Description") ?? string.Empty,
                    Status = ParseEnumByte(item, "Status"),
                    Method = ParseEnumByte(item, "Method"),
                    CalcType = ParseEnumByte(item, "Type"),
                    Value = decimal.TryParse((string)item.Attribute("Value"), NumberStyles.Any, CultureInfo.InvariantCulture, out var v)
                        ? v
                        : 0
                });
            }
        }
        catch
        {
            return list;
        }

        return list;
    }

    private static byte ParseEnumByte(XElement item, string name)
    {
        var raw = (string)item.Attribute(name);
        if (string.IsNullOrWhiteSpace(raw))
            return 0;
        if (byte.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
            return n;
        return raw.ToLowerInvariant() switch
        {
            "available" => StatusAvailable,
            "fixed" => CalcFixed,
            "rate" => CalcRate,
            "once" => MethodOnce,
            "onceaday" => MethodOnceADay,
            "each" => MethodEach,
            _ => 0
        };
    }

    /// <summary>积分项规则。</summary>
    internal sealed class PointItemRule
    {
        /// <summary>名称（Birthday / Betting）。</summary>
        public string Name { get; set; }

        /// <summary>说明。</summary>
        public string Description { get; set; }

        /// <summary>状态。</summary>
        public byte Status { get; set; }

        /// <summary>执行方式。</summary>
        public byte Method { get; set; }

        /// <summary>计算方式。</summary>
        public byte CalcType { get; set; }

        /// <summary>分值或比例。</summary>
        public decimal Value { get; set; }
    }
}
