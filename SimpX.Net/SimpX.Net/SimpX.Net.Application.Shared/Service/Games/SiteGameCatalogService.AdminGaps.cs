using SimpX.Net.Data.Entity.Games;
using SimpX.Net.Data.Entity.Sites;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 游戏列表页缺口。对齐旧 /admin/game/listing/*。
/// </summary>
public sealed partial class SiteGameCatalogService
{
    /// <summary>
    /// 全量列表（兼容旧 getlist 筛选）。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行</returns>
    public async Task<List<SiteGameListDetailDto>> GetListAsync(
        SiteGameListFilterInput input,
        CancellationToken cancellationToken = default)
    {
        input ??= new SiteGameListFilterInput();
        var siteId = _site.RequireSiteId();
        var query = _db.Queryable<SiteGameList>().Where(g => g.SiteID == siteId);

        // 旧：if (QF("select") != "0") Where GameCode == select；缺参/空串 → GameCode=="" → 0 行；select=0 不过滤
        if (!string.Equals(input.Select, "0", StringComparison.Ordinal))
            query = query.Where(g => g.GameCode == (input.Select ?? string.Empty).Trim());
        if (!string.IsNullOrWhiteSpace(input.Name))
            query = query.Where(g => g.Name.Contains(input.Name.Trim()));
        if (input.Type > 0)
            query = query.Where(g => g.Type == input.Type);
        if (input.Category >= 0)
            query = query.Where(g => g.Category == input.Category);
        if (input.IsEnable.HasValue)
            query = query.Where(g => g.IsEnable == input.IsEnable.Value);
        if (input.RecommendEnable.HasValue)
            query = query.Where(g => g.RecommendEnable == input.RecommendEnable.Value);
        if (input.NewEnable.HasValue)
            query = query.Where(g => g.NewEnable == input.NewEnable.Value);

        if (!string.IsNullOrWhiteSpace(input.OpenType) && input.CurStatus.HasValue)
        {
            var status = input.CurStatus.Value;
            query = input.OpenType.Trim().ToUpperInvariant() switch
            {
                "APP" => query.Where(g => g.AppEnable == status),
                "PC" => query.Where(g => g.PcEnable == status),
                "RECOMMEND" => query.Where(g => g.RecommendEnable == status),
                _ => query
            };
        }

        var rows = await query.OrderBy(g => g.Sort == 0 ? 1 : 0).OrderBy(g => g.Sort).ToListAsync(cancellationToken);

        // 旧：仅保留系统层 GameCategorys 覆盖的 Type+Category
        var sysRows = await _db.Queryable<SystemGameSetting>()
            .Where(s => s.Status != 0)
            .Select(s => new { s.Type, s.GameCategorys })
            .ToListAsync(cancellationToken);
        var sysCategoryMap = new Dictionary<int, HashSet<string>>();
        foreach (var s in sysRows)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var part in (s.GameCategorys ?? string.Empty)
                         .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                set.Add(part);
            sysCategoryMap[s.Type] = set;
        }

        rows = rows.Where(t =>
        {
            if (!sysCategoryMap.TryGetValue(t.Type, out var cats) || cats.Count == 0)
                return false;
            var name = GameCategoryNameMap.Format(t.Category);
            return cats.Contains(name) || cats.Contains(t.Category.ToString());
        }).ToList();

        return rows.Select(ToDetail).ToList();
    }

    /// <summary>
    /// 详情。对齐旧 gamelistinfo。
    /// </summary>
    /// <param name="id">编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>详情</returns>
    public async Task<SiteGameListDetailDto> GameListInfoAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw Oops.Oh("#参数错误#");
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<SiteGameList>()
            .Where(g => g.SiteID == siteId && g.ID == id)
            .Take(1)
            .ToListAsync(cancellationToken);
        return ToDetail(rows.FirstOrDefault() ?? throw Oops.Oh("#记录不存在#"));
    }

    /// <summary>
    /// 分类树。对齐旧 getcatetree。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>树节点</returns>
    public async Task<List<SiteGameCateTreeDto>> GetCateTreeAsync(CancellationToken cancellationToken = default)
    {
        var cates = await ListCatesAsync(cancellationToken);
        return cates.Select(c => new SiteGameCateTreeDto
        {
            ID = c.ID,
            GameName = c.GameName,
            GameCode = c.GameCode,
            GameID = c.GameID,
            GameParentID = c.GameParentID,
            IsEnable = c.IsEnable
        }).ToList();
    }

    /// <summary>
    /// 保存列表行。对齐旧 savegamelist。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SaveGameListAsync(SiteGameListDetailSaveInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.ID <= 0)
            throw Oops.Oh("#参数错误#");
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<SiteGameList>()
            .Where(g => g.SiteID == siteId && g.ID == input.ID)
            .Take(1)
            .ToListAsync(cancellationToken);
        var row = rows.FirstOrDefault() ?? throw Oops.Oh("#记录不存在#");
        row.Name = input.Name ?? row.Name;
        row.IsEnable = input.IsEnable;
        row.PcEnable = input.PcEnable;
        row.AppEnable = input.AppEnable;
        row.RecommendEnable = input.RecommendEnable;
        row.NewEnable = input.NewEnable;
        row.Recommend = input.Recommend;
        row.Sort = input.Sort;
        row.ShowImg = input.ShowImg ?? row.ShowImg;
        row.Remark = input.Remark ?? row.Remark;
        row.UpdateAt = DateTime.Now;
        await _db.Updateable(row).ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 改排序/置顶。对齐旧 updatesort。
    /// </summary>
    /// <param name="id">编号</param>
    /// <param name="isTop">是否置顶（写最大 Sort）</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task UpdateSortAsync(int id, bool isTop, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw Oops.Oh("#参数错误#");
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<SiteGameList>()
            .Where(g => g.SiteID == siteId && g.ID == id)
            .Take(1)
            .ToListAsync(cancellationToken);
        var row = rows.FirstOrDefault() ?? throw Oops.Oh("#记录不存在#");
        if (isTop)
        {
            var max = await _db.Queryable<SiteGameList>()
                .Where(g => g.SiteID == siteId)
                .MaxAsync(g => g.Sort, cancellationToken);
            row.Sort = max + 1;
        }
        else
            row.Sort = 0;
        row.UpdateAt = DateTime.Now;
        await _db.Updateable(row).UpdateColumns(g => new { g.Sort, g.UpdateAt }).ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 推荐列表。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行</returns>
    public async Task<List<SiteGameListDetailDto>> GetRecommendListAsync(CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<SiteGameList>()
            .Where(g => g.SiteID == siteId && g.RecommendEnable)
            .OrderBy(g => g.Recommend, OrderByType.Desc)
            .ToListAsync(cancellationToken);
        return rows.Select(ToDetail).ToList();
    }

    /// <summary>
    /// 最新列表。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行</returns>
    public async Task<List<SiteGameListDetailDto>> GetNewListAsync(CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<SiteGameList>()
            .Where(g => g.SiteID == siteId && g.NewEnable)
            .OrderBy(g => g.New, OrderByType.Desc)
            .ToListAsync(cancellationToken);
        return rows.Select(ToDetail).ToList();
    }

    /// <summary>
    /// 批量写推荐值。recommends 形如 id:value,id:value。
    /// </summary>
    /// <param name="recommends">串</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SaveRecommendListAsync(string? recommends, CancellationToken cancellationToken = default)
    {
        await SaveRankPairsAsync(recommends, recommend: true, cancellationToken);
    }

    /// <summary>
    /// 批量写最新值。news 形如 id:value。
    /// </summary>
    /// <param name="news">串</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SaveNewListAsync(string? news, CancellationToken cancellationToken = default)
    {
        await SaveRankPairsAsync(news, recommend: false, cancellationToken);
    }

    /// <summary>
    /// 从系统预设补当前站缺失游戏行。对齐旧 creategamelist（按 Type）。
    /// </summary>
    /// <param name="type">游戏类型；0=全部开通类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>新增条数</returns>
    public async Task<int> CreateGameListAsync(int type, CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var openTypes = await _db.Queryable<GameSetting>()
            .Where(g => g.SiteID == siteId && g.IsOpen && !g.IsDisable)
            .Select(g => g.Game)
            .ToListAsync(cancellationToken);
        if (type > 0)
            openTypes = openTypes.Where(t => t == type).ToList();
        if (openTypes.Count == 0)
            return 0;

        var existing = await _db.Queryable<SiteGameList>()
            .Where(g => g.SiteID == siteId && openTypes.Contains(g.Type))
            .Select(g => new { g.Type, g.Code })
            .ToListAsync(cancellationToken);
        var existKeys = existing.Select(e => $"{e.Type}|{e.Code}").ToHashSet(StringComparer.OrdinalIgnoreCase);

        // 无系统游戏明细表时：为每个已开通类型补一条占位 Code=*
        var added = 0;
        foreach (var t in openTypes.Distinct())
        {
            var key = $"{t}|*";
            if (existKeys.Contains(key))
                continue;
            await _db.Insertable(new SiteGameList
            {
                SiteID = siteId,
                GameName = t.ToString(),
                GameCode = t.ToString(),
                Name = t.ToString(),
                Type = t,
                Category = 0,
                Code = "*",
                IsEnable = true,
                PcEnable = true,
                AppEnable = true,
                RecommendEnable = false,
                ShowImg = string.Empty,
                Sort = 0,
                Tags = string.Empty,
                Recommend = 0,
                IsJack = false,
                Remark = string.Empty,
                UpdateAt = DateTime.Now,
                CreateAt = DateTime.Now,
                GameTag = 0,
                NewEnable = false,
                New = 0
            }).ExecuteCommandAsync(cancellationToken);
            added++;
        }

        return added;
    }

    /// <summary>
    /// 全量补预设。对齐旧 creategamelistall。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>新增条数</returns>
    public Task<int> CreateGameListAllAsync(CancellationToken cancellationToken = default) =>
        CreateGameListAsync(0, cancellationToken);

    /// <summary>
    /// 刷新缓存占位（无独立缓存键时返回成功）。
    /// </summary>
    /// <returns>说明</returns>
    public Task<string> RefreshCacheAsync() =>
        Task.FromResult("#已跳过：本站列表直接读库#");

    /// <summary>
    /// 分类详情。对齐旧 cate/info。
    /// </summary>
    public async Task<object> CateInfoAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw Oops.Oh("#参数错误#");
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<SiteGameCate>()
            .Where(c => c.SiteID == siteId && c.ID == id)
            .Take(1)
            .ToListAsync(cancellationToken);
        var row = rows.FirstOrDefault() ?? throw Oops.Oh("#记录不存在#");
        return new
        {
            row.ID,
            row.Sort,
            row.GameName,
            row.Remark
        };
    }

    /// <summary>
    /// 保存分类备注/排序。对齐旧 cate/save。
    /// </summary>
    public async Task SaveCateDetailAsync(int id, int sort, string? remark, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw Oops.Oh("#数据错误#");
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<SiteGameCate>()
            .Where(c => c.SiteID == siteId && c.ID == id)
            .Take(1)
            .ToListAsync(cancellationToken);
        var row = rows.FirstOrDefault() ?? throw Oops.Oh("#数据错误#");
        row.Sort = sort;
        row.Remark = remark ?? string.Empty;
        row.UpdateAt = DateTime.Now;
        await _db.Updateable(row).UpdateColumns(c => new { c.Sort, c.Remark, c.UpdateAt })
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 从系统分类名补站点分类行。对齐旧 creategamecate（精简：不删多余行）。
    /// </summary>
    public async Task<string> CreateGameCateAsync(CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var names = new (string Code, string Name, int GameId)[]
        {
            ("Lottery", "彩票", 10),
            ("ESport", "电竞", 11),
            ("Slot", "电子", 12),
            ("Live", "视讯", 13),
            ("Chess", "棋牌", 14),
            ("Sport", "体育", 15),
            ("Fish", "捕鱼", 16),
            ("Animal", "动物", 17),
            ("MiniGame", "小游戏", 18),
            ("Battle", "对战", 19),
        };
        var existing = await _db.Queryable<SiteGameCate>()
            .Where(c => c.SiteID == siteId)
            .Select(c => c.GameCode)
            .ToListAsync(cancellationToken);
        var set = existing.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var now = DateTime.Now;
        var inserts = new List<SiteGameCate>();
        foreach (var (code, name, gameId) in names)
        {
            if (set.Contains(code))
                continue;
            inserts.Add(new SiteGameCate
            {
                SiteID = siteId,
                GameName = name,
                GameCode = code,
                GameID = gameId,
                GameParentID = 0,
                IsEnable = true,
                PcEnable = true,
                AppEnable = true,
                Sort = 1,
                Remark = string.Empty,
                CreateAt = now,
                UpdateAt = now
            });
        }

        if (inserts.Count > 0)
            await _db.Insertable(inserts).ExecuteCommandAsync(cancellationToken);
        return "执行完成";
    }

    /// <summary>
    /// 改分类开关位。对齐旧 cate/updatestatus（Type:1开启 2PC 3App）。
    /// </summary>
    /// <param name="id">分类编号</param>
    /// <param name="type">位类型</param>
    /// <param name="value">开/关</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task UpdateCateStatusAsync(
        int id,
        int type,
        bool value,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw Oops.Oh("#参数错误#");
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<SiteGameCate>()
            .Where(c => c.SiteID == siteId && c.ID == id)
            .Take(1)
            .ToListAsync(cancellationToken);
        var row = rows.FirstOrDefault() ?? throw Oops.Oh("#记录不存在#");
        switch (type)
        {
            case 1:
                row.IsEnable = value;
                break;
            case 2:
                row.PcEnable = value;
                break;
            case 3:
                row.AppEnable = value;
                break;
            default:
                throw Oops.Oh("#参数错误#");
        }

        row.UpdateAt = DateTime.Now;
        await _db.Updateable(row).ExecuteCommandAsync(cancellationToken);
    }

    private async Task SaveRankPairsAsync(string? csv, bool recommend, CancellationToken cancellationToken)
    {
        var siteId = _site.RequireSiteId();
        var pairs = (csv ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var pair in pairs)
        {
            var segs = pair.Split(':', StringSplitOptions.TrimEntries);
            if (segs.Length != 2 || !int.TryParse(segs[0], out var id) || !int.TryParse(segs[1], out var val))
                continue;
            if (recommend)
            {
                await _db.Updateable<SiteGameList>()
                    .SetColumns(g => new SiteGameList { Recommend = val, RecommendEnable = val > 0, UpdateAt = DateTime.Now })
                    .Where(g => g.SiteID == siteId && g.ID == id)
                    .ExecuteCommandAsync(cancellationToken);
            }
            else
            {
                await _db.Updateable<SiteGameList>()
                    .SetColumns(g => new SiteGameList { New = val, NewEnable = val > 0, UpdateAt = DateTime.Now })
                    .Where(g => g.SiteID == siteId && g.ID == id)
                    .ExecuteCommandAsync(cancellationToken);
            }
        }
    }

    private static SiteGameListDetailDto ToDetail(SiteGameList g) => new()
    {
        ID = g.ID,
        GameName = g.GameName,
        GameCode = g.GameCode,
        Name = g.Name,
        Code = g.Code,
        Type = g.Type,
        Category = g.Category,
        IsEnable = g.IsEnable,
        PcEnable = g.PcEnable,
        AppEnable = g.AppEnable,
        RecommendEnable = g.RecommendEnable,
        NewEnable = g.NewEnable,
        Recommend = g.Recommend,
        New = g.New,
        Sort = g.Sort,
        ShowImg = g.ShowImg,
        Remark = g.Remark
    };
}

/// <summary>列表筛选。</summary>
public sealed class SiteGameListFilterInput
{
    /// <summary>厂商编码。</summary>
    public string Select { get; set; }

    /// <summary>名称。</summary>
    public string Name { get; set; }

    /// <summary>类型。</summary>
    public int Type { get; set; }

    /// <summary>分类；-1=全部。</summary>
    public int Category { get; set; } = -1;

    /// <summary>开启。</summary>
    public bool? IsEnable { get; set; }

    /// <summary>推荐开启。</summary>
    public bool? RecommendEnable { get; set; }

    /// <summary>最新开启。</summary>
    public bool? NewEnable { get; set; }

    /// <summary>APP/PC/Recommend。</summary>
    public string OpenType { get; set; }

    /// <summary>OpenType 对应状态。</summary>
    public bool? CurStatus { get; set; }
}

/// <summary>列表详情。</summary>
public sealed class SiteGameListDetailDto
{
    /// <summary>编号。</summary>
    public int ID { get; set; }

    /// <summary>厂商名。</summary>
    public string GameName { get; set; }

    /// <summary>厂商编码。</summary>
    public string GameCode { get; set; }

    /// <summary>显示名。</summary>
    public string Name { get; set; }

    /// <summary>代码。</summary>
    public string Code { get; set; }

    /// <summary>类型。</summary>
    public int Type { get; set; }

    /// <summary>分类。</summary>
    public int Category { get; set; }

    /// <summary>开启。</summary>
    public bool IsEnable { get; set; }

    /// <summary>PC。</summary>
    public bool PcEnable { get; set; }

    /// <summary>App。</summary>
    public bool AppEnable { get; set; }

    /// <summary>推荐。</summary>
    public bool RecommendEnable { get; set; }

    /// <summary>最新。</summary>
    public bool NewEnable { get; set; }

    /// <summary>推荐值。</summary>
    public int Recommend { get; set; }

    /// <summary>最新值。</summary>
    public int New { get; set; }

    /// <summary>排序。</summary>
    public int Sort { get; set; }

    /// <summary>图。</summary>
    public string ShowImg { get; set; }

    /// <summary>备注。</summary>
    public string Remark { get; set; }
}

/// <summary>保存列表。</summary>
public sealed class SiteGameListDetailSaveInput
{
    /// <summary>编号。</summary>
    public int ID { get; set; }

    /// <summary>名称。</summary>
    public string Name { get; set; }

    /// <summary>开启。</summary>
    public bool IsEnable { get; set; }

    /// <summary>PC。</summary>
    public bool PcEnable { get; set; }

    /// <summary>App。</summary>
    public bool AppEnable { get; set; }

    /// <summary>推荐。</summary>
    public bool RecommendEnable { get; set; }

    /// <summary>最新。</summary>
    public bool NewEnable { get; set; }

    /// <summary>推荐值。</summary>
    public int Recommend { get; set; }

    /// <summary>排序。</summary>
    public int Sort { get; set; }

    /// <summary>图。</summary>
    public string ShowImg { get; set; }

    /// <summary>备注。</summary>
    public string Remark { get; set; }
}

/// <summary>分类树节点。</summary>
public sealed class SiteGameCateTreeDto
{
    /// <summary>编号。</summary>
    public int ID { get; set; }

    /// <summary>名称。</summary>
    public string GameName { get; set; }

    /// <summary>编码。</summary>
    public string GameCode { get; set; }

    /// <summary>游戏枚举。</summary>
    public int GameID { get; set; }

    /// <summary>父级。</summary>
    public int GameParentID { get; set; }

    /// <summary>开启。</summary>
    public bool IsEnable { get; set; }
}
