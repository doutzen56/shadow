using System.Text.Json;
using Furion.FriendlyException;
using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 站点参数旧路径：/admin/site/config/*（Shared SiteConfigService 真逻辑）。
/// </summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "site-config", Order = 24)]
public class SiteConfigFormAppService : IDynamicApiController, ITransient
{
    /// <summary>沿用会员列表权限 GUID（本波先挂通路径；细权限后续按旧 Permission 拆）。</summary>
    public const string PermissionId = UserMemberFormAppService.MemberListPermissionId;

    private readonly SiteConfigService _svc;

    /// <summary>注入。</summary>
    public SiteConfigFormAppService(SiteConfigService svc) => _svc = svc;

    /// <summary>对齐旧 /admin/site/config/getbankconfig。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/config/getbankconfig")]
    [HttpGet("/admin/site/config/getbankconfig")]
    [NonUnify]
    [DisplayName("站点配置-getbankconfig")]
    public async Task<OldApiResult> Getbankconfig()
    {
        var result = await RunAsync(async ct =>
        {
            return await _svc.GetBankConfigAsync(ct);
        });
        return result;
    }
    /// <summary>对齐旧 /admin/site/config/getbanklist。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/config/getbanklist")]
    [HttpGet("/admin/site/config/getbanklist")]
    [NonUnify]
    [DisplayName("站点配置-getbanklist")]
    public async Task<OldApiResult> Getbanklist()
    {
        var result = await RunAsync(async ct =>
        {
            return await _svc.GetBankListAsync(ct);
        });
        return result;
    }
    /// <summary>对齐旧 /admin/site/config/savesitebank。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/config/savesitebank")]
    [NonUnify]
    [DisplayName("站点配置-savesitebank")]
    public async Task<OldApiResult> Savesitebank()
    {
        var result = await RunAsync(async ct =>
        {
            await _svc.SaveSiteBankAsync(ReadBankToggle(), ct); return (object?)null;
        });
        return result;
    }
    /// <summary>对齐旧 /admin/site/config/savesitecoin。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/config/savesitecoin")]
    [NonUnify]
    [DisplayName("站点配置-savesitecoin")]
    public async Task<OldApiResult> Savesitecoin()
    {
        var result = await RunAsync(async ct =>
        {
            await _svc.SaveSiteCoinAsync(ReadCoinToggle(), ct); return (object?)null;
        });
        return result;
    }
    /// <summary>对齐旧 /admin/site/config/moneytype 与 /admin/config/moneytype。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/config/moneytype")]
    [HttpGet("/admin/site/config/moneytype")]
    [HttpPost("/admin/config/moneytype")]
    [HttpGet("/admin/config/moneytype")]
    [NonUnify]
    [DisplayName("站点配置-moneytype")]
    public async Task<OldApiResult> Moneytype()
    {
        var result = await RunAsync(async ct =>
        {
            return await _svc.MoneyTypeAsync(ct);
        });
        return result;
    }
    /// <summary>对齐旧 /admin/site/config/sitemoneytypeconfig。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/config/sitemoneytypeconfig")]
    [HttpGet("/admin/site/config/sitemoneytypeconfig")]
    [NonUnify]
    [DisplayName("站点配置-sitemoneytypeconfig")]
    public async Task<OldApiResult> Sitemoneytypeconfig()
    {
        var result = await RunAsync(async ct =>
        {
            return await _svc.SiteMoneyTypeConfigAsync(ct);
        });
        return result;
    }
    /// <summary>对齐旧 /admin/site/config/savemoneytypeconfig。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/config/savemoneytypeconfig")]
    [NonUnify]
    [DisplayName("站点配置-savemoneytypeconfig")]
    public async Task<OldApiResult> Savemoneytypeconfig()
    {
        var result = await RunAsync(async ct =>
        {
            await _svc.SaveMoneyTypeConfigAsync(ReadMoneyTypeSave(), ct); return (object?)null;
        });
        return result;
    }
    /// <summary>对齐旧 /admin/site/config/register。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/config/register")]
    [HttpGet("/admin/site/config/register")]
    [NonUnify]
    [DisplayName("站点配置-register")]
    public async Task<OldApiResult> Register()
    {
        var result = await RunAsync(async ct =>
        {
            return await _svc.RegisterAsync(ct);
        });
        return result;
    }
    /// <summary>对齐旧 /admin/site/config/registermessage。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/config/registermessage")]
    [HttpGet("/admin/site/config/registermessage")]
    [NonUnify]
    [DisplayName("站点配置-registermessage")]
    public async Task<OldApiResult> Registermessage()
    {
        var result = await RunAsync(async ct =>
        {
            return await _svc.RegisterMessageAsync(ct);
        });
        return result;
    }
    /// <summary>对齐旧 /admin/site/config/registermessagecode。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/config/registermessagecode")]
    [HttpGet("/admin/site/config/registermessagecode")]
    [NonUnify]
    [DisplayName("站点配置-registermessagecode")]
    public async Task<OldApiResult> Registermessagecode()
    {
        var result = await RunAsync(async ct =>
        {
            return await _svc.RegisterMessageCodeAsync(ct);
        });
        return result;
    }
    /// <summary>对齐旧 /admin/site/config/saveregister。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/config/saveregister")]
    [NonUnify]
    [DisplayName("站点配置-saveregister")]
    public async Task<OldApiResult> Saveregister()
    {
        var result = await RunAsync(async ct =>
        {
            await _svc.SaveRegisterAsync(ReadSettingSave(), ct); return (object?)null;
        });
        return result;
    }
    /// <summary>对齐旧 /admin/site/config/saveregistermessage。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/config/saveregistermessage")]
    [NonUnify]
    [DisplayName("站点配置-saveregistermessage")]
    public async Task<OldApiResult> Saveregistermessage()
    {
        var result = await RunAsync(async ct =>
        {
            await _svc.SaveRegisterMessageAsync(ReadRegisterMessage(), ct); return (object?)null;
        });
        return result;
    }
    /// <summary>对齐旧 /admin/site/config/saveregistermessagecode。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/config/saveregistermessagecode")]
    [NonUnify]
    [DisplayName("站点配置-saveregistermessagecode")]
    public async Task<OldApiResult> Saveregistermessagecode()
    {
        var result = await RunAsync(async ct =>
        {
            await _svc.SaveRegisterMessageCodeAsync(ReadRegisterMessageCode(), ct); return (object?)null;
        });
        return result;
    }
    /// <summary>对齐旧 /admin/site/config/mail。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/config/mail")]
    [HttpGet("/admin/site/config/mail")]
    [NonUnify]
    [DisplayName("站点配置-mail")]
    public async Task<OldApiResult> Mail()
    {
        var result = await RunAsync(async ct =>
        {
            return await _svc.MailAsync(ct);
        });
        return result;
    }
    /// <summary>对齐旧 /admin/site/config/savemail。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/config/savemail")]
    [NonUnify]
    [DisplayName("站点配置-savemail")]
    public async Task<OldApiResult> Savemail()
    {
        var result = await RunAsync(async ct =>
        {
            await _svc.SaveMailAsync(ReadSettingSave(), ct); return (object?)null;
        });
        return result;
    }
    /// <summary>对齐旧 /admin/site/config/sound。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/config/sound")]
    [HttpGet("/admin/site/config/sound")]
    [NonUnify]
    [DisplayName("站点配置-sound")]
    public async Task<OldApiResult> Sound()
    {
        var result = await RunAsync(async ct =>
        {
            return await _svc.SoundAsync(ct);
        });
        return result;
    }
    /// <summary>对齐旧 /admin/site/config/savesound。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/config/savesound")]
    [NonUnify]
    [DisplayName("站点配置-savesound")]
    public async Task<OldApiResult> Savesound()
    {
        var result = await RunAsync(async ct =>
        {
            await _svc.SaveSoundAsync(ReadSoundSave(), ct); return (object?)null;
        });
        return result;
    }
    /// <summary>对齐旧 /admin/site/config/vip。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/config/vip")]
    [HttpGet("/admin/site/config/vip")]
    [NonUnify]
    [DisplayName("站点配置-vip")]
    public async Task<OldApiResult> Vip()
    {
        var result = await RunAsync(async ct =>
        {
            return await _svc.VipAsync(ct);
        });
        return result;
    }
    /// <summary>对齐旧 /admin/site/config/savevip。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/config/savevip")]
    [NonUnify]
    [DisplayName("站点配置-savevip")]
    public async Task<OldApiResult> Savevip()
    {
        var result = await RunAsync(async ct =>
        {
            await _svc.SaveVipAsync(ReadVipSave(), ct); return (object?)null;
        });
        return result;
    }
    /// <summary>对齐旧 /admin/site/config/clearvip。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/config/clearvip")]
    [NonUnify]
    [DisplayName("站点配置-clearvip")]
    public async Task<OldApiResult> Clearvip()
    {
        var result = await RunAsync(async ct =>
        {
            await _svc.ClearVipAsync(ct); return (object?)null;
        });
        return result;
    }
    /// <summary>对齐旧 /admin/site/config/agentjoinsetting。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/config/agentjoinsetting")]
    [HttpGet("/admin/site/config/agentjoinsetting")]
    [NonUnify]
    [DisplayName("站点配置-agentjoinsetting")]
    public async Task<OldApiResult> Agentjoinsetting()
    {
        var result = await RunAsync(async ct =>
        {
            return await _svc.AgentJoinSettingAsync(ct);
        });
        return result;
    }
    /// <summary>对齐旧 /admin/site/config/saveagentjoinsetting。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/config/saveagentjoinsetting")]
    [NonUnify]
    [DisplayName("站点配置-saveagentjoinsetting")]
    public async Task<OldApiResult> Saveagentjoinsetting()
    {
        var result = await RunAsync(async ct =>
        {
            await _svc.SaveAgentJoinSettingAsync(ReadSettingSave(), ct); return (object?)null;
        });
        return result;
    }
    /// <summary>对齐旧 /admin/site/config/smssetting。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/config/smssetting")]
    [HttpGet("/admin/site/config/smssetting")]
    [NonUnify]
    [DisplayName("站点配置-smssetting")]
    public async Task<OldApiResult> Smssetting()
    {
        var result = await RunAsync(async ct =>
        {
            return await _svc.SmsSettingAsync(ct);
        });
        return result;
    }
    /// <summary>对齐旧 /admin/site/config/smssettinginfo。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/config/smssettinginfo")]
    [HttpGet("/admin/site/config/smssettinginfo")]
    [NonUnify]
    [DisplayName("站点配置-smssettinginfo")]
    public async Task<OldApiResult> Smssettinginfo()
    {
        var result = await RunAsync(async ct =>
        {
            return await _svc.SmsSettingInfoAsync(ReadString("type", "Type"), ct);
        });
        return result;
    }
    /// <summary>对齐旧 /admin/site/config/savesmssetting。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/config/savesmssetting")]
    [NonUnify]
    [DisplayName("站点配置-savesmssetting")]
    public async Task<OldApiResult> Savesmssetting()
    {
        var result = await RunAsync(async ct =>
        {
            await _svc.SaveSmsSettingAsync(ReadSmsSave(), ct); return (object?)null;
        });
        return result;
    }
    /// <summary>对齐旧 /admin/site/config/seosetting。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/config/seosetting")]
    [HttpGet("/admin/site/config/seosetting")]
    [NonUnify]
    [DisplayName("站点配置-seosetting")]
    public async Task<OldApiResult> Seosetting()
    {
        var result = await RunAsync(async ct =>
        {
            return await _svc.SeoSettingAsync(ct);
        });
        return result;
    }
    /// <summary>对齐旧 /admin/site/config/saveseosetting。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/config/saveseosetting")]
    [NonUnify]
    [DisplayName("站点配置-saveseosetting")]
    public async Task<OldApiResult> Saveseosetting()
    {
        var result = await RunAsync(async ct =>
        {
            await _svc.SaveSeoSettingAsync(ReadSeoSave(), ct); return (object?)null;
        });
        return result;
    }
    /// <summary>对齐旧 /admin/site/config/gamesetting。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/config/gamesetting")]
    [HttpGet("/admin/site/config/gamesetting")]
    [NonUnify]
    [DisplayName("站点配置-gamesetting")]
    public async Task<OldApiResult> Gamesetting()
    {
        var result = await RunAsync(async ct =>
        {
            return await _svc.GameSettingAsync(ct);
        });
        return result;
    }
    /// <summary>对齐旧 /admin/site/config/savegamesetting。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/config/savegamesetting")]
    [NonUnify]
    [DisplayName("站点配置-savegamesetting")]
    public async Task<OldApiResult> Savegamesetting()
    {
        var result = await RunAsync(async ct =>
        {
            await _svc.SaveGameSettingAsync(ReadSettingSave(), ct); return (object?)null;
        });
        return result;
    }
    /// <summary>对齐旧 /admin/site/config/loginsetting。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/config/loginsetting")]
    [HttpGet("/admin/site/config/loginsetting")]
    [NonUnify]
    [DisplayName("站点配置-loginsetting")]
    public async Task<OldApiResult> Loginsetting()
    {
        var result = await RunAsync(async ct =>
        {
            return await _svc.LoginSettingAsync(ct);
        });
        return result;
    }
    /// <summary>对齐旧 /admin/site/config/saveloginsetting。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/config/saveloginsetting")]
    [NonUnify]
    [DisplayName("站点配置-saveloginsetting")]
    public async Task<OldApiResult> Saveloginsetting()
    {
        var result = await RunAsync(async ct =>
        {
            await _svc.SaveLoginSettingAsync(ReadSettingSave(), ct); return (object?)null;
        });
        return result;
    }
    /// <summary>对齐旧 /admin/site/config/sendsmstest。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/config/sendsmstest")]
    [NonUnify]
    [DisplayName("站点配置-sendsmstest")]
    public async Task<OldApiResult> Sendsmstest()
    {
        var result = await RunAsync(async ct =>
        {
            return await _svc.SendSmsTestAsync(ReadSmsTest(), ct);
        });
        return result;
    }
    /// <summary>对齐旧 /admin/site/config/smsareaconfig。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/config/smsareaconfig")]
    [HttpGet("/admin/site/config/smsareaconfig")]
    [NonUnify]
    [DisplayName("站点配置-smsareaconfig")]
    public async Task<OldApiResult> Smsareaconfig()
    {
        var result = await RunAsync(async ct =>
        {
            return await _svc.SmsAreaConfigAsync(ct);
        });
        return result;
    }
    /// <summary>对齐旧 /admin/site/config/savesmsareaconfig。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/config/savesmsareaconfig")]
    [NonUnify]
    [DisplayName("站点配置-savesmsareaconfig")]
    public async Task<OldApiResult> Savesmsareaconfig()
    {
        var result = await RunAsync(async ct =>
        {
            await _svc.SaveSmsAreaConfigAsync(ReadSmsAreaToggle(), ct); return (object?)null;
        });
        return result;
    }
    /// <summary>对齐旧 /admin/site/config/savesmsareaconfigsort。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/config/savesmsareaconfigsort")]
    [NonUnify]
    [DisplayName("站点配置-savesmsareaconfigsort")]
    public async Task<OldApiResult> Savesmsareaconfigsort()
    {
        var result = await RunAsync(async ct =>
        {
            await _svc.SaveSmsAreaConfigSortAsync(ReadSmsAreaSort(), ct); return (object?)null;
        });
        return result;
    }
    /// <summary>对齐旧 /admin/site/config/sitebackgroundconfig。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/config/sitebackgroundconfig")]
    [HttpGet("/admin/site/config/sitebackgroundconfig")]
    [NonUnify]
    [DisplayName("站点配置-sitebackgroundconfig")]
    public async Task<OldApiResult> Sitebackgroundconfig()
    {
        var result = await RunAsync(async ct =>
        {
            return await _svc.SiteBackgroundConfigAsync(ct);
        });
        return result;
    }
    /// <summary>对齐旧 /admin/site/config/savebackgroundconfig。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/config/savebackgroundconfig")]
    [NonUnify]
    [DisplayName("站点配置-savebackgroundconfig")]
    public async Task<OldApiResult> Savebackgroundconfig()
    {
        var result = await RunAsync(async ct =>
        {
            await _svc.SaveBackgroundConfigAsync(ReadBackgroundSave(), ct); return (object?)null;
        });
        return result;
    }
    /// <summary>对齐旧 /admin/site/config/getbackgroundconfig。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/config/getbackgroundconfig")]
    [HttpGet("/admin/site/config/getbackgroundconfig")]
    [NonUnify]
    [DisplayName("站点配置-getbackgroundconfig")]
    public async Task<OldApiResult> Getbackgroundconfig()
    {
        var result = await RunAsync(async ct =>
        {
            return await _svc.GetBackgroundConfigAsync(ct);
        });
        return result;
    }
    /// <summary>对齐旧 /admin/site/config/gettemplateconfig。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/config/gettemplateconfig")]
    [HttpGet("/admin/site/config/gettemplateconfig")]
    [HttpPost("/admin/site/template/list")]
    [HttpGet("/admin/site/template/list")]
    [NonUnify]
    [DisplayName("站点配置-gettemplateconfig")]
    public async Task<OldApiResult> Gettemplateconfig()
    {
        var result = await RunAsync(async ct =>
        {
            return await _svc.GetTemplateConfigAsync(ct);
        });
        return result;
    }
    /// <summary>对齐旧 /admin/site/config/telegram。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/config/telegram")]
    [HttpGet("/admin/site/config/telegram")]
    [NonUnify]
    [DisplayName("站点配置-telegram")]
    public async Task<OldApiResult> Telegram()
    {
        var result = await RunAsync(async ct =>
        {
            return await _svc.TelegramAsync(ct);
        });
        return result;
    }
    /// <summary>对齐旧 /admin/site/config/savetelegram。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/config/savetelegram")]
    [NonUnify]
    [DisplayName("站点配置-savetelegram")]
    public async Task<OldApiResult> Savetelegram()
    {
        var result = await RunAsync(async ct =>
        {
            await _svc.SaveTelegramAsync(ReadTelegramSave(), ct); return (object?)null;
        });
        return result;
    }
    /// <summary>对齐旧 /admin/site/config/risk。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/config/risk")]
    [HttpGet("/admin/site/config/risk")]
    [NonUnify]
    [DisplayName("站点配置-risk")]
    public async Task<OldApiResult> Risk()
    {
        var result = await RunAsync(async ct =>
        {
            return await _svc.RiskAsync(ct);
        });
        return result;
    }
    /// <summary>对齐旧 /admin/site/config/saverisk。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/config/saverisk")]
    [NonUnify]
    [DisplayName("站点配置-saverisk")]
    public async Task<OldApiResult> Saverisk()
    {
        var result = await RunAsync(async ct =>
        {
            await _svc.SaveRiskAsync(ReadSettingSave(), ct); return (object?)null;
        });
        return result;
    }
    /// <summary>对齐旧 /admin/site/config/jackpotconfig。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/config/jackpotconfig")]
    [HttpGet("/admin/site/config/jackpotconfig")]
    [NonUnify]
    [DisplayName("站点配置-jackpotconfig")]
    public async Task<OldApiResult> Jackpotconfig()
    {
        var result = await RunAsync(async ct =>
        {
            return await _svc.JackpotConfigAsync(ct);
        });
        return result;
    }
    /// <summary>对齐旧 /admin/site/config/savejackpotconfig。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/config/savejackpotconfig")]
    [NonUnify]
    [DisplayName("站点配置-savejackpotconfig")]
    public async Task<OldApiResult> Savejackpotconfig()
    {
        var result = await RunAsync(async ct =>
        {
            await _svc.SaveJackpotConfigAsync(ReadJackpotSave(), ct); return (object?)null;
        });
        return result;
    }
    /// <summary>对齐旧 /admin/site/config/deljackpotitem。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/config/deljackpotitem")]
    [NonUnify]
    [DisplayName("站点配置-deljackpotitem")]
    public async Task<OldApiResult> Deljackpotitem()
    {
        var result = await RunAsync(async ct =>
        {
            await _svc.DelJackpotItemAsync(ReadIdInput(), ct); return (object?)null;
        });
        return result;
    }
    private static SiteConfigSettingSaveInput ReadSettingSave() =>
        new() { Setting = ReadJsonMap() };

    private static SiteConfigBankToggleInput ReadBankToggle() =>
        new()
        {
            Bank = ReadString("Bank", "bank"),
            Value = ReadInt("Value", "value")
        };

    private static SiteConfigBankToggleInput ReadCoinToggle() =>
        new()
        {
            Coin = ReadString("Coin", "coin"),
            Value = ReadInt("Value", "value")
        };

    private static SiteConfigMoneyTypeSaveInput ReadMoneyTypeSave() =>
        new() { Selected = ReadJsonMap() };

    private static SiteConfigRegisterMessageSaveInput ReadRegisterMessage() =>
        new()
        {
            Title = ReadString("Title", "title"),
            Content = ReadString("Content", "content")
        };

    private static SiteConfigRegisterMessageCodeSaveInput ReadRegisterMessageCode() =>
        new() { Title = ReadString("Title", "title") };

    private static SiteConfigSoundSaveInput ReadSoundSave() =>
        new()
        {
            Type = ReadString("Type", "type"),
            Url = ReadString("Url", "url")
        };

    private static SiteConfigVipSaveInput ReadVipSave()
    {
        _ = DateTime.TryParse(ReadString("StartAt", "startAt"), out var at);
        return new SiteConfigVipSaveInput { StartAt = at };
    }

    private static SiteConfigSmsSaveInput ReadSmsSave() =>
        new()
        {
            Setting = ReadJsonMap(),
            Config = ReadPrefixedJsonMap("Setting.")
        };

    private static SiteConfigSeoSaveInput ReadSeoSave() =>
        new()
        {
            Title = ReadString("Title", "title"),
            Keyword = ReadString("Keyword", "keyword"),
            Description = ReadString("Description", "description"),
            Footer = ReadString("Footer", "footer")
        };

    private static SiteConfigSmsTestInput ReadSmsTest() =>
        new()
        {
            Language = ReadString("Language", "language"),
            Area = ReadString("Area", "area"),
            Phone = ReadString("Phone", "phone"),
            Context = ReadString("Context", "context")
        };

    private static SiteConfigSmsAreaToggleInput ReadSmsAreaToggle() =>
        new()
        {
            AreaCode = ReadString("AreaCode", "areaCode", "Area"),
            Value = ReadInt("Value", "value")
        };

    private static SiteConfigSmsAreaSortInput ReadSmsAreaSort() =>
        new() { SmsArea = ReadString("SmsArea", "smsArea") };

    private static SiteConfigBackgroundSaveInput ReadBackgroundSave() =>
        new()
        {
            Selected = ReadJsonMap(),
            Festival = ReadString("Festival", "festival"),
            FestivalStatus = ReadString("FestivalStatus", "festivalStatus")
        };

    private static SiteConfigTelegramSaveInput ReadTelegramSave() =>
        new() { Telegram = ReadString("Telegram", "telegram") };

    private static SiteConfigJackpotSaveInput ReadJackpotSave() =>
        new()
        {
            Setting = ReadJsonMap(),
            InterventionList = ReadString("InterventionList", "interventionList")
        };

    private static SiteConfigIdInput ReadIdInput() =>
        new() { ID = ReadInt("ID", "Id", "id") };

    private static Dictionary<string, JsonElement> ReadJsonMap()
    {
        var map = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        var http = App.HttpContext;
        if (http?.Request.HasFormContentType == true)
        {
            foreach (var key in http.Request.Form.Keys)
                map[key] = JsonSerializer.SerializeToElement(http.Request.Form[key].ToString());
        }

        foreach (var key in http?.Request.Query.Keys ?? Enumerable.Empty<string>())
        {
            if (!map.ContainsKey(key))
                map[key] = JsonSerializer.SerializeToElement(http.Request.Query[key].ToString());
        }

        return map;
    }

    private static Dictionary<string, JsonElement> ReadPrefixedJsonMap(string prefix)
    {
        var all = ReadJsonMap();
        var map = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        foreach (var (k, v) in all)
        {
            if (k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                map[k[prefix.Length..]] = v;
        }

        return map;
    }

    private static string ReadString(params string[] keys)
    {
        var http = App.HttpContext;
        if (http == null) return string.Empty;
        foreach (var key in keys)
        {
            if (http.Request.HasFormContentType && http.Request.Form.ContainsKey(key))
                return http.Request.Form[key].ToString();
            if (http.Request.Query.ContainsKey(key))
                return http.Request.Query[key].ToString();
        }

        return string.Empty;
    }

    private static int ReadInt(params string[] keys)
    {
        _ = int.TryParse(ReadString(keys), out var n);
        return n;
    }

    private static async Task<OldApiResult> RunAsync(Func<CancellationToken, Task<object?>> action)
    {
        var ct = App.HttpContext?.RequestAborted ?? default;
        try
        {
            var info = await action(ct);
            return info is null ? OldApiResult.Ok("#操作成功#") : OldApiResult.Ok(string.Empty, info);
        }
        catch (AppFriendlyException ex)
        {
            return OldApiResult.Fail(ex.Message);
        }
        catch (Exception ex)
        {
            return OldApiResult.Fail(ex.Message);
        }
    }

}
