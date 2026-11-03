using Furion.FriendlyException;
using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 站点后台兼容会员分组：/admin/user/member/group/*。
/// </summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "user-group", Order = 24)]
public class UserGroupFormAppService : IDynamicApiController, ITransient
{
    private readonly UserGroupService _groups;

    /// <summary>
    /// 注入分组服务。
    /// </summary>
    /// <param name="groups">分组查询</param>
    public UserGroupFormAppService(UserGroupService groups)
    {
        _groups = groups;
    }

    /// <summary>
    /// 会员分组列表。对应旧版 /admin/user/member/group/list。
    /// </summary>
    /// <returns>旧版返回格式</returns>
    [AllowAnonymous]
    [OldApiPermission(UserGroupService.GroupListPermissionId)]
    [HttpPost("/admin/user/member/group/list")]
    [HttpGet("/admin/user/member/group/list")]
    [NonUnify]
    [DisplayName("站点后台会员分组列表")]
    public async Task<OldApiResult> List()
    {
        var result = await _groups.ListOldApiAsync(App.HttpContext?.RequestAborted ?? default);
        return result;
    }

    /// <summary>分组详情。对应旧 info。</summary>
    [AllowAnonymous]
    [OldApiPermission(UserGroupService.GroupListPermissionId)]
    [HttpPost("/admin/user/member/group/info")]
    [HttpGet("/admin/user/member/group/info")]
    [NonUnify]
    [DisplayName("站点后台会员分组详情")]
    public async Task<OldApiResult> Info()
    {
        var id = OldApiRequestParam.ReadInt(App.HttpContext, "ID");
        var result = await RunAsync(async ct => await _groups.InfoAsync(id, ct));
        return result;
    }

    /// <summary>刷新组内人数。对应旧 updatemember。</summary>
    [AllowAnonymous]
    [OldApiPermission(UserGroupService.GroupEditPermissionId)]
    [HttpPost("/admin/user/member/group/updatemember")]
    [NonUnify]
    [DisplayName("站点后台刷新分组人数")]
    public async Task<OldApiResult> Updatemember()
    {
        var id = OldApiRequestParam.ReadInt(App.HttpContext, "ID");
        var result = await RunAsync(async ct =>
        {
            var member = await _groups.UpdateMemberAsync(id, ct);
            return new { Member = member };
        });
        return result;
    }

    [AllowAnonymous, OldApiPermission(UserGroupService.GroupEditPermissionId)]
    [HttpPost("/admin/user/member/group/updategroupisdefault")]
    [NonUnify, DisplayName("站点后台设默认分组")]
    public async Task<OldApiResult> Updategroupisdefault()
    {
        var result = await RunAsync(async ct =>
        {
            await _groups.UpdateGroupIsDefaultAsync(OldApiRequestParam.ReadInt(App.HttpContext, "ID"), ct);
            return null;
        });
        return result;
    }

    [AllowAnonymous, OldApiPermission(UserGroupService.GroupEditPermissionId)]
    [HttpPost("/admin/user/member/group/savesort")]
    [NonUnify, DisplayName("站点后台分组排序")]
    public async Task<OldApiResult> Savesort()
    {
        var raw = OldApiRequestParam.Read(App.HttpContext, "ID") ?? string.Empty;
        var ids = raw.Split([',', ';', '|', ' '], StringSplitOptions.RemoveEmptyEntries)
            .Select(x => int.TryParse(x.Trim(), out var n) ? n : 0)
            .Where(n => n > 0)
            .ToArray();
        var result = await RunAsync(async ct =>
        {
            await _groups.SaveSortAsync(ids, ct);
            return null;
        });
        return result;
    }

    /// <summary>保存分组。对应旧 /admin/user/member/group/save。</summary>
    [AllowAnonymous]
    [OldApiPermission(UserGroupService.GroupEditPermissionId)]
    [HttpPost("/admin/user/member/group/save")]
    [NonUnify]
    [DisplayName("站点后台保存会员分组")]
    public async Task<OldApiResult> Save()
    {
        var http = App.HttpContext;
        var withdraw = OldApiRequestParam.Read(http, "WithdrawString")
            ?? OldApiRequestParam.Read(http, "Withdraw")
            ?? string.Empty;
        var condition = OldApiRequestParam.Read(http, "ConditionString")
            ?? OldApiRequestParam.Read(http, "Condition")
            ?? string.Empty;
        var result = await RunAsync(async ct =>
        {
            await _groups.SaveAsync(new UserGroupSaveInput
            {
                ID = OldApiRequestParam.ReadInt(http, "ID"),
                Name = OldApiRequestParam.Read(http, "Name") ?? string.Empty,
                Sort = (short)OldApiRequestParam.ReadInt(http, "Sort"),
                IsUpdate = ReadFlag(http, "IsUpdate"),
                IsDefault = ReadFlag(http, "IsDefault"),
                IsNight = ReadFlag(http, "IsNight"),
                Payment = OldApiRequestParam.Read(http, "Payment") ?? string.Empty,
                WithdrawString = withdraw,
                ConditionString = condition,
                SettingInfo = OldApiRequestParam.Read(http, "SettingInfo") ?? string.Empty
            }, ct);
            return null;
        });
        return result;
    }

    /// <summary>删除分组。对应旧 /admin/user/member/group/delete。</summary>
    [AllowAnonymous]
    [OldApiPermission(UserGroupService.GroupDeletePermissionId)]
    [HttpPost("/admin/user/member/group/delete")]
    [NonUnify]
    [DisplayName("站点后台删除会员分组")]
    public async Task<OldApiResult> Delete()
    {
        var result = await RunAsync(async ct =>
        {
            await _groups.DeleteAsync(OldApiRequestParam.ReadInt(App.HttpContext, "ID"), ct);
            return null;
        });
        return result;
    }

    /// <summary>读开关字段（1/true/on）。</summary>
    private static bool ReadFlag(Microsoft.AspNetCore.Http.HttpContext http, string name)
    {
        var raw = OldApiRequestParam.Read(http, name);
        return raw is "1" or "true" or "True" or "TRUE" or "on" or "On";
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
