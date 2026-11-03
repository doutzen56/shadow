using System.Globalization;
using SimpX.Net.Data.Entity.Games;
using SimpX.Net.Data.Entity.Sites;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 租户后台返水设定（配置读写，不落账）。对齐旧 /admin/user/member/rebate。
/// </summary>
public sealed partial class RebateSettingService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;
    private readonly UserManager _userManager;

    /// <summary>
    /// 注入依赖。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站</param>
    /// <param name="userManager">后台用户</param>
    public RebateSettingService(IBizDbAccessor biz, ICurrentSiteAccessor site, UserManager userManager)
    {
        _db = biz.Db;
        _site = site;
        _userManager = userManager;
    }

    /// <summary>
    /// 当前站返水设定列表。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行列表</returns>
    public async Task<List<RebateSettingListItemDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<RebateSetting>()
            .Where(t => t.SiteID == siteId)
            .OrderBy(t => t.ID, OrderByType.Desc)
            .ToListAsync(cancellationToken);

        var groupMap = (await _db.Queryable<SimpX.Net.Data.Entity.Users.UserGroup>()
                .Where(g => g.SiteID == siteId)
                .Select(g => new { g.ID, g.Name })
                .ToListAsync(cancellationToken))
            .ToDictionary(g => g.ID, g => g.Name ?? string.Empty);

        return rows.Select(t => new RebateSettingListItemDto
        {
            ID = t.ID,
            Name = t.Name ?? string.Empty,
            IsAuto = t.IsAuto,
            Enabled = t.Enabled,
            BetMoney = t.BetMoney,
            MinMoney = t.MinMoney,
            MaxMoney = t.MaxMoney,
            Groups = JoinNamedCsv(t.Groups, groupMap),
            Levels = t.Levels ?? string.Empty,
            Type = t.Type ?? string.Empty,
            CreatorName = t.CreatorName ?? string.Empty
        }).ToList();
    }

    /// <summary>CSV id → 名称；未知 id 保留原值。</summary>
    private static string JoinNamedCsv(string? raw, Dictionary<int, string> map)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
        var names = new List<string>();
        foreach (var part in raw.Split(new[] { ',', '|', ';' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var key = part.Trim();
            if (int.TryParse(key, out var id) && map.TryGetValue(id, out var name) && !string.IsNullOrWhiteSpace(name))
                names.Add(name);
            else if (key.Length > 0)
                names.Add(key);
        }
        return string.Join(",", names);
    }

    /// <summary>
    /// 详情。
    /// </summary>
    /// <param name="id">编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>详情</returns>
    public async Task<RebateSettingDetailDto> DetailAsync(int id, CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        RebateSettingDetailDto dto;
        if (id <= 0)
        {
            dto = new RebateSettingDetailDto { SiteID = siteId, Enabled = true };
        }
        else
        {
            var rows = await _db.Queryable<RebateSetting>()
                .Where(t => t.SiteID == siteId && t.ID == id).Take(1).ToListAsync(cancellationToken);
            var t = rows.FirstOrDefault() ?? throw Oops.Oh("#记录不存在#");
            dto = MapDetail(t);
        }

        dto.GameGroups = await BuildGameGroupsAsync(siteId, cancellationToken);
        dto.Games = ParseGamesMap(dto.SettingString);
        return dto;
    }

    /// <summary>
    /// 保存（配置；不触发发放）。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>编号</returns>
    public async Task<int> SaveAsync(RebateSettingSaveInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (string.IsNullOrWhiteSpace(input.Name))
            throw Oops.Oh("#名称不能为空#");
        var siteId = _site.RequireSiteId();
        var creator = _userManager.Account ?? _userManager.RealName ?? string.Empty;
        var creatorId = _userManager.UserId > 0 ? (int)_userManager.UserId : 0;

        if (input.ID > 0)
        {
            var rows = await _db.Queryable<RebateSetting>()
                .Where(t => t.SiteID == siteId && t.ID == input.ID).Take(1).ToListAsync(cancellationToken);
            var entity = rows.FirstOrDefault() ?? throw Oops.Oh("#记录不存在#");
            Apply(entity, input);
            await _db.Updateable(entity).ExecuteCommandAsync(cancellationToken);
            return entity.ID;
        }

        var neu = new RebateSetting
        {
            SiteID = siteId,
            CreateAt = DateTime.Now,
            CreatorID = creatorId,
            CreatorName = creator,
            Enabled = true
        };
        Apply(neu, input);
        return await _db.Insertable(neu).ExecuteReturnIdentityAsync(cancellationToken);
    }

    /// <summary>
    /// 改自动发放开关。
    /// </summary>
    /// <param name="id">编号</param>
    /// <param name="isAuto">是否自动</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task UpdateAutoAsync(int id, bool isAuto, CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var n = await _db.Updateable<RebateSetting>()
            .SetColumns(t => t.IsAuto == isAuto)
            .Where(t => t.SiteID == siteId && t.ID == id)
            .ExecuteCommandAsync(cancellationToken);
        if (n <= 0)
            throw Oops.Oh("#记录不存在#");
    }

    /// <summary>
    /// 切换启用/停用。对齐旧 rebate/updateenabled（取反 Enabled）。
    /// </summary>
    /// <param name="id">编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>切换后的启用状态</returns>
    public async Task<bool> ToggleEnabledAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw Oops.Oh("#改信息不存在或已删除#");
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<RebateSetting>()
            .Where(t => t.SiteID == siteId && t.ID == id)
            .Take(1)
            .ToListAsync(cancellationToken);
        var model = rows.FirstOrDefault() ?? throw Oops.Oh("#改信息不存在或已删除#");
        var next = !model.Enabled;
        var n = await _db.Updateable<RebateSetting>()
            .SetColumns(t => t.Enabled == next)
            .Where(t => t.SiteID == siteId && t.ID == id)
            .ExecuteCommandAsync(cancellationToken);
        if (n <= 0)
            throw Oops.Oh("#改信息不存在或已删除#");
        return next;
    }

    /// <summary>
    /// 删除。
    /// </summary>
    /// <param name="id">编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        await _db.Deleteable<RebateSetting>()
            .Where(t => t.SiteID == siteId && t.ID == id)
            .ExecuteCommandAsync(cancellationToken);
    }

    private static void Apply(RebateSetting entity, RebateSettingSaveInput input)
    {
        entity.Name = input.Name.Trim();
        entity.Groups = input.Groups ?? string.Empty;
        entity.Levels = input.Levels ?? string.Empty;
        entity.BetMoney = input.BetMoney;
        entity.MinMoney = input.MinMoney;
        entity.MaxMoney = input.MaxMoney;
        entity.IsAuto = input.IsAuto;
        entity.Type = input.Type ?? string.Empty;
        entity.SettingString = input.SettingString ?? string.Empty;
        entity.Enabled = input.Enabled;
    }

    private static RebateSettingDetailDto MapDetail(RebateSetting t) => new()
    {
        ID = t.ID,
        SiteID = t.SiteID,
        Name = t.Name ?? string.Empty,
        Groups = t.Groups ?? string.Empty,
        Levels = t.Levels ?? string.Empty,
        BetMoney = t.BetMoney,
        MinMoney = t.MinMoney,
        MaxMoney = t.MaxMoney,
        IsAuto = t.IsAuto,
        Enabled = t.Enabled,
        Type = t.Type ?? string.Empty,
        SettingString = t.SettingString ?? string.Empty,
        CreatorName = t.CreatorName ?? string.Empty
    };

    /// <summary>
    /// 对齐旧 rebate/info GameGroups：按 GameCategory 分组的站点开启厂商。
    /// Key 形如「彩票_Lottery」，子项 key=厂商码（CB/PG…），比例键=Category.Code。
    /// </summary>
    private async Task<List<RebateGameGroupDto>> BuildGameGroupsAsync(int siteId, CancellationToken cancellationToken)
    {
        var rows = await _db.Queryable<GameSetting>()
            .Where(g => g.SiteID == siteId && g.IsOpen && !g.IsDisable)
            .OrderBy(g => g.Sort)
            .OrderBy(g => g.Game)
            .ToListAsync(cancellationToken);

        var groups = new List<RebateGameGroupDto>();
        foreach (var cat in OrderedCategories)
        {
            var label = CategoryDisplayName(cat);
            var games = new List<RebateGameItemDto>();
            foreach (var g in rows)
            {
                if (!GameTypeMeta.TryGet(g.Game, out var meta))
                    continue;
                if (!meta.Categories.Contains(cat, StringComparer.OrdinalIgnoreCase))
                    continue;
                var code = string.IsNullOrWhiteSpace(meta.Code) ? g.Game.ToString(CultureInfo.InvariantCulture) : meta.Code;
                var name = string.IsNullOrWhiteSpace(g.Name)
                    ? (string.IsNullOrWhiteSpace(meta.Description) ? code : meta.Description)
                    : g.Name;
                games.Add(new RebateGameItemDto { key = code, Value = name });
            }

            if (games.Count == 0)
                continue;

            groups.Add(new RebateGameGroupDto
            {
                Key = $"{label}_{cat}",
                Category = cat,
                Label = label,
                Games = games
            });
        }

        return groups;
    }

    private static readonly string[] OrderedCategories =
    [
        "Lottery", "ESport", "Slot", "Live", "Chess", "Sport", "Fish", "Animal", "MiniGame", "Battle"
    ];

    private static string CategoryDisplayName(string cat) => cat switch
    {
        "Lottery" => "彩票",
        "ESport" => "电竞",
        "Slot" => "电子",
        "Live" => "真人",
        "Chess" => "棋牌",
        "Sport" => "体育",
        "Fish" => "捕鱼",
        "Animal" => "动物",
        "MiniGame" => "小游戏",
        "Battle" => "对战",
        _ => cat
    };

    /// <summary>SettingString（query）→ Games 字典（Category.Code → 比例）。</summary>
    private static Dictionary<string, decimal> ParseGamesMap(string? raw)
    {
        var map = SiteSettingQuery.Parse(raw ?? string.Empty);
        var games = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in map)
        {
            if (decimal.TryParse(pair.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var rate))
                games[pair.Key] = rate;
        }
        return games;
    }
}

/// <summary>返水设定列表行。</summary>
public class RebateSettingListItemDto
{
    /// <summary>编号。</summary>
    public int ID { get; set; }

    /// <summary>名称。</summary>
    public string Name { get; set; }

    /// <summary>自动发放。</summary>
    public bool IsAuto { get; set; }

    /// <summary>启用。</summary>
    public bool Enabled { get; set; }

    /// <summary>有效投注。</summary>
    public decimal BetMoney { get; set; }

    /// <summary>最低发放。</summary>
    public decimal MinMoney { get; set; }

    /// <summary>最高发放。</summary>
    public decimal MaxMoney { get; set; }

    /// <summary>分组。</summary>
    public string Groups { get; set; }

    /// <summary>等级。</summary>
    public string Levels { get; set; }

    /// <summary>类型。</summary>
    public string Type { get; set; }

    /// <summary>创建人。</summary>
    public string CreatorName { get; set; }
}

/// <summary>返水设定详情。</summary>
public sealed class RebateSettingDetailDto : RebateSettingListItemDto
{
    /// <summary>站点。</summary>
    public int SiteID { get; set; }

    /// <summary>游戏返点原文（query：Category.Code=比例）。</summary>
    public string SettingString { get; set; }

    /// <summary>厂商分组元数据（对齐旧 GameGroups）。</summary>
    public List<RebateGameGroupDto> GameGroups { get; set; } = [];

    /// <summary>已保存比例（Category.Code → %）。</summary>
    public Dictionary<string, decimal> Games { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>返水厂商大类分组。</summary>
public sealed class RebateGameGroupDto
{
    /// <summary>旧站 Key：标签_Category。</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>GameCategory 枚举名。</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>中文标签。</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>该大类下开启的厂商。</summary>
    public List<RebateGameItemDto> Games { get; set; } = [];
}

/// <summary>返水厂商项（对齐旧 {key,Value}）。</summary>
public sealed class RebateGameItemDto
{
    /// <summary>厂商码（GameType Code）。</summary>
    public string key { get; set; } = string.Empty;

    /// <summary>显示名。</summary>
    public string Value { get; set; } = string.Empty;
}

/// <summary>保存返水设定。</summary>
public sealed class RebateSettingSaveInput
{
    /// <summary>0=新增。</summary>
    public int ID { get; set; }

    /// <summary>名称。</summary>
    public string Name { get; set; }

    /// <summary>分组 CSV。</summary>
    public string Groups { get; set; }

    /// <summary>等级 CSV。</summary>
    public string Levels { get; set; }

    /// <summary>有效投注。</summary>
    public decimal BetMoney { get; set; }

    /// <summary>最低。</summary>
    public decimal MinMoney { get; set; }

    /// <summary>最高。</summary>
    public decimal MaxMoney { get; set; }

    /// <summary>自动。</summary>
    public bool IsAuto { get; set; }

    /// <summary>启用。</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>类型。</summary>
    public string Type { get; set; }

    /// <summary>游戏返点 JSON。</summary>
    public string SettingString { get; set; }
}
