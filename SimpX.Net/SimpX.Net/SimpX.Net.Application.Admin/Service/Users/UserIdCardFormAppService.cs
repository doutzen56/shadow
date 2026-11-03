using Furion.FriendlyException;
using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>站点后台证件：/admin/user/member/idcard/*。</summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "user-idcard", Order = 26)]
public class UserIdCardFormAppService : IDynamicApiController, ITransient
{
    /// <summary>先挂会员列表权限。</summary>
    public const string PermissionId = UserMemberFormAppService.MemberListPermissionId;

    private readonly UserIdCardService _svc;

    /// <summary>注入。</summary>
    public UserIdCardFormAppService(UserIdCardService svc) => _svc = svc;

    /// <summary>证件列表。对应旧 list。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/member/idcard/list")]
    [HttpGet("/admin/user/member/idcard/list")]
    [NonUnify]
    [DisplayName("站点后台证件列表")]
    public async Task<OldApiResult> List()
    {
        var http = App.HttpContext;
        var (pageIndex, pageSize) = OldApiShowResult.ReadPaging(http);
        var input = new UserIdCardAdminPageInput
        {
            Page = pageIndex,
            PageSize = pageSize,
            StartAt = OldApiRequestParam.Read(http, "StartAt"),
            EndAt = OldApiRequestParam.Read(http, "EndAt"),
            UserNumber = OldApiRequestParam.Read(http, "UserNumber"),
            UserName = OldApiRequestParam.Read(http, "UserName") ?? OldApiRequestParam.Read(http, "User"),
            Status = OldApiRequestParam.Read(http, "Status"),
        };
        var result = await RunAsync(async ct =>
        {
            var page = await _svc.PageAsync(input, ct);
            return OldApiShowResult.OfPage(page.Data, page.Total, pageIndex, pageSize);
        });
        return result;
    }

    /// <summary>证件详情。对应旧 info。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/member/idcard/info")]
    [HttpGet("/admin/user/member/idcard/info")]
    [NonUnify]
    [DisplayName("站点后台证件详情")]
    public async Task<OldApiResult> Info()
    {
        var http = App.HttpContext;
        var id = OldApiRequestParam.ReadInt(http, "ID");
        var userId = OldApiRequestParam.ReadInt(http, "UserID");
        var status = OldApiRequestParam.ReadInt(http, "Status");
        var result = await RunAsync(async ct => await _svc.InfoAsync(id, userId, status, ct));
        return result;
    }

    /// <summary>按用户名确认。对应旧 verifyuser。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/member/idcard/verifyuser")]
    [HttpGet("/admin/user/member/idcard/verifyuser")]
    [NonUnify]
    [DisplayName("站点后台证件查会员")]
    public async Task<OldApiResult> Verifyuser()
    {
        var userName = OldApiRequestParam.Read(App.HttpContext, "UserName")
            ?? OldApiRequestParam.Read(App.HttpContext, "User");
        var result = await RunAsync(async ct => await _svc.VerifyUserAsync(userName, ct));
        return result;
    }

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/member/idcard/createidcard")]
    [NonUnify, DisplayName("站点后台创建证件")]
    public async Task<OldApiResult> Createidcard()
    {
        var http = App.HttpContext;
        var result = await RunAsync(async ct =>
        {
            // 旧站 hidden name=ID 存会员编号；新页传 UserID。两者兼容。
            var userId = OldApiRequestParam.ReadInt(http, "UserID");
            if (userId <= 0)
                userId = OldApiRequestParam.ReadInt(http, "ID");

            DateTime? cardCreateAt = null;
            DateTime? cardBirthday = null;
            if (DateTime.TryParse(OldApiRequestParam.Read(http, "CardCreateAt"), out var createAt))
                cardCreateAt = createAt;
            if (DateTime.TryParse(OldApiRequestParam.Read(http, "CardBirthday"), out var birthday))
                cardBirthday = birthday;

            await _svc.SaveAsync(new UserIdCardSaveInput
            {
                UserID = userId,
                UserName = OldApiRequestParam.Read(http, "UserName") ?? string.Empty,
                CardName = OldApiRequestParam.Read(http, "CardName") ?? string.Empty,
                CardNumber = OldApiRequestParam.Read(http, "CardNumber") ?? string.Empty,
                CardType = OldApiRequestParam.ReadInt(http, "CardType", 1),
                Status = OldApiRequestParam.ReadInt(http, "Status"),
                Remark = OldApiRequestParam.Read(http, "Remark") ?? string.Empty,
                CardCity = OldApiRequestParam.Read(http, "CardCity") ?? string.Empty,
                Country = OldApiRequestParam.Read(http, "Country") ?? string.Empty,
                Gender = ParseGender(OldApiRequestParam.Read(http, "Gender")),
                CardFront = OldApiRequestParam.Read(http, "CardFront") ?? string.Empty,
                CardBack = OldApiRequestParam.Read(http, "CardBack") ?? string.Empty,
                CardCreateAt = cardCreateAt,
                CardBirthday = cardBirthday,
            }, ct);
            return null;
        });
        return result;
    }

    /// <summary>
    /// 解析性别：整型 / Men|Women|Other / 男|女。
    /// </summary>
    private static int ParseGender(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return 0;
        var key = raw.Trim();
        if (int.TryParse(key, out var n))
            return n;
        if (key.Equals("Men", StringComparison.OrdinalIgnoreCase) || key == "男")
            return 1;
        if (key.Equals("Women", StringComparison.OrdinalIgnoreCase) || key == "女")
            return 2;
        if (key.Equals("Other", StringComparison.OrdinalIgnoreCase) || key == "其他")
            return 0;
        return 0;
    }

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/member/idcard/delete")]
    [NonUnify, DisplayName("站点后台删证件")]
    public async Task<OldApiResult> Delete()
    {
        var result = await RunAsync(async ct =>
        {
            await _svc.DeleteAsync(OldApiRequestParam.ReadInt(App.HttpContext, "ID"), ct);
            return null;
        });
        return result;
    }

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/member/idcard/reset")]
    [NonUnify, DisplayName("站点后台重置证件")]
    public async Task<OldApiResult> Reset()
    {
        var result = await RunAsync(async ct =>
        {
            await _svc.ResetAsync(OldApiRequestParam.ReadInt(App.HttpContext, "UserID"), ct);
            return null;
        });
        return result;
    }

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/user/member/idcard/updateinfo")]
    [NonUnify, DisplayName("站点后台证件审核")]
    public async Task<OldApiResult> Updateinfo()
    {
        var http = App.HttpContext;
        var result = await RunAsync(async ct =>
        {
            var statusRaw = OldApiRequestParam.Read(http, "Status") ?? string.Empty;
            if (!OldApiMoneyDisplay.TryParseCheckStatus(statusRaw, out var status))
                throw Oops.Oh("#参数错误#");
            await _svc.UpdateInfoAsync(new UserIdCardUpdateInfoInput
            {
                ID = OldApiRequestParam.ReadInt(http, "ID"),
                Status = status,
                Remark = OldApiRequestParam.Read(http, "Remark") ?? string.Empty,
                CardNumber = OldApiRequestParam.Read(http, "CardNumber") ?? string.Empty
            }, ct);
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
