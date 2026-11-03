using System.Collections;
using Furion.FriendlyException;
using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>站点后台救援金：/admin/money/rescue/*。</summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "money-rescue", Order = 35)]
public class MoneyRescueAppService : IDynamicApiController, ITransient
{
    public const string PermissionId = "2de04953030c418da89f5c0488b5a119";

    private readonly MoneyOperateService _svc;

    public MoneyRescueAppService(MoneyOperateService svc) => _svc = svc;

    /// <summary>救援金发放记录。对应旧 loglist。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/money/rescue/loglist")]
    [HttpGet("/admin/money/rescue/loglist")]
    [NonUnify]
    [DisplayName("站点后台救援金日志")]
    public async Task<OldApiResult> Loglist()
    {
        var http = App.HttpContext;
        var (pageIndex, pageSize) = OldApiShowResult.ReadPaging(http);
        var input = ReadForm(http);
        var result = await RunAsync(async ct =>
        {
            var rows = await _svc.LoglistAsync(input, ct);
            return OldApiShowResult.SlicePage(AsEnumerable(rows), pageIndex, pageSize);
        });
        return result;
    }

    /// <summary>救援金预览。对应旧 query。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/money/rescue/query")]
    [NonUnify]
    [DisplayName("站点后台救援金预览")]
    public async Task<OldApiResult> Query()
    {
        var input = ReadForm(App.HttpContext);
        var result = await RunAsync(async ct =>
        {
            var rows = await _svc.QueryAsync(input, ct);
            return OldApiShowResult.OfList(AsEnumerable(rows));
        });
        return result;
    }

    /// <summary>发放救援金。对应旧 post。</summary>
    [AllowAnonymous]
    [OldApiPermission(PermissionId)]
    [HttpPost("/admin/money/rescue/post")]
    [NonUnify]
    [DisplayName("站点后台发放救援金")]
    public async Task<OldApiResult> Post()
    {
        var input = ReadForm(App.HttpContext);
        var result = await RunAsync(ct => _svc.PostAsync(input, ct));
        return result;
    }

    private static Dictionary<string, object?> ReadForm(Microsoft.AspNetCore.Http.HttpContext http)
    {
        var map = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (http == null) return map;
        if (http.Request.HasFormContentType)
            foreach (var key in http.Request.Form.Keys)
                map[key] = http.Request.Form[key].ToString();
        foreach (var key in http.Request.Query.Keys)
            if (!map.ContainsKey(key))
                map[key] = http.Request.Query[key].ToString();
        return map;
    }

    private static IEnumerable AsEnumerable(object? value) =>
        value is IEnumerable e and not string ? e : Array.Empty<object>();

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
