using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using SimpX.Net.Data.Entity.Sites;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 租户后台站点设置：对齐旧版 /admin/site/info/get 与 save（Site + Setting + PCSetting）。
/// </summary>
public sealed partial class SiteInfoService : ITransient
{
    /// <summary>旧版 Site.SiteStauts.Stop。</summary>
    public const int SiteStatusStop = 1;

    private static readonly Regex PasswordRegex = new(@"^[a-zA-Z0-9]{6,20}$", RegexOptions.Compiled);

    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;

    /// <summary>
    /// 注入业务库和当前站点。站点资料在 BetWin30 业务库，不能走主库 Sqlite。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站点</param>
    public SiteInfoService(IBizDbAccessor biz, ICurrentSiteAccessor site)
    {
        _db = biz.Db;
        _site = site;
    }

    /// <summary>
    /// 获取当前站信息。对齐旧版 site/info/get。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>与旧版 info 同名字段</returns>
    public async Task<SiteInfoGetOutput> GetAsync(CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<Site>()
            .Where(s => s.ID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var site = rows.FirstOrDefault() ?? throw Oops.Oh("#站点不存在#");

        if (site.Status == SiteStatusStop)
            throw Oops.Oh("#站点已停止#");

        var settingMap = SiteSettingQuery.Parse(site.SettingString);
        var pcMap = SiteSettingQuery.Parse(site.PCSettingString);

        return new SiteInfoGetOutput
        {
            ID = site.ID,
            Name = FormatNameForGet(site.Name),
            Currency = site.Currency,
            Logo = FormatImage(SiteSettingQuery.Get(settingMap, "Logo")),
            UserLogo = FormatImage(SiteSettingQuery.Get(settingMap, "UserLogo")),
            Icon = FormatImage(SiteSettingQuery.Get(settingMap, "Icon")),
            Key = Md5HexLower(site.APIKey.ToString("N")),
            SiteIsSportHome = SiteSettingQuery.Get(pcMap, "IsSportHome"),
            HomeTemplateName = site.HomeTemplateName ?? string.Empty,
            PCHomeTemplateName = SiteSettingQuery.Get(pcMap, "HomeTemplateName"),
            Setting = SiteSettingQuery.ToSettingObject(settingMap)
        };
    }

    /// <summary>
    /// 保存当前站信息。对齐旧版 site/info/save + SiteAgent.SaveSiteInfo 关键校验。
    /// </summary>
    /// <param name="input">入参</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SaveAsync(SiteInfoSaveInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var siteId = _site.RequireSiteId();

        if (string.IsNullOrWhiteSpace(input.Name))
            throw Oops.Oh("#请输入站点名字#");
        if (input.Setting == null)
            throw Oops.Oh("#参数错误#");

        var rows = await _db.Queryable<Site>()
            .Where(s => s.ID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var site = rows.FirstOrDefault() ?? throw Oops.Oh("#站点不存在#");

        var oldSetting = SiteSettingQuery.Parse(site.SettingString);
        var oldGoogleSecret = SiteSettingQuery.Get(oldSetting, "GoogleSecret");
        var oldIsAutoReturn = SiteSettingQuery.GetBool(oldSetting, "IsAutoReturn");
        var oldAutoReturnLocked = SiteSettingQuery.GetBool(oldSetting, "AutoReturnV2Locked");
        var oldEffectiveAt = SiteSettingQuery.Get(oldSetting, "AutoReturnV2EffectiveAt");

        var newSetting = new Dictionary<string, string>(oldSetting, StringComparer.OrdinalIgnoreCase);
        SiteSettingQuery.MergeJson(newSetting, input.Setting);
        // 旧版：保存时强制保留库里的 GoogleSecret，防止表单漏传清掉
        newSetting["GoogleSecret"] = oldGoogleSecret;

        ApplyAutoReturnRules(newSetting, oldIsAutoReturn, oldAutoReturnLocked, oldEffectiveAt);

        var password = SiteSettingQuery.Get(newSetting, "Password");
        if (!PasswordRegex.IsMatch(password ?? string.Empty))
            throw Oops.Oh("#密码格式错误#");

        var pcMap = SiteSettingQuery.Parse(site.PCSettingString);
        var pcHome = string.IsNullOrWhiteSpace(input.PCHomeTemplateName)
            ? "casino"
            : input.PCHomeTemplateName.Trim();
        if (pcHome.Length == 0)
            pcHome = "casino";
        pcMap["HomeTemplateName"] = pcHome;

        site.Name = input.Name.Trim();
        site.Currency = input.Currency;
        site.SettingString = SiteSettingQuery.ToQueryString(newSetting);
        site.PCSettingString = SiteSettingQuery.ToQueryString(pcMap);

        var affected = await _db.Updateable(site)
            .UpdateColumns(s => new { s.Name, s.Currency, s.SettingString, s.PCSettingString })
            .Where(s => s.ID == siteId)
            .ExecuteCommandAsync(cancellationToken);
        if (affected != 1)
            throw Oops.Oh("#保存失败#");
    }

    /// <summary>
    /// 单独保存 GoogleSecret（旧 savegooglesecret）；普通 Save 会强制保留原值。
    /// </summary>
    /// <param name="secret">密钥</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SaveGoogleSecretAsync(string secret, CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<Site>()
            .Where(s => s.ID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var site = rows.FirstOrDefault() ?? throw Oops.Oh("#站点不存在#");
        var map = SiteSettingQuery.Parse(site.SettingString);
        map["GoogleSecret"] = secret ?? string.Empty;
        site.SettingString = SiteSettingQuery.ToQueryString(map);
        var affected = await _db.Updateable(site)
            .UpdateColumns(s => new { s.SettingString })
            .Where(s => s.ID == siteId)
            .ExecuteCommandAsync(cancellationToken);
        if (affected != 1)
            throw Oops.Oh("#保存失败#");
    }

    /// <summary>
    /// 自助反水 V1→V2 预约与锁定，对齐 SiteAgent.SaveSiteInfo。
    /// </summary>
    private static void ApplyAutoReturnRules(
        IDictionary<string, string> setting,
        bool oldIsAutoReturn,
        bool oldLocked,
        string oldEffectiveAt)
    {
        var newIsAutoReturn = SiteSettingQuery.GetBool(setting, "IsAutoReturn");

        if ((oldIsAutoReturn || oldLocked) && !newIsAutoReturn)
            throw Oops.Oh("#自助反水已启用，不允许切回手动反水#");

        if (!oldIsAutoReturn && newIsAutoReturn)
        {
            var now = DateTime.Now;
            DateTime? oldAt = null;
            if (!string.IsNullOrWhiteSpace(oldEffectiveAt) && DateTime.TryParse(oldEffectiveAt, out var parsed))
                oldAt = parsed;
            var effectiveAt = oldAt.HasValue && oldAt.Value > now
                ? oldAt.Value
                : now.Date.AddDays(2);

            setting["IsAutoReturn"] = bool.FalseString;
            setting["AutoReturnV2Locked"] = bool.FalseString;
            setting["AutoReturnV2EffectiveAt"] = effectiveAt.ToString("yyyy-MM-dd HH:mm:ss");
            return;
        }

        if (oldIsAutoReturn || oldLocked)
        {
            setting["IsAutoReturn"] = bool.TrueString;
            setting["AutoReturnV2Locked"] = bool.TrueString;
            setting["AutoReturnV2EffectiveAt"] = oldEffectiveAt ?? string.Empty;
            return;
        }

        setting["AutoReturnV2EffectiveAt"] = oldEffectiveAt ?? string.Empty;
        setting["AutoReturnV2Locked"] = oldLocked ? bool.TrueString : bool.FalseString;
    }

    /// <summary>
    /// 旧版 get 的 Name：不含 { 时返回 {}，否则原样（TranslateModel JSON）。
    /// </summary>
    private static string FormatNameForGet(string name)
    {
        if (string.IsNullOrEmpty(name) || !name.Contains('{'))
            return "{}";
        return name;
    }

    /// <summary>
    /// 旧版 Utils.GetImage：空→/images/space.png；已是 http 原样；否则相对路径原样返回（图床前缀有配置再补）。
    /// </summary>
    private static string FormatImage(string cover)
    {
        if (string.IsNullOrEmpty(cover))
            return "/images/space.png";
        if (cover.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            return cover;
        return cover.StartsWith('/') ? cover : "/" + cover;
    }

    /// <summary>
    /// 查看 API 密钥明文（Guid N 格式）。对齐 api。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>Key</returns>
    public async Task<object> GetApiKeyAsync(CancellationToken cancellationToken = default)
    {
        var site = await LoadSiteRowAsync(cancellationToken);
        return new { Key = site.APIKey.ToString("N") };
    }

    /// <summary>
    /// 重新生成 API 密钥。对齐 updateapikey。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>新 Key</returns>
    public async Task<object> UpdateApiKeyAsync(CancellationToken cancellationToken = default)
    {
        var site = await LoadSiteRowAsync(cancellationToken);
        var key = Guid.NewGuid();
        site.APIKey = key;
        var n = await _db.Updateable(site)
            .UpdateColumns(s => s.APIKey)
            .Where(s => s.ID == site.ID)
            .ExecuteCommandAsync(cancellationToken);
        if (n != 1)
            throw Oops.Oh("#更新失败#");
        return new { Key = key.ToString("N") };
    }

    /// <summary>
    /// 刷新缓存占位。对齐 cache；本刀无集中缓存宿主时只校验 Key。
    /// </summary>
    /// <param name="key">site / user</param>
    /// <param name="cancellationToken">取消令牌</param>
    public Task ClearCacheAsync(string key, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        if (string.IsNullOrWhiteSpace(key))
            throw Oops.Oh("#参数错误#");
        return Task.CompletedTask;
    }

    /// <summary>
    /// 在线人数占位。对齐 online；暂无 Online 表时返回 0。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>在线结构</returns>
    public Task<object> OnlineAsync(CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        _ = cancellationToken;
        return Task.FromResult<object>(new { SiteID = siteId, Online = 0, Guest = 0 });
    }

    /// <summary>
    /// 后台域名。对齐 admindomain。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>SiteID / AdminDomain</returns>
    public async Task<object> AdminDomainAsync(CancellationToken cancellationToken = default)
    {
        var site = await LoadSiteRowAsync(cancellationToken);
        return new { SiteID = site.ID, AdminDomain = (site.AdminDomain ?? string.Empty).Trim() };
    }

    /// <summary>
    /// 保存后台域名。对齐 saveadmindomain。
    /// </summary>
    /// <param name="adminDomain">域名后缀</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SaveAdminDomainAsync(string adminDomain, CancellationToken cancellationToken = default)
    {
        var site = await LoadSiteRowAsync(cancellationToken);
        site.AdminDomain = (adminDomain ?? string.Empty).Trim();
        var n = await _db.Updateable(site)
            .UpdateColumns(s => s.AdminDomain)
            .Where(s => s.ID == site.ID)
            .ExecuteCommandAsync(cancellationToken);
        if (n != 1)
            throw Oops.Oh("#保存失败#");
    }

    /// <summary>
    /// 当前站列表（单站过滤）。对齐 list。
    /// </summary>
    /// <param name="name">站名包含</param>
    /// <param name="type">状态；空=全部</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行列表</returns>
    public async Task<List<object>> ListAsync(string name, string type, CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var query = _db.Queryable<Site>().Where(s => s.ID == siteId);
        if (!string.IsNullOrWhiteSpace(name))
            query = query.Where(s => s.Name.Contains(name.Trim()));
        if (!string.IsNullOrWhiteSpace(type) && int.TryParse(type.Trim(), out var st))
            query = query.Where(s => s.Status == st);
        var rows = await query.OrderBy(s => s.CreateAt, OrderByType.Desc).ToListAsync(cancellationToken);
        return rows.Select(t => (object)new
        {
            t.ID,
            t.Name,
            t.CreateAt,
            t.Status,
            t.GameCredit,
            t.Currency
        }).ToList();
    }

    /// <summary>读当前站行。</summary>
    private async Task<Site> LoadSiteRowAsync(CancellationToken cancellationToken)
    {
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<Site>()
            .Where(s => s.ID == siteId).Take(1).ToListAsync(cancellationToken);
        return rows.FirstOrDefault() ?? throw Oops.Oh("#站点不存在#");
    }

    /// <summary>
    /// APIKey 展示用 MD5（小写十六进制），对齐旧版 MD5.toMD5。
    /// </summary>
    private static string Md5HexLower(string input)
    {
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(input ?? string.Empty));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    // --- merged from SiteInfoService.WaveGaps.cs ---


    /// <summary>
    /// 对齐旧 appinfo。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> AppinfoAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return AppinfoCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 moneytype。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> MoneytypeAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return MoneytypeCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 upload。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> UploadAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return UploadCoreAsync(input, cancellationToken);
    }


    /// <summary>核心：appinfo —— 读 Site.APPSettingString。</summary>
    private async Task<object?> AppinfoCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        _ = input;
        var site = await LoadSiteRowAsync(cancellationToken);
        var map = SiteSettingQuery.Parse(site.APPSettingString);
        var customDownload = SiteSettingQuery.Get(map, "CustomDownload");
        var download = string.IsNullOrWhiteSpace(customDownload)
            ? SiteSettingQuery.Get(map, "Download")
            : customDownload;
        var customQr = SiteSettingQuery.Get(map, "CustomQRCodeShow");
        var qr = SiteSettingQuery.Get(map, "QRCode");
        return new
        {
            Template = SiteSettingQuery.Get(map, "Template"),
            QRCode = qr,
            Download = download,
            QRCodeShow = string.IsNullOrWhiteSpace(customQr) ? qr : customQr,
            CustomQRCodeShow = customQr,
            CustomDownload = customDownload
        };
    }


    /// <summary>核心：moneytype —— 帐变分类树（与 SiteConfig / 会员端对齐）。</summary>
    private Task<object?> MoneytypeCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        _ = input; _ = cancellationToken;
        return Task.FromResult<object?>(MoneyTypeCatalog.BuildTree());
    }


    /// <summary>核心：upload —— JWT 包装走绝对路径 Form 上传；此处提示用旧路径。</summary>
    private Task<object?> UploadCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        _ = input; _ = cancellationToken;
        throw Oops.Oh("#请使用 /admin/system/upload 或 /admin/site/app/upload#");
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
