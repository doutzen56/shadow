using System.Text.Json;
using SimpX.Net.Data.Entity.Games;
using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 厂商进游戏适配。各 GameType 各自实现；未登记时登录链路在校验通过后返回未接入。
/// </summary>
public interface IGameVendorLogin
{
    /// <summary>是否支持该游戏类型整型。</summary>
    /// <param name="gameType">旧版 GameType 值</param>
    /// <returns>支持返回 true</returns>
    bool Supports(int gameType);

    /// <summary>
    /// 向厂商要登录地址（已有第三方帐号）。
    /// </summary>
    /// <param name="context">上下文</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>成功时带 url/method/data</returns>
    Task<GameVendorLoginOutcome> LoginAsync(GameVendorLoginContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// 厂商登录入参。
/// </summary>
public sealed class GameVendorLoginContext
{
    /// <summary>站点编号。</summary>
    public int SiteId { get; set; }

    /// <summary>游戏类型。</summary>
    public int GameType { get; set; }

    /// <summary>会员编号；游客为 0。</summary>
    public int UserId { get; set; }

    /// <summary>第三方帐号；游客可空。</summary>
    public string PlayerName { get; set; }

    /// <summary>站点 game_Setting.Setting JSON。</summary>
    public string SettingJson { get; set; }

    /// <summary>表单附加参数（含 Currency/Language/CODE/IP 等）。</summary>
    public Dictionary<string, string> Args { get; set; }
}

/// <summary>
/// 厂商登录结果。
/// </summary>
public sealed class GameVendorLoginOutcome
{
    /// <summary>是否成功。</summary>
    public bool Ok { get; set; }

    /// <summary>失败提示（可含 #标签#）。</summary>
    public string Message { get; set; }

    /// <summary>打开地址。</summary>
    public string Url { get; set; }

    /// <summary>GET 或 POST。</summary>
    public string Method { get; set; }

    /// <summary>POST 表单字段。</summary>
    public Dictionary<string, string> Data { get; set; }
}

/// <summary>
/// 会员进游戏（拿登录 URL）。校验对齐旧版 /request/game/api/login；不转账。
/// 厂商调用走 <see cref="IGameVendorLogin"/>；未登记厂商时校验通过后返回未接入。
/// </summary>
public sealed class GameLoginService : ITransient
{
    /// <summary>会员锁「禁止进游戏」位（旧 LockStatus.Game）。</summary>
    public const int UserLockGameFlag = 32;

    /// <summary>游戏帐号锁定位（旧 GameUserLock.Lock）。</summary>
    public const int GameUserLockFlag = 1;

    /// <summary>旧 GameType.AVIA。</summary>
    public const int GameTypeAvia = 9;

    /// <summary>系统层停用。</summary>
    private const int SysStatusStop = 0;

    /// <summary>系统层维护。</summary>
    private const int SysStatusMaintain = 2;

    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;
    private readonly IMemberCookieUserIdResolver _memberAuth;
    private readonly IEnumerable<IGameVendorLogin> _vendors;

    /// <summary>
    /// 注入业务库、当前站、会员会话、厂商适配列表。游戏配置和会员在 BetWin30 业务库，不能走主库 Sqlite。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站</param>
    /// <param name="memberAuth">会员 Cookie 会话</param>
    /// <param name="vendors">已登记的厂商登录适配</param>
    public GameLoginService(
        IBizDbAccessor biz,
        ICurrentSiteAccessor site,
        IMemberCookieUserIdResolver memberAuth,
        IEnumerable<IGameVendorLogin> vendors)
    {
        _db = biz.Db;
        _site = site;
        _memberAuth = memberAuth;
        _vendors = vendors ?? Array.Empty<IGameVendorLogin>();
    }

    /// <summary>
    /// 进游戏。Form 键对齐旧版 Type/Category/Currency/Language 及附加参数。
    /// </summary>
    /// <param name="args">表单字典（含 Type）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>旧版返回；成功时 info 为 url/method/data</returns>
    public async Task<OldApiResult> LoginAsync(
        IReadOnlyDictionary<string, string> args,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(args);
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in args)
        {
            if (string.IsNullOrEmpty(kv.Key))
                continue;
            var key = string.Equals(kv.Key, "code", StringComparison.OrdinalIgnoreCase) ? "CODE" : kv.Key;
            map[key] = kv.Value ?? string.Empty;
        }

        if (!map.TryGetValue("Type", out var typeText) || !int.TryParse(typeText, out var gameType))
            return OldApiResult.Fail("#参数错误#");

        if (!map.TryGetValue("Category", out var category) || string.IsNullOrWhiteSpace(category))
            return OldApiResult.Fail("Category#参数错误#");

        var siteId = _site.RequireSiteId();
        var settings = await _db.Queryable<GameSetting>()
            .Where(g => g.SiteID == siteId && g.Game == gameType)
            .Take(1)
            .ToListAsync(cancellationToken);
        var setting = settings.FirstOrDefault();
        if (setting == null || !setting.IsOpen)
            return OldApiResult.Fail("#游戏未开放#");

        var sysRows = await _db.Queryable<SystemGameSetting>()
            .Where(s => s.Type == gameType)
            .Take(1)
            .ToListAsync(cancellationToken);
        var sys = sysRows.FirstOrDefault();
        if (sys != null && (sys.Status == SysStatusStop || sys.Status == SysStatusMaintain))
            return OldApiResult.Fail("#维护中#");

        var userId = await _memberAuth.TryResolveUserIdFromCookieAsync(siteId, cancellationToken) ?? 0;

        // 泛亚游客：未登录时返回 GuestUrl（对齐旧版）
        if (userId == 0 && gameType == GameTypeAvia)
        {
            var guestUrl = TryReadGuestUrl(setting.SettingString);
            if (string.IsNullOrEmpty(guestUrl))
                return OldApiResult.Fail("#平台访问错误#");
            return OldApiResult.Ok("#登陆成功#", new GameLoginInfoDto
            {
                Url = guestUrl,
                Method = "GET",
                Data = null
            });
        }

        if (userId == 0)
            return OldApiResult.NeedLogin();

        if (!map.ContainsKey("Currency") || string.IsNullOrWhiteSpace(map["Currency"]))
            return OldApiResult.Fail("#货币不能为空#");
        if (!map.ContainsKey("Language") || string.IsNullOrWhiteSpace(map["Language"]))
            return OldApiResult.Fail("#语言不能为空#");

        if (!int.TryParse(map["Currency"], out var currency))
            return OldApiResult.Fail("#货币不能为空#");

        var users = await _db.Queryable<User>()
            .Where(u => u.ID == userId && u.SiteID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var user = users.FirstOrDefault();
        if (user == null)
            return OldApiResult.NeedLogin();

        if ((user.Lock & UserLockGameFlag) != 0)
            return OldApiResult.Fail("#该游戏暂无法进入#,#请与客服联系#");

        var gameUsers = await _db.Queryable<GameUser>()
            .Where(g => g.SiteID == siteId && g.UserID == userId && g.Game == gameType && g.Currency == currency)
            .Take(1)
            .ToListAsync(cancellationToken);
        var gameUser = gameUsers.FirstOrDefault();
        if (gameUser != null && (gameUser.Lock & GameUserLockFlag) != 0)
            return OldApiResult.Fail("#该游戏暂无法进入#,#请与客服联系#");

        var vendor = _vendors.FirstOrDefault(v => v.Supports(gameType));
        if (vendor == null)
            return OldApiResult.Fail("#游戏接口未接入#");

        var playerName = gameUser?.PlayerName;
        if (string.IsNullOrEmpty(playerName))
            return OldApiResult.Fail("#游戏接口未接入#");

        if (!map.ContainsKey("IP"))
            map["IP"] = string.Empty;

        var outcome = await vendor.LoginAsync(new GameVendorLoginContext
        {
            SiteId = siteId,
            GameType = gameType,
            UserId = userId,
            PlayerName = playerName,
            SettingJson = setting.SettingString ?? string.Empty,
            Args = map
        }, cancellationToken);

        if (!outcome.Ok)
            return OldApiResult.Fail(string.IsNullOrEmpty(outcome.Message) ? "#平台访问错误#" : outcome.Message);

        return OldApiResult.Ok("#成功#", new GameLoginInfoDto
        {
            Url = outcome.Url ?? string.Empty,
            Method = string.IsNullOrEmpty(outcome.Method) ? "GET" : outcome.Method,
            Data = outcome.Data
        });
    }

    /// <summary>
    /// 从厂商 Setting JSON 读游客地址（兼容 GuestUrl.value / GuestUrl 字符串）。
    /// </summary>
    private static string TryReadGuestUrl(string settingJson)
    {
        if (string.IsNullOrWhiteSpace(settingJson))
            return null;
        try
        {
            using var doc = JsonDocument.Parse(settingJson);
            var root = doc.RootElement;
            if (!root.TryGetProperty("GuestUrl", out var guest))
                return null;
            if (guest.ValueKind == JsonValueKind.String)
                return guest.GetString();
            if (guest.ValueKind == JsonValueKind.Object && guest.TryGetProperty("value", out var val))
                return val.GetString();
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }
}
