using SimpX.Net.Data.Entity.Games;
using SimpX.Net.Data.Entity.Sites;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 站点游戏入口同步。对齐旧 GameEnteranceAgent.SiteGameEnteranceSync。
/// </summary>
public interface IGameEntranceSyncService
{
    /// <summary>
    /// 从 GAMEENTRANCE 弹出入口并按 best-effort 写 site_GameList 开关。
    /// 残留：未移植完整 SystemGameUpdate / SiteGameCate / SiteGameCaching.SaveSiteGameList。
    /// </summary>
    /// <param name="maxCount">本轮最多消费条数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>消费条数与受影响站点行数摘要</returns>
    Task<GameEntranceSyncResult> DrainAndSyncAsync(int maxCount = 100, CancellationToken cancellationToken = default);

    /// <summary>
    /// 对单条入口载荷做站点列表开关同步（单测可直接调）。
    /// </summary>
    /// <param name="payload">入口载荷</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新或插入的 site_GameList 行数</returns>
    Task<int> SyncOneAsync(GameEntrancePayload payload, CancellationToken cancellationToken = default);
}

/// <summary>入口同步一轮结果。</summary>
public sealed class GameEntranceSyncResult
{
    /// <summary>弹出条数。</summary>
    public int Drained { get; set; }

    /// <summary>成功解析并处理条数。</summary>
    public int Synced { get; set; }

    /// <summary>site_GameList 变更行数合计。</summary>
    public int RowsTouched { get; set; }

    /// <summary>给人看的摘要。</summary>
    public string Message { get; set; }
}

/// <summary>
/// 默认实现：Schedule 显式注册。只 upsert 最小 open 开关，不做完整目录重建。
/// </summary>
public sealed class GameEntranceSyncService : IGameEntranceSyncService
{
    /// <summary>站点正常状态（旧 SiteStauts.Normal = 0）。</summary>
    public const int SiteStatusNormal = 0;

    private readonly ISqlSugarClient _db;
    private readonly IGameEntranceQueue _queue;
    private readonly ILogger<GameEntranceSyncService> _logger;

    /// <summary>
    /// 注入业务库、入口队列与日志。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="queue">GAMEENTRANCE 队列</param>
    /// <param name="logger">日志</param>
    public GameEntranceSyncService(
        IBizDbAccessor biz,
        IGameEntranceQueue queue,
        ILogger<GameEntranceSyncService> logger)
    {
        _db = biz.Db;
        _queue = queue;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<GameEntranceSyncResult> DrainAndSyncAsync(
        int maxCount = 100,
        CancellationToken cancellationToken = default)
    {
        var drained = _queue.Drain(maxCount <= 0 ? 100 : maxCount);
        var result = new GameEntranceSyncResult { Drained = drained.Count };
        if (drained.Count == 0)
        {
            result.Message = "游戏入口队列为空";
            return result;
        }

        foreach (var json in drained)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!GameEntranceQueue.TryParsePayload(json, out var payload))
            {
                _logger.LogWarning("游戏入口载荷无法解析，已丢弃");
                continue;
            }

            try
            {
                var n = await SyncOneAsync(payload, cancellationToken);
                result.Synced++;
                result.RowsTouched += n;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "同步游戏入口失败 Type={Type} Code={Code}", payload.Type, payload.Code);
            }
        }

        result.Message =
            $"消费入口 {result.Drained} 条，同步 {result.Synced} 条，site_GameList 变更 {result.RowsTouched} 行" +
            "（残留：未移植完整 SystemGameUpdate / 分类缓存刷新）";
        return result;
    }

    /// <inheritdoc />
    public async Task<int> SyncOneAsync(GameEntrancePayload payload, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);
        if (payload.Type <= 0 || string.IsNullOrWhiteSpace(payload.Code))
            return 0;

        var openSites = await _db.Queryable<Site>()
            .Where(s => s.Status == SiteStatusNormal)
            .Select(s => s.ID)
            .ToListAsync(cancellationToken);
        if (openSites.Count == 0)
            return 0;

        var settings = await _db.Queryable<GameSetting>()
            .Where(g => g.Game == payload.Type && g.IsOpen)
            .Where(g => openSites.Contains(g.SiteID))
            .Select(g => g.SiteID)
            .ToListAsync(cancellationToken);
        if (settings.Count == 0)
            return 0;

        var touched = 0;
        var enable = payload.IsOpen;
        var now = DateTime.Now;
        var code = payload.Code.Trim();
        foreach (var siteId in settings.Distinct())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var rows = await _db.Queryable<SiteGameList>()
                .Where(g => g.SiteID == siteId && g.Type == payload.Type && g.Code == code)
                .Take(1)
                .ToListAsync(cancellationToken);
            var row = rows.FirstOrDefault();
            if (row == null)
            {
                // 最小 upsert：新入口默认跟系统开关；不重建整站目录。
                row = new SiteGameList
                {
                    SiteID = siteId,
                    Type = payload.Type,
                    Code = code,
                    GameName = payload.Name ?? code,
                    Name = payload.Name ?? code,
                    Category = payload.Category,
                    IsEnable = enable,
                    PcEnable = enable,
                    AppEnable = enable,
                    ShowImg = payload.Cover ?? string.Empty,
                    Sort = payload.Sort,
                    GameTag = payload.GameTag,
                    Tags = string.Empty,
                    Remark = string.Empty,
                    CreateAt = now,
                    UpdateAt = now
                };
                await _db.Insertable(row).ExecuteCommandAsync(cancellationToken);
                touched++;
                continue;
            }

            var n = await _db.Updateable<SiteGameList>()
                .SetColumns(g => new SiteGameList
                {
                    IsEnable = enable,
                    PcEnable = enable,
                    AppEnable = enable,
                    GameName = string.IsNullOrWhiteSpace(payload.Name) ? g.GameName : payload.Name,
                    Name = string.IsNullOrWhiteSpace(payload.Name) ? g.Name : payload.Name,
                    UpdateAt = now
                })
                .Where(g => g.SiteID == siteId && g.Type == payload.Type && g.Code == code)
                .ExecuteCommandAsync(cancellationToken);
            if (n > 0) touched += n;
        }

        return touched;
    }
}
