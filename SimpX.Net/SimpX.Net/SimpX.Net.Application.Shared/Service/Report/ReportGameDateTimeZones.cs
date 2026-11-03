namespace SimpX.Net.Application.Shared;

/// <summary>
/// 游戏日报同步时区。对齐旧 SP.Studio.Core.TimeZoneHelper / TimeZoneType。
/// ES SettlementAt 存北京时间；EST/UTC 查窗换算到北京时间。
/// </summary>
public enum ReportGameDateTimeZone
{
    /// <summary>北京 UTC+8。</summary>
    BJ = 1,

    /// <summary>美东 UTC-4。</summary>
    EST = 2,

    /// <summary>UTC。</summary>
    UTC = 3
}

/// <summary>时区查窗辅助。</summary>
public static class ReportGameDateTimeZones
{
    /// <summary>
    /// 目标时区今日 00:00–次日 00:00，换算为北京时间起止（ES 用）与报表日。
    /// </summary>
    /// <param name="utcOffsetHours">相对 UTC 的小时偏移</param>
    public static (DateTime StartBj, DateTime EndBj, DateTime ReportDate) GetQueryRange(int utcOffsetHours)
    {
        var utcNow = DateTime.UtcNow;
        var targetTime = utcNow.AddHours(utcOffsetHours);
        var targetDate = targetTime.Date;
        var targetStart = targetDate;
        var targetEnd = targetDate.AddDays(1);
        const int beijingOffset = 8;
        var startBj = targetStart.AddHours(beijingOffset - utcOffsetHours);
        var endBj = targetEnd.AddHours(beijingOffset - utcOffsetHours);
        return (startBj, endBj, targetDate);
    }

    /// <summary>北京时区查窗。</summary>
    public static (DateTime StartBj, DateTime EndBj, DateTime ReportDate) Beijing() => GetQueryRange(8);

    /// <summary>美东时区查窗。</summary>
    public static (DateTime StartBj, DateTime EndBj, DateTime ReportDate) Est() => GetQueryRange(-4);

    /// <summary>UTC 查窗。</summary>
    public static (DateTime StartBj, DateTime EndBj, DateTime ReportDate) Utc() => GetQueryRange(0);

    /// <summary>
    /// 跨天回补窗口：目标时区当前 00:40–01:00。
    /// </summary>
    /// <param name="localNow">目标时区当前时刻</param>
    public static bool InCrossDayWindow(DateTime localNow) =>
        localNow.Hour >= 0 && localNow.Hour < 1 && localNow.Minute >= 40;

    /// <summary>写入 UETimeZone.TimeType（对齐旧 ToTimeType）。</summary>
    public static int ToTimeType(ReportGameDateTimeZone zone) =>
        zone switch
        {
            ReportGameDateTimeZone.EST => 2,
            ReportGameDateTimeZone.UTC => 3,
            _ => 0
        };

    /// <summary>game_UETimeZone.Hour：EST=12 / UTC=8。</summary>
    public static int GameTypeHour(ReportGameDateTimeZone zone) =>
        zone switch
        {
            ReportGameDateTimeZone.EST => 12,
            ReportGameDateTimeZone.UTC => 8,
            _ => 0
        };
}
