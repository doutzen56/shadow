using System.Globalization;
using System.Text.Json;
using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 幸运转盘抽奖：扣次数、按权重开奖、现金奖写待审 EventLog。
/// </summary>
public interface IDelotteryDrawService
{
    /// <summary>
    /// 查询会员当前剩余抽奖次数（usr_DelotteryLog 余额合计）。
    /// </summary>
    /// <param name="siteId">站点</param>
    /// <param name="userId">会员</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>剩余次数</returns>
    Task<decimal> GetBalanceAsync(int siteId, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 抽一次：扣 1 次；中现金写 EventLog；再来一次退回 1 次。
    /// </summary>
    /// <param name="siteId">站点</param>
    /// <param name="userId">会员</param>
    /// <param name="eventId">活动编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <param name="now">当前时间；测试可注入</param>
    /// <returns>抽奖结果</returns>
    Task<DelotteryDrawResult> DrawAsync(
        int siteId,
        int userId,
        int eventId,
        CancellationToken cancellationToken = default,
        DateTime? now = null);
}

/// <summary>转盘抽奖结果。</summary>
public sealed class DelotteryDrawResult
{
    /// <summary>是否成功开出。</summary>
    public bool Success { get; set; }

    /// <summary>失败说明。</summary>
    public string ErrorMessage { get; set; }

    /// <summary>奖品编号。</summary>
    public int ItemID { get; set; }

    /// <summary>奖品名。</summary>
    public string ItemName { get; set; }

    /// <summary>奖品类型（0 再来一次 / 1 现金 / 2 实物 / 3 积分）。</summary>
    public int ItemType { get; set; }

    /// <summary>现金奖（待审）。</summary>
    public decimal Reward { get; set; }

    /// <summary>积分奖。</summary>
    public short Coin { get; set; }

    /// <summary>扣次后余额。</summary>
    public decimal Balance { get; set; }

    /// <summary>待审日志 SourceID（现金/实物时）。</summary>
    public string SourceID { get; set; }
}

/// <summary>
/// 转盘开奖：余额来自 usr_DelotteryLog 合计；不依赖 Redis 次数镜像。
/// </summary>
public sealed class DelotteryDrawService : ITransient, IDelotteryDrawService
{
    /// <summary>旧 EventType.Delottery。</summary>
    public const int EventTypeDelottery = 11;

    /// <summary>旧 DelotteryCountItem.Delottery。</summary>
    public const int CountTypeDraw = 9;

    /// <summary>旧 DelotteryCountItem.Repeter。</summary>
    public const int CountTypeRepeter = 8;

    /// <summary>旧 DelotteryType.Repeter。</summary>
    public const int PrizeRepeter = 0;

    /// <summary>旧 DelotteryType.Money。</summary>
    public const int PrizeMoney = 1;

    /// <summary>旧 DelotteryType.Product。</summary>
    public const int PrizeProduct = 2;

    /// <summary>旧 DelotteryType.Point。</summary>
    public const int PrizePoint = 3;

    /// <summary>旧 CheckStatus.None。</summary>
    public const int StatusNone = 0;

    /// <summary>旧 PutType.Automatic。</summary>
    public const int PutAutomatic = 1;

    /// <summary>旧 LockStatus.Activity。</summary>
    public const int LockActivity = 128;

    private readonly ISqlSugarClient _db;
    private readonly IUserActivityLockService _activityLock;
    private readonly Func<int, int>? _randNext;

    /// <summary>
    /// 注入业务库与活动锁。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="activityLock">活动锁</param>
    public DelotteryDrawService(IBizDbAccessor biz, IUserActivityLockService activityLock)
        : this(biz, activityLock, null)
    {
    }

    /// <summary>
    /// 注入业务库、活动锁与可选随机源（单测固定开奖）。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="activityLock">活动锁</param>
    /// <param name="randNext">随机：传入上限（不含），返回 [0,上限)</param>
    public DelotteryDrawService(IBizDbAccessor biz, IUserActivityLockService activityLock, Func<int, int>? randNext)
    {
        _db = biz.Db;
        _activityLock = activityLock ?? new UserActivityLockService(biz);
        _randNext = randNext;
    }

    /// <inheritdoc />
    public async Task<decimal> GetBalanceAsync(int siteId, int userId, CancellationToken cancellationToken = default)
    {
        var points = await _db.Queryable<UserDelotteryLog>()
            .Where(l => l.SiteID == siteId && l.UserID == userId)
            .Select(l => l.Point)
            .ToListAsync(cancellationToken);
        return points.Sum();
    }

    /// <inheritdoc />
    public async Task<DelotteryDrawResult> DrawAsync(
        int siteId,
        int userId,
        int eventId,
        CancellationToken cancellationToken = default,
        DateTime? now = null)
    {
        var clock = now ?? DateTime.Now;
        var events = await _db.Queryable<SiteEvent>()
            .Where(e => e.ID == eventId && e.SiteID == siteId && e.Type == EventTypeDelottery && e.IsOpen)
            .Take(1)
            .ToListAsync(cancellationToken);
        var ev = events.FirstOrDefault();
        if (ev == null)
            return Fail("活动不存在或未开启");
        if (ev.StartAt > clock || ev.ExpireAt < clock)
            return Fail("不在活动时间内");

        var users = await _db.Queryable<User>()
            .Where(u => u.ID == userId && u.SiteID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var user = users.FirstOrDefault();
        if (user == null)
            return Fail("会员不存在");
        // 对齐旧 bet：本人活动锁或上级关活动 → #无奖品可领#
        if (await _activityLock.IsBlockedForActivityAsync(siteId, userId, user.Lock, cancellationToken))
            return Fail("#无奖品可领#");

        var setting = ParseDrawSetting(ev.SettingString);
        if (setting == null || setting.Items.Count == 0)
            return Fail("奖品配置错误");
        if (!GroupAllowed(setting.UserGroup, user.GroupID))
            return Fail("不支持当前会员分组");

        var balance = await GetBalanceAsync(siteId, userId, cancellationToken);
        if (balance < 1)
            return Fail("抽奖次数已用完");

        var available = setting.Items.Where(i => i.Rate > 0 && (i.Count < 0 || i.Remain > 0)).ToList();
        if (available.Count == 0)
            return Fail("奖品已经全部抽取完毕");

        var bag = new List<DrawItem>();
        foreach (var item in available)
        {
            for (var i = 0; i < item.Rate; i++)
                bag.Add(item);
        }

        var pick = bag[_randNext?.Invoke(bag.Count) ?? Random.Shared.Next(bag.Count)];

        // 扣 1 次
        var afterUse = balance - 1;
        await _db.Insertable(new UserDelotteryLog
        {
            SiteID = siteId,
            UserID = userId,
            Point = -1,
            Balance = afterUse,
            Type = CountTypeDraw,
            Content = $"draw-{pick.ID}-{clock:yyyyMMddHHmmss}",
            CreateAt = clock
        }).ExecuteCommandAsync(cancellationToken);

        if (pick.Type == PrizeRepeter)
        {
            afterUse += 1;
            await _db.Insertable(new UserDelotteryLog
            {
                SiteID = siteId,
                UserID = userId,
                Point = 1,
                Balance = afterUse,
                Type = CountTypeRepeter,
                Content = $"repeter-{pick.ID}-{clock:yyyyMMddHHmmss}",
                CreateAt = clock
            }).ExecuteCommandAsync(cancellationToken);

            return new DelotteryDrawResult
            {
                Success = true,
                ItemID = pick.ID,
                ItemName = pick.Name,
                ItemType = pick.Type,
                Balance = afterUse
            };
        }

        var reward = 0m;
        short coin = 0;
        var put = PutAutomatic;
        if (pick.Type == PrizeMoney)
            reward = Math.Round(pick.Value, 2, MidpointRounding.AwayFromZero);
        else if (pick.Type == PrizePoint)
            coin = (short)Math.Round(pick.Value, 0, MidpointRounding.AwayFromZero);
        else if (pick.Type == PrizeProduct)
        {
            reward = Math.Round(pick.Value * setting.Cash, 2, MidpointRounding.AwayFromZero);
            put = 0; // 实物走手动
        }

        string sourceId = null;
        if (reward > 0 || coin > 0)
        {
            sourceId = $"{clock:yyyyMMddHHmmss}-{userId}-{pick.ID}";
            if (sourceId.Length > 64)
                sourceId = sourceId[..64];
            await _db.Insertable(new EventLog
            {
                SiteID = siteId,
                UserID = userId,
                EventID = eventId,
                Type = EventTypeDelottery,
                CreateAt = clock,
                Reward = reward,
                Coin = coin,
                Status = StatusNone,
                ErrorMsg = string.Empty,
                IP = string.Empty,
                IPValue = 0,
                Put = put,
                Flow = reward * setting.WashFlow,
                ExpectedAt = clock,
                ReleaseAt = DateTime.MinValue,
                SourceID = sourceId,
                CheckMessage = $"转盘中奖 {pick.Name}",
                Currency = user.Currency,
                IsAuto = PutAutomatic,
                ReviewerID = 0
            }).ExecuteCommandAsync(cancellationToken);
        }

        return new DelotteryDrawResult
        {
            Success = true,
            ItemID = pick.ID,
            ItemName = pick.Name,
            ItemType = pick.Type,
            Reward = reward,
            Coin = coin,
            Balance = afterUse,
            SourceID = sourceId
        };
    }

    private static DelotteryDrawResult Fail(string msg) => new()
    {
        Success = false,
        ErrorMessage = msg
    };

    private static bool GroupAllowed(int[] groups, int groupId)
    {
        if (groups == null || groups.Length == 0)
            return true;
        if (groups.Length == 1 && groups[0] == 0)
            return true;
        return groups.Contains(groupId);
    }

    private static DrawSetting ParseDrawSetting(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || !raw.TrimStart().StartsWith('{'))
            return null;
        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            var setting = new DrawSetting
            {
                WashFlow = ReadDecimal(root, "WashFlow"),
                Cash = ReadDecimal(root, "Cash"),
                UserGroup = ReadIntArray(root, "UserGroup"),
                Items = []
            };
            if (setting.Cash <= 0)
                setting.Cash = 0.7m;

            var dataRaw = ReadString(root, "DataSetting");
            if (string.IsNullOrWhiteSpace(dataRaw) && root.TryGetProperty("Items", out _))
                dataRaw = raw;
            if (string.IsNullOrWhiteSpace(dataRaw))
                return setting;

            using var dataDoc = JsonDocument.Parse(dataRaw);
            var dataRoot = dataDoc.RootElement;
            if (!dataRoot.TryGetProperty("Items", out var items) || items.ValueKind != JsonValueKind.Array)
                return setting;
            foreach (var item in items.EnumerateArray())
            {
                var count = ReadInt(item, "Count", -1);
                setting.Items.Add(new DrawItem
                {
                    ID = ReadInt(item, "ID"),
                    Name = ReadString(item, "Name"),
                    Type = ReadInt(item, "Type"),
                    Value = ReadDecimal(item, "Value"),
                    Rate = ReadInt(item, "Rate", 1),
                    Count = count,
                    Remain = count
                });
            }

            return setting;
        }
        catch
        {
            return null;
        }
    }

    private static decimal ReadDecimal(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var p))
            return 0;
        return p.ValueKind switch
        {
            JsonValueKind.Number => p.GetDecimal(),
            JsonValueKind.String => decimal.TryParse(p.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var n) ? n : 0,
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
        return [];
    }

    private sealed class DrawSetting
    {
        public decimal WashFlow { get; set; }
        public decimal Cash { get; set; }
        public int[] UserGroup { get; set; }
        public List<DrawItem> Items { get; set; }
    }

    private sealed class DrawItem
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public int Type { get; set; }
        public decimal Value { get; set; }
        public int Rate { get; set; }
        public int Count { get; set; }
        public int Remain { get; set; }
    }
}
