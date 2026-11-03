using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 积分旧路径：/admin/points/point/*、/admin/shop|voucher/log/pointlog。
/// </summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "points", Order = 47)]
public class PointsAppService : IDynamicApiController, ITransient
{
    public const string PermissionId = UserMemberFormAppService.MemberListPermissionId;

    private readonly SitePointItemService _points;

    public PointsAppService(SitePointItemService points) => _points = points;

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/points/point/info"), HttpGet("/admin/points/point/info")]
    [HttpPost("/admin/points/point/getrule"), HttpGet("/admin/points/point/getrule")]
    [NonUnify, DisplayName("points-info")]
    public Task<OldApiResult> Info() =>
        Run(ct => _points.GetAsync(ct));

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/points/point/save"), HttpPost("/admin/points/point/saverule")]
    [NonUnify, DisplayName("points-save")]
    public Task<OldApiResult> Save() =>
        Run(async ct =>
        {
            var http = App.HttpContext;
            await _points.SaveAsync(new SitePointItemSaveInput
            {
                PointItemSetting = OldApiFormReader.ReadString(http, "PointItemSetting", "Setting"),
                ExperItemSetting = OldApiFormReader.ReadString(http, "ExperItemSetting"),
                PointRule = OldApiFormReader.ReadString(http, "PointRule", "Rule")
            }, ct);
            return (object?)null;
        });

    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/points/point/list"), HttpGet("/admin/points/point/list")]
    [HttpPost("/admin/shop/log/pointlog"), HttpGet("/admin/shop/log/pointlog")]
    [HttpPost("/admin/voucher/log/pointlog"), HttpGet("/admin/voucher/log/pointlog")]
    [NonUnify, DisplayName("points-log")]
    public Task<OldApiResult> Pointlog() =>
        Run(ct =>
        {
            var http = App.HttpContext;
            var (page, size) = OldApiShowResult.ReadPaging(http);
            return _points.PageLogsAsync(new PointLogPageInput
            {
                Page = page,
                PageSize = size,
                UserID = OldApiFormReader.ReadInt(http, "UserID", "userID")
            }, ct);
        });

    /// <summary>清理全站积分。对齐旧 clearallusers（仅 POST）。</summary>
    [AllowAnonymous, OldApiPermission(PermissionId)]
    [HttpPost("/admin/points/point/clearallusers")]
    [NonUnify, DisplayName("points-clearallusers")]
    public Task<OldApiResult> Clearallusers() =>
        Run(ct => _points.ClearallusersAsync(null, ct));

    private static async Task<OldApiResult> Run<T>(Func<CancellationToken, Task<T>> action) => await OldApiFormReader.RunAsync(action);
}
