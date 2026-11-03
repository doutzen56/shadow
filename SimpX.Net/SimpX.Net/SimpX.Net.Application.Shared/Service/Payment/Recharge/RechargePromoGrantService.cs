using System.Globalization;
using System.Net;
using System.Text.Json;
using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 充值相关活动发奖：Recharge / RechargeByLevel / FirstRecharge → usr_EventLog（待审）。
/// 对齐旧 ResetOrderAddEvent + FirstOrderAddEvent 的最小可验集；上限缓存/IP 地区等后置。
/// </summary>
public sealed class RechargePromoGrantService : ITransient, IRechargePromoGrantService
{
    /// <summary>旧 EventType.Recharge。</summary>
    public const int EventTypeRecharge = 9;

    /// <summary>旧 EventType.FirstRecharge。</summary>
    public const int EventTypeFirstRecharge = 19;

    /// <summary>旧 EventType.RechargeByLevel。</summary>
    public const int EventTypeRechargeByLevel = 20;

    /// <summary>旧 CheckStatus.None。</summary>
    public const int StatusNone = 0;

    /// <summary>旧 EventLog.PutType.Manual。</summary>
    public const int PutManual = 0;

    /// <summary>旧 EventLog.PutType.Automatic。</summary>
    public const int PutAutomatic = 1;

    /// <summary>旧 Wallet.SINGLE。</summary>
    public const int WalletSingle = 1;

    private readonly ISqlSugarClient _db;

    /// <summary>
    /// 注入业务库。
    /// </summary>
    /// <param name="biz">业务库</param>
    public RechargePromoGrantService(IBizDbAccessor biz)
    {
        _db = biz.Db;
    }

    /// <inheritdoc />
    public async Task<int> GrantForOrderAsync(string orderId, CancellationToken cancellationToken = default)
    {
        var id = (orderId ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(id))
            return 0;

        var orders = await _db.Queryable<RechargeOrder>()
            .Where(o => o.ID == id)
            .Take(1)
            .ToListAsync(cancellationToken);
        var order = orders.FirstOrDefault();
        if (order == null)
            return 0;

        var siteWallet = (await _db.Queryable<Site>()
            .Where(s => s.ID == order.SiteID)
            .Select(s => s.Wallet)
            .Take(1)
            .ToListAsync(cancellationToken)).FirstOrDefault();
        var isSingle = siteWallet == WalletSingle;

        var userRows = await _db.Queryable<User>()
            .Where(u => u.ID == order.UserID && u.SiteID == order.SiteID)
            .Select(u => new { u.GroupID, u.Currency, u.Lock })
            .Take(1)
            .ToListAsync(cancellationToken);
        var user = userRows.FirstOrDefault();
        if (user == null)
            return 0;
        if ((user.Lock & EventLogCheckService.LockActivity) != 0)
            return 0;

        var groupId = user.GroupID;
        var memberCurrency = user.Currency;
        var activeIds = ParseActiveIds(order.ActiveID);
        var written = 0;

        // 会员勾选的充值/阶梯活动
        if (activeIds.Count > 0)
        {
            foreach (var eventId in activeIds)
            {
                var ev = await LoadOpenEventAsync(order.SiteID, eventId, cancellationToken);
                if (ev == null)
                    continue;
                if (ev.Type != EventTypeRecharge && ev.Type != EventTypeRechargeByLevel)
                    continue;

                written += await TryInsertAsync(order, ev, groupId, memberCurrency, isSingle, cancellationToken);
            }
        }

        // 首充：首次到账（Count==1）尝试站点开启的首充活动
        if (order.Count == 1)
        {
            var firstEvents = await _db.Queryable<SiteEvent>()
                .Where(e =>
                    e.SiteID == order.SiteID
                    && e.IsOpen
                    && e.Type == EventTypeFirstRecharge
                    && e.StartAt <= DateTime.Now
                    && e.ExpireAt >= DateTime.Now)
                .ToListAsync(cancellationToken);

            foreach (var ev in firstEvents)
            {
                // ActiveID 非空时，仅处理勾选了的首充；全 0/空则尝试全部开启的首充
                if (activeIds.Count > 0 && !activeIds.Contains(ev.ID))
                    continue;
                written += await TryInsertAsync(order, ev, groupId, memberCurrency, isSingle, cancellationToken);
            }
        }

        return written;
    }

    private async Task<int> TryInsertAsync(
        RechargeOrder order,
        SiteEvent ev,
        int groupId,
        int memberCurrency,
        bool isSingle,
        CancellationToken cancellationToken)
    {
        EventLog log;
        string err;
        var ok = ev.Type switch
        {
            EventTypeRecharge => TryBuildRechargeLog(order, ev, groupId, memberCurrency, isSingle, out log, out err),
            EventTypeRechargeByLevel => TryBuildByLevelLog(order, ev, groupId, memberCurrency, isSingle, out log, out err),
            EventTypeFirstRecharge => TryBuildFirstRechargeLog(order, ev, groupId, memberCurrency, isSingle, out log, out err),
            _ => Fail(out log, out err, "#不支持的活动#")
        };
        if (!ok)
        {
            Log.Warning($"充值活动跳过 EventID={ev.ID} Type={ev.Type} Order={order.ID}：{err}");
            return 0;
        }

        var dup = await _db.Queryable<EventLog>()
            .Where(e => e.EventID == log.EventID && e.UserID == log.UserID && e.SourceID == log.SourceID)
            .AnyAsync(cancellationToken);
        if (dup)
            return 0;

        await _db.Insertable(log).ExecuteCommandAsync(cancellationToken);
        return 1;
    }

    private async Task<SiteEvent> LoadOpenEventAsync(int siteId, int eventId, CancellationToken cancellationToken)
    {
        var rows = await _db.Queryable<SiteEvent>()
            .Where(e => e.SiteID == siteId && e.ID == eventId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var ev = rows.FirstOrDefault();
        if (ev == null || !ev.IsOpen)
            return null;
        if (DateTime.Now < ev.StartAt || DateTime.Now > ev.ExpireAt)
            return null;
        return ev;
    }

    /// <summary>
    /// 普通充值优惠。
    /// </summary>
    private static bool TryBuildRechargeLog(
        RechargeOrder order,
        SiteEvent ev,
        int groupId,
        int memberCurrency,
        bool isSingle,
        out EventLog log,
        out string error)
    {
        log = null;
        error = string.Empty;
        var map = ParseSettingMap(ev.SettingString);
        var money = ResolveMoney(order, map);
        var rate = 1m;

        if (!AllowPayId(map, order.PayID))
        {
            error = "#不支持当前充值方式#";
            return false;
        }

        if (!AllowGroup(map, groupId))
        {
            error = "#不支持当前会员所在分组#";
            return false;
        }

        var minMoney = ParseCurrencyTagged(SiteSettingQuery.Get(map, "MinMoney"), order.Currency);
        if (minMoney > 0 && money < minMoney)
        {
            error = "#小于最低额度#";
            return false;
        }

        var rewardPct = ParseDecimal(SiteSettingQuery.Get(map, "Reward"));
        var fixedReward = ParseCurrencyTagged(SiteSettingQuery.Get(map, "FixedReward"), order.Currency);
        var reward = Math.Round(money * rewardPct / 100m, 2, MidpointRounding.AwayFromZero) + fixedReward * rate;
        reward = Math.Round(reward, 2, MidpointRounding.AwayFromZero);
        if (reward <= 0)
        {
            error = "#超过赠送上限#";
            return false;
        }

        log = BuildLog(
            order,
            ev,
            EventTypeRecharge,
            reward,
            ResolveFlow(map, reward, order.Currency, rate),
            coin: 0,
            sourceId: order.ID,
            memberCurrency,
            isSingle,
            map);
        return true;
    }

    /// <summary>
    /// 阶梯存款优惠：按 DataSetting.Items 取最高满足档。
    /// </summary>
    private static bool TryBuildByLevelLog(
        RechargeOrder order,
        SiteEvent ev,
        int groupId,
        int memberCurrency,
        bool isSingle,
        out EventLog log,
        out string error)
    {
        log = null;
        error = string.Empty;
        var map = ParseSettingMap(ev.SettingString);
        if (!AllowPayId(map, order.PayID) || !AllowGroup(map, groupId))
        {
            error = !AllowPayId(map, order.PayID) ? "#不支持当前充值方式#" : "#不支持当前会员所在分组#";
            return false;
        }

        var money = ResolveMoney(order, map);
        var rate = 1m;
        var items = ParseLevelItems(map);
        if (items.Count == 0)
        {
            error = "#奖品设置错误#";
            return false;
        }

        var matched = items
            .Where(i => i.NeedMoney <= money)
            .OrderByDescending(i => i.NeedMoney)
            .FirstOrDefault();
        if (matched == null)
        {
            error = "#未满足获奖条件#";
            return false;
        }

        var returnWay = SiteSettingQuery.Get(map, "ReturnWay");
        decimal reward;
        if (string.Equals(returnWay, "Rate", StringComparison.OrdinalIgnoreCase) || returnWay == "1")
            reward = Math.Round(money * matched.Money * rate, 2, MidpointRounding.AwayFromZero);
        else
            reward = Math.Round(matched.Money * rate, 2, MidpointRounding.AwayFromZero);

        if (reward <= 0)
        {
            error = "#超过赠送上限#";
            return false;
        }

        var flow = Math.Round(reward * matched.WashFlow, 2, MidpointRounding.AwayFromZero);
        log = BuildLog(
            order,
            ev,
            EventTypeRechargeByLevel,
            reward,
            flow,
            matched.AwardPoint,
            order.ID,
            memberCurrency,
            isSingle,
            map);
        log.ErrorMsg = matched.Name ?? string.Empty;
        return true;
    }

    /// <summary>
    /// 首充优惠：按 DataSetting.Items 取档；SourceID 用日期-档-用户，避免与订单号撞唯一。
    /// </summary>
    private static bool TryBuildFirstRechargeLog(
        RechargeOrder order,
        SiteEvent ev,
        int groupId,
        int memberCurrency,
        bool isSingle,
        out EventLog log,
        out string error)
    {
        log = null;
        error = string.Empty;
        var map = ParseSettingMap(ev.SettingString);
        if (!AllowPayId(map, order.PayID) || !AllowGroup(map, groupId))
        {
            error = !AllowPayId(map, order.PayID) ? "#不支持当前充值方式#" : "#不支持当前会员所在分组#";
            return false;
        }

        var money = ResolveMoney(order, map);
        var rate = 1m;
        var items = ParseFirstItems(map);
        if (items.Count == 0)
        {
            error = "#奖品设置错误#";
            return false;
        }

        var matched = items
            .Where(i =>
                (!i.IsFixedAmount && i.NeedMoney <= money)
                || (i.IsFixedAmount && i.NeedMoney == money))
            .OrderByDescending(i => i.NeedMoney)
            .FirstOrDefault();
        if (matched == null)
        {
            error = "#未满足获奖条件#";
            return false;
        }

        var returnWay = SiteSettingQuery.Get(map, "ReturnWay");
        decimal reward;
        if (string.Equals(returnWay, "Rate", StringComparison.OrdinalIgnoreCase) || returnWay == "1")
            reward = Math.Round(money * matched.Money * rate, 2, MidpointRounding.AwayFromZero);
        else
            reward = Math.Round(matched.Money * rate, 2, MidpointRounding.AwayFromZero);

        var upLimit = ParseCurrencyTagged(SiteSettingQuery.Get(map, "RewardUpLimit"), order.Currency);
        if (upLimit > 0 && reward > upLimit)
            reward = upLimit;
        reward = Math.Round(reward, 2, MidpointRounding.AwayFromZero);
        if (reward <= 0)
        {
            error = "#未满足获奖条件#";
            return false;
        }

        var sourceId = $"{DateTime.Now:yyyyMMdd}-{matched.Id}-{order.UserID}";
        log = BuildLog(
            order,
            ev,
            EventTypeFirstRecharge,
            reward,
            ResolveFlow(map, reward, order.Currency, rate),
            coin: 0,
            sourceId,
            memberCurrency,
            isSingle,
            map);
        log.ErrorMsg = "#首充优惠# " + (matched.Name ?? string.Empty) + " #奖励#";
        return true;
    }

    private static EventLog BuildLog(
        RechargeOrder order,
        SiteEvent ev,
        int type,
        decimal reward,
        decimal flow,
        short coin,
        string sourceId,
        int memberCurrency,
        bool isSingle,
        Dictionary<string, string> map)
    {
        var put = ParsePut(SiteSettingQuery.Get(map, "Put"));
        var isAuto = ParsePut(SiteSettingQuery.Get(map, "IsAuto"));
        if (type == EventTypeFirstRecharge && isAuto == PutManual)
        {
            // 首充常用 PutFirst；没有 IsAuto 时跟 Put
            var putFirst = SiteSettingQuery.Get(map, "PutFirst");
            if (!string.IsNullOrWhiteSpace(putFirst))
                isAuto = ParsePut(putFirst);
            else
                isAuto = put;
        }

        return new EventLog
        {
            SiteID = order.SiteID,
            UserID = order.UserID,
            EventID = ev.ID,
            Type = type,
            CreateAt = DateTime.Now,
            Reward = reward,
            Coin = coin,
            Status = StatusNone,
            ErrorMsg = string.Empty,
            IP = order.IP ?? string.Empty,
            IPValue = 0,
            Put = put,
            Flow = flow,
            ExpectedAt = DateTime.Now,
            ReleaseAt = DateTime.MinValue,
            SourceID = sourceId,
            CheckMessage = string.Empty,
            Currency = isSingle ? memberCurrency : order.Currency,
            IsAuto = isAuto,
            ReviewerID = 0
        };
    }

    private static bool Fail(out EventLog log, out string error, string msg)
    {
        log = null;
        error = msg;
        return false;
    }

    private static List<int> ParseActiveIds(string active)
    {
        var raw = (active ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(raw) || raw == "0")
            return [];
        var list = new List<int>();
        foreach (var part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (int.TryParse(part, out var id) && id > 0)
                list.Add(id);
        }

        return list;
    }

    private static decimal ResolveMoney(RechargeOrder order, Dictionary<string, string> map)
    {
        if (string.Equals(SiteSettingQuery.Get(map, "DiscountType"), "Amount", StringComparison.OrdinalIgnoreCase))
            return order.Money;
        return order.Paid;
    }

    private static Dictionary<string, string> ParseSettingMap(string setting)
    {
        var map = SiteSettingQuery.Parse(setting ?? string.Empty);
        MergeJsonSetting(setting, map);
        return map;
    }

    private static List<LevelItem> ParseLevelItems(Dictionary<string, string> map)
    {
        var json = SiteSettingQuery.Get(map, "DataSetting");
        if (string.IsNullOrWhiteSpace(json))
            json = SiteSettingQuery.Get(map, "Items");
        json = DecodeMaybeHtml(json);
        var result = new List<LevelItem>();
        if (string.IsNullOrWhiteSpace(json))
            return result;
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var arr = root.ValueKind == JsonValueKind.Array
                ? root
                : root.TryGetProperty("Items", out var items) ? items : default;
            if (arr.ValueKind != JsonValueKind.Array)
                return result;
            foreach (var el in arr.EnumerateArray())
            {
                var need = ReadDecimalProp(el, "needMoney", "NeedMoney");
                var money = ReadDecimalProp(el, "Money", "money");
                var wash = ReadDecimalProp(el, "WashFlow", "washFlow");
                short point = 0;
                if (el.TryGetProperty("AwardPoint", out var ap) && ap.TryGetInt32(out var p))
                    point = (short)p;
                var name = el.TryGetProperty("Name", out var n) ? n.GetString() : string.Empty;
                result.Add(new LevelItem(need, money, wash, point, name));
            }
        }
        catch (JsonException)
        {
            // ignore
        }

        return result;
    }

    private static List<FirstItem> ParseFirstItems(Dictionary<string, string> map)
    {
        var json = SiteSettingQuery.Get(map, "DataSetting");
        if (string.IsNullOrWhiteSpace(json))
            json = SiteSettingQuery.Get(map, "Items");
        json = DecodeMaybeHtml(json);
        var result = new List<FirstItem>();
        if (string.IsNullOrWhiteSpace(json))
            return result;
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var arr = root.ValueKind == JsonValueKind.Array
                ? root
                : root.TryGetProperty("Items", out var items) ? items : default;
            if (arr.ValueKind != JsonValueKind.Array)
                return result;
            foreach (var el in arr.EnumerateArray())
            {
                var id = 0;
                if (el.TryGetProperty("ID", out var idEl))
                    idEl.TryGetInt32(out id);
                var need = ReadDecimalProp(el, "needMoney", "NeedMoney");
                var money = ReadDecimalProp(el, "Money", "money");
                var isFixed = false;
                if (el.TryGetProperty("IsFixedAmount", out var fix))
                {
                    isFixed = fix.ValueKind switch
                    {
                        JsonValueKind.True => true,
                        JsonValueKind.String => fix.GetString() == "1" || bool.TryParse(fix.GetString(), out var b) && b,
                        JsonValueKind.Number => fix.GetInt32() == 1,
                        _ => false
                    };
                }

                var name = el.TryGetProperty("Name", out var n) ? n.GetString() : string.Empty;
                result.Add(new FirstItem(id, need, money, isFixed, name));
            }
        }
        catch (JsonException)
        {
            // ignore
        }

        return result;
    }

    private static string DecodeMaybeHtml(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;
        var s = raw.Trim();
        if (s.Contains("&quot;", StringComparison.OrdinalIgnoreCase)
            || s.Contains("&lt;", StringComparison.OrdinalIgnoreCase)
            || s.Contains("%7B", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                s = WebUtility.HtmlDecode(s);
                s = Uri.UnescapeDataString(s.Replace("+", " "));
            }
            catch
            {
                // keep
            }
        }

        return s;
    }

    private static decimal ReadDecimalProp(JsonElement el, params string[] names)
    {
        foreach (var name in names)
        {
            if (!el.TryGetProperty(name, out var p))
                continue;
            if (p.ValueKind == JsonValueKind.Number && p.TryGetDecimal(out var d))
                return d;
            if (p.ValueKind == JsonValueKind.String)
                return ParseCurrencyTagged(p.GetString(), 0);
        }

        return 0;
    }

    private static decimal ResolveFlow(Dictionary<string, string> map, decimal reward, int currency, decimal rate)
    {
        var withdraw = SiteSettingQuery.Get(map, "Withdraw");
        if (string.Equals(withdraw, "None", StringComparison.OrdinalIgnoreCase) || withdraw == "3")
            return 0;
        if (string.Equals(withdraw, "Fixed", StringComparison.OrdinalIgnoreCase) || withdraw == "0")
            return ParseCurrencyTagged(SiteSettingQuery.Get(map, "WithdrawFixed"), currency) * rate;
        if (string.Equals(withdraw, "Times", StringComparison.OrdinalIgnoreCase) || withdraw == "1")
            return ParseDecimal(SiteSettingQuery.Get(map, "WithdrawTimes")) * reward;
        var times = ParseDecimal(SiteSettingQuery.Get(map, "AmountWithdrawTimes"));
        return times > 0 ? times * reward : 0;
    }

    private static int ParsePut(string raw)
    {
        if (string.Equals(raw, "Automatic", StringComparison.OrdinalIgnoreCase) || raw == "1")
            return PutAutomatic;
        return PutManual;
    }

    private static bool AllowPayId(Dictionary<string, string> map, int payId)
    {
        var raw = SiteSettingQuery.Get(map, "Payment");
        if (string.IsNullOrWhiteSpace(raw))
            return true;
        foreach (var part in raw.Split(new[] { ',', '|', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (int.TryParse(part, out var id) && id == payId)
                return true;
        }

        return false;
    }

    private static bool AllowGroup(Dictionary<string, string> map, int groupId)
    {
        var raw = SiteSettingQuery.Get(map, "UserGroup");
        if (string.IsNullOrWhiteSpace(raw))
            return true;
        foreach (var part in raw.Split(new[] { ',', '|', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (int.TryParse(part, out var id) && id == groupId)
                return true;
        }

        return false;
    }

    private static decimal ParseDecimal(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return 0;
        return decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var n) ? n : 0;
    }

    private static decimal ParseCurrencyTagged(string raw, int currency)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return 0;
        if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var plain))
            return plain;
        if (currency <= 0)
            return 0;

        var token = currency.ToString(CultureInfo.InvariantCulture) + ":";
        foreach (var part in raw.Split(new[] { '|', ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!part.StartsWith(token, StringComparison.OrdinalIgnoreCase))
                continue;
            if (decimal.TryParse(part[token.Length..], NumberStyles.Any, CultureInfo.InvariantCulture, out var n))
                return n;
        }

        return 0;
    }

    private static void MergeJsonSetting(string setting, Dictionary<string, string> map)
    {
        if (string.IsNullOrWhiteSpace(setting) || !setting.TrimStart().StartsWith('{'))
            return;
        try
        {
            using var doc = JsonDocument.Parse(setting);
            foreach (var p in doc.RootElement.EnumerateObject())
            {
                if (map.ContainsKey(p.Name))
                    continue;
                map[p.Name] = p.Value.ValueKind switch
                {
                    JsonValueKind.String => p.Value.GetString() ?? string.Empty,
                    JsonValueKind.Number => p.Value.ToString(),
                    JsonValueKind.True => bool.TrueString,
                    JsonValueKind.False => bool.FalseString,
                    JsonValueKind.Array or JsonValueKind.Object => p.Value.GetRawText(),
                    _ => p.Value.ToString()
                };
            }
        }
        catch (JsonException)
        {
            // ignore
        }
    }

    private sealed record LevelItem(decimal NeedMoney, decimal Money, decimal WashFlow, short AwardPoint, string Name);

    private sealed record FirstItem(int Id, decimal NeedMoney, decimal Money, bool IsFixedAmount, string Name);
}
