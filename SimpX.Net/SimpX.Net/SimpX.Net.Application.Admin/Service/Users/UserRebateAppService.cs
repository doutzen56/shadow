using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>会员返水设定旧路径：/admin/user/member/rebate/*。</summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "user-rebate", Order = 28)]
public class UserRebateAppService : IDynamicApiController, ITransient
{
    public const string PermissionId = UserMemberFormAppService.MemberListPermissionId;

    private readonly RebateSettingService _svc;

    public UserRebateAppService(RebateSettingService svc) => _svc = svc;

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/member/rebate/list"), HttpGet("/admin/user/member/rebate/list")]
    [NonUnify, DisplayName("member-rebate-list")]
    public Task<OldApiResult> List() =>
        Run(async ct =>
        {
            var list = await _svc.ListAsync(ct);
            var groupId = OldApiFormReader.ReadInt(App.HttpContext, "GroupID", "GroupId");
            // 旧站：list.Where(t => t.Group.Contains(GroupID))
            if (groupId > 0)
            {
                var key = groupId.ToString();
                list = list
                    .Where(t =>
                    {
                        var raw = t.Groups ?? string.Empty;
                        return raw.Split(new[] { ',', '|', ';' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => s.Trim())
                            .Any(s => s == key);
                    })
                    .ToList();
            }
            return list;
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/member/rebate/info"), HttpGet("/admin/user/member/rebate/info")]
    [NonUnify, DisplayName("member-rebate-info")]
    public Task<OldApiResult> Info() =>
        Run(ct => _svc.DetailAsync(OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id"), ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/member/rebate/save")]
    [NonUnify, DisplayName("member-rebate-save")]
    public Task<OldApiResult> Save() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            var enabledRaw = OldApiFormReader.ReadString(http, "Enabled", "IsOpen");
            var enabled = string.IsNullOrEmpty(enabledRaw) || OldApiFormReader.ReadBool(http, "Enabled", "IsOpen");
            var id = await _svc.SaveAsync(new RebateSettingSaveInput
            {
                ID = OldApiFormReader.ReadInt(http, "ID", "Id"),
                Name = OldApiFormReader.ReadString(http, "Name"),
                Groups = OldApiFormReader.ReadString(http, "Groups", "Group"),
                Levels = OldApiFormReader.ReadString(http, "Levels", "Level"),
                BetMoney = OldApiFormReader.ReadDecimal(http, "BetMoney"),
                MinMoney = OldApiFormReader.ReadDecimal(http, "MinMoney"),
                MaxMoney = OldApiFormReader.ReadDecimal(http, "MaxMoney"),
                IsAuto = OldApiFormReader.ReadBool(http, "IsAuto"),
                Enabled = enabled,
                Type = OldApiFormReader.ReadString(http, "Type"),
                SettingString = OldApiFormReader.ReadString(http, "SettingString", "Setting")
            }, ct);
            return (object?)id;
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/member/rebate/delete")]
    [NonUnify, DisplayName("member-rebate-delete")]
    public Task<OldApiResult> Delete() =>
        Run(async ct =>
        {
            await _svc.DeleteAsync(OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id"), ct);
            return (object?)null;
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/member/rebate/returntype"), HttpGet("/admin/user/member/rebate/returntype")]
    [NonUnify, DisplayName("member-rebate-returntype")]
    public Task<OldApiResult> Returntype() =>
        Run(_ => Task.FromResult<object?>(_svc.ReturnType()));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/member/rebate/updateauto")]
    [NonUnify, DisplayName("member-rebate-updateauto")]
    public Task<OldApiResult> Updateauto() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            await _svc.UpdateAutoAsync(
                OldApiFormReader.ReadInt(http, "ID", "Id"),
                OldApiFormReader.ReadBool(http, "IsAuto", "Value", "Auto"),
                ct);
            return (object?)null;
        });

    /// <summary>切换启用。对应旧 /admin/user/member/rebate/updateenabled。</summary>
    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/member/rebate/updateenabled")]
    [NonUnify, DisplayName("member-rebate-updateenabled")]
    public Task<OldApiResult> Updateenabled() =>
        Run(async ct =>
        {
            var next = await _svc.ToggleEnabledAsync(
                OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id"),
                ct);
            return (object?)next;
        });

    private static async Task<OldApiResult> Run<T>(Func<CancellationToken, Task<T>> action) => await OldApiFormReader.RunAsync(action);
}
