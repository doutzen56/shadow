using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 旧 /admin/withdraw/* 残留。deletewithdraw 旧实现直接 return null（死代码），保持成功空 info。
/// </summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "withdraw-legacy", Order = 46)]
public class WithdrawLegacyAppService : IDynamicApiController, ITransient
{
    public const string PermissionId = MoneyWithdrawAppService.WithdrawChannelPermissionId;

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/withdraw/deletewithdraw")]
    [NonUnify, DisplayName("withdraw-deletewithdraw-noop")]
    public Task<OldApiResult> Deletewithdraw() =>
        Task.FromResult(OldApiResult.Ok(string.Empty, null));
}
