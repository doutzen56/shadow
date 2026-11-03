namespace SimpX.Net.Data;

/// <summary>
/// 站点币种枚举。对齐旧 SP.Studio.Core.Currency（下标即整型值，存库用 int）。
/// 展示名一律用枚举名；禁止再散落 CurrencyNames 字符串表。
/// </summary>
public enum Currency
{
    CNY = 0,
    USD = 1,
    TWD = 2,
    EUR = 3,
    THB = 4,
    VND = 5,
    IDR = 6,
    PHP = 7,
    RUB = 8,
    JPY = 9,
    KRW = 10,
    INR = 11,
    MYR = 12,
    KVND = 13,
    KIDR = 14,
    COIN = 15,
    HKD = 16,
    USDT = 17,
    BRL = 18,
    CUP = 19,
    KHR = 20,
    PKR = 21,
    MMK = 22,
    MMKK = 23,
    XAF = 24,
    BTC = 25,
    ETH = 26,
    LTC = 27,
    DOGE = 28,
    TRX = 29,
    BDT = 30,
}
