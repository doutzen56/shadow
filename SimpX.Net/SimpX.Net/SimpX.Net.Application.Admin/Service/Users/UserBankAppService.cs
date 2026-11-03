using Furion.FriendlyException;
using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>站点后台银行信息：/admin/user/member/bank/*。</summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "user-bank", Order = 25)]
public class UserBankAppService : IDynamicApiController, ITransient
{
    /// <summary>先挂会员列表权限；旧银行专用 GUID 对打后再收窄。</summary>
    public const string PermissionId = UserMemberFormAppService.MemberListPermissionId;

    private readonly BankAccountService _svc;

    /// <summary>注入。</summary>
    public UserBankAppService(BankAccountService svc) => _svc = svc;

    /// <summary>银行信息列表。对应旧 list。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/member/bank/list")]
    [HttpGet("/admin/user/member/bank/list")]
    [NonUnify]
    [DisplayName("站点后台银行信息列表")]
    public async Task<OldApiResult> List()
    {
        var http = App.HttpContext;
        var (pageIndex, pageSize) = OldApiShowResult.ReadPaging(http);
        var input = new BankAccountAdminPageInput
        {
            Page = pageIndex,
            PageSize = pageSize,
            StartAt = OldApiRequestParam.Read(http, "StartAt"),
            EndAt = OldApiRequestParam.Read(http, "EndAt"),
            UserNumber = OldApiRequestParam.Read(http, "UserNumber"),
            UserName = OldApiRequestParam.Read(http, "UserName") ?? OldApiRequestParam.Read(http, "User"),
            Status = OldApiRequestParam.Read(http, "Status"),
            IsVir = OldApiRequestParam.Read(http, "IsVir"),
            Currency = OldApiRequestParam.Read(http, "Currency"),
        };
        var result = await RunAsync(async ct =>
        {
            var page = await _svc.PageAsync(input, ct);
            return OldApiShowResult.OfPage(page.Items, page.Total, pageIndex, pageSize);
        });
        return result;
    }

    /// <summary>银行详情。对应旧 info。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/member/bank/info")]
    [HttpGet("/admin/user/member/bank/info")]
    [NonUnify]
    [DisplayName("站点后台银行信息详情")]
    public async Task<OldApiResult> Info()
    {
        var id = OldApiRequestParam.ReadInt(App.HttpContext, "ID");
        var result = await RunAsync(async ct => await _svc.DetailAsync(id, ct));
        return result;
    }

    /// <summary>按用户名确认会员。对应旧 verifyuser。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/member/bank/verifyuser")]
    [HttpGet("/admin/user/member/bank/verifyuser")]
    [NonUnify]
    [DisplayName("站点后台银行查会员")]
    public async Task<OldApiResult> Verifyuser()
    {
        var userName = OldApiRequestParam.Read(App.HttpContext, "UserName")
            ?? OldApiRequestParam.Read(App.HttpContext, "User");
        var result = await RunAsync(async ct => await _svc.VerifyUserAsync(userName, ct));
        return result;
    }

    /// <summary>银行类型。对应旧 bankcountrylist。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/member/bank/bankcountrylist")]
    [HttpGet("/admin/user/member/bank/bankcountrylist")]
    [NonUnify]
    [DisplayName("站点后台银行类型")]
    public async Task<OldApiResult> Bankcountrylist()
    {
        // 旧站 info 直接是数组，不是 ShowResult 包装
        var result = await RunAsync(async ct => await _svc.BankCountryListAsync(ct));
        return result;
    }

    /// <summary>超商城市。对应旧 bankcitylist。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/member/bank/bankcitylist")]
    [HttpGet("/admin/user/member/bank/bankcitylist")]
    [NonUnify]
    [DisplayName("站点后台超商城市")]
    public async Task<OldApiResult> Bankcitylist()
    {
        var type = OldApiRequestParam.ReadInt(App.HttpContext, "Type");
        var result = await RunAsync(async ct =>
        {
            var rows = await _svc.BankCityListAsync(type, ct);
            return OldApiShowResult.OfList(rows);
        });
        return result;
    }

    /// <summary>超商区县。对应旧 bankdistrictlist。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/member/bank/bankdistrictlist")]
    [HttpGet("/admin/user/member/bank/bankdistrictlist")]
    [NonUnify]
    [DisplayName("站点后台超商区县")]
    public async Task<OldApiResult> Bankdistrictlist()
    {
        var http = App.HttpContext;
        var type = OldApiRequestParam.ReadInt(http, "Type");
        var city = OldApiRequestParam.Read(http, "City");
        var result = await RunAsync(async ct =>
        {
            var rows = await _svc.BankDistrictListAsync(type, city, ct);
            return OldApiShowResult.OfList(rows);
        });
        return result;
    }

    /// <summary>超商门店。对应旧 bankadresslist。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/member/bank/bankadresslist")]
    [HttpGet("/admin/user/member/bank/bankadresslist")]
    [NonUnify]
    [DisplayName("站点后台超商门店")]
    public async Task<OldApiResult> Bankadresslist()
    {
        var http = App.HttpContext;
        var type = OldApiRequestParam.ReadInt(http, "Type");
        var city = OldApiRequestParam.Read(http, "City");
        var district = OldApiRequestParam.Read(http, "District");
        var result = await RunAsync(async ct =>
        {
            var rows = await _svc.BankAdressListAsync(type, city, district, ct);
            return OldApiShowResult.OfList(rows);
        });
        return result;
    }

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/member/bank/bankcountryinfo"), HttpGet("/admin/user/member/bank/bankcountryinfo")]
    [NonUnify, DisplayName("站点后台银行国家信息")]
    public async Task<OldApiResult> Bankcountryinfo()
    {
        var http = App.HttpContext;
        var result = await RunAsync(async ct =>
            await _svc.BankCountryInfoAsync(
                OldApiRequestParam.Read(http, "Country") ?? string.Empty,
                OldApiRequestParam.Read(http, "IsVir")
                    ?? OldApiRequestParam.Read(http, "isVir")
                    ?? string.Empty,
                ct));
        return result;
    }

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/member/bank/createbank")]
    [NonUnify, DisplayName("站点后台创建银行卡")]
    public async Task<OldApiResult> Createbank()
    {
        var http = App.HttpContext;
        var result = await RunAsync(async ct =>
        {
            await _svc.CreateAsync(new BankAccountCreateInput
            {
                ID = OldApiRequestParam.ReadInt(http, "ID", 0),
                UserName = OldApiRequestParam.Read(http, "UserName") ?? string.Empty,
                IsVir = OldApiRequestParam.Read(http, "IsVir")
                    ?? OldApiRequestParam.Read(http, "isVir")
                    ?? "0",
                Type = OldApiRequestParam.ReadInt(http, "Type"),
                Name = OldApiRequestParam.Read(http, "Name") ?? string.Empty,
                Account = OldApiRequestParam.Read(http, "Account") ?? string.Empty,
                AccountDetail = OldApiRequestParam.Read(http, "AccountDetail") ?? string.Empty,
                BankAddress = OldApiRequestParam.Read(http, "BankAddress")
                    ?? OldApiRequestParam.Read(http, "CBankAddressLabel")
                    ?? string.Empty,
                BranchCode = OldApiRequestParam.Read(http, "BranchCode") ?? string.Empty,
                BankInfo = OldApiRequestParam.Read(http, "BankInfo") ?? string.Empty,
                VCurrency = OldApiRequestParam.ReadInt(http, "VCurrency"),
                VAgreement = OldApiRequestParam.Read(http, "VAgreement") ?? string.Empty,
                VAddress = OldApiRequestParam.Read(http, "VAddress") ?? string.Empty,
                EType = OldApiRequestParam.ReadInt(http, "EType"),
                EUserName = OldApiRequestParam.Read(http, "EUserName") ?? string.Empty,
                EAccount = OldApiRequestParam.Read(http, "EAccount") ?? string.Empty,
                EArea = OldApiRequestParam.Read(http, "EArea") ?? string.Empty,
                CType = OldApiRequestParam.ReadInt(http, "CType"),
                CName = OldApiRequestParam.Read(http, "CName") ?? string.Empty,
                CAccount = OldApiRequestParam.Read(http, "CAccount") ?? string.Empty,
                CCity = OldApiRequestParam.Read(http, "CCity") ?? string.Empty,
                CDistrict = OldApiRequestParam.Read(http, "CDistrict") ?? string.Empty,
                CBankAddress = OldApiRequestParam.ReadInt(http, "CBankAddress")
            }, ct);
            return null;
        });
        return result;
    }

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/member/bank/delete")]
    [NonUnify, DisplayName("站点后台删银行卡")]
    public async Task<OldApiResult> Delete()
    {
        var result = await RunAsync(async ct =>
        {
            await _svc.DeleteAsync(OldApiRequestParam.ReadInt(App.HttpContext, "ID"), ct);
            return null;
        });
        return result;
    }

    /// <summary>审核银行信息。对应旧 updateinfo（Status 支持 Success/Faild/整型）。</summary>
    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/member/bank/updateinfo")]
    [NonUnify, DisplayName("站点后台银行卡审核")]
    public async Task<OldApiResult> Updateinfo()
    {
        var http = App.HttpContext;
        var result = await RunAsync(async ct =>
        {
            var statusRaw = OldApiRequestParam.Read(http, "Status") ?? string.Empty;
            if (!OldApiMoneyDisplay.TryParseCheckStatus(statusRaw, out var status))
                throw Oops.Oh("#参数错误#");
            await _svc.UpdateStatusAsync(new BankAccountUpdateStatusInput
            {
                ID = OldApiRequestParam.ReadInt(http, "ID"),
                Status = status,
                Remark = OldApiRequestParam.Read(http, "Remark") ?? string.Empty
            }, ct);
            return null;
        });
        return result;
    }

    /// <summary>撤回银行卡审核。对齐旧 bank/reset。</summary>
    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/member/bank/reset")]
    [NonUnify, DisplayName("站点后台银行卡撤回审核")]
    public async Task<OldApiResult> Bankreset()
    {
        var result = await RunAsync(async ct =>
        {
            await _svc.ResetAsync(OldApiRequestParam.ReadInt(App.HttpContext, "ID"), ct);
            return null;
        });
        return result;
    }

    private static async Task<OldApiResult> RunAsync(Func<CancellationToken, Task<object?>> action)
    {
        try
        {
            return OldApiResult.Ok(string.Empty, await action(App.HttpContext?.RequestAborted ?? default));
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
