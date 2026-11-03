using System.Text.Json;
using Furion.FriendlyException;
using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 站点后台加扣款兼容路由：/admin/money/operate/*。
/// </summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "money-operate", Order = 33)]
public class MoneyOperateFormAppService : IDynamicApiController, ITransient
{
    /// <summary>会员加款权限。</summary>
    public const string GrantPermissionId = "ccdc67c12b4f42058263a17320abae51";

    /// <summary>会员扣款权限。</summary>
    public const string DeductPermissionId = "37d28f95553e4ceca3be467612f69723";

    private readonly MoneyOperateService _svc;

    /// <summary>注入。</summary>
    public MoneyOperateFormAppService(MoneyOperateService svc) => _svc = svc;

    /// <summary>过滤游戏下拉。对应旧 filtergame。</summary>
    [AllowAnonymous]
    [OldApiPermission(GrantPermissionId)]
    [HttpPost("/admin/money/operate/filtergame")]
    [HttpGet("/admin/money/operate/filtergame")]
    [NonUnify]
    [DisplayName("站点后台加款过滤游戏")]
    public async Task<OldApiResult> Filtergame()
    {
        var result = await RunAsync(async ct =>
        {
            var dto = await _svc.FilterGameAsync(ct);
            // 旧版 info 里 openPlatforms/openTransfer 是 JSON 数组，不是字符串
            object platforms = ParseJsonOrEmpty(dto.OpenPlatforms);
            object transfers = ParseJsonOrEmpty(dto.OpenTransfer);
            object init = ParseJsonOrEmpty(dto.InitPlatform);
            return new
            {
                openPlatforms = platforms,
                openTransfer = transfers,
                initPlatform = init
            };
        });
        return result;
    }

    /// <summary>回显活动排除游戏。对应旧 initplatform。</summary>
    [AllowAnonymous]
    [OldApiPermission(GrantPermissionId)]
    [HttpPost("/admin/money/operate/initplatform")]
    [NonUnify]
    [DisplayName("站点后台初始化排除游戏")]
    public async Task<OldApiResult> Initplatform()
    {
        var http = App.HttpContext;
        var id = OldApiRequestParam.ReadInt(http, "ID");
        var type = OldApiRequestParam.ReadInt(http, "Type");
        var result = await RunAsync(async ct =>
        {
            var dto = await _svc.InitPlatformAsync(id, type, ct);
            return new { initPlatform = ParseJsonOrEmpty(dto.InitPlatform) };
        });
        return result;
    }

    /// <summary>会员加款。对应旧 addmoney。</summary>
    [AllowAnonymous]
    [OldApiPermission(GrantPermissionId)]
    [HttpPost("/admin/money/operate/addmoney")]
    [NonUnify]
    [DisplayName("站点后台会员加款")]
    public async Task<OldApiResult> Addmoney()
    {
        var input = ReadSubmitInput(App.HttpContext);
        var result = await RunAsync(async ct =>
        {
            var r = await _svc.GrantAsync(input, ct);
            return new { Summary = r.Summary, Items = r.Items };
        });
        return result;
    }

    /// <summary>会员扣款。对应旧 redeductmoney。</summary>
    [AllowAnonymous]
    [OldApiPermission(DeductPermissionId)]
    [HttpPost("/admin/money/operate/redeductmoney")]
    [NonUnify]
    [DisplayName("站点后台会员扣款")]
    public async Task<OldApiResult> Redeductmoney()
    {
        var input = ReadSubmitInput(App.HttpContext);
        var result = await RunAsync(async ct =>
        {
            var r = await _svc.DeductAsync(input, ct);
            return new { Summary = r.Summary, Items = r.Items };
        });
        return result;
    }

    /// <summary>批量加款。对应旧 addmoneybatch。</summary>
    [AllowAnonymous]
    [OldApiPermission(GrantPermissionId)]
    [HttpPost("/admin/money/operate/addmoneybatch")]
    [NonUnify]
    [DisplayName("站点后台批量加款")]
    public async Task<OldApiResult> Addmoneybatch()
    {
        var http = App.HttpContext;
        var result = await RunAsync(async ct =>
        {
            var r = await _svc.GrantBatchTabAsync(new MoneyOperateBatchTabInput
            {
                Batch = OldApiRequestParam.Read(http, "Batch") ?? OldApiRequestParam.Read(http, "Text") ?? string.Empty,
                SourceID = OldApiRequestParam.Read(http, "SourceID")
            }, ct);
            return new { Summary = r.Summary, Items = r.Items };
        });
        return result;
    }

    /// <summary>Excel 批量加款。对应旧 addmoneybatchbyexcel。</summary>
    [AllowAnonymous]
    [OldApiPermission(GrantPermissionId)]
    [HttpPost("/admin/money/operate/addmoneybatchbyexcel")]
    [NonUnify]
    [DisplayName("站点后台Excel批量加款")]
    public Task<OldApiResult> Addmoneybatchbyexcel() => RunExcelBatchAsync(grant: true);

    /// <summary>Excel 批量扣款。对应旧 submoneybatchbyexcel。</summary>
    [AllowAnonymous]
    [OldApiPermission(DeductPermissionId)]
    [HttpPost("/admin/money/operate/submoneybatchbyexcel")]
    [NonUnify]
    [DisplayName("站点后台Excel批量扣款")]
    public Task<OldApiResult> Submoneybatchbyexcel() => RunExcelBatchAsync(grant: false);

    /// <summary>回显排除转入。对应旧 inittransfer。</summary>
    [AllowAnonymous]
    [OldApiPermission(GrantPermissionId)]
    [HttpPost("/admin/money/operate/inittransfer")]
    [NonUnify]
    [DisplayName("站点后台初始化排除转入")]
    public async Task<OldApiResult> Inittransfer()
    {
        var http = App.HttpContext;
        var id = OldApiRequestParam.ReadInt(http, "ID");
        var type = OldApiRequestParam.ReadInt(http, "Type");
        var result = await RunAsync(async ct =>
        {
            var dto = await _svc.InitTransferAsync(id, type, ct);
            return new { initTransfer = ParseJsonOrEmpty(dto.InitPlatform) };
        });
        return result;
    }

    /// <summary>活动下拉。对应旧 show0。</summary>
    [AllowAnonymous]
    [OldApiPermission(GrantPermissionId)]
    [HttpPost("/admin/money/operate/show0")]
    [HttpGet("/admin/money/operate/show0")]
    [NonUnify]
    [DisplayName("站点后台活动下拉show0")]
    public async Task<OldApiResult> Show0()
    {
        var type = OldApiRequestParam.ReadInt(App.HttpContext, "Type");
        var result = await RunAsync(async ct => await _svc.Show0Async(type, ct));
        return result;
    }

    /// <summary>活动下拉。对应旧 showtransfer0。</summary>
    [AllowAnonymous]
    [OldApiPermission(GrantPermissionId)]
    [HttpPost("/admin/money/operate/showtransfer0")]
    [HttpGet("/admin/money/operate/showtransfer0")]
    [NonUnify]
    [DisplayName("站点后台活动下拉showtransfer0")]
    public async Task<OldApiResult> Showtransfer0()
    {
        var type = OldApiRequestParam.ReadInt(App.HttpContext, "Type");
        var result = await RunAsync(async ct => await _svc.ShowTransfer0Async(type, ct));
        return result;
    }

    private static MoneyOperateSubmitInput ReadSubmitInput(Microsoft.AspNetCore.Http.HttpContext http)
    {
        _ = decimal.TryParse(OldApiRequestParam.Read(http, "Money"), out var money);
        _ = decimal.TryParse(OldApiRequestParam.Read(http, "Flow"), out var flow);
        _ = decimal.TryParse(OldApiRequestParam.Read(http, "MinFlow"), out var minFlow);
        return new MoneyOperateSubmitInput
        {
            User = OldApiRequestParam.Read(http, "User"),
            Type = OldApiRequestParam.ReadInt(http, "Type"),
            Currency = OldApiRequestParam.ReadInt(http, "Currency"),
            Money = money,
            SourceID = OldApiRequestParam.Read(http, "SourceID"),
            Description = OldApiRequestParam.Read(http, "Description"),
            DescriptionA = OldApiRequestParam.Read(http, "DescriptionA"),
            Flow = flow == 0 ? 1 : flow,
            MinFlow = minFlow,
            PayID = OldApiRequestParam.ReadInt(http, "PayID")
        };
    }

    /// <summary>读上传 Excel，逐行走加/扣款。</summary>
    private async Task<OldApiResult> RunExcelBatchAsync(bool grant)
    {
        var http = App.HttpContext ?? throw Oops.Oh("#请求无效#");
        var file = http.Request.Form.Files.GetFile("file")
            ?? http.Request.Form.Files.GetFile("File")
            ?? http.Request.Form.Files.FirstOrDefault();
        if (file == null || file.Length == 0)
            return OldApiResult.Fail("#请上传Excel#");

        return await RunAsync(async ct =>
        {
            var rows = await CommonHelper.ImportExcelData<MoneyOperateExcelImport>(file);
            if (rows == null || rows.Count == 0)
                throw Oops.Oh("#Excel无有效行#");
            if (rows.Count > 1000)
                throw Oops.Oh("#一次最多1000个#");

            var items = new List<MoneyOperateUserResult>();
            var i = 0;
            foreach (var row in rows)
            {
                i++;
                if (string.IsNullOrWhiteSpace(row.UserName))
                {
                    items.Add(new MoneyOperateUserResult { UserName = $"行{i}", Success = false, Message = "用户名为空" });
                    continue;
                }

                if (!MoneyTypeNameResolver.TryResolve(row.Type, out var moneyType))
                {
                    items.Add(new MoneyOperateUserResult
                    {
                        UserName = row.UserName.Trim(),
                        Success = false,
                        Message = $"无法识别类型「{row.Type}」"
                    });
                    continue;
                }

                if (!MoneyTypeNameResolver.TryResolveCurrency(row.Currency, out var currency))
                {
                    items.Add(new MoneyOperateUserResult
                    {
                        UserName = row.UserName.Trim(),
                        Success = false,
                        Message = $"无法识别币种「{row.Currency}」"
                    });
                    continue;
                }

                var input = new MoneyOperateSubmitInput
                {
                    User = row.UserName.Trim(),
                    Type = moneyType,
                    Currency = currency,
                    Money = row.Money,
                    SourceID = $"X{DateTime.Now:HHmmss}{i:000}",
                    Description = row.Description ?? string.Empty,
                    DescriptionA = row.DescriptionA ?? string.Empty,
                    Flow = row.Flow == 0 ? 1 : row.Flow,
                    MinFlow = row.MinFlow
                };
                try
                {
                    var one = grant
                        ? await _svc.GrantAsync(input, ct)
                        : await _svc.DeductAsync(input, ct);
                    if (one?.Items != null)
                        items.AddRange(one.Items);
                }
                catch (Exception ex)
                {
                    items.Add(new MoneyOperateUserResult
                    {
                        UserName = row.UserName.Trim(),
                        Success = false,
                        Message = ex.Message
                    });
                }
            }

            var ok = items.Count(t => t.Success);
            return new
            {
                Summary = $"成功 {ok} / 共 {items.Count}",
                Items = items
            };
        });
    }

    private static object ParseJsonOrEmpty(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return Array.Empty<object>();
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.Clone();
        }
        catch
        {
            return json;
        }
    }

    private static async Task<OldApiResult> RunAsync(Func<CancellationToken, Task<object?>> action)
    {
        var ct = App.HttpContext?.RequestAborted ?? default;
        try
        {
            return OldApiResult.Ok(string.Empty, await action(ct));
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
