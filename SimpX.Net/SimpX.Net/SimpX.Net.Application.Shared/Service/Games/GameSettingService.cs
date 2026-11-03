using SimpX.Net.Data.Entity.Games;
using SimpX.Net.Data.Entity.Sites;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 租户后台厂商 API（游戏配置）。对齐旧版 /admin/game/setting/gamesetlist|info|save。
/// </summary>
public sealed partial class GameSettingService : ITransient
{
    /// <summary>系统层「停用」状态（旧 GameStatus.Stop）。</summary>
    private const int SysStatusStop = 0;

    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;

    /// <summary>
    /// 注入业务库与当前站。厂商配置表在 BetWin30 业务库。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站</param>
    public GameSettingService(IBizDbAccessor biz, ICurrentSiteAccessor site)
    {
        _db = biz.Db;
        _site = site;
    }

    /// <summary>
    /// 当前站已开启且未下架的游戏配置列表；内联系统层非停用配置。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>列表</returns>
    public async Task<List<GameSettingListItemDto>> ListAsync(
        GameSettingListInput input,
        CancellationToken cancellationToken = default)
    {
        input ??= new GameSettingListInput();
        var siteId = _site.RequireSiteId();

        var siteQuery = _db.Queryable<GameSetting>()
            .Where(g => g.SiteID == siteId && g.IsOpen && !g.IsDisable);

        if (!string.IsNullOrEmpty(input.IsOpen))
        {
            var open = input.IsOpen == "1";
            siteQuery = siteQuery.Where(g => g.IsOpen == open);
        }

        var siteRows = await siteQuery
            .OrderBy(g => g.Sort, OrderByType.Desc)
            .ToListAsync(cancellationToken);

        var sysQuery = _db.Queryable<SystemGameSetting>()
            .Where(s => s.Status != SysStatusStop);
        if (!string.IsNullOrWhiteSpace(input.Currency))
        {
            var cur = input.Currency.Trim();
            sysQuery = sysQuery.Where(s => s.Currencys != null && s.Currencys.Contains(cur));
        }

        if (!string.IsNullOrWhiteSpace(input.GameCategory))
        {
            var cat = input.GameCategory.Trim();
            sysQuery = sysQuery.Where(s => s.GameCategorys != null && s.GameCategorys.Contains(cat));
        }

        var sysRows = await sysQuery.ToListAsync(cancellationToken);
        var sysMap = sysRows.ToDictionary(s => s.Type, s => s);

        var list = new List<GameSettingListItemDto>();
        foreach (var a in siteRows)
        {
            if (!sysMap.TryGetValue(a.Game, out var b))
                continue;

            var display = !string.IsNullOrEmpty(a.Name)
                ? a.Name
                : (!string.IsNullOrEmpty(b.Name) ? b.Name : a.Game.ToString());

            list.Add(new GameSettingListItemDto
            {
                Type = a.Game,
                Game = display,
                Name = a.Name ?? string.Empty,
                IsOpen = a.IsOpen,
                Credit = a.Credit,
                IsMonthly = a.IsMonthly,
                IsFree = a.IsFree,
                Rate = a.Rate,
                Label = a.Label ?? string.Empty,
                Backdrop = a.Backdrop ?? string.Empty,
                Currencys = b.Currencys ?? string.Empty,
                Languages = b.Languages ?? string.Empty,
                GameCategorys = b.GameCategorys ?? string.Empty,
                Logo = a.Logo ?? string.Empty,
                IsBackdrop = a.IsBackdrop,
                DetailRate = a.DetailRate ?? string.Empty,
                MinTransfer = a.MinTransfer,
                MaxTransfer = a.MaxTransfer,
                Status = a.Status
            });
        }

        return list;
    }

    /// <summary>
    /// 当前站某一游戏类型详情。对齐旧版 info。
    /// </summary>
    /// <param name="type">GameType 整型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>详情</returns>
    public async Task<GameSettingDetailDto> DetailAsync(int type, CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<GameSetting>()
            .Where(g => g.SiteID == siteId && g.Game == type)
            .Take(1)
            .ToListAsync(cancellationToken);
        var setting = rows.FirstOrDefault()
                      ?? throw Oops.Oh("#游戏不存在#");

        var sys = await _db.Queryable<SystemGameSetting>()
            .Where(s => s.Type == type)
            .Take(1)
            .ToListAsync(cancellationToken);
        var sysName = sys.FirstOrDefault()?.Name;

        var display = !string.IsNullOrEmpty(setting.Name)
            ? setting.Name
            : (!string.IsNullOrEmpty(sysName) ? sysName : type.ToString());

        return new GameSettingDetailDto
        {
            Type = setting.Game,
            Game = display,
            Name = setting.Name ?? string.Empty,
            IsOpen = setting.IsOpen,
            IsSelf = setting.IsSelf,
            IsWater = setting.IsWater,
            MinTransfer = setting.MinTransfer,
            MaxTransfer = setting.MaxTransfer,
            Status = setting.Status,
            Setting = setting.IsSelf ? (setting.SettingString ?? string.Empty) : "null"
        };
    }

    /// <summary>
    /// 保存。对齐旧版 save：校验转账上下限相对站点 Setting，写 IsWater/Name/Min/Max/Setting。
    /// </summary>
    /// <param name="input">入参</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SaveAsync(GameSettingSaveInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.Game == 0)
            throw Oops.Oh("#游戏不存在#");

        var siteId = _site.RequireSiteId();
        var siteRows = await _db.Queryable<Site>()
            .Where(s => s.ID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var site = siteRows.FirstOrDefault() ?? throw Oops.Oh("#站点不存在#");
        var map = SiteSettingQuery.Parse(site.SettingString);
        var siteMin = 0;
        var siteMax = 0;
        if (map.TryGetValue("MinTransfer", out var minRaw))
            int.TryParse(minRaw, out siteMin);
        if (map.TryGetValue("MaxTransfer", out var maxRaw))
            int.TryParse(maxRaw, out siteMax);

        var minInvalid = input.MinTransfer > 0 && siteMin > 0 && input.MinTransfer < siteMin;
        var maxInvalid = input.MaxTransfer > 0 && siteMax > 0 && input.MaxTransfer > siteMax;
        if (minInvalid || maxInvalid)
            throw Oops.Oh("#转账金额超出站点限制#");

        var rows = await _db.Queryable<GameSetting>()
            .Where(g => g.SiteID == siteId && g.Game == input.Game)
            .Take(1)
            .ToListAsync(cancellationToken);
        var setting = rows.FirstOrDefault();
        if (setting == null)
        {
            setting = new GameSetting
            {
                SiteID = siteId,
                Game = input.Game,
                IsOpen = true
            };
            setting.Name = input.Name ?? string.Empty;
            setting.IsWater = input.IsWater;
            setting.MinTransfer = input.MinTransfer;
            setting.MaxTransfer = input.MaxTransfer;
            if (input.Setting != null)
                setting.SettingString = input.Setting;
            await _db.Insertable(setting).ExecuteCommandAsync(cancellationToken);
            return;
        }

        setting.Name = input.Name ?? setting.Name;
        setting.IsWater = input.IsWater;
        setting.MinTransfer = input.MinTransfer;
        setting.MaxTransfer = input.MaxTransfer;
        if (input.Setting != null)
            setting.SettingString = input.Setting;
        await _db.Updateable(setting).ExecuteCommandAsync(cancellationToken);
    }
}
