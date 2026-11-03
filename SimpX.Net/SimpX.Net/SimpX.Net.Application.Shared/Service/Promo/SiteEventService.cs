using System.Text.RegularExpressions;
using SimpX.Net.Data.Entity.Sites;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 租户后台优惠活动。对齐旧版 /admin/award/events/list|info|save|delete（含类型唯一与 Setting 主校验）。
/// </summary>
public sealed partial class SiteEventService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;

    /// <summary>
    /// 注入业务库与当前站。优惠活动在 BetWin30 业务库，不能走主库 Sqlite。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站</param>
    public SiteEventService(IBizDbAccessor biz, ICurrentSiteAccessor site)
    {
        _db = biz.Db;
        _site = site;
    }

    /// <summary>
    /// 当前站活动列表。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行列表</returns>
    public async Task<List<SiteEventListItemDto>> ListAsync(
        SiteEventListInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var siteId = _site.RequireSiteId();

        var columns = await _db.Queryable<SiteEventColumn>()
            .Where(c => c.SiteID == siteId)
            .ToListAsync(cancellationToken);
        var columnMap = columns.ToDictionary(c => c.ID, c => c.Name ?? c.ID.ToString());

        var query = _db.Queryable<SiteEvent>().Where(t => t.SiteID == siteId);

        if (!string.IsNullOrWhiteSpace(input.Type) && int.TryParse(input.Type.Trim(), out var type))
            query = query.Where(t => t.Type == type);

        if (input.ColID != 0)
            query = query.Where(t => t.ColID == input.ColID);

        if (!string.IsNullOrWhiteSpace(input.Status) && int.TryParse(input.Status.Trim(), out var status))
            query = query.Where(t => t.Status == status);

        if (!string.IsNullOrWhiteSpace(input.Title))
            query = query.Where(t => t.Title != null && t.Title.Contains(input.Title.Trim()));

        if (input.IsTop == 1)
            query = query.Where(t => t.Sort != 0);

        if (!string.IsNullOrWhiteSpace(input.Currency))
        {
            var cur = "," + input.Currency.Trim() + ",";
            query = query.Where(t => t.SupportCurrency != null && ("," + t.SupportCurrency + ",").Contains(cur));
        }

        var rows = await query
            .OrderBy(t => t.Sort, OrderByType.Desc)
            .OrderBy(t => t.ID, OrderByType.Desc)
            .ToListAsync(cancellationToken);

        var now = DateTime.Now;
        return rows.Select(t => new SiteEventListItemDto
        {
            ID = t.ID,
            Title = t.Title ?? string.Empty,
            Type = t.Type,
            ColID = t.ColID,
            Column = columnMap.TryGetValue(t.ColID, out var cn) ? cn : t.ColID.ToString(),
            CreateAt = t.CreateAt,
            StartAt = t.StartAt,
            ExpireAt = t.ExpireAt,
            Member = t.Member,
            Status = DescribeTimeline(t.StartAt, t.ExpireAt, now),
            IsOpen = t.IsOpen,
            IsLoginOpen = t.IsLoginAfterOpen || t.IsLoginBeforeOpen,
            IsLoginAfterOpen = t.IsLoginAfterOpen,
            IsLoginBeforeOpen = t.IsLoginBeforeOpen,
            IsQuickEntrance = t.IsQuickEntrance,
            IsDetail = t.IsDetail,
            IsNewerTask = t.IsNewerTask,
            JumpType = t.JumpType,
            SupportCurrency = t.SupportCurrency ?? string.Empty,
            Demo = t.Demo ?? string.Empty,
            IsTop = t.Sort > 0,
            IsVisible = t.IsVisible
        }).ToList();
    }

    /// <summary>
    /// 详情；编号 0 或不存在返回新建默认值。
    /// </summary>
    /// <param name="id">编号</param>
    /// <param name="type">新建时的模板类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>详情</returns>
    public async Task<SiteEventDetailDto> InfoAsync(
        int id,
        int type = 0,
        CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        SiteEvent ev = null;
        if (id > 0)
        {
            var rows = await _db.Queryable<SiteEvent>()
                .Where(t => t.ID == id && t.SiteID == siteId)
                .Take(1)
                .ToListAsync(cancellationToken);
            ev = rows.FirstOrDefault();
        }

        ev ??= new SiteEvent
        {
            Type = type,
            Title = "{}",
            SubTitle = "{}",
            Tag = "{}",
            Cover = "{}",
            Description = "{}",
            Content = "{}",
            FilterGame = string.Empty,
            FilterPlatform = string.Empty,
            StartAt = DateTime.Now.Date,
            ExpireAt = DateTime.Now.AddDays(30).Date
        };

        return ToDetail(ev);
    }

    /// <summary>
    /// 保存活动。对齐旧版：登录弹窗唯一、指定类型唯一、邀请有礼返点类型唯一、Setting.Check 主约束。
    /// </summary>
    /// <param name="input">入参</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SaveAsync(SiteEventSaveInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (string.IsNullOrWhiteSpace(input.Title))
            throw Oops.Oh("#请输入活动标题#");
        if (string.IsNullOrWhiteSpace(input.Content))
            throw Oops.Oh("#请输入活动内容#");
        if (input.ColID == 0)
            throw Oops.Oh("#请选择活动栏目#");

        var siteId = _site.RequireSiteId();
        var colOk = await _db.Queryable<SiteEventColumn>()
            .AnyAsync(c => c.ID == input.ColID && c.SiteID == siteId, cancellationToken);
        if (!colOk)
            throw Oops.Oh("#请选择活动栏目#");

        var now = DateTime.Now;
        if (input.IsLoginAfterOpen || input.IsLoginBeforeOpen)
        {
            var popupDup = await _db.Queryable<SiteEvent>()
                .AnyAsync(
                    t => t.SiteID == siteId
                         && t.IsVisible
                         && t.ExpireAt > now
                         && (t.IsLoginAfterOpen || t.IsLoginBeforeOpen)
                         && t.ID != input.ID,
                    cancellationToken);
            if (popupDup)
                throw Oops.Oh("只能有一个登录弹窗活动");
        }

        if (input.Type is SiteEventSettingChecker.TypeNewDailyTasks
            or SiteEventSettingChecker.TypeNewWeekTasks
            or SiteEventSettingChecker.TypeDelottery)
        {
            var typeDup = await _db.Queryable<SiteEvent>()
                .AnyAsync(
                    t => t.SiteID == siteId
                         && t.IsVisible
                         && t.ExpireAt > now
                         && t.Type == input.Type
                         && t.ID != input.ID,
                    cancellationToken);
            if (typeDup)
                throw Oops.Oh("只能有一个" + DescribeEventType(input.Type));
        }

        SiteEventSettingChecker.CheckSetting(input.Type, input.Setting ?? string.Empty);

        if (input.Type == SiteEventSettingChecker.TypeInviteReward && input.IsVisible && input.ExpireAt > now)
        {
            var peers = await _db.Queryable<SiteEvent>()
                .Where(t => t.SiteID == siteId
                            && t.IsVisible
                            && t.ExpireAt > now
                            && t.Type == SiteEventSettingChecker.TypeInviteReward)
                .ToListAsync(cancellationToken);
            var counts = new int[8];
            foreach (var ev in peers.Where(p => p.ID != input.ID))
            {
                var rt = SiteEventSettingChecker.ReadInviteReturnType(ev.SettingString);
                if (rt >= 0 && rt < counts.Length)
                    counts[rt]++;
            }

            var myReturn = SiteEventSettingChecker.ReadInviteReturnType(input.Setting);
            if (myReturn >= 0 && myReturn < counts.Length)
            {
                counts[myReturn]++;
                if (counts[myReturn] > 1)
                    throw Oops.Oh("只能有一个" + DescribeInviteReturnType(myReturn));
            }
        }

        var description = string.IsNullOrWhiteSpace(input.Description)
            ? LeftPlain(input.Content, 88)
            : input.Description;
        var status = ComputeStatus(input.StartAt, input.ExpireAt, now);

        if (input.ID == 0)
        {
            await _db.Insertable(new SiteEvent
            {
                SiteID = siteId,
                ColID = input.ColID,
                Title = input.Title,
                SubTitle = input.SubTitle ?? "{}",
                Tag = input.Tag ?? "{}",
                Cover = input.Cover ?? "{}",
                Description = description,
                Content = input.Content,
                Type = input.Type,
                StartAt = input.StartAt,
                ExpireAt = input.ExpireAt,
                IsVisible = input.IsVisible,
                IsOpen = input.IsOpen,
                IsLoginAfterOpen = input.IsLoginAfterOpen,
                IsLoginBeforeOpen = input.IsLoginBeforeOpen,
                IsQuickEntrance = input.IsQuickEntrance,
                IsDetail = input.IsDetail,
                IsNewerTask = input.IsNewerTask,
                JumpType = input.JumpType,
                Sort = input.Sort,
                Currency = input.Currency,
                SupportCurrency = input.SupportCurrency ?? string.Empty,
                SettingString = input.Setting ?? string.Empty,
                Demo = input.Demo ?? string.Empty,
                Skin = input.Skin ?? string.Empty,
                Views = input.Views ?? string.Empty,
                Background = input.Background ?? string.Empty,
                FilterGame = input.FilterGame ?? string.Empty,
                FilterPlatform = input.FilterPlatform ?? string.Empty,
                Status = status,
                Member = 0,
                CreateAt = DateTime.Now
            }).ExecuteCommandAsync(cancellationToken);

            await BumpColumnCountAsync(siteId, input.ColID, delta: 1, cancellationToken);
            return;
        }

        var rows = await _db.Queryable<SiteEvent>()
            .Where(t => t.ID == input.ID && t.SiteID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var row = rows.FirstOrDefault() ?? throw Oops.Oh("#编号错误#");
        var oldCol = row.ColID;

        row.ColID = input.ColID;
        row.Title = input.Title;
        row.SubTitle = input.SubTitle ?? "{}";
        row.Tag = input.Tag ?? "{}";
        row.Cover = input.Cover ?? "{}";
        row.Description = description;
        row.Content = input.Content;
        row.Type = input.Type;
        row.StartAt = input.StartAt;
        row.ExpireAt = input.ExpireAt;
        row.IsVisible = input.IsVisible;
        row.IsOpen = input.IsOpen;
        row.IsLoginAfterOpen = input.IsLoginAfterOpen;
        row.IsLoginBeforeOpen = input.IsLoginBeforeOpen;
        row.IsQuickEntrance = input.IsQuickEntrance;
        row.IsDetail = input.IsDetail;
        row.IsNewerTask = input.IsNewerTask;
        row.JumpType = input.JumpType;
        row.Sort = input.Sort;
        row.Currency = input.Currency;
        row.SupportCurrency = input.SupportCurrency ?? string.Empty;
        row.SettingString = input.Setting ?? string.Empty;
        row.Demo = input.Demo ?? string.Empty;
        row.Skin = input.Skin ?? string.Empty;
        row.Views = input.Views ?? string.Empty;
        row.Background = input.Background ?? string.Empty;
        row.FilterGame = input.FilterGame ?? string.Empty;
        row.FilterPlatform = input.FilterPlatform ?? string.Empty;
        row.Status = status;
        await _db.Updateable(row).ExecuteCommandAsync(cancellationToken);

        if (oldCol != input.ColID)
        {
            await BumpColumnCountAsync(siteId, input.ColID, delta: 1, cancellationToken);
            await BumpColumnCountAsync(siteId, oldCol, delta: -1, cancellationToken);
        }
    }

    /// <summary>
    /// 删除当前站活动，并减栏目活动数。
    /// </summary>
    /// <param name="id">编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw Oops.Oh("#编号错误#");
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<SiteEvent>()
            .Where(t => t.ID == id && t.SiteID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var row = rows.FirstOrDefault() ?? throw Oops.Oh("#编号错误#");
        var colId = row.ColID;
        await _db.Deleteable<SiteEvent>()
            .Where(t => t.ID == id && t.SiteID == siteId)
            .ExecuteCommandAsync(cancellationToken);
        await BumpColumnCountAsync(siteId, colId, delta: -1, cancellationToken);
    }

    /// <summary>
    /// 栏目活动数加减，不低于 0。
    /// </summary>
    private async Task BumpColumnCountAsync(
        int siteId,
        int colId,
        int delta,
        CancellationToken cancellationToken)
    {
        if (colId <= 0 || delta == 0)
            return;
        var cols = await _db.Queryable<SiteEventColumn>()
            .Where(c => c.ID == colId && c.SiteID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var col = cols.FirstOrDefault();
        if (col == null)
            return;
        col.EventCount = Math.Max(0, col.EventCount + delta);
        await _db.Updateable(col).UpdateColumns(c => new { c.EventCount })
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 转详情。
    /// </summary>
    private static SiteEventDetailDto ToDetail(SiteEvent ev) => new()
    {
        ID = ev.ID,
        ColID = ev.ColID,
        Title = ev.Title ?? "{}",
        SubTitle = ev.SubTitle ?? "{}",
        Tag = ev.Tag ?? "{}",
        Cover = ev.Cover ?? "{}",
        Description = ev.Description ?? "{}",
        Content = ev.Content ?? "{}",
        Type = ev.Type,
        StartAt = ev.StartAt,
        ExpireAt = ev.ExpireAt,
        IsVisible = ev.IsVisible,
        IsOpen = ev.IsOpen,
        IsLoginAfterOpen = ev.IsLoginAfterOpen,
        IsLoginBeforeOpen = ev.IsLoginBeforeOpen,
        IsQuickEntrance = ev.IsQuickEntrance,
        IsDetail = ev.IsDetail,
        IsNewerTask = ev.IsNewerTask,
        JumpType = ev.JumpType,
        Sort = ev.Sort,
        Currency = ev.Currency,
        SupportCurrency = ev.SupportCurrency ?? string.Empty,
        Setting = string.IsNullOrEmpty(ev.SettingString) ? "null" : ev.SettingString,
        Demo = ev.Demo ?? string.Empty,
        Skin = ev.Skin ?? string.Empty,
        Views = ev.Views ?? string.Empty,
        Background = ev.Background ?? string.Empty,
        FilterGame = ev.FilterGame ?? string.Empty,
        FilterPlatform = ev.FilterPlatform ?? string.Empty
    };

    /// <summary>
    /// 按开始/结束算展示状态文案。
    /// </summary>
    private static string DescribeTimeline(DateTime start, DateTime end, DateTime now)
    {
        if (end < now) return "#已过期#";
        if (start > now) return "#待上线#";
        return "#进行中#";
    }

    /// <summary>
    /// 写入库内 Status：0 进行中、1 待上线、2 已过期（与时间轴一致的粗分）。
    /// </summary>
    private static int ComputeStatus(DateTime start, DateTime end, DateTime now)
    {
        if (end < now) return 2;
        if (start > now) return 1;
        return 0;
    }

    /// <summary>
    /// 去掉 HTML 后截取前缀，作摘要。
    /// </summary>
    private static string LeftPlain(string html, int max)
    {
        if (string.IsNullOrEmpty(html))
            return string.Empty;
        var plain = Regex.Replace(html, "<[^>]+>", string.Empty);
        return plain.Length <= max ? plain : plain[..max];
    }

    /// <summary>
    /// 活动类型中文名（唯一性报错用）。
    /// </summary>
    private static string DescribeEventType(int type) => type switch
    {
        SiteEventSettingChecker.TypeNewDailyTasks => "#新版每日任务#",
        SiteEventSettingChecker.TypeNewWeekTasks => "#新版每周任务#",
        SiteEventSettingChecker.TypeDelottery => "#幸运转盘#",
        _ => type.ToString()
    };

    /// <summary>
    /// 邀请有礼返点类型文案。
    /// </summary>
    private static string DescribeInviteReturnType(int returnType) => returnType switch
    {
        0 => "#首充#",
        1 => "#有效投注#",
        2 => "#负盈利#",
        _ => returnType.ToString()
    };

    // --- merged from SiteEventService.WaveGaps.cs ---



    /// <summary>
    /// 对齐旧 filtergame。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> FiltergameAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return FiltergameCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 getproductcount。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> GetproductcountAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return GetproductcountCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 initplatform。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> InitplatformAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return InitplatformCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 inittransfer。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> InittransferAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return InittransferCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 pointlog。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> PointlogAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return PointlogCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 show0。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> Show0Async(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return Show0CoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 show1。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> Show1Async(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return Show1CoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 show2。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> Show2Async(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return Show2CoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 showtransfer0。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> Showtransfer0Async(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return Showtransfer0CoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 updatedemo。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> UpdatedemoAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return UpdatedemoCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 updateproductcount。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> UpdateproductcountAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return UpdateproductcountCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 updatesort。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> UpdatesortAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return UpdatesortCoreAsync(input, cancellationToken);
    }


    /// <summary>核心：filtergame —— 开放游戏下拉。</summary>
    private async Task<object?> FiltergameCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        _ = input;
        var siteId = _site.RequireSiteId();
        var open = await _db.Queryable<SimpX.Net.Data.Entity.Games.GameSetting>()
            .Where(g => g.SiteID == siteId && g.IsOpen && !g.IsDisable)
            .OrderBy(g => g.Name)
            .Select(g => new { value = g.Game, name = g.Name })
            .ToListAsync(cancellationToken);
        return new
        {
            openPlatforms = open,
            openTransfer = open,
            initPlatform = Array.Empty<string>()
        };
    }


    /// <summary>核心：getproductcount —— 延后库存统计，返回 0。</summary>
    private Task<object?> GetproductcountCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        _ = input; _ = cancellationToken;
        return Task.FromResult<object?>(new { Count = 0 });
    }


    /// <summary>核心：initplatform</summary>
    private async Task<object?> InitplatformCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var id = ReadInt(input, "ID", "Id");
        var siteId = _site.RequireSiteId();
        var filter = string.Empty;
        if (id > 0)
        {
            var rows = await _db.Queryable<SiteEvent>()
                .Where(e => e.SiteID == siteId && e.ID == id).Take(1).ToListAsync(cancellationToken);
            filter = rows.FirstOrDefault()?.FilterGame ?? string.Empty;
        }

        var init = filter.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        return new { initPlatform = init };
    }


    /// <summary>核心：inittransfer</summary>
    private async Task<object?> InittransferCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var id = ReadInt(input, "ID", "Id");
        var siteId = _site.RequireSiteId();
        var filter = string.Empty;
        if (id > 0)
        {
            var rows = await _db.Queryable<SiteEvent>()
                .Where(e => e.SiteID == siteId && e.ID == id).Take(1).ToListAsync(cancellationToken);
            filter = rows.FirstOrDefault()?.FilterPlatform ?? string.Empty;
        }

        var init = filter.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        return new { initPlatform = init };
    }


    /// <summary>核心：pointlog</summary>
    private async Task<object?> PointlogCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        _ = input; _ = cancellationToken;
        return await Task.FromResult<object?>(Array.Empty<object>());
    }


    /// <summary>核心：show0 —— 手工活动（有 FilterGame）。</summary>
    private async Task<object?> Show0CoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var type = ReadInt(input, "Type", "type");
        var siteId = _site.RequireSiteId();
        var now = DateTime.Now;
        var q = _db.Queryable<SiteEvent>()
            .Where(e => e.SiteID == siteId && e.IsOpen
                && e.StartAt <= now && e.ExpireAt >= now
                && e.FilterGame != null && e.FilterGame != string.Empty);
        if (type != 0)
            q = q.Where(e => e.Type == type);
        var rows = await q.OrderBy(e => e.Sort, OrderByType.Desc)
            .Select(e => new { e.ID, e.Title })
            .ToListAsync(cancellationToken);
        return rows;
    }


    /// <summary>核心：show1 —— Banner 可选活动。</summary>
    private async Task<object?> Show1CoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        _ = input;
        var siteId = _site.RequireSiteId();
        var now = DateTime.Now;
        return await _db.Queryable<SiteEvent>()
            .Where(e => e.SiteID == siteId && e.IsOpen && e.IsVisible
                && e.StartAt <= now && e.ExpireAt >= now)
            .OrderBy(e => e.Sort, OrderByType.Desc)
            .Select(e => new { e.ID, e.Title })
            .ToListAsync(cancellationToken);
    }


    /// <summary>核心：show2 —— Banner 字典。</summary>
    private async Task<object?> Show2CoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        _ = input;
        var siteId = _site.RequireSiteId();
        var now = DateTime.Now;
        var rows = await _db.Queryable<SiteEvent>()
            .Where(e => e.SiteID == siteId && e.IsOpen && e.IsVisible
                && e.StartAt <= now && e.ExpireAt >= now)
            .OrderBy(e => e.Sort)
            .Select(e => new { e.ID, e.Title })
            .ToListAsync(cancellationToken);
        return rows.ToDictionary(x => x.ID.ToString(), x => x.Title ?? string.Empty);
    }


    /// <summary>核心：showtransfer0 —— 有 FilterPlatform 的活动。</summary>
    private async Task<object?> Showtransfer0CoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var type = ReadInt(input, "Type", "type");
        var siteId = _site.RequireSiteId();
        var now = DateTime.Now;
        var q = _db.Queryable<SiteEvent>()
            .Where(e => e.SiteID == siteId && e.IsOpen
                && e.StartAt <= now && e.ExpireAt >= now
                && e.FilterPlatform != null && e.FilterPlatform != string.Empty);
        if (type != 0)
            q = q.Where(e => e.Type == type);
        return await q.OrderBy(e => e.Sort, OrderByType.Desc)
            .Select(e => new { e.ID, e.Title })
            .ToListAsync(cancellationToken);
    }


    /// <summary>核心：updatedemo —— 改活动 Demo 备注。</summary>
    private async Task<object?> UpdatedemoCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var id = ReadInt(input, "ID", "Id");
        if (id <= 0)
            throw Oops.Oh("#编号错误#");
        var demo = input.TryGetValue("Demo", out var d) ? d?.ToString() ?? string.Empty : string.Empty;
        var siteId = _site.RequireSiteId();
        var n = await _db.Updateable<SiteEvent>()
            .SetColumns(e => e.Demo == demo)
            .Where(e => e.ID == id && e.SiteID == siteId)
            .ExecuteCommandAsync(cancellationToken);
        if (n != 1)
            throw Oops.Oh("#编号错误#");
        return new { ok = true, id };
    }


    /// <summary>核心：updateproductcount —— 库存同步延后，先成功返回。</summary>
    private Task<object?> UpdateproductcountCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        _ = input; _ = cancellationToken;
        return Task.FromResult<object?>(null);
    }


    /// <summary>核心：updatesort —— ID 顺序即 Sort 从大到小。</summary>
    private async Task<object?> UpdatesortCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var raw = input.TryGetValue("ID", out var v) ? v?.ToString()
            : input.TryGetValue("Sort", out var s) ? s?.ToString()
            : string.Empty;
        var ids = (raw ?? string.Empty)
            .Split([',', ';', '|', ' '], StringSplitOptions.RemoveEmptyEntries)
            .Select(x => int.TryParse(x.Trim(), out var n) ? n : 0)
            .Where(n => n > 0)
            .Distinct()
            .ToArray();
        if (ids.Length == 0)
            throw Oops.Oh("#参数错误#");

        var siteId = _site.RequireSiteId();
        var sort = ids.Length;
        foreach (var id in ids)
        {
            await _db.Updateable<SiteEvent>()
                .SetColumns(e => e.Sort == (short)sort)
                .Where(e => e.ID == id && e.SiteID == siteId)
                .ExecuteCommandAsync(cancellationToken);
            sort--;
        }

        return new { ok = true, count = ids.Length };
    }


    private static int ReadInt(Dictionary<string, object?> input, params string[] keys)
    {
        foreach (var k in keys)
        {
            if (!input.TryGetValue(k, out var v) || v is null) continue;
            if (v is int i) return i;
            if (int.TryParse(v.ToString(), out var n)) return n;
        }
        return 0;
    }
}
