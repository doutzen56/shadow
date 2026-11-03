using SimpX.Net.Data.Entity.Games;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 游戏日志采集延时检查。对齐旧 GameLogAgent.CheckDelayCheck。
/// </summary>
public interface IGameLogDelayCheckService
{
    /// <summary>
    /// 扫 DelayTime&gt;0 的 game_Mark：UpdateAt 早于「现在 − DelayTime 分钟」则记警告。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>本轮告警条数；查询失败返回 -1</returns>
    Task<int> CheckDelayAsync(CancellationToken cancellationToken = default);
}

/// <summary>默认实现：Schedule 显式注册；通知未移植时用 Log.Warning。</summary>
public sealed class GameLogDelayCheckService : IGameLogDelayCheckService
{
    private readonly ISqlSugarClient _db;
    private readonly ILogger<GameLogDelayCheckService> _logger;

    /// <summary>
    /// 注入业务库与日志。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="logger">日志</param>
    public GameLogDelayCheckService(IBizDbAccessor biz, ILogger<GameLogDelayCheckService> logger)
    {
        _db = biz.Db;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<int> CheckDelayAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var marks = await _db.Queryable<GameMark>()
                .Where(t => t.DelayTime > 0)
                .ToListAsync(cancellationToken);

            var now = DateTime.Now;
            var count = 0;
            var lines = new List<string>();
            foreach (var mark in marks)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (mark.UpdateAt >= now.AddMinutes(-mark.DelayTime))
                    continue;

                var delayMinutes = (int)(now - mark.UpdateAt).TotalMinutes;
                lines.Add(
                    $"平台[{mark.Type}],延时[{delayMinutes}]分钟,最后更新时间[{mark.UpdateAt}],设置延时[{mark.DelayTime}]分钟");
                count++;
            }

            if (lines.Count > 0)
            {
                var content = string.Join("\n", lines);
                _logger.LogWarning("游戏日志采集延时告警:\n{Content}", content);
            }

            return count;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "检查游戏日志采集延时失败");
            return -1;
        }
    }
}
