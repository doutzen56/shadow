using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 第三方登录配置旧路径：/admin/site/auth/*。
/// </summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "site-auth", Order = 29)]
public class SiteAuthAppService : IDynamicApiController, ITransient
{
    public const string PermissionId = UserMemberFormAppService.MemberListPermissionId;

    private readonly SiteAuthorizationService _authz;

    public SiteAuthAppService(SiteAuthorizationService authz) => _authz = authz;

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/auth/getsetting"), HttpGet("/admin/site/auth/getsetting")]
    [NonUnify, DisplayName("site-auth-getsetting")]
    public Task<OldApiResult> Getsetting() =>
        Run(ct =>
        {
            var typeRaw = OldApiFormReader.ReadString(App.HttpContext, "Type", "type");
            var type = ResolveAuthType(typeRaw);
            return _authz.GetSettingAsync(type, ct);
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/site/auth/savesetting")]
    [NonUnify, DisplayName("site-auth-savesetting")]
    public Task<OldApiResult> Savesetting() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            var type = ResolveAuthType(OldApiFormReader.ReadString(http, "Type", "type"));
            var isOpen = OldApiFormReader.ReadInt(http, "IsOpen", "isOpen") == 1;
            var setting = OldApiFormReader.ReadString(http, "Setting", "setting");
            await _authz.SaveSettingAsync(type, isOpen, setting, ct);
            return (object?)null;
        });

    private static int ResolveAuthType(string raw)
    {
        if (int.TryParse(raw, out var n))
            return n;
        if (Enum.TryParse<SiteAuthType>(raw, true, out var e))
            return (int)e;
        return 0;
    }

    private static async Task<OldApiResult> Run<T>(Func<CancellationToken, Task<T>> action) => await OldApiFormReader.RunAsync(action);
}
