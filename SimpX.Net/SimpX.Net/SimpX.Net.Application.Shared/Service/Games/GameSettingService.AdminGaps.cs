using System.Text.Json;
using SimpX.Net.Data.Entity.Games;
using SimpX.Net.Data.Entity.Sites;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 厂商 API 列表/排序/开通下拉。对齐旧 /admin/game/setting/gamelist|gameopenlist|gamesort 等。
/// </summary>
public sealed partial class GameSettingService
{
    /// <summary>
    /// 当前站未下架的游戏简表。对齐旧 gamelist。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行</returns>
    public async Task<List<GameSettingOpenItemDto>> GameListAsync(CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<GameSetting>()
            .Where(g => g.SiteID == siteId && !g.IsDisable)
            .OrderBy(g => g.Sort, OrderByType.Desc)
            .ToListAsync(cancellationToken);
        return rows.Select(ToOpenItem).ToList();
    }

    /// <summary>
    /// 已开通下拉。对齐旧 gameopenlist。
    /// </summary>
    /// <param name="cate">分类关键字（系统层 GameCategorys 包含）；空=全部</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行</returns>
    public async Task<List<GameSettingOpenItemDto>> GameOpenListAsync(
        string? cate = null,
        CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var siteRows = await _db.Queryable<GameSetting>()
            .Where(g => g.SiteID == siteId && !g.IsDisable)
            .OrderBy(g => g.Name)
            .ToListAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(cate))
            return siteRows.Select(ToOpenItem).ToList();

        var key = cate.Trim();
        var sys = await _db.Queryable<SystemGameSetting>()
            .Where(s => s.GameCategorys != null && s.GameCategorys.Contains(key))
            .Select(s => s.Type)
            .ToListAsync(cancellationToken);
        var allow = sys.ToHashSet();
        return siteRows.Where(g => allow.Contains(g.Game)).Select(ToOpenItem).ToList();
    }

    /// <summary>
    /// 打赏用开通列表（含 WGLottery 占位 Type=0 时仍返回开通项）。对齐旧 gameopenlistreward。
    /// </summary>
    /// <param name="cate">分类</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行</returns>
    public Task<List<GameSettingOpenItemDto>> GameOpenListRewardAsync(
        string? cate = null,
        CancellationToken cancellationToken = default) =>
        GameOpenListAsync(cate, cancellationToken);

    /// <summary>
    /// 按分类筛已开通。对齐旧 gamelistbycategory。
    /// </summary>
    /// <param name="category">分类名或编号串</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行</returns>
    public async Task<List<GameSettingOpenItemDto>> GameListByCategoryAsync(
        string? category,
        CancellationToken cancellationToken = default)
    {
        var list = await GameOpenListAsync(category, cancellationToken);
        return list.Where(t => t.IsOpen).ToList();
    }

    /// <summary>
    /// 保存排序。对齐旧 gamesort：Type 逗号分隔，越靠前 Sort 越大。
    /// </summary>
    /// <param name="typesCsv">游戏类型 CSV</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task GameSortAsync(string? typesCsv, CancellationToken cancellationToken = default)
    {
        var parts = (typesCsv ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => int.TryParse(s, out var n) ? n : 0)
            .Where(n => n != 0)
            .ToList();
        if (parts.Count == 0)
            throw Oops.Oh("#参数错误#");

        var siteId = _site.RequireSiteId();
        short sort = (short)parts.Count;
        foreach (var game in parts)
        {
            var n = await _db.Updateable<GameSetting>()
                .SetColumns(g => new GameSetting { Sort = sort })
                .Where(g => g.SiteID == siteId && g.Game == game)
                .ExecuteCommandAsync(cancellationToken);
            if (n == 0)
                throw Oops.Oh("#游戏不存在#");
            sort--;
        }
    }

    /// <summary>
    /// 币种列表：读系统层 Currencys。对齐旧 gamecurrencylist。
    /// </summary>
    /// <param name="type">游戏类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>币种串列表</returns>
    public async Task<List<string>> GameCurrencyListAsync(int type, CancellationToken cancellationToken = default)
    {
        var rows = await _db.Queryable<SystemGameSetting>()
            .Where(s => s.Type == type)
            .Take(1)
            .ToListAsync(cancellationToken);
        var raw = rows.FirstOrDefault()?.Currencys ?? string.Empty;
        return raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// 维护信息。对齐旧 maintaininfo。
    /// </summary>
    /// <param name="type">游戏类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>维护</returns>
    public async Task<GameMaintainDto> MaintainInfoAsync(int type, CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<GameSetting>()
            .Where(g => g.SiteID == siteId && g.Game == type)
            .Take(1)
            .ToListAsync(cancellationToken);
        var g = rows.FirstOrDefault() ?? throw Oops.Oh("#游戏不存在#");
        var sys = await _db.Queryable<SystemGameSetting>()
            .Where(s => s.Type == type)
            .Take(1)
            .ToListAsync(cancellationToken);
        return new GameMaintainDto
        {
            Type = g.Game,
            Name = g.Name ?? string.Empty,
            Status = g.Status,
            Maintenance = g.Maintenance ?? string.Empty,
            GameCategorys = sys.FirstOrDefault()?.GameCategorys ?? string.Empty,
            RemainTime = "0天0小时0分",
            RemainTimeSpan = "00:00:00"
        };
    }

    /// <summary>
    /// 保存维护。对齐旧 savemaintain。
    /// </summary>
    /// <param name="input">入参</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SaveMaintainAsync(GameMaintainSaveInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.Type == 0)
            throw Oops.Oh("#游戏不存在#");
        var siteId = _site.RequireSiteId();
        var n = await _db.Updateable<GameSetting>()
            .SetColumns(g => new GameSetting
            {
                Status = input.Status,
                Maintenance = input.Maintenance ?? string.Empty
            })
            .Where(g => g.SiteID == siteId && g.Game == input.Type)
            .ExecuteCommandAsync(cancellationToken);
        if (n != 1)
            throw Oops.Oh("#游戏不存在#");
    }

    /// <summary>
    /// 标签展示字段。对齐旧 gametag 只读。
    /// </summary>
    /// <param name="type">游戏类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>标签</returns>
    public async Task<GameTagSettingDto> GameTagAsync(int type, CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<GameSetting>()
            .Where(g => g.SiteID == siteId && g.Game == type)
            .Take(1)
            .ToListAsync(cancellationToken);
        var g = rows.FirstOrDefault() ?? throw Oops.Oh("#游戏不存在#");
        return new GameTagSettingDto
        {
            Type = g.Game,
            Label = g.Label ?? string.Empty,
            Logo = g.Logo ?? string.Empty,
            Backdrop = g.Backdrop ?? string.Empty,
            IsBackdrop = g.IsBackdrop
        };
    }

    /// <summary>
    /// 保存标签。对齐旧 setgametag。
    /// </summary>
    /// <param name="input">入参</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SetGameTagAsync(GameTagSettingDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.Type == 0)
            throw Oops.Oh("#游戏不存在#");
        var siteId = _site.RequireSiteId();
        var n = await _db.Updateable<GameSetting>()
            .SetColumns(g => new GameSetting
            {
                Label = input.Label ?? string.Empty,
                Logo = input.Logo ?? string.Empty,
                Backdrop = input.Backdrop ?? string.Empty,
                IsBackdrop = input.IsBackdrop
            })
            .Where(g => g.SiteID == siteId && g.Game == input.Type)
            .ExecuteCommandAsync(cancellationToken);
        if (n != 1)
            throw Oops.Oh("#游戏不存在#");
    }

    /// <summary>
    /// 开通分类列表（站点分类名）。对齐旧 sitegamecate。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分类名</returns>
    public async Task<List<string>> SiteGameCateNamesAsync(CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<SystemGameSetting>()
            .Where(s => s.Status != SysStatusStop && s.GameCategorys != null && s.GameCategorys != string.Empty)
            .Select(s => s.GameCategorys)
            .ToListAsync(cancellationToken);
        return rows
            .SelectMany(s => (s ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(s => s)
            .ToList();
    }

    /// <summary>
    /// 旧 /admin/game/info/get：Type 枚举名 + Category 字典。
    /// </summary>
    public async Task<List<object>> InfoGetForOldApiAsync(CancellationToken cancellationToken = default)
    {
        var rows = await GameListAsync(cancellationToken);
        return rows.Select(g => (object)new
        {
            Type = GameTypeMeta.Format(g.Type),
            Name = string.IsNullOrEmpty(g.Name) ? GameTypeMeta.Format(g.Type) : g.Name,
            Category = GameTypeMeta.CategoryDict(g.Type)
        }).ToList();
    }

    /// <summary>
    /// 旧 /admin/game/info/category：按分类分组。
    /// </summary>
    public async Task<Dictionary<string, List<object>>> CategoryForOldApiAsync(
        CancellationToken cancellationToken = default)
    {
        var rows = await GameListAsync(cancellationToken);
        var map = new Dictionary<string, List<object>>(StringComparer.OrdinalIgnoreCase);
        foreach (var g in rows)
        {
            if (!GameTypeMeta.TryGet(g.Type, out var meta) || meta.Categories.Length == 0)
                continue;
            var item = new
            {
                Type = meta.Code,
                Name = string.IsNullOrEmpty(g.Name) ? meta.Code : g.Name
            };
            foreach (var c in meta.Categories)
            {
                if (!map.TryGetValue(c, out var list))
                {
                    list = [];
                    map[c] = list;
                }

                list.Add(item);
            }
        }

        return map;
    }

    /// <summary>
    /// 旧 /admin/game/info/gamecategory：站点层未下架厂商 Type/Name。
    /// </summary>
    public async Task<List<object>> GameCategoryForOldApiAsync(CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<GameSetting>()
            .Where(g => g.SiteID == siteId && !g.IsDisable)
            .OrderBy(g => g.Name)
            .ToListAsync(cancellationToken);
        return rows.Select(s => (object)new
        {
            Type = GameTypeMeta.Format(s.Game),
            Name = string.IsNullOrWhiteSpace(s.Name) ? GameTypeMeta.Format(s.Game) : s.Name
        }).ToList();
    }

    /// <summary>
    /// 旧 gamelist 行（Type 为枚举名）。
    /// </summary>
    public async Task<List<object>> GameListForOldApiAsync(CancellationToken cancellationToken = default)
    {
        var rows = await GameListAsync(cancellationToken);
        return rows.Select(g => (object)new
        {
            IsOpen = g.IsOpen ? 1 : 0,
            Name = string.IsNullOrEmpty(g.Name) ? GameTypeMeta.Format(g.Type) : g.Name,
            Type = GameTypeMeta.Format(g.Type),
            Game = string.IsNullOrEmpty(g.Game) ? GameTypeMeta.Format(g.Type) : g.Game
        }).ToList();
    }

    /// <summary>
    /// 旧 gameopenlist 行（Type/Game）。
    /// </summary>
    public async Task<List<object>> GameOpenListForOldApiAsync(
        string? cate = null,
        CancellationToken cancellationToken = default)
    {
        var rows = await GameOpenListAsync(cate, cancellationToken);
        return rows.Select(g => (object)new
        {
            Type = GameTypeMeta.Format(g.Type),
            Game = string.IsNullOrEmpty(g.Name) ? GameTypeMeta.Format(g.Type) : g.Name
        }).ToList();
    }

    private static GameSettingOpenItemDto ToOpenItem(GameSetting g) => new()
    {
        Type = g.Game,
        Name = g.Name ?? string.Empty,
        Game = string.IsNullOrEmpty(g.Name) ? GameTypeMeta.Format(g.Game) : g.Name,
        IsOpen = g.IsOpen
    };

    /// <summary>
    /// 游戏预设初始化下拉。对齐旧 gameinit。
    /// </summary>
    public async Task<object> GameInitAsync(CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var currencies = await _db.Queryable<SiteCurrency>()
            .Where(c => c.SiteID == siteId && c.IsOpen)
            .OrderBy(c => c.Sort)
            .Select(c => new { c.Currency })
            .ToListAsync(cancellationToken);
        var languages = await _db.Queryable<SiteSupportLanguage>()
            .Where(l => l.SiteID == siteId && l.IsOpen)
            .OrderBy(l => l.Sort)
            .Select(l => new { l.Language })
            .ToListAsync(cancellationToken);
        var baseCurrency = await SiteCurrencyFx.GetBaseCurrencyAsync(_db, siteId, cancellationToken);
        return new
        {
            Currency = JsonSerializer.Serialize(currencies),
            Language = JsonSerializer.Serialize(languages),
            BaseCurrency = baseCurrency
        };
    }

    /// <summary>
    /// 流水比例表。无 GameFlowRate 实体时返回空表（路径先挂通）。
    /// </summary>
    public Task<List<GameFlowRateItemDto>> WashFlowAsync(CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        _ = _site.RequireSiteId();
        return Task.FromResult(new List<GameFlowRateItemDto>());
    }

    /// <summary>
    /// 保存流水比例。表未迁移：占位。
    /// </summary>
    public Task SaveWashFlowAsync(CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        _ = _site.RequireSiteId();
        throw Oops.Oh("#功能尚未接通#");
    }
}

/// <summary>开通/简表行。</summary>
public sealed class GameSettingOpenItemDto
{
    /// <summary>游戏类型。</summary>
    public int Type { get; set; }

    /// <summary>显示名。</summary>
    public string Name { get; set; }

    /// <summary>游戏名（兼容旧 Game 字段）。</summary>
    public string Game { get; set; }

    /// <summary>是否开通。</summary>
    public bool IsOpen { get; set; }
}

/// <summary>维护信息。</summary>
public sealed class GameMaintainDto
{
    /// <summary>类型。</summary>
    public int Type { get; set; }

    /// <summary>名称。</summary>
    public string Name { get; set; }

    /// <summary>状态。</summary>
    public int Status { get; set; }

    /// <summary>维护内容。</summary>
    public string Maintenance { get; set; }

    /// <summary>系统分类串。</summary>
    public string GameCategorys { get; set; }

    /// <summary>剩余时间文案。</summary>
    public string RemainTime { get; set; }

    /// <summary>剩余 TimeSpan 串。</summary>
    public string RemainTimeSpan { get; set; }
}

/// <summary>保存维护。</summary>
public sealed class GameMaintainSaveInput
{
    /// <summary>类型。</summary>
    public int Type { get; set; }

    /// <summary>状态。</summary>
    public int Status { get; set; }

    /// <summary>维护分类。</summary>
    public string Maintenance { get; set; }

    /// <summary>恢复时间（兼容旧字段；本刀未落库）。</summary>
    public DateTime? ResumeAt { get; set; }
}

/// <summary>标签设置。</summary>
public sealed class GameTagSettingDto
{
    /// <summary>类型。</summary>
    public int Type { get; set; }

    /// <summary>标签。</summary>
    public string Label { get; set; }

    /// <summary>Logo。</summary>
    public string Logo { get; set; }

    /// <summary>背景。</summary>
    public string Backdrop { get; set; }

    /// <summary>是否推荐背景。</summary>
    public bool IsBackdrop { get; set; }
}

/// <summary>流水比例行（无实体时只读空表占位）。</summary>
public sealed class GameFlowRateItemDto
{
    /// <summary>厂商类型。</summary>
    public int Type { get; set; }

    /// <summary>厂商名。</summary>
    public string Game { get; set; }

    /// <summary>分类比例 JSON。</summary>
    public string Setting { get; set; }
}
