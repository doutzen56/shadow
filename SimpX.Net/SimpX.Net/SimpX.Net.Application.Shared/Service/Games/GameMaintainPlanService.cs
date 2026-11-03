using System.Collections.Concurrent;
using System.Text.Json;
using NewLife.Caching;
using SimpX.Net.Caching;
using SimpX.Net.Data.Entity.Games;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 执行系统游戏维护计划。对齐旧 GameMaintainAgent.GameMaintainPlanTask。
/// </summary>
public interface IGameMaintainPlanService
{
    /// <summary>
    /// 评估并执行 MaintainPlan：进入维护 / 恢复；写 Redis TTL；通知走日志。
    /// 残留：临时维护进入时不调用 TransferByGame / CreateTransferOrder OUT（转账 OUT 后置大模块）。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>本轮处理条数与摘要</returns>
    Task<GameMaintainPlanRunResult> RunAsync(CancellationToken cancellationToken = default);
}

/// <summary>维护计划一轮结果。</summary>
public sealed class GameMaintainPlanRunResult
{
    /// <summary>进入维护条数。</summary>
    public int Entered { get; set; }

    /// <summary>恢复条数。</summary>
    public int Resumed { get; set; }

    /// <summary>摘要。</summary>
    public string Message { get; set; }
}

/// <summary>默认实现：Schedule 显式注册；Redis 用游戏缓存库号 4。</summary>
public sealed class GameMaintainPlanService : IGameMaintainPlanService
{
    /// <summary>旧维护键前缀（系统层无站点）。</summary>
    public const string MaintainKeyPrefix = "MAINTAIN_";

    /// <summary>旧游戏缓存 Redis 库号。</summary>
    public const int GameCacheDbIndex = 4;

    /// <summary>本日已完成恢复：Type → yyyyMMdd。</summary>
    private static readonly ConcurrentDictionary<int, string> DoneToday = new();

    private readonly ISqlSugarClient _db;
    private readonly ILogger<GameMaintainPlanService> _logger;
    private readonly FullRedis _gameRedis;

    /// <summary>
    /// 注入业务库与日志；按需懒建游戏缓存 Redis。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="logger">日志</param>
    public GameMaintainPlanService(IBizDbAccessor biz, ILogger<GameMaintainPlanService> logger)
    {
        _db = biz.Db;
        _logger = logger;
        try
        {
            _gameRedis = LegacyLedgerRedisFactory.TryCreate(
                App.GetConfig<CacheOptions>("Cache", true),
                GameCacheDbIndex);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "维护计划 Redis 未就绪，仍写库侧状态");
            _gameRedis = null;
        }
    }

    /// <inheritdoc />
    public async Task<GameMaintainPlanRunResult> RunAsync(CancellationToken cancellationToken = default)
    {
        var plans = await _db.Queryable<SystemGameSetting>()
            .Where(t => t.Status != 0 && t.MaintainPlan != null && t.MaintainPlan != "")
            .Select(t => new { t.Type, t.MaintainPlan, t.GameCategorys })
            .ToListAsync(cancellationToken);

        var result = new GameMaintainPlanRunResult();
        var now = DateTime.Now;
        foreach (var item in plans)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                if (!TryParsePlan(item.MaintainPlan, item.Type, out var plan))
                    continue;

                var window = EvaluateWindow(plan, now);
                if (window == GameMaintainPlanWindow.None)
                    continue;

                var inMaintain = IsInMaintain(plan.Type);
                var doneToday = IsDoneToday(plan.Type, now);
                var skipped = plan.SkipCycleFlag == 1
                    && plan.SkipCycleExpireAt.HasValue
                    && plan.SkipCycleExpireAt.Value > now;

                _logger.LogInformation(
                    "维护计划 Type={Type} window={Window} inMaintain={In} doneToday={Done} skipped={Skipped}",
                    plan.Type, (int)window, inMaintain, doneToday, skipped);

                if (window == GameMaintainPlanWindow.Enter && !inMaintain && !doneToday && !skipped)
                {
                    if (await EnterMaintainAsync(plan, item.GameCategorys, now, cancellationToken))
                        result.Entered++;
                }
                else if (window == GameMaintainPlanWindow.Resume && !doneToday)
                {
                    if (await ResumeAsync(plan, now, cancellationToken))
                        result.Resumed++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "维护计划异常 Type={Type}", item.Type);
            }
        }

        result.Message =
            $"维护计划 进入 {result.Entered} / 恢复 {result.Resumed}" +
            "（残留：转账 OUT 后置大模块，未调用 TransferByGame / CreateTransferOrder OUT）";
        return result;
    }

    /// <summary>
    /// 评估计划窗口：临时看起止日期时间；每周/每月看 FrequencyValue + StartAt/EndAt。
    /// 单测也会直接调。
    /// </summary>
    /// <param name="plan">计划</param>
    /// <param name="now">当前时间</param>
    /// <returns>窗口状态</returns>
    public static GameMaintainPlanWindow EvaluateWindow(GameMaintainPlanDto plan, DateTime now)
    {
        if (plan == null) return GameMaintainPlanWindow.None;

        switch (plan.Frequency)
        {
            case GameMaintainFrequency.Temporary:
            {
                if (!DateTime.TryParse(plan.FrequencyValue, out var startDate)
                    || !TimeSpan.TryParse((plan.StartAt ?? string.Empty).Trim(), out var startSpan)
                    || !TimeSpan.TryParse((plan.EndAt ?? string.Empty).Trim(), out var endSpan))
                    return GameMaintainPlanWindow.None;

                var endDate = startDate;
                if (!string.IsNullOrWhiteSpace(plan.EndDate) && !DateTime.TryParse(plan.EndDate, out endDate))
                    endDate = startDate;

                var startTime = startDate.Date.Add(startSpan);
                var endTime = endDate.Date.Add(endSpan);
                if (endTime <= startTime)
                    return GameMaintainPlanWindow.None;
                if (now >= startTime && now < endTime)
                    return GameMaintainPlanWindow.Enter;
                if (now >= endTime)
                    return GameMaintainPlanWindow.Resume;
                return GameMaintainPlanWindow.None;
            }
            case GameMaintainFrequency.EveryWeek:
            {
                if (!int.TryParse(plan.FrequencyValue, out var freValue))
                    return GameMaintainPlanWindow.None;
                if ((int)now.DayOfWeek != freValue % 7)
                    return GameMaintainPlanWindow.None;
                return EvaluateDayInterval(plan.StartAt, plan.EndAt, now);
            }
            case GameMaintainFrequency.EveryMonth:
            {
                if (!int.TryParse(plan.FrequencyValue, out var freValue))
                    return GameMaintainPlanWindow.None;
                if (now.Day != freValue)
                    return GameMaintainPlanWindow.None;
                return EvaluateDayInterval(plan.StartAt, plan.EndAt, now);
            }
            default:
                return GameMaintainPlanWindow.None;
        }
    }

    /// <summary>
    /// 解析维护计划 JSON；Type 缺省时用库行 Type。
    /// </summary>
    /// <param name="json">MaintainPlan 原文</param>
    /// <param name="fallbackType">库行 Type</param>
    /// <param name="plan">解析结果</param>
    /// <returns>成功为 true</returns>
    public static bool TryParsePlan(string json, int fallbackType, out GameMaintainPlanDto plan)
    {
        plan = null;
        if (string.IsNullOrWhiteSpace(json)) return false;
        try
        {
            plan = JsonSerializer.Deserialize<GameMaintainPlanDto>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            if (plan == null) return false;
            if (plan.Type <= 0) plan.Type = fallbackType;
            return plan.Type > 0;
        }
        catch
        {
            plan = null;
            return false;
        }
    }

    /// <summary>计算计划结束时间（ResumeAt）。</summary>
    /// <param name="plan">计划</param>
    /// <returns>结束时刻</returns>
    public static DateTime GetPlanEndTime(GameMaintainPlanDto plan)
    {
        if (!TimeSpan.TryParse((plan.EndAt ?? string.Empty).Trim(), out var endTime))
            endTime = new TimeSpan(23, 59, 59);

        switch (plan.Frequency)
        {
            case GameMaintainFrequency.Temporary:
            {
                if (!DateTime.TryParse(plan.FrequencyValue, out var startDate))
                    startDate = DateTime.Today;
                var endDate = startDate;
                if (string.IsNullOrWhiteSpace(plan.EndDate) || !DateTime.TryParse(plan.EndDate, out endDate))
                    endDate = startDate;
                return endDate.Date.Add(endTime);
            }
            default:
                return DateTime.Today.Add(endTime);
        }
    }

    /// <summary>同日 StartAt~EndAt：1 进入 / 2 恢复（对齐旧 IsInterval 宽容约 1 分钟）。</summary>
    private static GameMaintainPlanWindow EvaluateDayInterval(string startAt, string endAt, DateTime now)
    {
        if (!TimeSpan.TryParse(startAt, out var start) || !TimeSpan.TryParse(endAt, out var end))
            return GameMaintainPlanWindow.None;

        var current = new TimeSpan(now.Hour, now.Minute, 0);
        var fromStart = (current - start).TotalMinutes;
        var fromEnd = (current - end).TotalMinutes;
        if (fromStart >= -1 && fromEnd < 1)
            return GameMaintainPlanWindow.Enter;
        if (fromStart > 0 && fromEnd >= 1)
            return GameMaintainPlanWindow.Resume;
        return GameMaintainPlanWindow.None;
    }

    /// <summary>进入维护。</summary>
    private async Task<bool> EnterMaintainAsync(
        GameMaintainPlanDto plan,
        string gameCategorys,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (plan.SkipCycleFlag != 0 || plan.SkipCycleExpireAt.HasValue)
        {
            plan.SkipCycleFlag = 0;
            plan.SkipCycleExpireAt = null;
            await _db.Updateable<SystemGameSetting>()
                .SetColumns(t => new SystemGameSetting { MaintainPlan = JsonSerializer.Serialize(plan) })
                .Where(t => t.Type == plan.Type)
                .ExecuteCommandAsync(cancellationToken);
            _logger.LogInformation("进入维护，清理本周期跳过标记 Type={Type}", plan.Type);
        }

        var status = GameMaintainStatus.Maintain;
        var maintenanceCount = string.IsNullOrWhiteSpace(plan.Maintenance)
            ? 0
            : plan.Maintenance.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length;
        var categoryCount = string.IsNullOrWhiteSpace(gameCategorys)
            ? 0
            : gameCategorys.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length;
        if (categoryCount > 0 && categoryCount != maintenanceCount)
            status = GameMaintainStatus.MaintainPart;

        var n = await _db.Updateable<SystemGameSetting>()
            .SetColumns(t => new SystemGameSetting
            {
                Status = status,
                Maintenance = plan.Maintenance ?? string.Empty
            })
            .Where(t => t.Type == plan.Type)
            .ExecuteCommandAsync(cancellationToken);
        if (n <= 0) return false;

        var resumeAt = GetPlanEndTime(plan);
        var payload = JsonSerializer.Serialize(new { Status = status, Game = plan.Maintenance ?? string.Empty });
        SetMaintainKey(plan.Type, resumeAt, payload);

        var siteIds = await _db.Queryable<GameSetting>()
            .Where(g => g.Game == plan.Type)
            .Select(g => g.SiteID)
            .ToListAsync(cancellationToken);
        foreach (var siteId in siteIds.Distinct())
        {
            await _db.Updateable<GameSetting>()
                .SetColumns(g => new GameSetting
                {
                    Status = status,
                    Maintenance = plan.Maintenance ?? string.Empty
                })
                .Where(g => g.SiteID == siteId && g.Game == plan.Type)
                .ExecuteCommandAsync(cancellationToken);
        }

        _logger.LogInformation(
            "维护计划进入维护 Type={Type} Status={Status} ResumeAt={ResumeAt:yyyy-MM-dd HH:mm:ss} Sites={Sites}",
            plan.Type, status, resumeAt, siteIds.Count);

        // 残留：转账 OUT 后置大模块。旧临时维护会 TransferByGame → CreateTransferOrder(OUT)，此处故意不调。
        // if (plan.Frequency == GameMaintainFrequency.Temporary) TransferByGame(plan.Type);

        return true;
    }

    /// <summary>恢复正常。</summary>
    private async Task<bool> ResumeAsync(
        GameMaintainPlanDto plan,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var clearPlan = plan.Frequency == GameMaintainFrequency.Temporary;
        int n;
        if (clearPlan)
        {
            n = await _db.Updateable<SystemGameSetting>()
                .SetColumns(t => new SystemGameSetting
                {
                    Status = GameMaintainStatus.Normal,
                    Maintenance = string.Empty,
                    MaintainPlan = string.Empty
                })
                .Where(t => t.Type == plan.Type)
                .ExecuteCommandAsync(cancellationToken);
        }
        else
        {
            n = await _db.Updateable<SystemGameSetting>()
                .SetColumns(t => new SystemGameSetting
                {
                    Status = GameMaintainStatus.Normal,
                    Maintenance = plan.Maintenance ?? string.Empty
                })
                .Where(t => t.Type == plan.Type)
                .ExecuteCommandAsync(cancellationToken);
        }

        if (n <= 0) return false;

        RemoveMaintainKey(plan.Type);

        var siteIds = await _db.Queryable<GameSetting>()
            .Where(g => g.Game == plan.Type)
            .Select(g => g.SiteID)
            .ToListAsync(cancellationToken);
        var siteMaintenance = clearPlan ? string.Empty : (plan.Maintenance ?? string.Empty);
        foreach (var siteId in siteIds.Distinct())
        {
            await _db.Updateable<GameSetting>()
                .SetColumns(g => new GameSetting
                {
                    Status = GameMaintainStatus.Normal,
                    Maintenance = siteMaintenance
                })
                .Where(g => g.SiteID == siteId && g.Game == plan.Type)
                .ExecuteCommandAsync(cancellationToken);
        }

        MarkDoneToday(plan.Type, now);
        _logger.LogInformation(
            "维护计划恢复正常 Type={Type} Sites={Sites}",
            plan.Type, siteIds.Count);
        return true;
    }

    /// <summary>Redis 是否仍有维护键。</summary>
    private bool IsInMaintain(int type)
    {
        if (_gameRedis == null) return false;
        try
        {
            return _gameRedis.ContainsKey(MaintainKeyPrefix + type);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "读维护键失败 Type={Type}", type);
            return false;
        }
    }

    /// <summary>写维护键 TTL。</summary>
    private void SetMaintainKey(int type, DateTime resumeAt, string payload)
    {
        if (_gameRedis == null) return;
        try
        {
            var key = MaintainKeyPrefix + type;
            var ts = resumeAt - DateTime.Now;
            if (ts <= TimeSpan.Zero)
            {
                if (ts > TimeSpan.FromMinutes(-1))
                    ts = TimeSpan.FromSeconds(60);
                else
                {
                    _gameRedis.Remove(key);
                    return;
                }
            }
            else if (ts.TotalSeconds < 2)
            {
                ts = TimeSpan.FromSeconds(2);
            }

            _gameRedis.Set(key, payload ?? string.Empty, ts);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "写维护键失败 Type={Type}", type);
        }
    }

    /// <summary>删维护键。</summary>
    private void RemoveMaintainKey(int type)
    {
        if (_gameRedis == null) return;
        try
        {
            _gameRedis.Remove(MaintainKeyPrefix + type);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "删维护键失败 Type={Type}", type);
        }
    }

    /// <summary>本日是否已恢复过。</summary>
    private static bool IsDoneToday(int type, DateTime now) =>
        DoneToday.TryGetValue(type, out var day) && day == now.ToString("yyyyMMdd");

    /// <summary>标记本日已恢复。</summary>
    private static void MarkDoneToday(int type, DateTime now) =>
        DoneToday[type] = now.ToString("yyyyMMdd");
}
