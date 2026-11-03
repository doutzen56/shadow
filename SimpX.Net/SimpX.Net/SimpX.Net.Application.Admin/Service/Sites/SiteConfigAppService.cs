using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 租户后台站点参数配置。对齐旧 /admin/site/config/*；路由 /api/system/siteConfig/*。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "siteConfig", Order = 456, Description = "站点参数配置")]
public partial class SiteConfigAppService : IDynamicApiController, ITransient
{
    private readonly SiteConfigService _svc;

    /// <summary>
    /// 注入站点配置服务。
    /// </summary>
    /// <param name="svc">配置</param>
    public SiteConfigAppService(SiteConfigService svc) => _svc = svc;

    /// <summary>提现银行目录。</summary>
    [ApiDescriptionSettings(Name = "GetBankConfig"), HttpPost]
    [DisplayName("提现银行目录")]
    public Task<object> GetBankConfig() =>
        _svc.GetBankConfigAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>当前站已选银行。</summary>
    [ApiDescriptionSettings(Name = "GetBankList"), HttpPost]
    [DisplayName("当前站银行列表")]
    public Task<object> GetBankList() =>
        _svc.GetBankListAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>勾选银行。</summary>
    /// <param name="input">Bank/Value</param>
    [ApiDescriptionSettings(Name = "SaveSiteBank"), HttpPost]
    [DisplayName("勾选提现银行")]
    public Task SaveSiteBank(SiteConfigBankToggleInput input) =>
        _svc.SaveSiteBankAsync(input ?? new SiteConfigBankToggleInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>勾选虚拟币。</summary>
    /// <param name="input">Coin/Value</param>
    [ApiDescriptionSettings(Name = "SaveSiteCoin"), HttpPost]
    [DisplayName("勾选虚拟币")]
    public Task SaveSiteCoin(SiteConfigBankToggleInput input) =>
        _svc.SaveSiteCoinAsync(input ?? new SiteConfigBankToggleInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>资金类型目录。</summary>
    [ApiDescriptionSettings(Name = "MoneyType"), HttpPost]
    [DisplayName("资金类型目录")]
    public Task<object> MoneyType() =>
        _svc.MoneyTypeAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>站点资金类型勾选。</summary>
    [ApiDescriptionSettings(Name = "SiteMoneyTypeConfig"), HttpPost]
    [DisplayName("站点资金类型配置")]
    public Task<object> SiteMoneyTypeConfig() =>
        _svc.SiteMoneyTypeConfigAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>保存资金类型。</summary>
    /// <param name="input">勾选表</param>
    [ApiDescriptionSettings(Name = "SaveMoneyTypeConfig"), HttpPost]
    [DisplayName("保存资金类型配置")]
    public Task SaveMoneyTypeConfig(SiteConfigMoneyTypeSaveInput input) =>
        _svc.SaveMoneyTypeConfigAsync(input ?? new SiteConfigMoneyTypeSaveInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>注册配置。</summary>
    [ApiDescriptionSettings(Name = "Register"), HttpPost]
    [DisplayName("注册配置")]
    public Task<Dictionary<string, object>> Register() =>
        _svc.RegisterAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>保存注册配置。</summary>
    /// <param name="input">表单</param>
    [ApiDescriptionSettings(Name = "SaveRegister"), HttpPost]
    [DisplayName("保存注册配置")]
    public Task SaveRegister(SiteConfigSettingSaveInput input) =>
        _svc.SaveRegisterAsync(input ?? new SiteConfigSettingSaveInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>注册站内信。</summary>
    [ApiDescriptionSettings(Name = "RegisterMessage"), HttpPost]
    [DisplayName("注册站内信")]
    public Task<object> RegisterMessage() =>
        _svc.RegisterMessageAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>手机验证码提示。</summary>
    [ApiDescriptionSettings(Name = "RegisterMessageCode"), HttpPost]
    [DisplayName("手机验证码提示")]
    public Task<object> RegisterMessageCode() =>
        _svc.RegisterMessageCodeAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>保存注册站内信。</summary>
    /// <param name="input">Title/Content</param>
    [ApiDescriptionSettings(Name = "SaveRegisterMessage"), HttpPost]
    [DisplayName("保存注册站内信")]
    public Task SaveRegisterMessage(SiteConfigRegisterMessageSaveInput input) =>
        _svc.SaveRegisterMessageAsync(input ?? new SiteConfigRegisterMessageSaveInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>保存手机验证码提示。</summary>
    /// <param name="input">Title</param>
    [ApiDescriptionSettings(Name = "SaveRegisterMessageCode"), HttpPost]
    [DisplayName("保存手机验证码提示")]
    public Task SaveRegisterMessageCode(SiteConfigRegisterMessageCodeSaveInput input) =>
        _svc.SaveRegisterMessageCodeAsync(input ?? new SiteConfigRegisterMessageCodeSaveInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>邮箱配置。</summary>
    [ApiDescriptionSettings(Name = "Mail"), HttpPost]
    [DisplayName("邮箱配置")]
    public Task<Dictionary<string, object>> Mail() =>
        _svc.MailAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>保存邮箱。</summary>
    /// <param name="input">表单</param>
    [ApiDescriptionSettings(Name = "SaveMail"), HttpPost]
    [DisplayName("保存邮箱配置")]
    public Task SaveMail(SiteConfigSettingSaveInput input) =>
        _svc.SaveMailAsync(input ?? new SiteConfigSettingSaveInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>提示音。</summary>
    [ApiDescriptionSettings(Name = "Sound"), HttpPost]
    [DisplayName("提示音配置")]
    public Task<Dictionary<string, object>> Sound() =>
        _svc.SoundAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>保存提示音。</summary>
    /// <param name="input">type/url</param>
    [ApiDescriptionSettings(Name = "SaveSound"), HttpPost]
    [DisplayName("保存提示音")]
    public Task SaveSound(SiteConfigSoundSaveInput input) =>
        _svc.SaveSoundAsync(input ?? new SiteConfigSoundSaveInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>VIP 开始时间。</summary>
    [ApiDescriptionSettings(Name = "Vip"), HttpPost]
    [DisplayName("VIP系统时间")]
    public Task<Dictionary<string, object>> Vip() =>
        _svc.VipAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>保存 VIP 开始时间。</summary>
    /// <param name="input">StartAt</param>
    [ApiDescriptionSettings(Name = "SaveVip"), HttpPost]
    [DisplayName("保存VIP系统时间")]
    public Task SaveVip(SiteConfigVipSaveInput input) =>
        _svc.SaveVipAsync(input ?? new SiteConfigVipSaveInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>清空 VIP 配置。</summary>
    [ApiDescriptionSettings(Name = "ClearVip"), HttpPost]
    [DisplayName("清空VIP配置")]
    public Task ClearVip() =>
        _svc.ClearVipAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>代理加盟条件。</summary>
    [ApiDescriptionSettings(Name = "AgentJoinSetting"), HttpPost]
    [DisplayName("代理加盟条件")]
    public Task<Dictionary<string, object>> AgentJoinSetting() =>
        _svc.AgentJoinSettingAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>保存代理加盟条件。</summary>
    /// <param name="input">表单</param>
    [ApiDescriptionSettings(Name = "SaveAgentJoinSetting"), HttpPost]
    [DisplayName("保存代理加盟条件")]
    public Task SaveAgentJoinSetting(SiteConfigSettingSaveInput input) =>
        _svc.SaveAgentJoinSettingAsync(input ?? new SiteConfigSettingSaveInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>短信网关。</summary>
    [ApiDescriptionSettings(Name = "SmsSetting"), HttpPost]
    [DisplayName("短信网关")]
    public Task<Dictionary<string, object>> SmsSetting() =>
        _svc.SmsSettingAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>短信网关子参数。</summary>
    /// <param name="type">类型</param>
    [ApiDescriptionSettings(Name = "SmsSettingInfo"), HttpPost]
    [DisplayName("短信网关参数")]
    public Task<Dictionary<string, object>> SmsSettingInfo([FromQuery] string type) =>
        _svc.SmsSettingInfoAsync(type, App.HttpContext?.RequestAborted ?? default);

    /// <summary>保存短信网关。</summary>
    /// <param name="input">表单</param>
    [ApiDescriptionSettings(Name = "SaveSmsSetting"), HttpPost]
    [DisplayName("保存短信网关")]
    public Task SaveSmsSetting(SiteConfigSmsSaveInput input) =>
        _svc.SaveSmsSettingAsync(input ?? new SiteConfigSmsSaveInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>SEO。</summary>
    [ApiDescriptionSettings(Name = "SeoSetting"), HttpPost]
    [DisplayName("SEO设定")]
    public Task<object> SeoSetting() =>
        _svc.SeoSettingAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>保存 SEO。</summary>
    /// <param name="input">字段</param>
    [ApiDescriptionSettings(Name = "SaveSeoSetting"), HttpPost]
    [DisplayName("保存SEO设定")]
    public Task SaveSeoSetting(SiteConfigSeoSaveInput input) =>
        _svc.SaveSeoSettingAsync(input ?? new SiteConfigSeoSaveInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>游戏参数。</summary>
    [ApiDescriptionSettings(Name = "GameSetting"), HttpPost]
    [DisplayName("游戏参数")]
    public Task<Dictionary<string, object>> GameSetting() =>
        _svc.GameSettingAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>保存游戏参数。</summary>
    /// <param name="input">表单</param>
    [ApiDescriptionSettings(Name = "SaveGameSetting"), HttpPost]
    [DisplayName("保存游戏参数")]
    public Task SaveGameSetting(SiteConfigSettingSaveInput input) =>
        _svc.SaveGameSettingAsync(input ?? new SiteConfigSettingSaveInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>登录方式。</summary>
    [ApiDescriptionSettings(Name = "LoginSetting"), HttpPost]
    [DisplayName("登录方式")]
    public Task<Dictionary<string, object>> LoginSetting() =>
        _svc.LoginSettingAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>保存登录方式。</summary>
    /// <param name="input">表单</param>
    [ApiDescriptionSettings(Name = "SaveLoginSetting"), HttpPost]
    [DisplayName("保存登录方式")]
    public Task SaveLoginSetting(SiteConfigSettingSaveInput input) =>
        _svc.SaveLoginSettingAsync(input ?? new SiteConfigSettingSaveInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>短信测试。</summary>
    /// <param name="input">参数</param>
    [ApiDescriptionSettings(Name = "SendSmsTest"), HttpPost]
    [DisplayName("短信测试")]
    public Task<object> SendSmsTest(SiteConfigSmsTestInput input) =>
        _svc.SendSmsTestAsync(input ?? new SiteConfigSmsTestInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>短信区号。</summary>
    [ApiDescriptionSettings(Name = "SmsAreaConfig"), HttpPost]
    [DisplayName("短信区号配置")]
    public Task<object> SmsAreaConfig() =>
        _svc.SmsAreaConfigAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>勾选短信区号。</summary>
    /// <param name="input">区号</param>
    [ApiDescriptionSettings(Name = "SaveSmsAreaConfig"), HttpPost]
    [DisplayName("勾选短信区号")]
    public Task SaveSmsAreaConfig(SiteConfigSmsAreaToggleInput input) =>
        _svc.SaveSmsAreaConfigAsync(input ?? new SiteConfigSmsAreaToggleInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>短信区号排序。</summary>
    /// <param name="input">SmsArea</param>
    [ApiDescriptionSettings(Name = "SaveSmsAreaConfigSort"), HttpPost]
    [DisplayName("短信区号排序")]
    public Task SaveSmsAreaConfigSort(SiteConfigSmsAreaSortInput input) =>
        _svc.SaveSmsAreaConfigSortAsync(input ?? new SiteConfigSmsAreaSortInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>背景配置。</summary>
    [ApiDescriptionSettings(Name = "SiteBackgroundConfig"), HttpPost]
    [DisplayName("背景配置")]
    public Task<object> SiteBackgroundConfig() =>
        _svc.SiteBackgroundConfigAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>保存背景。</summary>
    /// <param name="input">勾选</param>
    [ApiDescriptionSettings(Name = "SaveBackgroundConfig"), HttpPost]
    [DisplayName("保存背景配置")]
    public Task SaveBackgroundConfig(SiteConfigBackgroundSaveInput input) =>
        _svc.SaveBackgroundConfigAsync(input ?? new SiteConfigBackgroundSaveInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>已启用背景。</summary>
    [ApiDescriptionSettings(Name = "GetBackgroundConfig"), HttpPost]
    [DisplayName("已启用背景")]
    public Task<object> GetBackgroundConfig() =>
        _svc.GetBackgroundConfigAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>模板类型。</summary>
    [ApiDescriptionSettings(Name = "GetTemplateConfig"), HttpPost]
    [DisplayName("模板类型")]
    public Task<object> GetTemplateConfig() =>
        _svc.GetTemplateConfigAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>Telegram。</summary>
    [ApiDescriptionSettings(Name = "Telegram"), HttpPost]
    [DisplayName("Telegram推送")]
    public Task<object> Telegram() =>
        _svc.TelegramAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>保存 Telegram。</summary>
    /// <param name="input">JSON</param>
    [ApiDescriptionSettings(Name = "SaveTelegram"), HttpPost]
    [DisplayName("保存Telegram推送")]
    public Task SaveTelegram(SiteConfigTelegramSaveInput input) =>
        _svc.SaveTelegramAsync(input ?? new SiteConfigTelegramSaveInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>风控。</summary>
    [ApiDescriptionSettings(Name = "Risk"), HttpPost]
    [DisplayName("风控设置")]
    public Task<Dictionary<string, object>> Risk() =>
        _svc.RiskAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>保存风控。</summary>
    /// <param name="input">表单</param>
    [ApiDescriptionSettings(Name = "SaveRisk"), HttpPost]
    [DisplayName("保存风控设置")]
    public Task SaveRisk(SiteConfigSettingSaveInput input) =>
        _svc.SaveRiskAsync(input ?? new SiteConfigSettingSaveInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>Jackpot。</summary>
    [ApiDescriptionSettings(Name = "JackpotConfig"), HttpPost]
    [DisplayName("Jackpot配置")]
    public Task<object> JackpotConfig() =>
        _svc.JackpotConfigAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>保存 Jackpot。</summary>
    /// <param name="input">设置</param>
    [ApiDescriptionSettings(Name = "SaveJackpotConfig"), HttpPost]
    [DisplayName("保存Jackpot配置")]
    public Task SaveJackpotConfig(SiteConfigJackpotSaveInput input) =>
        _svc.SaveJackpotConfigAsync(input ?? new SiteConfigJackpotSaveInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>删除 Jackpot 行。</summary>
    /// <param name="input">ID</param>
    [ApiDescriptionSettings(Name = "DelJackpotItem"), HttpPost]
    [DisplayName("删除Jackpot行")]
    public Task DelJackpotItem(SiteConfigIdInput input) =>
        _svc.DelJackpotItemAsync(input ?? new SiteConfigIdInput(), App.HttpContext?.RequestAborted ?? default);

    // --- merged from SiteConfigAppService.WaveGaps.cs ---



    /// <summary>
    /// 对齐旧 smssettinginfo。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Smssettinginfo"), HttpPost]
    [DisplayName("smssettinginfo")]
    public Task<object?> Smssettinginfo(Dictionary<string, object?>? input = null) =>
        _svc.SmssettinginfoAsync(input, App.HttpContext?.RequestAborted ?? default);
}
