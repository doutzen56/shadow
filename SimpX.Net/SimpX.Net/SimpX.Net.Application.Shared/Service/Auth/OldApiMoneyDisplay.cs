using System.Globalization;
using SimpX.Net.Data;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 旧版资金接口展示：状态/币种等走 <see cref="OldApiEnumDisplay"/>，勿在此再硬编码枚举表。
/// </summary>
public static class OldApiMoneyDisplay
{
    /// <summary>审核类状态：None/Failed/Success/Processing。</summary>
    public static string FormatCheckStatus(int status) => OldApiEnumDisplay.CheckStatus(status);

    /// <summary>
    /// 解析旧站审核状态筛选：支持整型字符串，或 None / Faild / Failed / Success / Processing。
    /// </summary>
    /// <param name="raw">请求里的 Status</param>
    /// <param name="status">解析出的整型</param>
    /// <returns>能否解析</returns>
    public static bool TryParseCheckStatus(string? raw, out int status)
    {
        status = 0;
        if (string.IsNullOrWhiteSpace(raw)) return false;
        var key = raw.Trim();
        if (int.TryParse(key, NumberStyles.Integer, CultureInfo.InvariantCulture, out status))
            return true;
        if (Enum.TryParse<MoneyCheckStatus>(key, ignoreCase: true, out var parsed))
        {
            status = (int)parsed;
            return true;
        }

        // 旧站拼写 Faild
        if (key.Equals("Faild", StringComparison.OrdinalIgnoreCase))
        {
            status = (int)MoneyCheckStatus.Failed;
            return true;
        }

        return false;
    }

    /// <summary>币种整型 → 枚举名（CNY/USDT/…），统一走 <see cref="OldApiEnumDisplay.Currency"/>。</summary>
    public static string FormatCurrency(int currency) => OldApiEnumDisplay.Currency(currency);

    /// <summary>
    /// 充值单 Remark 管道编码：<c>备注A|备注B|实充金额 币种</c>（单一钱包入账常写成 <c>||5975.00 TWD</c>）。
    /// 详情「备注」禁止原样吐管道符；仅展示人工备注段。
    /// </summary>
    /// <param name="remark">库里的 Remark</param>
    /// <param name="paid">订单 Paid（实充兜底）</param>
    /// <param name="currency">订单币种整型</param>
    /// <param name="displayRemark">给人看的备注（无管道）</param>
    /// <param name="realPaid">实充展示串</param>
    public static void ParseRechargeRemark(
        string? remark,
        decimal paid,
        int currency,
        out string displayRemark,
        out string realPaid)
    {
        var raw = remark ?? string.Empty;
        var parts = raw.Split('|');
        if (parts.Length >= 3)
        {
            displayRemark = string.Join(
                " / ",
                new[] { parts[0].Trim(), parts[1].Trim() }.Where(static s => s.Length > 0));
            realPaid = NormalizeRealPaidToken(string.Join("|", parts.Skip(2)).Trim(), currency);
            if (string.IsNullOrWhiteSpace(realPaid))
                realPaid = FormatMoneyCurrency(paid, currency);
            else
                realPaid = NormalizeCurrencySuffix(realPaid, currency);
            return;
        }

        displayRemark = raw.Trim();
        realPaid = FormatMoneyCurrency(paid, currency);
    }

    /// <summary>金额 + 币种展示（币种必为枚举名，禁止拼整型）。</summary>
    public static string FormatMoneyCurrency(decimal money, int currency) =>
        $"{money.ToString("0.##", CultureInfo.InvariantCulture)} {FormatCurrency(currency)}";

    /// <summary>
    /// 任意展示串里「末尾币种整型」→ 枚举名（如 <c>47.50 2</c> → <c>47.50 TWD</c>）。
    /// </summary>
    public static string NormalizeCurrencySuffix(string? text, int? fallbackCurrency = null)
    {
        if (string.IsNullOrWhiteSpace(text))
            return fallbackCurrency is int c ? FormatCurrency(c) : string.Empty;
        var bits = text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (bits.Length == 0) return string.Empty;
        if (bits.Length == 1)
            return fallbackCurrency is int c
                ? $"{bits[0]} {FormatCurrency(c)}"
                : bits[0];
        if (int.TryParse(bits[^1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var code))
            bits[^1] = FormatCurrency(code);
        return string.Join(' ', bits);
    }

    /// <summary>实充段规范化。</summary>
    private static string NormalizeRealPaidToken(string token, int fallbackCurrency) =>
        NormalizeCurrencySuffix(token, fallbackCurrency);

    /// <summary>金额四位小数（帐变/余额 TotalMoney）。</summary>
    public static string FormatMoney4(decimal money) =>
        money.ToString("0.0000", CultureInfo.InvariantCulture);

    /// <summary>金额去尾零（余额 Money 字段）。</summary>
    public static string FormatMoneyTrim(decimal money) =>
        money.ToString("0.####", CultureInfo.InvariantCulture);

    /// <summary>旧站日期：2026/11/2 19:20:58。</summary>
    public static string FormatDateTime(DateTime value) =>
        value.ToString("yyyy/M/d H:mm:ss", CultureInfo.InvariantCulture);

    /// <summary>资金类型整型 → 旧枚举名（TypeValue）。</summary>
    public static string FormatMoneyTypeValue(int type) => type switch
    {
        1 => "Recharge",
        2 => "Withdraw",
        3 => "RechargeReward",
        4 => "WithdrawFee",
        5 => "GameTransferOut",
        6 => "RechargeFee",
        8 => "GameTransferIn",
        9 => "RechargeDue",
        10 => "WithdrawReturn",
        11 => "WithdrawMoney",
        31 => "WithdrawFeeBack",
        32 => "RechargeFeeBack",
        44 => "RechargeDueReverse",
        60 => "Rebate",
        61 => "RebateAdd",
        62 => "RebateFee",
        66 => "GameBet",
        99 => "GameReturn",
        111 => "ProxyRecharge",
        120 => "TransferIn",
        121 => "TransferOut",
        130 => "Commission",
        131 => "AgentReturn",
        132 => "CommissionFee",
        133 => "AgentReturnFee",
        140 => "ProxyIn",
        141 => "ProxyOut",
        150 => "ExchangeIn",
        151 => "ExchangeOut",
        200 => "Event",
        230 => "EventFee",
        250 => "AdminIn",
        251 => "AdminOut",
        _ => type.ToString(CultureInfo.InvariantCulture)
    };
}
