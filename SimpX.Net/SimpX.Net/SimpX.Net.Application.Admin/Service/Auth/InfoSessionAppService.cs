using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 后台会话旧路径：/admin/info/*（除已挂 menu）。
/// </summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "info-session", Order = 12)]
public class InfoSessionAppService : IDynamicApiController, ITransient
{
    public const string PermissionId = UserMemberFormAppService.MemberListPermissionId;

    private readonly AdminSessionService _svc;

    public InfoSessionAppService(AdminSessionService svc) => _svc = svc;

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/info/get"), HttpGet("/admin/info/get")]
    [NonUnify, DisplayName("info-get")]
    public Task<OldApiResult> Get() => Run(ct => _svc.GetAsync(ToDict(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/info/init"), HttpGet("/admin/info/init")]
    [NonUnify, DisplayName("info-init")]
    public Task<OldApiResult> Init() => Run(ct => _svc.InitAsync(ToDict(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/info/channel"), HttpGet("/admin/info/channel")]
    [NonUnify, DisplayName("info-channel")]
    public Task<OldApiResult> Channel() => Run(ct => _svc.ChannelAsync(ToDict(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/info/newnotify"), HttpGet("/admin/info/newnotify")]
    [NonUnify, DisplayName("info-newnotify")]
    public Task<OldApiResult> Newnotify() => Run(ct => _svc.NewnotifyAsync(ToDict(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/info/menuall"), HttpGet("/admin/info/menuall")]
    [NonUnify, DisplayName("info-menuall")]
    public Task<OldApiResult> Menuall() => Run(ct => _svc.MenuallAsync(ToDict(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/info/password")]
    [NonUnify, DisplayName("info-password")]
    public Task<OldApiResult> Password() => Run(ct => _svc.PasswordAsync(ToDict(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/info/save")]
    [NonUnify, DisplayName("info-save")]
    public Task<OldApiResult> Save() => Run(ct => _svc.SaveAsync(ToDict(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/info/getcode"), HttpGet("/admin/info/getcode")]
    [NonUnify, DisplayName("info-getcode")]
    public Task<OldApiResult> Getcode() => Run(ct => _svc.GetcodeAsync(ToDict(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/info/savecode")]
    [NonUnify, DisplayName("info-savecode")]
    public Task<OldApiResult> Savecode() => Run(ct => _svc.SavecodeAsync(ToDict(), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/info/test"), HttpGet("/admin/info/test")]
    [NonUnify, DisplayName("info-test")]
    public Task<OldApiResult> Test() => Run(ct => _svc.TestAsync(ToDict(), ct));

    private static Dictionary<string, object?> ToDict()
    {
        var map = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (k, v) in OldApiFormReader.ReadJsonMap(App.HttpContext))
            map[k] = v.ToString();
        return map;
    }

    private static async Task<OldApiResult> Run(Func<CancellationToken, Task<object?>> action) => await OldApiFormReader.RunAsync(action);
}
