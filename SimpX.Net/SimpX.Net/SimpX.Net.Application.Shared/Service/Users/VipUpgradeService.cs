using System.Globalization;
using System.Xml.Linq;
using SimpX.Net.Data.Entity.Reports;
using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// VIP 升级。对齐旧 ExeUpgrade：开站开关 → 保级窗口 → 累计存款/流水条件 → 改 Level → 升级日志 → 晋级礼金。
/// </summary>
public sealed class VipUpgradeService : ITransient, IVipUpgradeService
{
    /// <summary>旧 LockStatus.Upgrade。</summary>
    public const int LockUpgrade = 256;

    /// <summary>旧 Privilege.Upgrade。</summary>
    public const int PrivilegeUpgrade = 2;

    /// <summary>旧 MoneyType：180 + Privilege.Upgrade。</summary>
    public const int MoneyTypeUpgradeGift = 182;

    /// <summary>旧 MoneyType.Recharge。</summary>
    public const int MoneyTypeRecharge = 1;

    /// <summary>旧 MoneyType.ProxyRecharge。</summary>
    public const int MoneyTypeProxyRecharge = 111;

    /// <summary>旧 UserType.ProxyMember。</summary>
    public const int UserTypeProxyMember = 2;

    /// <summary>旧 UserType.ProxyAgent。</summary>
    public const int UserTypeProxyAgent = 4;

    /// <summary>旧 Wallet.SINGLE。</summary>
    public const int WalletSingle = 1;

    /// <summary>保级窗口：上次升级后 90 天内不查保级。</summary>
    public const int HoldGraceDays = 90;

    private static readonly DateTime DefaultVipStart = new(2018, 1, 1);

    private readonly ISqlSugarClient _db;
    private readonly IMoneyLedger _money;
    private readonly IUserMessageNotifyService _messages;

    /// <summary>
    /// 注入业务库、账本与站内信。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="money">账本；晋级礼金走加款</param>
    /// <param name="messages">站内信；可空</param>
    public VipUpgradeService(
        IBizDbAccessor biz,
        IMoneyLedger money,
        IUserMessageNotifyService messages = null)
    {
        _db = biz.Db;
        _money = money;
        _messages = messages ?? NoOpUserMessageNotifyService.Instance;
    }

    /// <inheritdoc />
    public async Task<bool> TryUpgradeAsync(int userId, CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
            return false;

        var users = await _db.Queryable<User>()
            .Where(u => u.ID == userId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var user = users.FirstOrDefault();
        if (user == null)
            return false;
        if (IsAgentSystem(user.Type))
            return false;
        if ((user.Lock & LockUpgrade) != 0)
            return false;

        var sites = await _db.Queryable<Site>()
            .Where(s => s.ID == user.SiteID)
            .Take(1)
            .ToListAsync(cancellationToken);
        var site = sites.FirstOrDefault();
        if (site == null)
            return false;
        if (!SiteSettingQuery.GetBool(SiteSettingQuery.Parse(site.SettingString ?? string.Empty), "IsEnabledVIP"))
            return false;

        // 旧 DoDowngrade 已关闭：保级不达标只阻断本轮升级，不降级。
        if (!await CheckIsHoldAsync(user, cancellationToken))
            return false;

        var (canUpgrade, newLevel) = await TryResolveNewLevelAsync(user, site, cancellationToken);
        if (!canUpgrade)
            return false;

        if (!await DoUpgradeAsync(user, newLevel, cancellationToken))
            return false;

        // 礼金失败不影响已升级；幂等靠 usr_VipGiftLog.Mark=新等级。
        try
        {
            await TrySendUpgradeGiftAsync(user, site, newLevel, cancellationToken);
        }
        catch (Exception ex)
        {
            try { Log.Warning($"VIP 晋级礼金失败 userId={userId}：{ex.Message}"); }
            catch { /* 单测无 Furion 宿主 */ }
        }

        return true;
    }

    /// <summary>
    /// 保级检查。90 天内或无升级记录视为通过；否则按当前档 Hold2 流水要求，不达标返回 false。
    /// </summary>
    private async Task<bool> CheckIsHoldAsync(User user, CancellationToken cancellationToken)
    {
        var lastUpgrade = await GetLastUpgradeAtAsync(user.ID, cancellationToken);
        if (lastUpgrade == DateTime.MinValue || lastUpgrade > DateTime.Now.AddDays(-HoldGraceDays))
            return true;

        var config = await LoadVipConfigAsync(user.SiteID, cancellationToken);
        if (config == null || string.IsNullOrWhiteSpace(config.VIPConditionSetting))
            return true;

        var levels = ParseLevelItems(config.VIPConditionSetting);
        var current = levels.FirstOrDefault(t => t.Level == user.Level);
        if (current == null)
            return true;

        var startAt = lastUpgrade;
        var bet = await SumBetAsync(user.ID, startAt, cancellationToken);
        return bet >= current.Hold2;
    }

    /// <summary>
    /// 按等级条件算出目标档；对齐旧 CheckIsUpgrade（InitLevel 门槛相减 + AND/OR）。
    /// </summary>
    private async Task<(bool Ok, byte NewLevel)> TryResolveNewLevelAsync(
        User user,
        Site site,
        CancellationToken cancellationToken)
    {
        var newLevel = user.Level;
        var config = await LoadVipConfigAsync(user.SiteID, cancellationToken);
        if (config == null || string.IsNullOrWhiteSpace(config.VIPConditionSetting))
            return (false, newLevel);

        var levels = ParseLevelItems(config.VIPConditionSetting)
            .Where(t => !t.UpgradeClose && t.Level >= user.InitLevel)
            .OrderBy(t => t.Level)
            .ToList();
        if (levels.Count == 0)
            return (false, newLevel);

        var initItem = levels.FirstOrDefault(t => t.Level == user.InitLevel);
        // 会员币种标签解析门槛；本波不做跨币种汇率折算（与单一钱包常见路径一致）。
        var requireRechargeSub = ParseCurrencyTagged(initItem?.Upgrade3Raw ?? "0", user.Currency);
        var requireBetSub = ParseCurrencyTagged(initItem?.Upgrade4Raw ?? "0", user.Currency);

        levels = levels.Where(t => t.Level >= user.Level).ToList();
        var startAt = DefaultVipStart;
        var recharge = await SumRechargeAsync(user.ID, startAt, cancellationToken);
        var bet = await SumBetAsync(user.ID, startAt, cancellationToken);

        foreach (var item in levels)
        {
            var requireRecharge = ParseCurrencyTagged(item.Upgrade3Raw, user.Currency) - requireRechargeSub;
            var requireBet = ParseCurrencyTagged(item.Upgrade4Raw, user.Currency) - requireBetSub;

            if (!item.UpgradeFlag)
            {
                if (bet >= requireBet && recharge >= requireRecharge)
                    newLevel = item.Level;
                else
                    break;
            }
            else if (requireBet > 0 && requireRecharge > 0)
            {
                if (bet >= requireBet || recharge >= requireRecharge)
                    newLevel = item.Level;
            }
            else if (requireBet > 0 && bet >= requireBet)
            {
                newLevel = item.Level;
            }
            else if (requireRecharge > 0 && recharge >= requireRecharge)
            {
                newLevel = item.Level;
            }
        }

        _ = site;
        return (newLevel > user.Level, newLevel);
    }

    /// <summary>
    /// 写 Level 与升级日志。
    /// </summary>
    private async Task<bool> DoUpgradeAsync(User user, byte newLevel, CancellationToken cancellationToken)
    {
        if (user.Level >= newLevel)
            return false;

        var oldLevel = user.Level;
        var affected = await _db.Updateable<User>()
            .SetColumns(u => u.Level == newLevel)
            .Where(u => u.ID == user.ID && u.Level < newLevel)
            .ExecuteCommandAsync(cancellationToken);
        if (affected <= 0)
            return false;

        user.Level = newLevel;
        await _db.Insertable(new SiteVipUpGradeLog
        {
            SiteID = user.SiteID,
            Type = 1,
            UserID = user.ID,
            UserName = user.UserName ?? string.Empty,
            OldLevel = oldLevel,
            NewLevel = newLevel,
            CreateAt = DateTime.Now,
            Status = true,
            Remark = string.Empty
        }).ExecuteCommandAsync(cancellationToken);

        return true;
    }

    /// <summary>
    /// 发晋级礼金：查 VIPRightSetting 中 Upgrade 特权 L{level}，幂等 Mark=新等级。
    /// </summary>
    private async Task TrySendUpgradeGiftAsync(
        User user,
        Site site,
        byte newLevel,
        CancellationToken cancellationToken)
    {
        var mark = newLevel.ToString(CultureInfo.InvariantCulture);
        var exists = await _db.Queryable<UserVipGiftLog>()
            .Where(l => l.UserID == user.ID && l.Type == PrivilegeUpgrade && l.Mark == mark)
            .AnyAsync(cancellationToken);
        if (exists)
            return;

        var config = await LoadVipConfigAsync(user.SiteID, cancellationToken);
        if (config == null || string.IsNullOrWhiteSpace(config.VIPRightSetting))
            return;

        var privilege = ParsePrivilegeItems(config.VIPRightSetting)
            .FirstOrDefault(p => p.Id == PrivilegeUpgrade);
        if (privilege == null || !privilege.Enabled)
            return;

        privilege.AmountsByLevel.TryGetValue(newLevel, out var rawAmount);
        var currency = site.Wallet == WalletSingle ? user.Currency : site.Currency;
        var gift = ParseCurrencyTagged(rawAmount ?? "0", currency);
        if (gift <= 0)
            return;

        var sourceId = $"VIP{PrivilegeUpgrade}-{user.ID}-{mark}";
        if (sourceId.Length > 20)
            sourceId = sourceId[..20];

        var title = "恭喜,VIP 升级成功!";
        var content =
            $"恭喜升级为 VIP{newLevel}. 你的升级奖金为 {Math.Round(gift, 2)} 已经发放到你的账号.";
        var description = $"VIP 升级奖金为 {Math.Round(gift, 2)}";

        await _money.AddMoneyLogAsync(
            user.SiteID,
            user.ID,
            gift,
            MoneyTypeUpgradeGift,
            sourceId,
            description,
            currency,
            cancellationToken,
            flowRate: privilege.Ratio,
            minFlow: 0,
            eventId: PrivilegeUpgrade);

        await _db.Insertable(new UserVipGiftLog
        {
            SiteID = user.SiteID,
            UserID = user.ID,
            Money = gift,
            Type = PrivilegeUpgrade,
            Content = description,
            CreateAt = DateTime.Now,
            Mark = mark
        }).ExecuteCommandAsync(cancellationToken);

        await _messages.SendNotifyAsync(user.SiteID, user.ID, content, title, cancellationToken);
    }

    /// <summary>
    /// 最近一次升级时间：优先升级日志，没有再看晋级礼金。
    /// </summary>
    private async Task<DateTime> GetLastUpgradeAtAsync(int userId, CancellationToken cancellationToken)
    {
        var logAt = await _db.Queryable<SiteVipUpGradeLog>()
            .Where(l => l.UserID == userId && l.Type == 1 && l.Status)
            .OrderBy(l => l.CreateAt, OrderByType.Desc)
            .Select(l => l.CreateAt)
            .Take(1)
            .ToListAsync(cancellationToken);
        if (logAt.Count > 0)
            return logAt[0];

        var giftAt = await _db.Queryable<UserVipGiftLog>()
            .Where(l => l.UserID == userId && l.Type == PrivilegeUpgrade)
            .OrderBy(l => l.CreateAt, OrderByType.Desc)
            .Select(l => l.CreateAt)
            .Take(1)
            .ToListAsync(cancellationToken);
        return giftAt.Count > 0 ? giftAt[0] : DateTime.MinValue;
    }

    private async Task<SiteVipConfig> LoadVipConfigAsync(int siteId, CancellationToken cancellationToken)
    {
        var rows = await _db.Queryable<SiteVipConfig>()
            .Where(c => c.ID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        return rows.FirstOrDefault();
    }

    private async Task<decimal> SumRechargeAsync(int userId, DateTime startAt, CancellationToken cancellationToken)
    {
        var sum = await _db.Queryable<UserDateMoney>()
            .Where(t => t.UserID == userId
                        && t.Date >= startAt
                        && (t.Type == MoneyTypeRecharge || t.Type == MoneyTypeProxyRecharge))
            .SumAsync(t => t.Money);
        return sum;
    }

    private async Task<decimal> SumBetAsync(int userId, DateTime startAt, CancellationToken cancellationToken)
    {
        // 与本仓其他报表一致用 BetMoney；旧站同表列名为 BetAmount 时需后续对齐实体。
        var sum = await _db.Queryable<UserDateGame>()
            .Where(t => t.UserID == userId && t.Date >= startAt)
            .SumAsync(t => t.BetMoney);
        return sum;
    }

    /// <summary>
    /// 解析 VIPConditionSetting XML 的 item 节点。
    /// </summary>
    internal static List<VipLevelRule> ParseLevelItems(string xml)
    {
        var list = new List<VipLevelRule>();
        if (string.IsNullOrWhiteSpace(xml))
            return list;
        try
        {
            var root = XDocument.Parse(xml);
            foreach (var item in root.Descendants("item"))
            {
                var levelRaw = (string)item.Attribute("Level") ?? "0";
                byte.TryParse(levelRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var level);
                list.Add(new VipLevelRule
                {
                    Level = level,
                    Name = (string)item.Attribute("Name") ?? string.Empty,
                    UpgradeFlag = ParseBoolAttr(item, "UpgradeFlag"),
                    UpgradeClose = ParseBoolAttr(item, "UpgradeClose"),
                    Upgrade3Raw = (string)item.Attribute("Upgrade3") ?? "0",
                    Upgrade4Raw = (string)item.Attribute("Upgrade4") ?? "0",
                    Hold2 = ParseDecimalAttr(item, "Hold2"),
                    Upgrade3 = ParseDecimalAttr(item, "Upgrade3"),
                    Upgrade4 = ParseDecimalAttr(item, "Upgrade4")
                });
            }
        }
        catch
        {
            return list;
        }

        return list;
    }

    /// <summary>
    /// 解析 VIPRightSetting XML 的特权 item。
    /// </summary>
    internal static List<VipPrivilegeRule> ParsePrivilegeItems(string xml)
    {
        var list = new List<VipPrivilegeRule>();
        if (string.IsNullOrWhiteSpace(xml))
            return list;
        try
        {
            var root = XDocument.Parse(xml);
            foreach (var item in root.Descendants("item"))
            {
                var rule = new VipPrivilegeRule
                {
                    Id = (int)(item.Attribute("ID") ?? new XAttribute("ID", "0")),
                    Enabled = item.Attribute("Enabled") == null || ParseBoolAttrTrueDefault(item, "Enabled"),
                    Ratio = ParseDecimalAttr(item, "Ratio"),
                    SettingRaw = item.Value ?? string.Empty,
                    AmountsByLevel = new Dictionary<byte, string>()
                };

                foreach (var attr in item.Attributes())
                {
                    var name = attr.Name.LocalName;
                    if (name.Length < 2 || name[0] != 'L')
                        continue;
                    if (!byte.TryParse(name[1..], NumberStyles.Integer, CultureInfo.InvariantCulture, out var lv))
                        continue;
                    var raw = attr.Value ?? string.Empty;
                    if (raw.Contains("%7B", StringComparison.OrdinalIgnoreCase))
                        raw = Uri.UnescapeDataString(raw);
                    rule.AmountsByLevel[lv] = raw;
                }

                list.Add(rule);
            }
        }
        catch
        {
            return list;
        }

        return list;
    }

    /// <summary>
    /// 解析币种标签金额（纯数字或 <c>币种:金额</c> 分段）。
    /// </summary>
    internal static decimal ParseCurrencyTagged(string raw, int currency)
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

    private static bool IsAgentSystem(int type) =>
        type is UserTypeProxyMember or UserTypeProxyAgent;

    private static bool ParseBoolAttr(XElement item, string name)
    {
        var v = (string)item.Attribute(name);
        if (string.IsNullOrWhiteSpace(v))
            return false;
        if (bool.TryParse(v, out var b))
            return b;
        return v is "1" or "True" or "true";
    }

    private static bool ParseBoolAttrTrueDefault(XElement item, string name)
    {
        var v = (string)item.Attribute(name);
        if (string.IsNullOrWhiteSpace(v))
            return true;
        if (bool.TryParse(v, out var b))
            return b;
        return v is not ("0" or "False" or "false");
    }

    private static decimal ParseDecimalAttr(XElement item, string name)
    {
        var v = (string)item.Attribute(name);
        if (string.IsNullOrWhiteSpace(v))
            return 0;
        return decimal.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out var n) ? n : 0;
    }

    /// <summary>等级条件行。</summary>
    internal sealed class VipLevelRule
    {
        /// <summary>等级。</summary>
        public byte Level { get; set; }

        /// <summary>名称。</summary>
        public string Name { get; set; }

        /// <summary>true=或，false=且。</summary>
        public bool UpgradeFlag { get; set; }

        /// <summary>自定档关闭。</summary>
        public bool UpgradeClose { get; set; }

        /// <summary>累计存款原文。</summary>
        public string Upgrade3Raw { get; set; }

        /// <summary>总流水原文。</summary>
        public string Upgrade4Raw { get; set; }

        /// <summary>累计存款（纯数字兜底）。</summary>
        public decimal Upgrade3 { get; set; }

        /// <summary>总流水（纯数字兜底）。</summary>
        public decimal Upgrade4 { get; set; }

        /// <summary>保级流水 Hold2。</summary>
        public decimal Hold2 { get; set; }
    }

    /// <summary>特权行。</summary>
    internal sealed class VipPrivilegeRule
    {
        /// <summary>特权编号。</summary>
        public int Id { get; set; }

        /// <summary>是否启用。</summary>
        public bool Enabled { get; set; }

        /// <summary>提现流水倍率。</summary>
        public decimal Ratio { get; set; }

        /// <summary>各等级礼金原文 L{n}。</summary>
        public Dictionary<byte, string> AmountsByLevel { get; set; }

        /// <summary>特权内嵌设置（CDATA / 元素文本，如 Day/Hour）。</summary>
        public string SettingRaw { get; set; }
    }
}
