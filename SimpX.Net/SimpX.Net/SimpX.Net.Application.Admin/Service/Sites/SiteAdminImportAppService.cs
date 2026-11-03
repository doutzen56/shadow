using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 站点管理员导入旧路径：/admin/site/admin/{import*|create*|save*|edit*|delete*|batchdelete*}。
/// </summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "site-admin-import", Order = 32)]
public class SiteAdminImportAppService : IDynamicApiController, ITransient
{
    public const string PermissionId = UserMemberFormAppService.MemberListPermissionId;

    private readonly SiteAdminManageService _svc;

    public SiteAdminImportAppService(SiteAdminManageService svc) => _svc = svc;

    // ----- lists -----

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/admin/importuserlist"), HttpGet("/admin/site/admin/importuserlist")]
    [NonUnify, DisplayName("importuserlist")]
    public Task<OldApiResult> Importuserlist() =>
        Run(ct => _svc.ImportUserListAsync(ReadMemberPage(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/admin/importidcardlist"), HttpGet("/admin/site/admin/importidcardlist")]
    [NonUnify, DisplayName("importidcardlist")]
    public Task<OldApiResult> Importidcardlist() =>
        Run(ct => _svc.ImportIdCardListAsync(ReadMemberPage(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/admin/importbanklist"), HttpGet("/admin/site/admin/importbanklist")]
    [NonUnify, DisplayName("importbanklist")]
    public Task<OldApiResult> Importbanklist() =>
        Run(ct => _svc.ImportBankListAsync(ReadMemberPage(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/admin/importeventlist"), HttpGet("/admin/site/admin/importeventlist")]
    [NonUnify, DisplayName("importeventlist")]
    public Task<OldApiResult> Importeventlist() =>
        Run(ct => _svc.ImportEventListAsync(ReadSimplePage(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/admin/importmobilelist"), HttpGet("/admin/site/admin/importmobilelist")]
    [NonUnify, DisplayName("importmobilelist")]
    public Task<OldApiResult> Importmobilelist() =>
        Run(ct => _svc.ImportMobileListAsync(ReadSimplePage(), ct));

    // ----- create / apply -----

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/admin/createuser")]
    [NonUnify, DisplayName("createuser")]
    public Task<OldApiResult> Createuser() =>
        Run(ct => _svc.CreateUserAsync(new MemberImportApplyInput { Ids = ReadIds() }, ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/admin/createidcard")]
    [NonUnify, DisplayName("createidcard")]
    public Task<OldApiResult> Createidcard() =>
        Run(ct => _svc.CreateIdCardAsync(new MemberImportApplyInput { Ids = ReadIds() }, ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/admin/createbank")]
    [NonUnify, DisplayName("createbank")]
    public Task<OldApiResult> Createbank() =>
        Run(ct => _svc.CreateBankAsync(new MemberImportApplyInput { Ids = ReadIds() }, ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/admin/createevent")]
    [NonUnify, DisplayName("createevent")]
    public Task<OldApiResult> Createevent() =>
        Run(ct => _svc.CreateEventAsync(new SimpleImportBatchInput
        {
            Text = OldApiFormReader.ReadString(App.HttpContext, "Text", "Content", "data")
        }, ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/admin/createmobile")]
    [NonUnify, DisplayName("createmobile")]
    public Task<OldApiResult> Createmobile() =>
        Run(ct => _svc.CreateMobileAsync(new SimpleImportBatchInput
        {
            Text = OldApiFormReader.ReadString(App.HttpContext, "Text", "Content", "data", "Mobile")
        }, ct));

    // ----- save / edit -----

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/admin/saveuser"), HttpPost("/admin/site/admin/edituser")]
    [NonUnify, DisplayName("saveuser")]
    public Task<OldApiResult> Saveuser() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            _ = decimal.TryParse(OldApiFormReader.ReadString(http, "Balance"), out var bal);
            await _svc.SaveUserImportAsync(new UserImportSaveInput
            {
                ID = OldApiFormReader.ReadInt(http, "ID", "Id"),
                UserName = OldApiFormReader.ReadString(http, "UserName", "userName"),
                AgentName = OldApiFormReader.ReadString(http, "AgentName", "agentName"),
                Password = OldApiFormReader.ReadString(http, "Password", "password"),
                Mobile = OldApiFormReader.ReadString(http, "Mobile", "mobile"),
                Currency = OldApiFormReader.ReadInt(http, "Currency"),
                Balance = bal,
                AccountName = OldApiFormReader.ReadString(http, "AccountName")
            }, ct);
            return (object?)null;
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/admin/saveidcard"), HttpPost("/admin/site/admin/editidcard")]
    [NonUnify, DisplayName("saveidcard")]
    public Task<OldApiResult> Saveidcard() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            await _svc.SaveIdCardImportAsync(new IdCardImportSaveInput
            {
                ID = OldApiFormReader.ReadInt(http, "ID", "Id"),
                UserName = OldApiFormReader.ReadString(http, "UserName", "userName"),
                CardNumber = OldApiFormReader.ReadString(http, "CardNumber", "IdCard", "IDCard"),
                CardFront = OldApiFormReader.ReadString(http, "CardFront"),
                CardBack = OldApiFormReader.ReadString(http, "CardBack"),
                CardCity = OldApiFormReader.ReadString(http, "CardCity"),
                CardType = OldApiFormReader.ReadInt(http, "CardType")
            }, ct);
            return (object?)null;
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/admin/savebank"), HttpPost("/admin/site/admin/editbank")]
    [NonUnify, DisplayName("savebank")]
    public Task<OldApiResult> Savebank() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            await _svc.SaveBankImportAsync(new BankImportSaveInput
            {
                ID = OldApiFormReader.ReadInt(http, "ID", "Id"),
                UserName = OldApiFormReader.ReadString(http, "UserName", "userName"),
                Type = OldApiFormReader.ReadInt(http, "Type"),
                BankName = OldApiFormReader.ReadString(http, "BankName", "Bank", "bank"),
                Name = OldApiFormReader.ReadString(http, "Name", "AccountName"),
                Account = OldApiFormReader.ReadString(http, "Account", "AccountNumber"),
                AccountDetail = OldApiFormReader.ReadString(http, "AccountDetail"),
                IsVir = OldApiFormReader.ReadString(http, "IsVir"),
                BankAddress = OldApiFormReader.ReadString(http, "BankAddress"),
                BranchCode = OldApiFormReader.ReadString(http, "BranchCode"),
                BankInfo = OldApiFormReader.ReadString(http, "BankInfo")
            }, ct);
            return (object?)null;
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/admin/saveevent"), HttpPost("/admin/site/admin/editevent")]
    [HttpPost("/admin/site/admin/savemobile"), HttpPost("/admin/site/admin/editmobile")]
    [NonUnify, DisplayName("saveevent-mobile")]
    public Task<OldApiResult> Saveeventormobile() =>
        Run(async ct =>
        {
            // 活动/手机导入行编辑在 Shared 无独立 Save；创建走 create*，删走 delete*
            _ = OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id");
            return (object?)null;
        });

    // ----- delete -----

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/admin/deleteuser")]
    [NonUnify, DisplayName("deleteuser")]
    public Task<OldApiResult> Deleteuser() =>
        Run(async ct =>
        {
            await _svc.DeleteUserImportAsync(OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id"), ct);
            return (object?)null;
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/admin/deleteidcard")]
    [NonUnify, DisplayName("deleteidcard")]
    public Task<OldApiResult> Deleteidcard() =>
        Run(async ct =>
        {
            await _svc.DeleteIdCardImportAsync(OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id"), ct);
            return (object?)null;
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/admin/deletebank")]
    [NonUnify, DisplayName("deletebank")]
    public Task<OldApiResult> Deletebank() =>
        Run(async ct =>
        {
            await _svc.DeleteBankImportAsync(OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id"), ct);
            return (object?)null;
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/admin/deleteevent")]
    [NonUnify, DisplayName("deleteevent")]
    public Task<OldApiResult> Deleteevent() =>
        Run(async ct =>
        {
            await _svc.DeleteEventImportAsync(OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id"), ct);
            return (object?)null;
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/admin/deletemobile")]
    [NonUnify, DisplayName("deletemobile")]
    public Task<OldApiResult> Deletemobile() =>
        Run(async ct =>
        {
            await _svc.DeleteMobileImportAsync(OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id"), ct);
            return (object?)null;
        });

    // ----- batch delete -----

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/admin/batchdeleteuser")]
    [NonUnify, DisplayName("batchdeleteuser")]
    public Task<OldApiResult> Batchdeleteuser() =>
        Run(async ct =>
        {
            await _svc.BatchDeleteUserImportAsync(new SiteAdminBatchIdsInput { Ids = ReadIds() }, ct);
            return (object?)null;
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/admin/batchdeleteidcard")]
    [NonUnify, DisplayName("batchdeleteidcard")]
    public Task<OldApiResult> Batchdeleteidcard() =>
        Run(async ct =>
        {
            await _svc.BatchDeleteIdCardImportAsync(new SiteAdminBatchIdsInput { Ids = ReadIds() }, ct);
            return (object?)null;
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/admin/batchdeletebank")]
    [NonUnify, DisplayName("batchdeletebank")]
    public Task<OldApiResult> Batchdeletebank() =>
        Run(async ct =>
        {
            await _svc.BatchDeleteBankImportAsync(new SiteAdminBatchIdsInput { Ids = ReadIds() }, ct);
            return (object?)null;
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/admin/batchdeleteevent")]
    [NonUnify, DisplayName("batchdeleteevent")]
    public Task<OldApiResult> Batchdeleteevent() =>
        Run(async ct =>
        {
            await _svc.BatchDeleteEventImportAsync(new SiteAdminBatchIdsInput { Ids = ReadIds() }, ct);
            return (object?)null;
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/admin/batchdeletemobile")]
    [NonUnify, DisplayName("batchdeletemobile")]
    public Task<OldApiResult> Batchdeletemobile() =>
        Run(async ct =>
        {
            await _svc.BatchDeleteMobileImportAsync(new SiteAdminBatchIdsInput { Ids = ReadIds() }, ct);
            return (object?)null;
        });

    private static MemberImportPageInput ReadMemberPage()
    {
        var http = App.HttpContext;
        var (page, size) = OldApiShowResult.ReadPaging(http);
        return new MemberImportPageInput
        {
            Page = page,
            PageSize = size,
            UserName = OldApiFormReader.ReadString(http, "UserName", "User", "user"),
            Status = OldApiFormReader.ReadString(http, "Status", "status")
        };
    }

    private static SimpleImportPageInput ReadSimplePage()
    {
        var http = App.HttpContext;
        var (page, size) = OldApiShowResult.ReadPaging(http);
        return new SimpleImportPageInput { Page = page, PageSize = size };
    }

    private static List<int> ReadIds()
    {
        var raw = OldApiFormReader.ReadString(App.HttpContext, "ID", "Ids", "ids");
        return (raw ?? string.Empty)
            .Split([',', ';', '|', ' '], StringSplitOptions.RemoveEmptyEntries)
            .Select(s => int.TryParse(s.Trim(), out var n) ? n : 0)
            .Where(n => n > 0)
            .Distinct()
            .ToList();
    }

    private static async Task<OldApiResult> Run<T>(Func<CancellationToken, Task<T>> action) => await OldApiFormReader.RunAsync(action);
}
