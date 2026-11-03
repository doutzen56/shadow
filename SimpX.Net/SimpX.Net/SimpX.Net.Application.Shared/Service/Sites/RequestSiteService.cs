using SimpX.Net.Data;
using SimpX.Net.Data.Entity.Sites;
using SqlSugar;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 会员侧站点公开信息：按域名确定当前站点，并返回 Setting 里对外公开的字段。
/// </summary>
public sealed class RequestSiteService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;

    /// <summary>
    /// 注入业务库和当前站点。站点资料在 BetWin30 业务库，不能走主库 Sqlite。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站点</param>
    public RequestSiteService(IBizDbAccessor biz, ICurrentSiteAccessor site)
    {
        _db = biz.Db;
        _site = site;
    }

    /// <summary>
    /// 当前站点的公开资料。对应旧版 /request/site/info/get 精简 + Setting 子集。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>旧版返回格式</returns>
    public async Task<OldApiResult> GetAsync(CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<Site>()
            .Where(s => s.ID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var site = rows.FirstOrDefault();
        if (site == null)
            return OldApiResult.Fail("#站点不存在#");

        var setting = ParsePublicSetting(site.SettingString);
        var info = new RequestSiteInfoDto
        {
            ID = site.ID,
            SiteID = site.ID,
            Name = SystemSiteService.PickChineseName(site.Name),
            Status = site.Status,
            Currency = site.Currency,
            Wallet = site.Wallet,
            IsCurrencySelect = site.IsCurrencySelect ? 1 : 0,
            HomeTemplateName = site.HomeTemplateName ?? string.Empty,
            Logo = setting.Logo ?? string.Empty,
            Customer = setting.Customer ?? string.Empty,
            Service = setting.Service ?? string.Empty,
            InviteCode = setting.InviteCode ?? string.Empty,
            Setting = setting
        };
        return OldApiResult.Ok(string.Empty, info);
    }

    /// <summary>会员侧 Logo（对齐 /request/site/info/logo）。</summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>{ Logo }</returns>
    public async Task<object> LogoAsync(CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<Site>()
            .Where(s => s.ID == siteId).Take(1).ToListAsync(cancellationToken);
        var site = rows.FirstOrDefault() ?? throw Oops.Oh("#站点不存在#");
        var setting = ParsePublicSetting(site.SettingString);
        return new { Logo = setting.Logo ?? string.Empty };
    }

    /// <summary>会员侧区号配置原文（对齐 /request/site/info/getareacode）。</summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>AreaCode JSON/CSV 原文</returns>
    public async Task<object> GetAreaCodeAsync(CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<SiteConfig>()
            .Where(c => c.SiteID == siteId).Take(1).ToListAsync(cancellationToken);
        var value = rows.FirstOrDefault()?.AreaCode;
        if (string.IsNullOrWhiteSpace(value))
            value = "[]";
        return value;
    }

    /// <summary>
    /// 从站点 Setting 取出前台常用公开字段。库里多半是旧版 querystring；也兼容 JSON。
    /// </summary>
    /// <param name="raw">库中 Setting 字符串</param>
    /// <returns>公开 Setting 子集</returns>
    public static SitePublicSettingDto ParsePublicSetting(string raw)
    {
        var dto = new SitePublicSettingDto
        {
            Logo = string.Empty,
            UserLogo = string.Empty,
            Customer = string.Empty,
            Service = string.Empty,
            InviteCode = string.Empty,
            QQ = string.Empty,
            Telegram = string.Empty,
            Line = string.Empty,
            Facebook = string.Empty,
            Whatapp = string.Empty,
            Letstalk = string.Empty,
            WX = string.Empty,
            Trial = string.Empty,
            IsSportHome = string.Empty
        };
        if (string.IsNullOrWhiteSpace(raw))
            return dto;

        var map = ParseSettingMap(raw);
        dto.Logo = GetMap(map, "Logo");
        dto.UserLogo = GetMap(map, "UserLogo");
        dto.Customer = GetMap(map, "Customer");
        dto.Service = GetMap(map, "Service");
        dto.InviteCode = GetMap(map, "InviteCode");
        dto.QQ = GetMap(map, "QQ");
        dto.Telegram = GetMap(map, "Telegram");
        dto.Line = GetMap(map, "Line");
        dto.Facebook = GetMap(map, "Facebook");
        dto.Whatapp = GetMap(map, "Whatapp");
        dto.Letstalk = GetMap(map, "Letstalk");
        dto.WX = GetMap(map, "WX");
        dto.Trial = GetMap(map, "Trial");
        dto.IsSportHome = GetMap(map, "IsSportHome");
        dto.CustomerIframe = GetMapBool(map, "CustomerIframe");
        dto.MobileGuest = GetMapBool(map, "MobileGuest");
        dto.IsMaintain = GetMapBool(map, "IsMaintain");
        dto.MinOnlineMember = GetMapInt(map, "MinOnlineMember");
        dto.MaxOnlineMember = GetMapInt(map, "MaxOnlineMember");
        dto.JackPot = GetMapDouble(map, "JackPot");
        dto.MinJackPot = GetMapInt(map, "MinJackPot");
        dto.MaxJackPot = GetMapInt(map, "MaxJackPot");
        dto.MinTransfer = GetMapInt(map, "MinTransfer");
        dto.MaxTransfer = GetMapInt(map, "MaxTransfer");
        return dto;
    }

    /// <summary>
    /// 把 Setting 原文解析成键值表；优先按 JSON；不是 JSON 就按 querystring 解析（跟旧版 SettingBase 一样）。
    /// </summary>
    /// <param name="raw">Setting 原文</param>
    /// <returns>键值（已 URL 解码）</returns>
    public static Dictionary<string, string> ParseSettingMap(string raw)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(raw))
            return map;

        var trimmed = raw.Trim();
        if (trimmed.StartsWith('{'))
        {
            try
            {
                using var doc = JsonDocument.Parse(trimmed);
                foreach (var prop in doc.RootElement.EnumerateObject())
                    map[prop.Name] = prop.Value.ValueKind == JsonValueKind.String
                        ? (prop.Value.GetString() ?? string.Empty)
                        : prop.Value.ToString();
                return map;
            }
            catch (JsonException)
            {
                // 继续走 querystring
            }
        }

        foreach (var pair in trimmed.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var idx = pair.IndexOf('=');
            if (idx <= 0)
                continue;
            var key = Uri.UnescapeDataString(pair[..idx].Replace('+', ' '));
            var value = Uri.UnescapeDataString(pair[(idx + 1)..].Replace('+', ' '));
            if (!string.IsNullOrEmpty(key))
                map[key] = value;
        }

        return map;
    }

    /// <summary>读字符串；缺失返回空串。</summary>
    private static string GetMap(Dictionary<string, string> map, string name) =>
        map.TryGetValue(name, out var v) ? (v ?? string.Empty) : string.Empty;

    /// <summary>读布尔。</summary>
    private static bool GetMapBool(Dictionary<string, string> map, string name)
    {
        if (!map.TryGetValue(name, out var v) || string.IsNullOrWhiteSpace(v))
            return false;
        if (bool.TryParse(v, out var b))
            return b;
        return int.TryParse(v, out var n) && n != 0;
    }

    /// <summary>读整数。</summary>
    private static int GetMapInt(Dictionary<string, string> map, string name)
    {
        if (!map.TryGetValue(name, out var v) || string.IsNullOrWhiteSpace(v))
            return 0;
        return int.TryParse(v, out var n) ? n : 0;
    }

    /// <summary>读浮点。</summary>
    private static double GetMapDouble(Dictionary<string, string> map, string name)
    {
        if (!map.TryGetValue(name, out var v) || string.IsNullOrWhiteSpace(v))
            return 0;
        return double.TryParse(v, out var n) ? n : 0;
    }
}

/// <summary>
/// 会员侧站点公开信息 DTO。
/// </summary>
public sealed class RequestSiteInfoDto
{
    /// <summary>站点编号（跟旧版字段 ID 一样）。</summary>
    [JsonPropertyName("ID")]
    public int ID { get; set; }

    /// <summary>站点编号别名。</summary>
    [JsonPropertyName("SiteID")]
    public int SiteID { get; set; }

    /// <summary>站名。</summary>
    [JsonPropertyName("Name")]
    public string Name { get; set; }

    /// <summary>状态。</summary>
    [JsonPropertyName("Status")]
    public int Status { get; set; }

    /// <summary>主币种。</summary>
    [JsonPropertyName("Currency")]
    public int Currency { get; set; }

    /// <summary>钱包模式。</summary>
    [JsonPropertyName("Wallet")]
    public int Wallet { get; set; }

    /// <summary>是否可选币种。</summary>
    [JsonPropertyName("IsCurrencySelect")]
    public int IsCurrencySelect { get; set; }

    /// <summary>首页模板名。</summary>
    [JsonPropertyName("HomeTemplateName")]
    public string HomeTemplateName { get; set; }

    /// <summary>Logo（来自 Setting）。</summary>
    [JsonPropertyName("Logo")]
    public string Logo { get; set; }

    /// <summary>客服链接。</summary>
    [JsonPropertyName("Customer")]
    public string Customer { get; set; }

    /// <summary>客服图标。</summary>
    [JsonPropertyName("Service")]
    public string Service { get; set; }

    /// <summary>默认邀请码（来自 Setting）。</summary>
    [JsonPropertyName("InviteCode")]
    public string InviteCode { get; set; }

    /// <summary>Setting 公开子集。</summary>
    [JsonPropertyName("Setting")]
    public SitePublicSettingDto Setting { get; set; }
}

/// <summary>
/// 站点 Setting 对会员前台公开的字段子集（不含密钥类）。
/// </summary>
public sealed class SitePublicSettingDto
{
    /// <summary>站 Logo。</summary>
    [JsonPropertyName("Logo")]
    public string Logo { get; set; }

    /// <summary>默认头像。</summary>
    [JsonPropertyName("UserLogo")]
    public string UserLogo { get; set; }

    /// <summary>客服链接。</summary>
    [JsonPropertyName("Customer")]
    public string Customer { get; set; }

    /// <summary>客服图标。</summary>
    [JsonPropertyName("Service")]
    public string Service { get; set; }

    /// <summary>默认邀请码。</summary>
    [JsonPropertyName("InviteCode")]
    public string InviteCode { get; set; }

    /// <summary>QQ。</summary>
    [JsonPropertyName("QQ")]
    public string QQ { get; set; }

    /// <summary>Telegram。</summary>
    [JsonPropertyName("Telegram")]
    public string Telegram { get; set; }

    /// <summary>Line。</summary>
    [JsonPropertyName("Line")]
    public string Line { get; set; }

    /// <summary>Facebook。</summary>
    [JsonPropertyName("Facebook")]
    public string Facebook { get; set; }

    /// <summary>WhatsApp（旧版字段名 Whatapp）。</summary>
    [JsonPropertyName("Whatapp")]
    public string Whatapp { get; set; }

    /// <summary>Letstalk 链接。</summary>
    [JsonPropertyName("Letstalk")]
    public string Letstalk { get; set; }

    /// <summary>微信客服图。</summary>
    [JsonPropertyName("WX")]
    public string WX { get; set; }

    /// <summary>试玩链接。</summary>
    [JsonPropertyName("Trial")]
    public string Trial { get; set; }

    /// <summary>是否体育首页。</summary>
    [JsonPropertyName("IsSportHome")]
    public string IsSportHome { get; set; }

    /// <summary>客服是否内嵌。</summary>
    [JsonPropertyName("CustomerIframe")]
    public bool CustomerIframe { get; set; }

    /// <summary>是否允许游客访问手机页。</summary>
    [JsonPropertyName("MobileGuest")]
    public bool MobileGuest { get; set; }

    /// <summary>是否维护中。</summary>
    [JsonPropertyName("IsMaintain")]
    public bool IsMaintain { get; set; }

    /// <summary>最小在线人数展示。</summary>
    [JsonPropertyName("MinOnlineMember")]
    public int MinOnlineMember { get; set; }

    /// <summary>最大在线人数展示。</summary>
    [JsonPropertyName("MaxOnlineMember")]
    public int MaxOnlineMember { get; set; }

    /// <summary>彩池初始值。</summary>
    [JsonPropertyName("JackPot")]
    public double JackPot { get; set; }

    /// <summary>最小彩池增量。</summary>
    [JsonPropertyName("MinJackPot")]
    public int MinJackPot { get; set; }

    /// <summary>最大彩池增量。</summary>
    [JsonPropertyName("MaxJackPot")]
    public int MaxJackPot { get; set; }

    /// <summary>最低转账金额。</summary>
    [JsonPropertyName("MinTransfer")]
    public int MinTransfer { get; set; }

    /// <summary>单笔转账最大金额。</summary>
    [JsonPropertyName("MaxTransfer")]
    public int MaxTransfer { get; set; }
}
