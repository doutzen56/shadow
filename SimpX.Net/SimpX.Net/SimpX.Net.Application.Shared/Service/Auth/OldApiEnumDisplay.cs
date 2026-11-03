using System.Globalization;
using SimpX.Net.Data;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 旧 API 枚举展示：整型 → 枚举名。唯一入口，禁止各服务自建 CurrencyNames / switch 表。
/// </summary>
public static class OldApiEnumDisplay
{
    /// <summary>
    /// 通用：整型转枚举名；未定义则原样吐整型字符串。
    /// </summary>
    /// <typeparam name="TEnum">枚举类型</typeparam>
    /// <param name="value">存库整型</param>
    /// <returns>枚举名或原值</returns>
    public static string Name<TEnum>(int value)
        where TEnum : struct, Enum
    {
        if (Enum.IsDefined(typeof(TEnum), value))
            return Enum.GetName(typeof(TEnum), value)!;
        return value.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// 通用：可空整型；空返回空串。
    /// </summary>
    public static string Name<TEnum>(int? value)
        where TEnum : struct, Enum =>
        value.HasValue ? Name<TEnum>(value.Value) : string.Empty;

    /// <summary>币种整型 → CNY/USDT/…</summary>
    public static string Currency(int value) => Name<Currency>(value);

    /// <summary>审核状态整型 → None/Failed/Success/Processing。</summary>
    public static string CheckStatus(int value) => Name<MoneyCheckStatus>(value);

    /// <summary>提现状态整型 → None/Wait/Finish/Faild/InPayment。</summary>
    public static string WithdrawStatus(int value) => Name<WithdrawStatus>(value);
}
