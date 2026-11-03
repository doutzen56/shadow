using System.Globalization;
using System.Text.Json;
using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 充值订单确认到账。对齐旧 CheckRechargeOrder：多币种 / 单一钱包换汇上分，旁路队列、站内信与充值活动日志。
/// RechargeByLevel / FirstRecharge 完整规则与真实飞机 Bot 仍后置。
/// </summary>
public sealed class RechargeOrderCheckService : ITransient
{
    /// <summary>充值账变类型。</summary>
    public const int MoneyTypeRecharge = 1;

    /// <summary>代客充值账变类型。</summary>
    public const int MoneyTypeProxyRecharge = 111;

    /// <summary>充值手续费账变类型。</summary>
    public const int MoneyTypeRechargeFee = 6;

    /// <summary>旧 Wallet.SINGLE。</summary>
    public const int WalletSingle = 1;

    /// <summary>旧 EventType.Recharge。</summary>
    public const int EventTypeRecharge = 9;

    /// <summary>旧 Currency.USDT。</summary>
    public const int CurrencyUsdt = 17;

    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;
    private readonly IMoneyLedger _money;
    private readonly ILedgerSideEffectQueues _queues;
    private readonly IUserMessageNotifyService _messages;
    private readonly IRechargeTelegramNotifier _telegram;
    private readonly IRechargePromoGrantService _promo;

    /// <summary>
    /// 注入库、定站、账本、旁路队列、站内信、飞机钩子、活动发奖。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">定站</param>
    /// <param name="money">账本</param>
    /// <param name="queues">旁路队列</param>
    /// <param name="messages">站内信；单测可空</param>
    /// <param name="telegram">飞机钩子；单测可空</param>
    /// <param name="promo">充值活动；单测可空</param>
    public RechargeOrderCheckService(
        IBizDbAccessor biz,
        ICurrentSiteAccessor site,
        IMoneyLedger money,
        ILedgerSideEffectQueues queues,
        IUserMessageNotifyService messages = null,
        IRechargeTelegramNotifier telegram = null,
        IRechargePromoGrantService promo = null)
    {
        _db = biz.Db;
        _site = site;
        _money = money;
        _queues = queues;
        _messages = messages ?? NoOpUserMessageNotifyService.Instance;
        _telegram = telegram ?? NoOpRechargeTelegramNotifier.Instance;
        _promo = promo ?? NoOpRechargePromoGrantService.Instance;
    }

    /// <summary>
    /// 确认到账。
    /// </summary>
    public async Task CheckAsync(
        RechargeOrderCheckInput input,
        int adminId,
        int? expectedPayId = null,
        CancellationToken cancellationToken = default,
        int? siteIdOverride = null)
    {
        ArgumentNullException.ThrowIfNull(input);
        var money = input.Paid;
        var fee = input.Fee;
        var cost = input.Cost;
        var systemId = (input.SystemID ?? string.Empty).Trim();
        var orderId = (input.ID ?? string.Empty).Trim();
        var payAccount = input.PayAccount ?? string.Empty;
        var descriptionExtra = input.Description ?? string.Empty;

        if (money <= 0)
            throw new InvalidOperationException("#充值金额错误#");
        if (money + fee <= 0)
            throw new InvalidOperationException("#手续费错误#");
        if (string.IsNullOrEmpty(systemId))
            throw new InvalidOperationException("#网关编号错误#");
        if (string.IsNullOrEmpty(orderId))
            throw new InvalidOperationException("#订单编号错误#");

        var siteId = siteIdOverride ?? _site.RequireSiteId();
        var rows = await _db.Queryable<RechargeOrder>()
            .Where(o => o.SiteID == siteId && o.ID == orderId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var order = rows.FirstOrDefault();
        if (order == null)
            throw new InvalidOperationException("#充值编号错误#");
        if (order.IsPay)
            throw new InvalidOperationException("#充值订单已经入账#");

        if (await NeedCheckTimeAsync(siteId, cancellationToken)
            && order.CreateAt < DateTime.Now.AddDays(-1))
            throw new InvalidOperationException("充值订单已经超过24小时,无法入账");

        if (money > order.Money)
            throw new InvalidOperationException("实际到帐金额不能大于充值额度");
        if (expectedPayId.HasValue && order.PayID != expectedPayId.Value)
            throw new InvalidOperationException("#支付渠道不匹配#");

        var dup = await _db.Queryable<RechargeOrder>()
            .Where(o => o.SiteID == siteId && o.SystemID == systemId && o.ID != orderId)
            .AnyAsync(cancellationToken);
        if (dup)
            throw new InvalidOperationException("存在相同的网关订单编号,请注意是否重复到帐");

        var maxCount = await _db.Queryable<RechargeOrder>()
            .Where(o => o.SiteID == siteId && o.UserID == order.UserID)
            .MaxAsync(o => o.Count, cancellationToken);
        var nextCount = maxCount + 1;

        var feeRounded = Math.Round(fee, 2, MidpointRounding.AwayFromZero);
        var costRounded = Math.Round(cost, 2, MidpointRounding.AwayFromZero);
        var paid = decimal.Parse((money - feeRounded).ToString("F2"));
        var moneyType = order.IsProxyRecharge ? MoneyTypeProxyRecharge : MoneyTypeRecharge;
        var finalPayAccount = string.IsNullOrEmpty(payAccount) ? (order.PayAccount ?? string.Empty) : payAccount;
        var description = "#充值入账#,订单号:" + order.ID;
        if (!string.IsNullOrEmpty(order.Remark))
            description += "," + order.Remark;
        if (!string.IsNullOrWhiteSpace(descriptionExtra))
            description += "," + descriptionExtra;

        var siteWallet = (await _db.Queryable<Site>()
            .Where(s => s.ID == siteId)
            .Select(s => s.Wallet)
            .Take(1)
            .ToListAsync(cancellationToken)).FirstOrDefault();
        var isSingle = siteWallet == WalletSingle;

        var creditCurrency = order.Currency;
        var creditPaid = paid;
        var creditFee = feeRounded;
        var creditCost = costRounded;
        var remark = order.Remark ?? string.Empty;
        var flowRate = await ResolveFlowRateAsync(siteId, order, money, cancellationToken);

        if (isSingle)
        {
            var userCurrency = (await _db.Queryable<User>()
                .Where(u => u.ID == order.UserID && u.SiteID == siteId)
                .Select(u => u.Currency)
                .Take(1)
                .ToListAsync(cancellationToken)).FirstOrDefault();
            var rate = await GetExchangeRatePointAsync(siteId, order.Currency, userCurrency, cancellationToken);
            creditPaid = decimal.Parse((paid * rate).ToString("F2"));
            creditFee = decimal.Parse((feeRounded * rate).ToString("F2"));
            creditCost = decimal.Parse((costRounded * rate).ToString("F2"));
            // Remark 管道编码：备注A|备注B|实充金额 币种名（列表用第 3 段做 RealPaid）
            var ch = descriptionExtra.Count(c => c == '|');
            var split = ch == 1 ? "|" : "||";
            remark += descriptionExtra + split + OldApiMoneyDisplay.FormatMoneyCurrency(creditPaid, userCurrency);
            description = OldApiMoneyDisplay.FormatCurrency(order.Currency) + " " + description + "[单一钱包]";
            creditCurrency = userCurrency;
            if (userCurrency == CurrencyUsdt && await IsUsdtZeroFlowAsync(siteId, cancellationToken))
                flowRate = 0;
        }

        var payAt = DateTime.Now;
        var affected = await _db.Updateable<RechargeOrder>()
            .SetColumns(r => new RechargeOrder
            {
                Paid = creditPaid,
                SystemID = systemId,
                IsPay = true,
                PayAt = payAt,
                Fee = creditFee,
                Cost = creditCost,
                AdminID = adminId,
                Count = nextCount,
                PayAccount = finalPayAccount,
                IsDeleted = false,
                Currency = creditCurrency,
                Remark = remark
            })
            .Where(r => r.SiteID == siteId && r.ID == orderId && !r.IsPay)
            .ExecuteCommandAsync(cancellationToken);
        if (affected == 0)
            throw new InvalidOperationException("#充值订单已经入账#");

        try
        {
            await _money.AddMoneyLogAsync(
                siteId,
                order.UserID,
                creditPaid + creditFee,
                moneyType,
                order.ID,
                description,
                creditCurrency,
                cancellationToken,
                flowRate: flowRate);

            if (creditFee != 0)
            {
                await _money.AddMoneyLogAsync(
                    siteId,
                    order.UserID,
                    Math.Abs(creditFee) * -1,
                    MoneyTypeRechargeFee,
                    order.ID,
                    isSingle ? OldApiMoneyDisplay.FormatCurrency(order.Currency) + " 充值费用[单一钱包]" : "充值手续费",
                    creditCurrency,
                    cancellationToken);
            }
        }
        catch
        {
            await _db.Updateable<RechargeOrder>()
                .SetColumns(r => new RechargeOrder
                {
                    IsPay = false,
                    Paid = 0,
                    SystemID = string.Empty,
                    Fee = order.Fee,
                    Cost = order.Cost,
                    AdminID = 0,
                    Count = order.Count,
                    Currency = order.Currency,
                    Remark = order.Remark
                })
                .Where(r => r.SiteID == siteId && r.ID == orderId && r.IsPay && r.SystemID == systemId)
                .ExecuteCommandAsync(cancellationToken);
            throw;
        }

        try
        {
            _queues.EnqueueRechargeLog(order.ID);
        }
        catch (Exception ex)
        {
            Log.Warning($"充值旁路队列入队失败（入账已完成）：{ex.Message}");
        }

        // 入账已成功：通知与活动失败只记日志，不回滚余额。
        var paidOrder = await _db.Queryable<RechargeOrder>()
            .Where(o => o.SiteID == siteId && o.ID == orderId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var fresh = paidOrder.FirstOrDefault() ?? order;
        fresh.Paid = creditPaid;
        fresh.Fee = creditFee;
        fresh.Cost = creditCost;
        fresh.Currency = creditCurrency;
        fresh.IsPay = true;
        fresh.Count = nextCount;
        fresh.SystemID = systemId;
        fresh.PayAccount = finalPayAccount;
        fresh.Remark = remark;

        var msgTitle = description.Split('|')[0];
        var msgBody = $"{msgTitle} #到帐#,{OldApiMoneyDisplay.FormatMoneyCurrency(creditPaid, creditCurrency)}";
        try
        {
            await _messages.SendNotifyAsync(siteId, fresh.UserID, msgBody, msgTitle, cancellationToken);
        }
        catch (Exception ex)
        {
            Log.Warning($"充值站内信失败（入账已完成）：{ex.Message}");
        }

        try
        {
            await _telegram.NotifyCreditedAsync(fresh, cancellationToken);
            if (fresh.Count == 1)
                await _telegram.NotifyFirstRechargeAsync(fresh, cancellationToken);
        }
        catch (Exception ex)
        {
            Log.Warning($"充值飞机通知失败（入账已完成）：{ex.Message}");
        }

        try
        {
            await _promo.GrantForOrderAsync(fresh.ID, cancellationToken);
        }
        catch (Exception ex)
        {
            Log.Warning($"充值活动发奖失败（入账已完成，仍可由 RECHARGE_LOG 消费端补）：{ex.Message}");
        }
    }

    private async Task<decimal> ResolveFlowRateAsync(
        int siteId,
        RechargeOrder order,
        decimal callbackMoney,
        CancellationToken cancellationToken)
    {
        var active = (order.ActiveID ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(active) || active == "0")
            return 1;
        decimal flowRate = 1;
        foreach (var part in active.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!int.TryParse(part, out var eventId) || eventId <= 0)
                continue;
            var ev = (await _db.Queryable<SiteEvent>()
                .Where(e => e.SiteID == siteId && e.ID == eventId)
                .Take(1)
                .ToListAsync(cancellationToken)).FirstOrDefault();
            if (ev == null || ev.Type != EventTypeRecharge)
                continue;
            var times = ReadAmountWithdrawTimes(ev.SettingString);
            if (times > flowRate)
                flowRate = times;
            _ = callbackMoney;
        }

        return flowRate;
    }

    private static decimal ReadAmountWithdrawTimes(string setting)
    {
        if (string.IsNullOrWhiteSpace(setting))
            return 0;
        var map = SiteSettingQuery.Parse(setting);
        var raw = SiteSettingQuery.Get(map, "AmountWithdrawTimes");
        if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var n) && n > 0)
            return n;
        if (!setting.TrimStart().StartsWith('{'))
            return 0;
        try
        {
            using var doc = JsonDocument.Parse(setting);
            if (doc.RootElement.TryGetProperty("AmountWithdrawTimes", out var p)
                && p.TryGetDecimal(out var d)
                && d > 0)
                return d;
        }
        catch (JsonException)
        {
            // ignore
        }

        return 0;
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

    private async Task<bool> IsUsdtZeroFlowAsync(int siteId, CancellationToken cancellationToken)
    {
        var sites = await _db.Queryable<Site>()
            .Where(s => s.ID == siteId)
            .Select(s => s.SettingString)
            .Take(1)
            .ToListAsync(cancellationToken);
        var setting = sites.FirstOrDefault();
        return SiteSettingQuery.GetBool(SiteSettingQuery.Parse(setting ?? string.Empty), "IsUSDTZeroFlow")
               || ReadJsonBool(setting, "IsUSDTZeroFlow");
    }

    private async Task<bool> NeedCheckTimeAsync(int siteId, CancellationToken cancellationToken)
    {
        var sites = await _db.Queryable<Site>()
            .Where(s => s.ID == siteId)
            .Select(s => new { s.SettingString })
            .Take(1)
            .ToListAsync(cancellationToken);
        var setting = sites.FirstOrDefault()?.SettingString;
        if (string.IsNullOrWhiteSpace(setting))
            return false;
        return SiteSettingQuery.GetBool(SiteSettingQuery.Parse(setting), "NeedCheckTime")
               || ReadJsonBool(setting, "NeedCheckTime");
    }

    private static bool ReadJsonBool(string setting, string name)
    {
        if (string.IsNullOrWhiteSpace(setting) || !setting.TrimStart().StartsWith('{'))
            return false;
        try
        {
            using var doc = JsonDocument.Parse(setting);
            if (!doc.RootElement.TryGetProperty(name, out var p))
                return false;
            return p.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Number => p.GetInt32() != 0,
                JsonValueKind.String => bool.TryParse(p.GetString(), out var b) && b,
                _ => false
            };
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
