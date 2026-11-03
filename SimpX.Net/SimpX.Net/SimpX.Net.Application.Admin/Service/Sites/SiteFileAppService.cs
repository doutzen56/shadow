using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 站点报表文件：/admin/site/file/*。
/// </summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "site-file", Order = 31)]
public class SiteFileAppService : IDynamicApiController, ITransient
{
    public const string PermissionId = UserMemberFormAppService.MemberListPermissionId;

    private readonly SiteFileDownloadService _files;

    public SiteFileAppService(SiteFileDownloadService files) => _files = files;

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/file/report"), HttpGet("/admin/site/file/report")]
    [NonUnify, DisplayName("site-file-report")]
    public Task<OldApiResult> Report() =>
        Run(ct => _files.ReportListAsync(ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/file/downfile")]
    [NonUnify, DisplayName("site-file-downfile")]
    public Task<OldApiResult> Downfile() =>
        Run(ct => _files.DownfileAsync(OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id"), ct));

    private static async Task<OldApiResult> Run<T>(Func<CancellationToken, Task<T>> action) => await OldApiFormReader.RunAsync(action);
}
