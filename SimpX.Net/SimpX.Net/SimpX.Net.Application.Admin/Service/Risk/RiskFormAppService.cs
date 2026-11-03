using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 风控旧路径：/admin/risk/{ip|device|control|user}/*（接 Shared 真逻辑）。
/// </summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "risk", Order = 40)]
public class RiskFormAppService : IDynamicApiController, ITransient
{
    public const string PermissionId = UserMemberFormAppService.MemberListPermissionId;

    private readonly IpBlackService _ip;
    private readonly BlackRegionService _region;
    private readonly SameRiskService _same;
    private readonly RiskControlService _control;
    private readonly BlackMobileService _mobile;
    private readonly BlackKycService _kyc;

    public RiskFormAppService(
        IpBlackService ip,
        BlackRegionService region,
        SameRiskService same,
        RiskControlService control,
        BlackMobileService mobile,
        BlackKycService kyc)
    {
        _ip = ip;
        _region = region;
        _same = same;
        _control = control;
        _mobile = mobile;
        _kyc = kyc;
    }

    // ----- IP / Device 国别与黑名单 -----

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/risk/ip/getblackregion"), HttpGet("/admin/risk/ip/getblackregion")]
    [HttpPost("/admin/risk/device/getblackregion"), HttpGet("/admin/risk/device/getblackregion")]
    [NonUnify, DisplayName("risk-getblackregion")]
    public Task<OldApiResult> Getblackregion() =>
        Run(ct => _region.ListAsync(ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/risk/ip/saveblackregion"), HttpPost("/admin/risk/device/saveblackregion")]
    [NonUnify, DisplayName("risk-saveblackregion")]
    public Task<OldApiResult> Saveblackregion() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            var raw = OldApiFormReader.ReadString(http, "Value", "Region", "region", "Name");
            // 只读 Save/save，禁止回落到 Value（否则地区名会被当成 Save 标志）。
            var saveFlag = OldApiFormReader.ReadString(http, "Save", "save");
            // 旧站常见：Save=1 加入；Save=0 移除；若无 Save 键则默认加入
            var save = string.IsNullOrEmpty(saveFlag)
                ? true
                : saveFlag is "1" or "true" or "True" || (int.TryParse(saveFlag, out var n) && n == 1);
            await _region.SaveAsync(new BlackRegionSaveInput { Value = raw, Save = save }, ct);
            return (object?)null;
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/risk/ip/blacklist"), HttpGet("/admin/risk/ip/blacklist")]
    [HttpPost("/admin/risk/device/blacklist"), HttpGet("/admin/risk/device/blacklist")]
    [NonUnify, DisplayName("risk-blacklist")]
    public Task<OldApiResult> Blacklist() =>
        Run(async ct => OldApiShowResult.OfList(await _ip.BlackListAsync(
            OldApiRequestParam.Read(App.HttpContext, "User")
            ?? OldApiRequestParam.Read(App.HttpContext, "Keyword"),
            ct)));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/risk/ip/blackinfo"), HttpGet("/admin/risk/ip/blackinfo")]
    [HttpPost("/admin/risk/device/blackinfo"), HttpGet("/admin/risk/device/blackinfo")]
    [NonUnify, DisplayName("risk-blackinfo")]
    public Task<OldApiResult> Blackinfo() =>
        Run(ct => _ip.BlackInfoAsync(OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id"), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/risk/ip/editbalckip"), HttpGet("/admin/risk/ip/editbalckip")]
    [NonUnify, DisplayName("risk-editbalckip")]
    public Task<OldApiResult> Editbalckip() =>
        Run(ct => _ip.EditBalckIpAsync(OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id"), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/risk/ip/saveblackip"), HttpPost("/admin/risk/device/saveblackip")]
    [NonUnify, DisplayName("risk-saveblackip")]
    public Task<OldApiResult> Saveblackip() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            var ip = OldApiFormReader.ReadString(http, "StartIP", "IP", "Ip");
            await _ip.SaveBlackIpAsync(new IpBlackSaveInput
            {
                ID = OldApiFormReader.ReadInt(http, "ID", "Id"),
                Name = OldApiFormReader.ReadString(http, "Name", "name"),
                StartIP = ip,
                EndIP = OldApiFormReader.ReadString(http, "EndIP", "endIP"),
                Remark = OldApiFormReader.ReadString(http, "Remark", "Demo", "demo")
            }, ct);
            return (object?)null;
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/risk/ip/deleteblackip"), HttpPost("/admin/risk/device/deleteblackip")]
    [NonUnify, DisplayName("risk-deleteblackip")]
    public Task<OldApiResult> Deleteblackip() =>
        Run(async ct =>
        {
            await _ip.DeleteBlackIpAsync(OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id"), ct);
            return (object?)null;
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/risk/ip/same"), HttpGet("/admin/risk/ip/same")]
    [NonUnify, DisplayName("risk-ip-same")]
    public Task<OldApiResult> Ipsame() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            DateTime? start = null, end = null;
            if (DateTime.TryParse(OldApiFormReader.ReadString(http, "StartAt", "startAt", "StartDate"), out var s))
                start = s;
            if (DateTime.TryParse(OldApiFormReader.ReadString(http, "EndAt", "endAt", "EndDate"), out var e))
                end = e;
            var rows = await _same.SameIpAsync(new SameRiskQueryInput
            {
                User = OldApiFormReader.ReadString(http, "User", "user"),
                IP = OldApiFormReader.ReadString(http, "IP", "Ip"),
                StartAt = start,
                EndAt = end
            }, ct);
            return OldApiShowResult.OfList(rows);
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/risk/device/same"), HttpGet("/admin/risk/device/same")]
    [NonUnify, DisplayName("risk-device-same")]
    public Task<OldApiResult> Devicesame() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            DateTime? start = null, end = null;
            if (DateTime.TryParse(OldApiFormReader.ReadString(http, "StartAt", "startAt", "StartDate"), out var s))
                start = s;
            if (DateTime.TryParse(OldApiFormReader.ReadString(http, "EndAt", "endAt", "EndDate"), out var e))
                end = e;
            var rows = await _same.SameDeviceAsync(new SameRiskQueryInput
            {
                User = OldApiFormReader.ReadString(http, "User", "user"),
                Device = OldApiFormReader.ReadString(http, "Device", "device"),
                StartAt = start,
                EndAt = end
            }, ct);
            return OldApiShowResult.OfList(rows);
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/risk/device/detail"), HttpGet("/admin/risk/device/detail")]
    [HttpPost("/admin/risk/ip/detail"), HttpGet("/admin/risk/ip/detail")]
    [NonUnify, DisplayName("risk-detail")]
    public Task<OldApiResult> Detail() =>
        Run(async ct =>
        {
            var rows = await _same.SameDeviceAsync(new SameRiskQueryInput
            {
                User = OldApiFormReader.ReadString(App.HttpContext, "User", "user"),
                Device = OldApiFormReader.ReadString(App.HttpContext, "Device", "device"),
                IP = OldApiFormReader.ReadString(App.HttpContext, "IP", "Ip")
            }, ct);
            return OldApiShowResult.OfList(rows);
        });

    // ----- control -----

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/risk/control/profit"), HttpGet("/admin/risk/control/profit")]
    [NonUnify, DisplayName("risk-control-profit")]
    public Task<OldApiResult> Profit() =>
        Run(async ct =>
        {
            var outp = await _control.ProfitAsync(ReadControlQuery(), ct);
            return OldApiShowResult.OfList(outp.Rows ?? []);
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/risk/control/game"), HttpGet("/admin/risk/control/game")]
    [NonUnify, DisplayName("risk-control-game")]
    public Task<OldApiResult> Game() =>
        Run(async ct =>
        {
            var outp = await _control.GameProfitAsync(ReadControlQuery(), ct);
            return OldApiShowResult.OfList(outp.Rows ?? []);
        });

    // ----- user（风险会员列表等）-----

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/risk/user/list"), HttpGet("/admin/risk/user/list")]
    [NonUnify, DisplayName("risk-user-list")]
    public Task<OldApiResult> Userlist() =>
        Run(async ct => OldApiShowResult.OfList(await _control.ListAsync(ReadControlQuery(), ct)));

    /// <summary>批量处理风险会员：按 RiskID 删除记录（移出列表）。对齐旧 /admin/risk/user/updatestatus。</summary>
    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/risk/user/updatestatus")]
    [NonUnify, DisplayName("risk-user-updatestatus")]
    public Task<OldApiResult> Userupdatestatus() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            var ids = new List<int>();
            foreach (var key in new[] { "IDs", "ids", "RiskIDs", "RiskID", "ID", "Id" })
            {
                var raw = OldApiFormReader.ReadString(http, key);
                if (string.IsNullOrWhiteSpace(raw)) continue;
                foreach (var part in raw.Split([',', ';', '|'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    if (int.TryParse(part, out var id) && id > 0)
                        ids.Add(id);
                }
            }

            var single = OldApiFormReader.ReadInt(http, "RiskID", "ID", "Id");
            if (single > 0) ids.Add(single);
            ids = ids.Distinct().ToList();
            var n = await _control.UpdateStatusAsync(ids, ct);
            return (object?)n;
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/risk/user/info"), HttpGet("/admin/risk/user/info")]
    [NonUnify, DisplayName("risk-user-info")]
    public Task<OldApiResult> Userinfo() =>
        Run(ct => _control.InfoAsync(OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id", "RiskID"), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/risk/user/ip"), HttpGet("/admin/risk/user/ip")]
    [NonUnify, DisplayName("risk-user-ip")]
    public Task<OldApiResult> Userip() =>
        Run(ct => _control.IpAsync(OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id", "RiskID"), ct));

    // ----- mobile / kyc 高频 -----

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/risk/device/addbalckmobile")]
    [NonUnify, DisplayName("risk-addbalckmobile")]
    public Task<OldApiResult> Addbalckmobile() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            await _mobile.AddBalckMobileAsync(
                OldApiFormReader.ReadString(http, "Mobile", "mobile"),
                OldApiFormReader.ReadString(http, "Demo", "demo", "Remark"),
                ct);
            return (object?)null;
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/risk/device/editbalckmobile"), HttpGet("/admin/risk/device/editbalckmobile")]
    [NonUnify, DisplayName("risk-editbalckmobile")]
    public Task<OldApiResult> Editbalckmobile() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            await _mobile.EditBalckMobileAsync(new BlackMobileSaveInput
            {
                ID = OldApiFormReader.ReadInt(http, "ID", "Id"),
                BlackMobile = OldApiFormReader.ReadString(http, "BlackMobile", "Mobile", "mobile"),
                Demo = OldApiFormReader.ReadString(http, "Demo", "demo", "Remark")
            }, ct);
            return (object?)null;
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/risk/device/deletebalckmobile")]
    [NonUnify, DisplayName("risk-deletebalckmobile")]
    public Task<OldApiResult> Deletebalckmobile() =>
        Run(async ct =>
        {
            await _mobile.DeleteBalckMobileAsync(OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id"), ct);
            return (object?)null;
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/risk/device/createbalckkyc")]
    [NonUnify, DisplayName("risk-createbalckkyc")]
    public Task<OldApiResult> Createbalckkyc() =>
        Run(ct => _kyc.CreateBalckKycAsync());

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/risk/device/editbalckkyc"), HttpGet("/admin/risk/device/editbalckkyc")]
    [NonUnify, DisplayName("risk-editbalckkyc")]
    public Task<OldApiResult> Editbalckkyc() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            await _kyc.EditBalckKycAsync(new BlackKycSaveInput
            {
                ID = OldApiFormReader.ReadInt(http, "ID", "Id"),
                BlackKyc = OldApiFormReader.ReadString(http, "BlackKyc", "Number", "Kyc", "kyc"),
                Demo = OldApiFormReader.ReadString(http, "Demo", "demo", "Remark")
            }, ct);
            return (object?)null;
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/risk/device/deletebalckkyc")]
    [HttpPost("/admin/risk/ip/deletebalckkyc")]
    [NonUnify, DisplayName("risk-deletebalckkyc")]
    public Task<OldApiResult> Deletebalckkyc() =>
        Run(async ct =>
        {
            await _kyc.DeleteBalckKycAsync(OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id"), ct);
            return (object?)null;
        });

    // ----- risk user 弹窗深路径 -----

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/risk/user/ipdifuser"), HttpGet("/admin/risk/user/ipdifuser")]
    [NonUnify, DisplayName("risk-user-ipdifuser")]
    public Task<OldApiResult> Useripdifuser() =>
        Run(ct => _control.IpDifUserAsync(OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id", "RiskID"), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/risk/user/userdifip"), HttpGet("/admin/risk/user/userdifip")]
    [NonUnify, DisplayName("risk-user-userdifip")]
    public Task<OldApiResult> Useruserdifip() =>
        Run(ct => _control.UserDifIpAsync(OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id", "RiskID"), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/risk/user/login"), HttpGet("/admin/risk/user/login")]
    [NonUnify, DisplayName("risk-user-login")]
    public Task<OldApiResult> Userlogin() =>
        Run(ct => _control.LoginAsync(OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id", "RiskID"), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/risk/user/amount"), HttpGet("/admin/risk/user/amount")]
    [NonUnify, DisplayName("risk-user-amount")]
    public Task<OldApiResult> Useramount() =>
        Run(ct => _control.AmountAsync(OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id", "RiskID"), ct));

    // ----- mobile / kyc 批量与导入别名（ip|device 双挂）-----

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/risk/device/createbalckmobile"), HttpPost("/admin/risk/ip/createbalckmobile")]
    [NonUnify, DisplayName("risk-createbalckmobile")]
    public Task<OldApiResult> Createbalckmobile() =>
        Run(ct => _mobile.CreateBalckMobileAsync());

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/risk/ip/addbalckmobile")]
    [NonUnify, DisplayName("risk-ip-addbalckmobile")]
    public Task<OldApiResult> Ipaddbalckmobile() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            await _mobile.AddBalckMobileAsync(
                OldApiFormReader.ReadString(http, "Mobile", "mobile"),
                OldApiFormReader.ReadString(http, "Demo", "demo", "Remark"),
                ct);
            return (object?)null;
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/risk/ip/addbalckkyc")]
    [NonUnify, DisplayName("risk-ip-addbalckkyc")]
    public Task<OldApiResult> Ipaddbalckkyc() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            await _kyc.AddBalckKycAsync(
                OldApiFormReader.ReadString(http, "BlackKyc", "Number", "Kyc"),
                OldApiFormReader.ReadString(http, "Demo", "demo", "Remark"),
                cancellationToken: ct);
            return (object?)null;
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/risk/ip/createbalckkyc")]
    [NonUnify, DisplayName("risk-ip-createbalckkyc")]
    public Task<OldApiResult> Ipcreatebalckkyc() =>
        Run(ct => _kyc.CreateBalckKycAsync());

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/risk/ip/editbalckmobile"), HttpGet("/admin/risk/ip/editbalckmobile")]
    [NonUnify, DisplayName("risk-ip-editbalckmobile")]
    public Task<OldApiResult> Ipeditbalckmobile() => Editbalckmobile();

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/risk/ip/editbalckkyc"), HttpGet("/admin/risk/ip/editbalckkyc")]
    [NonUnify, DisplayName("risk-ip-editbalckkyc")]
    public Task<OldApiResult> Ipeditbalckkyc() => Editbalckkyc();

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/risk/ip/deletebalckmobile")]
    [NonUnify, DisplayName("risk-ip-deletebalckmobile")]
    public Task<OldApiResult> Ipdeletebalckmobile() => Deletebalckmobile();

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/risk/device/savebalckmobile"), HttpPost("/admin/risk/ip/savebalckmobile")]
    [NonUnify, DisplayName("risk-savebalckmobile")]
    public Task<OldApiResult> Savebalckmobile() =>
        Run(async ct =>
        {
            await _mobile.SaveBalckMobileImportAsync(OldApiFormReader.ReadString(App.HttpContext, "Url", "url", "File"));
            return (object?)null;
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/risk/device/savebalckkyc"), HttpPost("/admin/risk/ip/savebalckkyc")]
    [NonUnify, DisplayName("risk-savebalckkyc")]
    public Task<OldApiResult> Savebalckkyc() =>
        Run(async ct =>
        {
            await _kyc.SaveBalckKycImportAsync(OldApiFormReader.ReadString(App.HttpContext, "Url", "url", "File"));
            return (object?)null;
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/risk/device/importbalckmobilelist"), HttpGet("/admin/risk/device/importbalckmobilelist")]
    [HttpPost("/admin/risk/ip/importbalckmobilelist"), HttpGet("/admin/risk/ip/importbalckmobilelist")]
    [NonUnify, DisplayName("risk-importbalckmobilelist")]
    public Task<OldApiResult> Importbalckmobilelist() =>
        Run(async ct => OldApiShowResult.OfList(await _mobile.ImportBalckMobileListAsync(
            OldApiFormReader.ReadString(App.HttpContext, "UserName", "Mobile", "user"), ct)));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/risk/device/importbalckkyclist"), HttpGet("/admin/risk/device/importbalckkyclist")]
    [HttpPost("/admin/risk/ip/importbalckkyclist"), HttpGet("/admin/risk/ip/importbalckkyclist")]
    [NonUnify, DisplayName("risk-importbalckkyclist")]
    public Task<OldApiResult> Importbalckkyclist() =>
        Run(async ct => OldApiShowResult.OfList(await _kyc.ImportBalckKycListAsync(
            OldApiFormReader.ReadString(App.HttpContext, "UserName", "BlackKyc", "user"), ct)));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/risk/device/batchdeletebalckmobile"), HttpPost("/admin/risk/ip/batchdeletebalckmobile")]
    [NonUnify, DisplayName("risk-batchdeletebalckmobile")]
    public Task<OldApiResult> Batchdeletebalckmobile() =>
        Run(async ct =>
        {
            await _mobile.BatchDeleteBalckMobileAsync(ReadIdArray(), ct);
            return (object?)null;
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/risk/device/batchlockbalckmobile"), HttpPost("/admin/risk/ip/batchlockbalckmobile")]
    [NonUnify, DisplayName("risk-batchlockbalckmobile")]
    public Task<OldApiResult> Batchlockbalckmobile() =>
        Run(async ct =>
        {
            await _mobile.BatchLockBalckMobileAsync(ReadIdArray(), ct);
            return (object?)null;
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/risk/device/batchdeletebalckkyc"), HttpPost("/admin/risk/ip/batchdeletebalckkyc")]
    [NonUnify, DisplayName("risk-batchdeletebalckkyc")]
    public Task<OldApiResult> Batchdeletebalckkyc() =>
        Run(async ct =>
        {
            await _kyc.BatchDeleteBalckKycAsync(ReadIdArray(), ct);
            return (object?)null;
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/risk/ip/checkblackkyc"), HttpGet("/admin/risk/ip/checkblackkyc")]
    [NonUnify, DisplayName("risk-checkblackkyc")]
    public Task<OldApiResult> Checkblackkyc() =>
        Run(ct => _kyc.CheckBlackKycAsync(
            OldApiFormReader.ReadString(App.HttpContext, "BlackKyc", "Number", "Kyc"), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/risk/ip/batchsaveblackip")]
    [NonUnify, DisplayName("risk-batchsaveblackip")]
    public Task<OldApiResult> Batchsaveblackip() =>
        Run(async ct =>
        {
            var raw = OldApiFormReader.ReadString(App.HttpContext, "IP", "Ips", "ips");
            var items = (raw ?? string.Empty)
                .Split([',', ';', '|', '\n', '\r', ' '], StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .Select(s => new IpBlackSaveInput { Name = s, StartIP = s, EndIP = s })
                .ToList();
            await _ip.BatchSaveBlackIpAsync(
                items,
                OldApiFormReader.ReadString(App.HttpContext, "Url", "url"),
                ct);
            return (object?)null;
        });

    private static int[] ReadIdArray()
    {
        var raw = OldApiFormReader.ReadString(App.HttpContext, "ID", "Ids", "ids");
        return (raw ?? string.Empty)
            .Split([',', ';', '|', ' '], StringSplitOptions.RemoveEmptyEntries)
            .Select(s => int.TryParse(s.Trim(), out var n) ? n : 0)
            .Where(n => n > 0)
            .ToArray();
    }

    private static RiskControlQueryInput ReadControlQuery()
    {
        var http = App.HttpContext;
        DateTime? start = null, end = null;
        if (DateTime.TryParse(OldApiFormReader.ReadString(http, "StartAt", "startAt", "StartDate"), out var s))
            start = s;
        if (DateTime.TryParse(OldApiFormReader.ReadString(http, "EndAt", "endAt", "EndDate"), out var e))
            end = e;
        return new RiskControlQueryInput
        {
            User = OldApiFormReader.ReadString(http, "User", "user"),
            StartAt = start,
            EndAt = end,
            Status = OldApiFormReader.ReadString(http, "Status", "status", "Type")
        };
    }

    private static async Task<OldApiResult> Run<T>(Func<CancellationToken, Task<T>> action) => await OldApiFormReader.RunAsync(action);
}
