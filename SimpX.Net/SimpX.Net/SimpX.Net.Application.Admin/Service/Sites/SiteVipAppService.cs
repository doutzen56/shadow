using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// VIP 旧路径：/admin/site/vip/*
/// </summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "site-vip", Order = 32)]
public class SiteVipAppService : IDynamicApiController, ITransient
{
    public const string PermissionId = UserMemberFormAppService.MemberListPermissionId;
    private readonly SiteVipConditionService _svc;

    public SiteVipAppService(SiteVipConditionService svc) => _svc = svc;

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/vip/levels")]
    [HttpGet("/admin/site/vip/levels")]
    [NonUnify]
    [DisplayName("VIP-levels")]
    public Task<OldApiResult> Levels() => Run(ct => _svc.ListLevelsAsync(ct));

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/vip/getlevels")]
    [HttpGet("/admin/site/vip/getlevels")]
    [NonUnify]
    [DisplayName("VIP-getlevels")]
    public Task<OldApiResult> Getlevels() => Run(ct => _svc.ListLevelsAsync(ct));

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/vip/levelinfo")]
    [HttpGet("/admin/site/vip/levelinfo")]
    [NonUnify]
    [DisplayName("VIP-levelinfo")]
    public Task<OldApiResult> Levelinfo() =>
        Run(ct => _svc.LevelInfoAsync(OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id", "Level"), ct));

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/vip/savelevel")]
    [NonUnify]
    [DisplayName("VIP-savelevel")]
    public Task<OldApiResult> Savelevel() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            await _svc.SaveLevelAsync(new SiteVipLevelSaveInput
            {
                Level = (byte)OldApiFormReader.ReadInt(http, "Level", "level", "ID"),
                Name = OldApiFormReader.ReadString(http, "Name", "name"),
                Badge = OldApiFormReader.ReadString(http, "Badge", "badge"),
                GiftDescription = OldApiFormReader.ReadString(http, "GiftDescription"),
                UpgradeFlag = OldApiFormReader.ReadBool(http, "UpgradeFlag"),
                UpgradeClose = OldApiFormReader.ReadBool(http, "UpgradeClose"),
                HoldFlag = OldApiFormReader.ReadBool(http, "HoldFlag"),
                GiftFlag = OldApiFormReader.ReadBool(http, "GiftFlag")
            }, ct);
            return (object?)null;
        });

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/vip/deletelevel")]
    [NonUnify]
    [DisplayName("VIP-deletelevel")]
    public Task<OldApiResult> Deletelevel() =>
        Run(async ct =>
        {
            await _svc.DeleteLevelAsync((byte)OldApiFormReader.ReadInt(App.HttpContext, "Level", "level", "ID"), ct);
            return (object?)null;
        });

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/vip/rights")]
    [HttpGet("/admin/site/vip/rights")]
    [NonUnify]
    [DisplayName("VIP-rights")]
    public Task<OldApiResult> Rights() => Run(ct => _svc.RightsAsync(ct));

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/vip/rightinfo")]
    [HttpGet("/admin/site/vip/rightinfo")]
    [NonUnify]
    [DisplayName("VIP-rightinfo")]
    public Task<OldApiResult> Rightinfo() =>
        Run(ct => _svc.RightInfoAsync(OldApiFormReader.ReadString(App.HttpContext, "ID", "Id"), ct));

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/vip/saveright")]
    [NonUnify]
    [DisplayName("VIP-saveright")]
    public Task<OldApiResult> Saveright() =>
        Run(async ct =>
        {
            await _svc.SaveRightAsync(new SiteVipRightSaveInput
            {
                VIPRightSetting = OldApiFormReader.ReadString(App.HttpContext, "VIPRightSetting", "Raw", "Content", "content")
            }, ct);
            return (object?)null;
        });

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/vip/initvipright")]
    [NonUnify]
    [DisplayName("VIP-initvipright")]
    public Task<OldApiResult> Initvipright() =>
        Run(async ct =>
        {
            await _svc.InitVipRightAsync(ct);
            return (object?)null;
        });

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/vip/clearviprightcache")]
    [NonUnify]
    [DisplayName("VIP-clearviprightcache")]
    public Task<OldApiResult> Clearviprightcache() =>
        Run(async ct =>
        {
            await _svc.InitVipRightAsync(ct);
            return (object?)null;
        });

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/vip/deleteright")]
    [NonUnify]
    [DisplayName("VIP-deleteright")]
    public Task<OldApiResult> Deleteright() =>
        Run(async ct =>
        {
            await _svc.DeleteRightAsync(OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id"), ct);
            return (object?)null;
        });

    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/vip/upgradelog")]
    [HttpGet("/admin/site/vip/upgradelog")]
    [NonUnify]
    [DisplayName("VIP-upgradelog")]
    public Task<OldApiResult> Upgradelog() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            var (page, size) = OldApiShowResult.ReadPaging(http);
            return await _svc.PageUpgradeLogAsync(new SiteVipUpgradeLogPageInput
            {
                Page = page,
                PageSize = size,
                UserName = OldApiFormReader.ReadString(http, "User", "UserName", "user")
            }, ct);
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/vip/getrule"), HttpGet("/admin/site/vip/getrule")]
    [HttpPost("/admin/site/vip/getreturnrule"), HttpGet("/admin/site/vip/getreturnrule")]
    [HttpPost("/admin/site/vip/getbasecurrency"), HttpGet("/admin/site/vip/getbasecurrency")]
    [NonUnify, DisplayName("VIP-getrule")]
    public Task<OldApiResult> Getrule() =>
        Run(ct => _svc.PointRuleAsync(ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/vip/savereturnrule")]
    [NonUnify, DisplayName("VIP-savereturnrule")]
    public Task<OldApiResult> Savereturnrule() =>
        Run(async ct =>
        {
            var raw = OldApiFormReader.ReadString(App.HttpContext, "AutoReturnRule", "ReturnRule", "Rule", "Setting", "SettingString");
            await _svc.SaveReturnRuleAsync(raw, ct);
            return (object?)null;
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/vip/saverule")]
    [HttpPost("/admin/site/vip/savebasecurrency")]
    [HttpPost("/admin/site/vip/saverightstatus")]
    [HttpPost("/admin/site/vip/setuserslevel")]
    [NonUnify, DisplayName("VIP-saverule-stub")]
    public Task<OldApiResult> Saverule() =>
        Task.FromResult(OldApiResult.Fail("#功能尚未接通#"));

    private static async Task<OldApiResult> Run<T>(Func<CancellationToken, Task<T>> action) => await OldApiFormReader.RunAsync(action);
}
