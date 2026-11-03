using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 站点语言旧路径：/admin/site/language/*。
/// </summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "site-language", Order = 30)]
public class SiteLanguageAppService : IDynamicApiController, ITransient
{
    public const string PermissionId = UserMemberFormAppService.MemberListPermissionId;

    private readonly SiteLanguageService _lang;

    public SiteLanguageAppService(SiteLanguageService lang) => _lang = lang;

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/language/list"), HttpGet("/admin/site/language/list")]
    [HttpPost("/admin/site/language/siteinfo"), HttpGet("/admin/site/language/siteinfo")]
    [NonUnify, DisplayName("site-language-list")]
    public Task<OldApiResult> List() =>
        Run(ct =>
        {
            var http = App.HttpContext;
            return _lang.ListAsync(
                OldApiFormReader.ReadInt(http, "Category", "category") is var c && c != 0 ? c : -1,
                OldApiFormReader.ReadInt(http, "ExpandID", "expandID"),
                ct);
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/language/info"), HttpGet("/admin/site/language/info")]
    [NonUnify, DisplayName("site-language-info")]
    public Task<OldApiResult> Info() =>
        Run(ct => _lang.InfoAsync(OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id"), ct));

    /// <summary>富文本编辑器内容。对齐旧 editor/info。</summary>
    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/editor/info"), HttpGet("/admin/site/editor/info")]
    [NonUnify, DisplayName("site-editor-info")]
    public Task<OldApiResult> Editorinfo() =>
        Run(ct =>
        {
            var http = App.HttpContext;
            return _lang.EditorInfoAsync(
                OldApiFormReader.ReadInt(http, "Type", "Category", "type"),
                OldApiFormReader.ReadInt(http, "Language", "language"),
                OldApiFormReader.ReadInt(http, "ExpandId", "ExpandID", "expandId"),
                ct);
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/language/save")]
    [NonUnify, DisplayName("site-language-save")]
    public Task<OldApiResult> Save() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            var id = await _lang.SaveAsync(
                OldApiFormReader.ReadInt(http, "ID", "Id"),
                OldApiFormReader.ReadInt(http, "Category", "category"),
                OldApiFormReader.ReadInt(http, "Language", "language"),
                OldApiFormReader.ReadInt(http, "ExpandID", "expandID"),
                OldApiFormReader.ReadString(http, "Content", "content"),
                ct);
            return (object?)id;
        });

    private static async Task<OldApiResult> Run<T>(Func<CancellationToken, Task<T>> action) => await OldApiFormReader.RunAsync(action);
}