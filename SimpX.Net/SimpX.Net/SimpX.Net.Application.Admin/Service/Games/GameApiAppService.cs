using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>站点后台游戏厂商动作：/admin/game/api/*（厂商联调延后）。</summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "game-api", Order = 44)]
public class GameApiAppService : IDynamicApiController, ITransient
{
    public const string PermissionId = UserMemberFormAppService.MemberListPermissionId;

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/game/api/action")]
    [NonUnify, DisplayName("站点后台游戏厂商动作")]
    public Task<OldApiResult> Action() =>
        Task.FromResult(OldApiResult.Fail("#功能尚未接通#"));
}
