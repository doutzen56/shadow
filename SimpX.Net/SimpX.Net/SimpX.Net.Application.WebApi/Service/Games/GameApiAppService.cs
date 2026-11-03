using Furion.UnifyResult;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.WebApi;

/// <summary>
/// 会员兼容：游戏 API。进游戏对齐旧版 POST /request/game/api/login；推送对齐 /request/gamepush/{Type}；
/// 转账对齐 /request/game/api/transfer、transfertocenter。
/// </summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "game-api", Order = 30)]
public class GameApiAppService : IDynamicApiController, ITransient
{
    private readonly GameLoginService _login;
    private readonly GamePushService _push;
    private readonly GameTransferService _transfer;
    private readonly MemberAuthService _memberAuth;
    private readonly ICurrentSiteAccessor _site;

    /// <summary>
    /// 注入进游戏、推送、转账与会员会话。
    /// </summary>
    /// <param name="login">进游戏</param>
    /// <param name="push">厂商推送</param>
    /// <param name="transfer">转账编排</param>
    /// <param name="memberAuth">会员 Cookie 会话</param>
    /// <param name="site">当前站</param>
    public GameApiAppService(
        GameLoginService login,
        GamePushService push,
        GameTransferService transfer,
        MemberAuthService memberAuth,
        ICurrentSiteAccessor site)
    {
        _login = login;
        _push = push;
        _transfer = transfer;
        _memberAuth = memberAuth;
        _site = site;
    }

    /// <summary>
    /// 进游戏拿登录 URL。路径对齐旧版 POST /request/game/api/login。
    /// </summary>
    /// <returns>旧版 success/msg/info（info 为 url/method/data）</returns>
    [OldApiGuest]
    [AllowAnonymous]
    [HttpPost("/request/game/api/login")]
    [NonUnify]
    [DisplayName("会员进游戏")]
    public Task<OldApiResult> Login()
    {
        var http = App.HttpContext;
        var map = ReadFormAndQuery(http);
        return _login.LoginAsync(map, http?.RequestAborted ?? default);
    }

    /// <summary>
    /// 中心钱包与游戏互转。对齐旧 POST /request/game/api/transfer（Form：Money/Currency/IN|OUT）。
    /// 需会员登录（类上 OldApiAuth；本方法不挂 OldApiGuest）。
    /// </summary>
    /// <returns>旧版 success/msg/info</returns>
    [AllowAnonymous]
    [HttpPost("/request/game/api/transfer")]
    [NonUnify]
    [DisplayName("会员游戏转账")]
    public async Task<OldApiResult> Transfer()
    {
        var http = App.HttpContext;
        var ct = http?.RequestAborted ?? default;
        var siteId = _site.RequireSiteId();
        var userId = await _memberAuth.TryResolveUserIdFromCookieAsync(siteId, ct);
        if (userId == null)
            return OldApiResult.NeedLogin();

        var map = ReadFormAndQuery(http);
        if (!TryReadCurrency(map, out var currency))
            return OldApiResult.Fail("#货币不能为空#");

        if (!map.TryGetValue("Money", out var moneyText) ||
            !decimal.TryParse(moneyText, out var money) ||
            money <= 0)
            return OldApiResult.Fail("#金额错误#");

        var hasIn = map.TryGetValue("IN", out var inText) && !string.IsNullOrWhiteSpace(inText);
        var hasOut = map.TryGetValue("OUT", out var outText) && !string.IsNullOrWhiteSpace(outText);
        if (hasIn == hasOut)
            return OldApiResult.Fail("#未正确选择转入转出账号#");

        if (hasIn)
        {
            if (!TryReadGameType(inText, out var gameType))
                return OldApiResult.Fail("#参数错误#");
            return await _transfer.TransferToGameAsync(siteId, userId.Value, gameType, money, currency, ct);
        }

        if (!TryReadGameType(outText, out var outGameType))
            return OldApiResult.Fail("#参数错误#");
        return await _transfer.TransferToCenterAsync(siteId, userId.Value, outGameType, money, currency, ct);
    }

    /// <summary>
    /// 一键从游戏转回中心钱包。对齐旧 POST /request/game/api/transfertocenter（Form：GameType/Currency/Money）。
    /// Stub 阶段由调用方传 Money；旧版会先查厂商余额再整额转出。
    /// </summary>
    /// <returns>旧版 success/msg/info</returns>
    [AllowAnonymous]
    [HttpPost("/request/game/api/transfertocenter")]
    [NonUnify]
    [DisplayName("会员转回中心钱包")]
    public async Task<OldApiResult> TransferToCenter()
    {
        var http = App.HttpContext;
        var ct = http?.RequestAborted ?? default;
        var siteId = _site.RequireSiteId();
        var userId = await _memberAuth.TryResolveUserIdFromCookieAsync(siteId, ct);
        if (userId == null)
            return OldApiResult.NeedLogin();

        var map = ReadFormAndQuery(http);
        if (!TryReadCurrency(map, out var currency))
            return OldApiResult.Fail("#货币不能为空#");

        if (!map.TryGetValue("GameType", out var typeText) || !TryReadGameType(typeText, out var gameType))
            return OldApiResult.Fail("#参数错误#");

        if (!map.TryGetValue("Money", out var moneyText) ||
            !decimal.TryParse(moneyText, out var money) ||
            money <= 0)
            return OldApiResult.Fail("#金额错误#");

        return await _transfer.TransferToCenterAsync(siteId, userId.Value, gameType, money, currency, ct);
    }

    [AllowAnonymous]
    [HttpPost("/request/game/api/getbalance"), HttpGet("/request/game/api/getbalance")]
    [HttpPost("/request/game/info/getgamebalance"), HttpGet("/request/game/info/getgamebalance")]
    [HttpPost("/request/game/info/account"), HttpGet("/request/game/info/account")]
    [HttpPost("/request/game/info/gameuser"), HttpGet("/request/game/info/gameuser")]
    [NonUnify, DisplayName("game-balance-stub")]
    public async Task<OldApiResult> GetbalanceStub()
    {
        var siteId = _site.RequireSiteId();
        var userId = await _memberAuth.TryResolveUserIdFromCookieAsync(siteId, App.HttpContext?.RequestAborted ?? default);
        if (userId == null)
            return OldApiResult.NeedLogin();
        return OldApiResult.Ok(string.Empty, new { Balance = 0m });
    }

    [AllowAnonymous]
    [HttpPost("/request/game/api/login2")]
    [HttpPost("/request/game/api/register")]
    [HttpPost("/request/game/api/transferalltogame")]
    [NonUnify, DisplayName("game-api-extra-stub")]
    public Task<OldApiResult> ApiExtraStub() =>
        Task.FromResult(OldApiResult.Fail("#功能尚未接通#"));

    /// <summary>
    /// 厂商推送入口。无适配时 505；mock 通道可验签并可入账。
    /// </summary>
    /// <param name="gameType">游戏类型名或编号</param>
    /// <returns>厂商约定正文</returns>
    [OldApiGuest]
    [AllowAnonymous]
    [HttpPost("/request/gamepush/{gameType}")]
    [HttpGet("/request/gamepush/{gameType}")]
    [NonUnify]
    [DisplayName("游戏厂商推送")]
    public async Task<IActionResult> GamePush(string gameType)
    {
        var http = App.HttpContext;
        string rawBody = string.Empty;
        if (http?.Request?.Body != null &&
            http.Request.ContentLength is > 0 &&
            !(http.Request.HasFormContentType))
        {
            using var reader = new StreamReader(http.Request.Body);
            rawBody = await reader.ReadToEndAsync();
        }

        var form = ReadFormAndQuery(http);
        var outcome = await _push.HandleAsync(
            gameType,
            rawBody,
            form,
            http?.RequestAborted ?? default);
        return new ContentResult
        {
            StatusCode = outcome.StatusCode,
            Content = outcome.Body,
            ContentType = "text/plain; charset=utf-8"
        };
    }

    /// <summary>
    /// 合并 Form 与 Query（Form 优先）。
    /// </summary>
    private static Dictionary<string, string> ReadFormAndQuery(HttpContext http)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (http == null)
            return map;

        foreach (var kv in http.Request.Query)
            map[kv.Key] = kv.Value.ToString();

        if (http.Request.HasFormContentType)
        {
            foreach (var key in http.Request.Form.Keys)
                map[key] = http.Request.Form[key].ToString();
        }

        return map;
    }

    /// <summary>Currency 支持整型或 TWD/CNY 字母码。</summary>
    private static bool TryReadCurrency(Dictionary<string, string> map, out int currency)
    {
        currency = 0;
        if (!map.TryGetValue("Currency", out var text) || string.IsNullOrWhiteSpace(text))
            return false;
        text = text.Trim();
        if (int.TryParse(text, out currency))
            return true;
        for (var i = 0; i <= 16; i++)
        {
            if (string.Equals(OldApiMoneyDisplay.FormatCurrency(i), text, StringComparison.OrdinalIgnoreCase))
            {
                currency = i;
                return true;
            }
        }

        return false;
    }

    /// <summary>游戏类型支持整型或 KS/PG 等枚举名。</summary>
    private static bool TryReadGameType(string text, out int gameType)
    {
        gameType = 0;
        if (string.IsNullOrWhiteSpace(text))
            return false;
        text = text.Trim();
        if (int.TryParse(text, out gameType))
            return true;
        return GameTypeMeta.TryParse(text, out gameType);
    }
}
