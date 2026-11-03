using System.Text.Json;
using SimpX.Net.Data.Entity.Sites;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 租户后台站点参数配置。对齐旧 /admin/site/config/*（site_Config 列 + Site.PCSetting）。
/// </summary>
public sealed partial class SiteConfigService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;

    /// <summary>
    /// 注入库和当前站。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站</param>
    public SiteConfigService(IBizDbAccessor biz, ICurrentSiteAccessor site)
    {
        _db = biz.Db;
        _site = site;
    }

    /// <summary>提现银行枚举目录（精简：返回当前库已用类型；完整枚举待后续补）。</summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>Coin / Bank</returns>
    public async Task<object> GetBankConfigAsync(CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var banks = await _db.Queryable<SimpX.Net.Data.Entity.Users.BankAccount>()
            .Where(t => t.SiteID == siteId)
            .Select(t => t.Type)
            .Distinct()
            .ToListAsync(cancellationToken);
        var bankMap = banks.OrderBy(t => t).ToDictionary(t => t.ToString(), t => t.ToString());
        return new { Coin = "{}", Bank = JsonSerializer.Serialize(bankMap) };
    }

    /// <summary>当前站已选银行/虚拟币。</summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>Bank / Coin JSON</returns>
    public async Task<object> GetBankListAsync(CancellationToken cancellationToken = default)
    {
        var cfg = await EnsureConfigAsync(cancellationToken);
        var bank = SplitCsv(cfg.Bank).ToDictionary(t => t, t => t);
        var coin = SplitCsv(cfg.VirtualCoin).ToDictionary(t => t, t => t);
        return new
        {
            Bank = JsonSerializer.Serialize(bank),
            Coin = JsonSerializer.Serialize(coin)
        };
    }

    /// <summary>勾选/取消提现银行。</summary>
    /// <param name="input">Bank + Value</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SaveSiteBankAsync(SiteConfigBankToggleInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (string.IsNullOrWhiteSpace(input.Bank))
            throw Oops.Oh("#参数错误#");
        var cfg = await EnsureConfigAsync(cancellationToken);
        cfg.Bank = ToggleCsv(cfg.Bank, input.Bank.Trim(), input.Value == 1);
        await UpdateConfigColumnsAsync(cfg, c => new { c.Bank }, cancellationToken);
    }

    /// <summary>勾选/取消虚拟币。</summary>
    /// <param name="input">Coin + Value</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SaveSiteCoinAsync(SiteConfigBankToggleInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (string.IsNullOrWhiteSpace(input.Coin))
            throw Oops.Oh("#参数错误#");
        var cfg = await EnsureConfigAsync(cancellationToken);
        cfg.VirtualCoin = ToggleCsv(cfg.VirtualCoin, input.Coin.Trim(), input.Value == 1);
        await UpdateConfigColumnsAsync(cfg, c => new { c.VirtualCoin }, cancellationToken);
    }

    /// <summary>资金类型目录（帐变筛选：完整分类树；对齐旧 moneytype）。</summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分类 →（枚举名 → 中文）</returns>
    public Task<object> MoneyTypeAsync(CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        return Task.FromResult<object>(MoneyTypeCatalog.BuildTree());
    }

    /// <summary>站点资金类型勾选状态。</summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>按动作分组的勾选表（精简为扁平 checked）</returns>
    public async Task<object> SiteMoneyTypeConfigAsync(CancellationToken cancellationToken = default)
    {
        var cfg = await EnsureConfigAsync(cancellationToken);
        var selected = new HashSet<string>(SplitCsv(cfg.MoneyType), StringComparer.OrdinalIgnoreCase);
        var items = selected.ToDictionary(
            t => t,
            t => new { name = t, checked_ = 1 },
            StringComparer.OrdinalIgnoreCase);
        return new { Selected = items, Raw = cfg.MoneyType ?? string.Empty };
    }

    /// <summary>保存资金类型勾选。</summary>
    /// <param name="input">勾选表</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SaveMoneyTypeConfigAsync(SiteConfigMoneyTypeSaveInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var cfg = await EnsureConfigAsync(cancellationToken);
        cfg.MoneyType = string.Join(",", SelectedKeys(input.Selected));
        await UpdateConfigColumnsAsync(cfg, c => new { c.MoneyType }, cancellationToken);
    }

    /// <summary>注册配置。</summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>Setting 对象</returns>
    public Task<Dictionary<string, object>> RegisterAsync(CancellationToken cancellationToken = default) =>
        GetColumnSettingAsync(c => c.RegisterString, cancellationToken);

    /// <summary>保存注册配置。</summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    public Task SaveRegisterAsync(SiteConfigSettingSaveInput input, CancellationToken cancellationToken = default) =>
        SaveColumnSettingAsync(input, (c, raw) => c.RegisterString = raw, c => new { c.RegisterString }, cancellationToken);

    /// <summary>注册站内信。</summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>Title/Content</returns>
    public async Task<object> RegisterMessageAsync(CancellationToken cancellationToken = default)
    {
        var cfg = await EnsureConfigAsync(cancellationToken);
        return new { RegisterTitle = cfg.RegisterTitle ?? string.Empty, RegisterContent = cfg.RegisterContent ?? string.Empty };
    }

    /// <summary>手机验证码提示。</summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>PhoneCodeMessage</returns>
    public async Task<object> RegisterMessageCodeAsync(CancellationToken cancellationToken = default)
    {
        var cfg = await EnsureConfigAsync(cancellationToken);
        return new { PhoneCodeMessage = cfg.PhoneCodeMessage ?? string.Empty };
    }

    /// <summary>保存注册站内信。</summary>
    /// <param name="input">Title/Content</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SaveRegisterMessageAsync(SiteConfigRegisterMessageSaveInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var cfg = await EnsureConfigAsync(cancellationToken);
        cfg.RegisterTitle = input.Title ?? string.Empty;
        cfg.RegisterContent = input.Content ?? string.Empty;
        await UpdateConfigColumnsAsync(cfg, c => new { c.RegisterTitle, c.RegisterContent }, cancellationToken);
    }

    /// <summary>保存手机验证码提示。</summary>
    /// <param name="input">Title→PhoneCodeMessage</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SaveRegisterMessageCodeAsync(SiteConfigRegisterMessageCodeSaveInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var cfg = await EnsureConfigAsync(cancellationToken);
        cfg.PhoneCodeMessage = input.Title ?? string.Empty;
        await UpdateConfigColumnsAsync(cfg, c => new { c.PhoneCodeMessage }, cancellationToken);
    }

    /// <summary>邮箱配置。</summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>Setting</returns>
    public Task<Dictionary<string, object>> MailAsync(CancellationToken cancellationToken = default) =>
        GetColumnSettingAsync(c => c.MailString, cancellationToken);

    /// <summary>保存邮箱。</summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    public Task SaveMailAsync(SiteConfigSettingSaveInput input, CancellationToken cancellationToken = default) =>
        SaveColumnSettingAsync(input, (c, raw) => c.MailString = raw, c => new { c.MailString }, cancellationToken);

    /// <summary>提示音配置。</summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>Setting</returns>
    public Task<Dictionary<string, object>> SoundAsync(CancellationToken cancellationToken = default) =>
        GetColumnSettingAsync(c => c.WarningSoundString, cancellationToken);

    /// <summary>保存单项提示音。</summary>
    /// <param name="input">type/url</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SaveSoundAsync(SiteConfigSoundSaveInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (string.IsNullOrWhiteSpace(input.Type))
            throw Oops.Oh("#参数错误#");
        var cfg = await EnsureConfigAsync(cancellationToken);
        var map = SiteSettingQuery.Parse(cfg.WarningSoundString);
        map[input.Type.Trim()] = input.Url ?? string.Empty;
        cfg.WarningSoundString = SiteSettingQuery.ToQueryString(map);
        await UpdateConfigColumnsAsync(cfg, c => new { c.WarningSoundString }, cancellationToken);
    }

    /// <summary>VIP 系统开始时间配置。</summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>Setting</returns>
    public async Task<Dictionary<string, object>> VipAsync(CancellationToken cancellationToken = default)
    {
        var map = await GetColumnSettingAsync(c => c.VIPSettingString, cancellationToken);
        if (!map.ContainsKey("VIPStartAt"))
            map["VIPStartAt"] = "1900-01-01";
        return map;
    }

    /// <summary>保存 VIP 开始时间。</summary>
    /// <param name="input">StartAt</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SaveVipAsync(SiteConfigVipSaveInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var cfg = await EnsureConfigAsync(cancellationToken);
        var map = SiteSettingQuery.Parse(cfg.VIPSettingString);
        var at = input.StartAt == default ? new DateTime(1900, 1, 1) : input.StartAt;
        map["VIPStartAt"] = at.ToString("yyyy-MM-dd HH:mm:ss");
        cfg.VIPSettingString = SiteSettingQuery.ToQueryString(map);
        await UpdateConfigColumnsAsync(cfg, c => new { c.VIPSettingString }, cancellationToken);
    }

    /// <summary>清理 VIP 配置（清空 VIPSetting）。</summary>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task ClearVipAsync(CancellationToken cancellationToken = default)
    {
        var cfg = await EnsureConfigAsync(cancellationToken);
        cfg.VIPSettingString = string.Empty;
        await UpdateConfigColumnsAsync(cfg, c => new { c.VIPSettingString }, cancellationToken);
    }

    /// <summary>代理加盟条件。</summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>Setting</returns>
    public Task<Dictionary<string, object>> AgentJoinSettingAsync(CancellationToken cancellationToken = default) =>
        GetColumnSettingAsync(c => c.AgentJoinString, cancellationToken);

    /// <summary>保存代理加盟条件。</summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    public Task SaveAgentJoinSettingAsync(SiteConfigSettingSaveInput input, CancellationToken cancellationToken = default) =>
        SaveColumnSettingAsync(input, (c, raw) => c.AgentJoinString = raw, c => new { c.AgentJoinString }, cancellationToken);

    /// <summary>短信网关。</summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>Setting</returns>
    public Task<Dictionary<string, object>> SmsSettingAsync(CancellationToken cancellationToken = default) =>
        GetColumnSettingAsync(c => c.SMSString, cancellationToken);

    /// <summary>短信网关子参数（Config 原文解析）。</summary>
    /// <param name="type">网关类型；空则返回空</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>子参数</returns>
    public async Task<Dictionary<string, object>> SmsSettingInfoAsync(string type, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(type))
            return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        var map = await GetColumnSettingAsync(c => c.SMSString, cancellationToken);
        var configRaw = map.TryGetValue("Config", out var cfgObj) ? cfgObj?.ToString() ?? string.Empty : string.Empty;
        return SiteSettingQuery.ToSettingObject(SiteSettingQuery.Parse(configRaw));
    }

    /// <summary>保存短信网关。</summary>
    /// <param name="input">主字段 + Config</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SaveSmsSettingAsync(SiteConfigSmsSaveInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var cfg = await EnsureConfigAsync(cancellationToken);
        var map = SiteSettingQuery.Parse(cfg.SMSString);
        SiteSettingQuery.MergeJson(map, input.Setting);
        if (input.Config is { Count: > 0 })
        {
            var nested = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            SiteSettingQuery.MergeJson(nested, input.Config);
            map["Config"] = SiteSettingQuery.ToQueryString(nested);
        }

        cfg.SMSString = SiteSettingQuery.ToQueryString(map);
        await UpdateConfigColumnsAsync(cfg, c => new { c.SMSString }, cancellationToken);
    }

    /// <summary>SEO（读 Site.PCSetting）。</summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>Title/Keyword/Description/Footer</returns>
    public async Task<object> SeoSettingAsync(CancellationToken cancellationToken = default)
    {
        var site = await LoadSiteAsync(cancellationToken);
        var pc = SiteSettingQuery.Parse(site.PCSettingString);
        static string OrEmptyJson(string v) => string.IsNullOrEmpty(v) ? "{}" : v;
        return new
        {
            Title = OrEmptyJson(SiteSettingQuery.Get(pc, "Title")),
            Keyword = OrEmptyJson(SiteSettingQuery.Get(pc, "Keyword")),
            Description = OrEmptyJson(SiteSettingQuery.Get(pc, "Description")),
            Footer = SiteSettingQuery.Get(pc, "Footer")
        };
    }

    /// <summary>保存 SEO。</summary>
    /// <param name="input">SEO 字段</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SaveSeoSettingAsync(SiteConfigSeoSaveInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var site = await LoadSiteAsync(cancellationToken);
        var pc = SiteSettingQuery.Parse(site.PCSettingString);
        if (input.Title != null) pc["Title"] = input.Title;
        if (input.Keyword != null) pc["Keyword"] = input.Keyword;
        if (input.Description != null) pc["Description"] = input.Description;
        if (input.Footer != null) pc["Footer"] = input.Footer;
        site.PCSettingString = SiteSettingQuery.ToQueryString(pc);
        var n = await _db.Updateable(site)
            .UpdateColumns(s => new { s.PCSettingString })
            .Where(s => s.ID == site.ID)
            .ExecuteCommandAsync(cancellationToken);
        if (n != 1)
            throw Oops.Oh("#保存失败#");
    }

    /// <summary>游戏参数。</summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>Setting</returns>
    public Task<Dictionary<string, object>> GameSettingAsync(CancellationToken cancellationToken = default) =>
        GetColumnSettingAsync(c => c.GameSetting, cancellationToken);

    /// <summary>保存游戏参数。</summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    public Task SaveGameSettingAsync(SiteConfigSettingSaveInput input, CancellationToken cancellationToken = default) =>
        SaveColumnSettingAsync(input, (c, raw) => c.GameSetting = raw, c => new { c.GameSetting }, cancellationToken);

    /// <summary>登录方式。</summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>Setting</returns>
    public Task<Dictionary<string, object>> LoginSettingAsync(CancellationToken cancellationToken = default) =>
        GetColumnSettingAsync(c => c.LoginString, cancellationToken);

    /// <summary>保存登录方式。</summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    public Task SaveLoginSettingAsync(SiteConfigSettingSaveInput input, CancellationToken cancellationToken = default) =>
        SaveColumnSettingAsync(input, (c, raw) => c.LoginString = raw, c => new { c.LoginString }, cancellationToken);

    /// <summary>
    /// 短信测试。当前环境未接短信网关，只校验手机号并返回未执行说明。
    /// </summary>
    /// <param name="input">测试参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果说明</returns>
    public Task<object> SendSmsTestAsync(SiteConfigSmsTestInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (string.IsNullOrWhiteSpace(input.Phone))
            throw Oops.Oh("#手机号错误#");
        _ = cancellationToken;
        return Task.FromResult<object>(new { Success = false, Message = "#短信网关未接入，测试未发送#" });
    }

    /// <summary>短信区号配置。</summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>AreaCodes / SiteAreas</returns>
    public async Task<object> SmsAreaConfigAsync(CancellationToken cancellationToken = default)
    {
        var cfg = await EnsureConfigAsync(cancellationToken);
        var areas = SplitCsv(cfg.AreaCode);
        var siteAreas = areas.Select(a => new { Name = a, Code = a }).ToList();
        return new { AreaCodes = "{}", SiteAreas = JsonSerializer.Serialize(siteAreas) };
    }

    /// <summary>勾选短信区号。</summary>
    /// <param name="input">区号</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SaveSmsAreaConfigAsync(SiteConfigSmsAreaToggleInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (string.IsNullOrWhiteSpace(input.AreaCode))
            throw Oops.Oh("#参数错误#");
        var cfg = await EnsureConfigAsync(cancellationToken);
        cfg.AreaCode = ToggleCsv(cfg.AreaCode, input.AreaCode.Trim(), input.Value == 1);
        await UpdateConfigColumnsAsync(cfg, c => new { c.AreaCode }, cancellationToken);
    }

    /// <summary>保存短信区号排序。</summary>
    /// <param name="input">SmsArea</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SaveSmsAreaConfigSortAsync(SiteConfigSmsAreaSortInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var cfg = await EnsureConfigAsync(cancellationToken);
        cfg.AreaCode = string.Join(",", SplitCsv(input.SmsArea));
        await UpdateConfigColumnsAsync(cfg, c => new { c.AreaCode }, cancellationToken);
    }

    /// <summary>背景类型配置。</summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>BackgroundType / festival / festivalStatus</returns>
    public async Task<object> SiteBackgroundConfigAsync(CancellationToken cancellationToken = default)
    {
        var cfg = await EnsureConfigAsync(cancellationToken);
        var selected = new HashSet<string>(SplitCsv(cfg.BackgroundType), StringComparer.OrdinalIgnoreCase);
        var bg = selected.ToDictionary(t => t, t => new { name = t, @checked = 1 });
        var festSelected = new HashSet<string>(SplitCsv(cfg.Festival), StringComparer.OrdinalIgnoreCase);
        var festival = festSelected.ToDictionary(t => t, t => new { name = t, @checked = 1 });
        return new
        {
            BackgroundType = JsonSerializer.Serialize(bg),
            festival = JsonSerializer.Serialize(festival),
            festivalStatus = cfg.FestivalStatus ?? string.Empty
        };
    }

    /// <summary>保存背景。</summary>
    /// <param name="input">勾选 + 节日</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SaveBackgroundConfigAsync(SiteConfigBackgroundSaveInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var keys = SelectedKeys(input.Selected);
        if (keys.Count == 0)
            throw Oops.Oh("#参数错误#");
        var cfg = await EnsureConfigAsync(cancellationToken);
        cfg.BackgroundType = string.Join(",", keys);
        if (!string.IsNullOrWhiteSpace(input.Festival))
            cfg.Festival = input.Festival.Trim();
        if (input.FestivalStatus != null)
            cfg.FestivalStatus = input.FestivalStatus;
        await UpdateConfigColumnsAsync(cfg, c => new { c.BackgroundType, c.Festival, c.FestivalStatus }, cancellationToken);
    }

    /// <summary>已启用背景列表。</summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>Type/Name</returns>
    public async Task<object> GetBackgroundConfigAsync(CancellationToken cancellationToken = default)
    {
        var cfg = await EnsureConfigAsync(cancellationToken);
        var list = SplitCsv(cfg.BackgroundType).Select(t => new { Type = t, Name = t }).ToList();
        return list;
    }

    /// <summary>模板类型（读 PCSetting.SysTC）。</summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>Type/Name</returns>
    public async Task<object> GetTemplateConfigAsync(CancellationToken cancellationToken = default)
    {
        var site = await LoadSiteAsync(cancellationToken);
        var pc = SiteSettingQuery.Parse(site.PCSettingString);
        var raw = SiteSettingQuery.Get(pc, "SysTC");
        var list = SplitCsv(raw).Select(t => new { Type = t, Name = t }).ToList();
        return list;
    }

    /// <summary>Telegram 推送配置。</summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>Telegram JSON / CurrencyList</returns>
    public async Task<object> TelegramAsync(CancellationToken cancellationToken = default)
    {
        var cfg = await EnsureConfigAsync(cancellationToken);
        var raw = string.IsNullOrWhiteSpace(cfg.TelegramString) ? "{}" : cfg.TelegramString;
        return new { Telegram = raw, CurrencyList = "{}" };
    }

    /// <summary>保存 Telegram。</summary>
    /// <param name="input">JSON 原文</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SaveTelegramAsync(SiteConfigTelegramSaveInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var cfg = await EnsureConfigAsync(cancellationToken);
        cfg.TelegramString = input.Telegram ?? "{}";
        await UpdateConfigColumnsAsync(cfg, c => new { c.TelegramString }, cancellationToken);
    }

    /// <summary>风控设置。</summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>Setting</returns>
    public Task<Dictionary<string, object>> RiskAsync(CancellationToken cancellationToken = default) =>
        GetColumnSettingAsync(c => c.RiskSettingString, cancellationToken);

    /// <summary>保存风控。</summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    public Task SaveRiskAsync(SiteConfigSettingSaveInput input, CancellationToken cancellationToken = default) =>
        SaveColumnSettingAsync(input, (c, raw) => c.RiskSettingString = raw, c => new { c.RiskSettingString }, cancellationToken);

    /// <summary>Jackpot 配置 + 人工干预列表。</summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>设置字段与 Data</returns>
    public async Task<object> JackpotConfigAsync(CancellationToken cancellationToken = default)
    {
        var cfg = await EnsureConfigAsync(cancellationToken);
        var map = SiteSettingQuery.ToSettingObject(SiteSettingQuery.Parse(cfg.JackpotSettingString));
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<SiteJackpot>()
            .Where(j => j.SiteID == siteId && !j.IsAuto)
            .OrderBy(j => j.ID, OrderByType.Desc)
            .Take(200)
            .ToListAsync(cancellationToken);
        return new
        {
            TimeSpan = map.TryGetValue("TimeSpan", out var ts) ? ts : 24,
            TopRank = map.TryGetValue("TopRank", out var tr) ? tr : 20,
            Live = IsSettingTruthy(map, "Live"),
            Slot = IsSettingTruthy(map, "Slot"),
            Sport = IsSettingTruthy(map, "Sport"),
            Lottery = IsSettingTruthy(map, "Lottery"),
            PlatformOptions = "{}",
            ContextOptionsMap = "{}",
            CurrencyOptions = "[]",
            Data = JsonSerializer.Serialize(rows)
        };
    }

    /// <summary>保存 Jackpot 设置；InterventionList 写入 site_Jackpot（精简：只更新已有 ID 或插入基础字段）。</summary>
    /// <param name="input">设置 + 列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SaveJackpotConfigAsync(SiteConfigJackpotSaveInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var cfg = await EnsureConfigAsync(cancellationToken);
        var map = SiteSettingQuery.Parse(cfg.JackpotSettingString);
        SiteSettingQuery.MergeJson(map, input.Setting);
        cfg.JackpotSettingString = SiteSettingQuery.ToQueryString(map);
        await UpdateConfigColumnsAsync(cfg, c => new { c.JackpotSettingString }, cancellationToken);

        if (string.IsNullOrWhiteSpace(input.InterventionList))
            return;

        using var doc = JsonDocument.Parse(input.InterventionList);
        if (doc.RootElement.ValueKind != JsonValueKind.Array)
            return;
        var siteId = _site.RequireSiteId();
        foreach (var el in doc.RootElement.EnumerateArray())
        {
            var id = el.TryGetProperty("ID", out var idEl) && idEl.TryGetInt32(out var i) ? i : 0;
            var row = id > 0
                ? (await _db.Queryable<SiteJackpot>().Where(j => j.SiteID == siteId && j.ID == id).Take(1).ToListAsync(cancellationToken)).FirstOrDefault()
                : null;
            row ??= new SiteJackpot
            {
                SiteID = siteId,
                CreateAt = DateTime.Now,
                IsAuto = false,
                Content = string.Empty,
                PlatformIcon = string.Empty,
                GameID = string.Empty,
                GameName = string.Empty,
                GameIcon = string.Empty,
                Currency = string.Empty,
                UserName = string.Empty
            };
            if (el.TryGetProperty("UserName", out var un)) row.UserName = un.GetString() ?? string.Empty;
            if (el.TryGetProperty("Money", out var money) && money.TryGetDecimal(out var m)) row.Money = m;
            if (el.TryGetProperty("Category", out var cat))
            {
                if (cat.ValueKind == JsonValueKind.Number && cat.TryGetInt32(out var ci)) row.Category = ci;
                else if (cat.ValueKind == JsonValueKind.String && int.TryParse(cat.GetString(), out var cs)) row.Category = cs;
            }

            if (el.TryGetProperty("Type", out var typ))
            {
                if (typ.ValueKind == JsonValueKind.Number && typ.TryGetInt32(out var ti)) row.Type = ti;
                else if (typ.ValueKind == JsonValueKind.String && int.TryParse(typ.GetString(), out var ts)) row.Type = ts;
            }

            if (el.TryGetProperty("Content", out var content)) row.Content = content.GetString() ?? string.Empty;
            if (el.TryGetProperty("Currency", out var cur)) row.Currency = cur.ToString();
            row.GameMoney = row.Money;
            if (row.ID > 0)
                await _db.Updateable(row).ExecuteCommandAsync(cancellationToken);
            else
                await _db.Insertable(row).ExecuteCommandAsync(cancellationToken);
        }
    }

    /// <summary>删除 Jackpot 人工行。</summary>
    /// <param name="input">ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task DelJackpotItemAsync(SiteConfigIdInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.ID <= 0)
            throw Oops.Oh("#参数错误#");
        var siteId = _site.RequireSiteId();
        var n = await _db.Deleteable<SiteJackpot>()
            .Where(j => j.SiteID == siteId && j.ID == input.ID)
            .ExecuteCommandAsync(cancellationToken);
        if (n != 1)
            throw Oops.Oh("#删除失败#");
    }

    /// <summary>读某一列 Setting 原文并解析。</summary>
    private async Task<Dictionary<string, object>> GetColumnSettingAsync(
        Func<SiteConfig, string> getter,
        CancellationToken cancellationToken)
    {
        var cfg = await EnsureConfigAsync(cancellationToken);
        return SiteSettingQuery.ToSettingObject(SiteSettingQuery.Parse(getter(cfg) ?? string.Empty));
    }

    /// <summary>合并表单后写回某一列。</summary>
    private async Task SaveColumnSettingAsync(
        SiteConfigSettingSaveInput input,
        Action<SiteConfig, string> apply,
        System.Linq.Expressions.Expression<Func<SiteConfig, object>> columns,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        var cfg = await EnsureConfigAsync(cancellationToken);
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        SiteSettingQuery.MergeJson(map, input.Setting);
        apply(cfg, SiteSettingQuery.ToQueryString(map));
        await UpdateConfigColumnsAsync(cfg, columns, cancellationToken);
    }

    /// <summary>确保 site_Config 有当前站一行。</summary>
    private async Task<SiteConfig> EnsureConfigAsync(CancellationToken cancellationToken)
    {
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<SiteConfig>()
            .Where(c => c.SiteID == siteId).Take(1).ToListAsync(cancellationToken);
        var cfg = rows.FirstOrDefault();
        if (cfg != null)
            return cfg;

        cfg = new SiteConfig
        {
            SiteID = siteId,
            Bank = string.Empty,
            RegisterString = string.Empty,
            MailString = string.Empty,
            BlackRegion = string.Empty,
            AgentJoinString = string.Empty,
            SMSString = string.Empty,
            MoneyType = string.Empty,
            GameSetting = string.Empty,
            AdminIP = string.Empty,
            PCStyle = string.Empty,
            MobileStyle = string.Empty,
            WarningSoundString = string.Empty,
            VIPSettingString = string.Empty,
            VirtualCoin = string.Empty,
            LoginString = string.Empty,
            BackgroundType = string.Empty,
            Festival = string.Empty,
            FestivalStatus = string.Empty,
            AreaCode = string.Empty,
            TelegramString = string.Empty,
            RegisterTitle = string.Empty,
            RegisterContent = string.Empty,
            PhoneCodeMessage = string.Empty,
            RiskSettingString = string.Empty,
            JackpotSettingString = string.Empty
        };
        await _db.Insertable(cfg).ExecuteCommandAsync(cancellationToken);
        return cfg;
    }

    /// <summary>按列更新 site_Config。</summary>
    private async Task UpdateConfigColumnsAsync(
        SiteConfig cfg,
        System.Linq.Expressions.Expression<Func<SiteConfig, object>> columns,
        CancellationToken cancellationToken)
    {
        var n = await _db.Updateable(cfg)
            .UpdateColumns(columns)
            .Where(c => c.SiteID == cfg.SiteID)
            .ExecuteCommandAsync(cancellationToken);
        if (n != 1)
            throw Oops.Oh("#保存失败#");
    }

    /// <summary>读当前 Site 行。</summary>
    private async Task<Site> LoadSiteAsync(CancellationToken cancellationToken)
    {
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<Site>().Where(s => s.ID == siteId).Take(1).ToListAsync(cancellationToken);
        return rows.FirstOrDefault() ?? throw Oops.Oh("#站点不存在#");
    }

    /// <summary>逗号分隔列表。</summary>
    private static List<string> SplitCsv(string raw) =>
        (raw ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(s => s.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    /// <summary>在 CSV 里加/删一项。</summary>
    private static string ToggleCsv(string raw, string item, bool selected)
    {
        var set = new HashSet<string>(SplitCsv(raw), StringComparer.OrdinalIgnoreCase);
        if (selected) set.Add(item);
        else set.Remove(item);
        return string.Join(",", set);
    }

    /// <summary>从表单里取出值为真的键。</summary>
    private static List<string> SelectedKeys(Dictionary<string, JsonElement> selected)
    {
        if (selected == null || selected.Count == 0)
            return [];
        var list = new List<string>();
        foreach (var kv in selected)
        {
            if (string.IsNullOrEmpty(kv.Key))
                continue;
            if (IsTruthy(kv.Value))
                list.Add(kv.Key);
        }

        return list.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>JSON 值是否表示勾选。</summary>
    private static bool IsTruthy(JsonElement el) =>
        el.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.Number => el.TryGetInt32(out var i) && i == 1,
            JsonValueKind.String => el.GetString() is "1" or "true" or "True",
            _ => false
        };

    /// <summary>Setting 对象里是否为真。</summary>
    private static bool IsSettingTruthy(Dictionary<string, object> map, string key)
    {
        if (map == null || !map.TryGetValue(key, out var v) || v == null)
            return false;
        return v switch
        {
            bool b => b,
            int i => i == 1,
            string s => s is "1" or "true" or "True",
            _ => false
        };
    }

    // --- merged from SiteConfigService.WaveGaps.cs ---



    /// <summary>
    /// 对齐旧 smssettinginfo。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> SmssettinginfoAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return SmssettinginfoCoreAsync(input, cancellationToken);
    }


    /// <summary>核心：smssettinginfo —— 转调 SmsSettingInfoAsync。</summary>
    private async Task<object?> SmssettinginfoCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var type = ReadString(input, "Type", "type", "Gateway", "gateway");
        return await SmsSettingInfoAsync(type, cancellationToken);
    }

    private static string ReadString(Dictionary<string, object?> input, params string[] keys)
    {
        foreach (var k in keys)
        {
            if (!input.TryGetValue(k, out var v) || v is null) continue;
            var s = v.ToString();
            if (!string.IsNullOrWhiteSpace(s)) return s.Trim();
        }
        return string.Empty;
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
