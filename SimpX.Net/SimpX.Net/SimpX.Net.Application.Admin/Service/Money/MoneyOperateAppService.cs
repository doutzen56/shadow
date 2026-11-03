using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using SimpX.Net.Application.Shared;
namespace SimpX.Net.Application.Admin;

/// <summary>
/// 租户后台会员加款/扣款（JWT）。对齐旧版 /admin/money/operate/*。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "moneyOperate", Order = 461, Description = "款项加扣款")]
public partial class MoneyOperateAppService : IDynamicApiController, ITransient
{
    private readonly MoneyOperateService _svc;

    /// <summary>
    /// 注入服务。
    /// </summary>
    /// <param name="svc">服务</param>
    public MoneyOperateAppService(MoneyOperateService svc) => _svc = svc;

    /// <summary>
    /// 会员加款。
    /// </summary>
    /// <param name="input">表单</param>
    /// <returns>逐用户结果</returns>
    [ApiDescriptionSettings(Name = "Grant"), HttpPost]
    [DisplayName("会员加款")]
    public Task<MoneyOperateSubmitResult> Grant(MoneyOperateSubmitInput input) =>
        _svc.GrantAsync(input ?? new MoneyOperateSubmitInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 会员加款（旧方法名别名）。对齐旧 addmoney。
    /// </summary>
    /// <param name="input">表单</param>
    /// <returns>逐用户结果</returns>
    [ApiDescriptionSettings(Name = "AddMoney"), HttpPost]
    [DisplayName("会员加款AddMoney")]
    public Task<MoneyOperateSubmitResult> AddMoney(MoneyOperateSubmitInput input) =>
        Grant(input);

    /// <summary>
    /// 会员扣款。
    /// </summary>
    /// <param name="input">表单</param>
    /// <returns>逐用户结果</returns>
    [ApiDescriptionSettings(Name = "Deduct"), HttpPost]
    [DisplayName("会员扣款")]
    public Task<MoneyOperateSubmitResult> Deduct(MoneyOperateSubmitInput input) =>
        _svc.DeductAsync(input ?? new MoneyOperateSubmitInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 会员扣款（旧方法名别名）。对齐旧 redeductmoney。
    /// </summary>
    /// <param name="input">表单</param>
    /// <returns>逐用户结果</returns>
    [ApiDescriptionSettings(Name = "RedeductMoney"), HttpPost]
    [DisplayName("会员扣款RedeductMoney")]
    public Task<MoneyOperateSubmitResult> RedeductMoney(MoneyOperateSubmitInput input) =>
        Deduct(input);

    /// <summary>
    /// 制表符批量加款。对齐旧 addmoneybatch。
    /// </summary>
    /// <param name="input">Batch / SourceID</param>
    /// <returns>逐用户结果</returns>
    [ApiDescriptionSettings(Name = "AddMoneyBatch"), HttpPost]
    [DisplayName("制表符批量加款")]
    public Task<MoneyOperateSubmitResult> AddMoneyBatch(MoneyOperateBatchTabInput input) =>
        _svc.GrantBatchTabAsync(input ?? new MoneyOperateBatchTabInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// Excel 批量加款（对齐旧 addmoneybatchbyexcel）。
    /// </summary>
    /// <param name="file">xlsx</param>
    /// <returns>逐用户结果</returns>
    [ApiDescriptionSettings(Name = "GrantByExcel"), HttpPost]
    [DisplayName("Excel批量加款")]
    public Task<MoneyOperateSubmitResult> GrantByExcel(IFormFile file) =>
        ImportExcelAndRun(file, grant: true);

    /// <summary>
    /// Excel 批量加款（旧方法名别名）。
    /// </summary>
    /// <param name="file">xlsx</param>
    /// <returns>逐用户结果</returns>
    [ApiDescriptionSettings(Name = "AddMoneyBatchByExcel"), HttpPost]
    [DisplayName("Excel批量加款AddMoneyBatchByExcel")]
    public Task<MoneyOperateSubmitResult> AddMoneyBatchByExcel(IFormFile file) =>
        GrantByExcel(file);

    /// <summary>
    /// Excel 批量扣款。
    /// </summary>
    /// <param name="file">xlsx</param>
    /// <returns>逐用户结果</returns>
    [ApiDescriptionSettings(Name = "DeductByExcel"), HttpPost]
    [DisplayName("Excel批量扣款")]
    public Task<MoneyOperateSubmitResult> DeductByExcel(IFormFile file) =>
        ImportExcelAndRun(file, grant: false);

    /// <summary>
    /// Excel 批量扣款（旧方法名别名）。对齐旧 submoneybatchbyexcel。
    /// </summary>
    /// <param name="file">xlsx</param>
    /// <returns>逐用户结果</returns>
    [ApiDescriptionSettings(Name = "SubMoneyBatchByExcel"), HttpPost]
    [DisplayName("Excel批量扣款SubMoneyBatchByExcel")]
    public Task<MoneyOperateSubmitResult> SubMoneyBatchByExcel(IFormFile file) =>
        DeductByExcel(file);

    /// <summary>
    /// 加款页游戏过滤选项。对齐旧 filtergame。
    /// </summary>
    /// <returns>开放厂商与初始勾选</returns>
    [ApiDescriptionSettings(Name = "FilterGame"), HttpPost]
    [DisplayName("加款游戏过滤")]
    public Task<MoneyOperateFilterGameDto> FilterGame() =>
        _svc.FilterGameAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 回显活动排除游戏。对齐旧 initplatform。
    /// </summary>
    /// <param name="id">活动编号</param>
    /// <param name="type">活动类型</param>
    /// <returns>已选 JSON</returns>
    [ApiDescriptionSettings(Name = "InitPlatform"), HttpPost]
    [DisplayName("回显排除游戏")]
    public Task<MoneyOperateInitPlatformDto> InitPlatform([FromQuery] int id, [FromQuery] int type) =>
        _svc.InitPlatformAsync(id, type, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 回显活动排除转入。对齐旧 inittransfer。
    /// </summary>
    /// <param name="id">活动编号</param>
    /// <param name="type">活动类型</param>
    /// <returns>已选 JSON</returns>
    [ApiDescriptionSettings(Name = "InitTransfer"), HttpPost]
    [DisplayName("回显排除转入")]
    public Task<MoneyOperateInitPlatformDto> InitTransfer([FromQuery] int id, [FromQuery] int type) =>
        _svc.InitTransferAsync(id, type, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 手工活动下拉（排除游戏）。对齐旧 show0。
    /// </summary>
    /// <param name="type">活动类型</param>
    /// <returns>选项</returns>
    [ApiDescriptionSettings(Name = "Show0"), HttpPost]
    [DisplayName("手工活动下拉")]
    public Task<List<MoneyOperateEventOptionDto>> Show0([FromQuery] int type) =>
        _svc.Show0Async(type, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 手工活动下拉（排除转入）。对齐旧 showtransfer0。
    /// </summary>
    /// <param name="type">活动类型</param>
    /// <returns>选项</returns>
    [ApiDescriptionSettings(Name = "ShowTransfer0"), HttpPost]
    [DisplayName("排除转入活动下拉")]
    public Task<List<MoneyOperateEventOptionDto>> ShowTransfer0([FromQuery] int type) =>
        _svc.ShowTransfer0Async(type, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 解析 Excel 后逐行走加/扣款。
    /// </summary>
    private async Task<MoneyOperateSubmitResult> ImportExcelAndRun(IFormFile file, bool grant)
    {
        if (file == null || file.Length == 0)
            throw Oops.Oh("#请上传Excel#");
        var rows = await CommonHelper.ImportExcelData<MoneyOperateExcelImport>(file);
        if (rows == null || rows.Count == 0)
            throw Oops.Oh("#Excel无有效行#");
        if (rows.Count > 1000)
            throw Oops.Oh("#一次最多1000个#");

        var ct = App.HttpContext?.RequestAborted ?? default;
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
                SourceID = Truncate20($"X{DateTime.Now:HHmmss}{i:000}"),
                Description = row.Description ?? string.Empty,
                DescriptionA = row.DescriptionA ?? string.Empty,
                Flow = row.Flow,
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
                    UserName = row.UserName,
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        return new MoneyOperateSubmitResult
        {
            Items = items,
            Summary = string.Join("<br/>", items.Select(x =>
                x.Success
                    ? $"成功: {x.UserName}"
                    : $"<span style='color:red'>失败: {x.UserName},原因: {x.Message}</span>"))
        };
    }

    /// <summary>流水号截到 20。</summary>
    private static string Truncate20(string s) =>
        string.IsNullOrEmpty(s) || s.Length <= 20 ? (s ?? string.Empty) : s[..20];

    // --- merged from MoneyOperateAppService.WaveGaps.cs ---



    /// <summary>
    /// 对齐旧 loglist。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Loglist"), HttpPost]
    [DisplayName("loglist")]
    public Task<object?> Loglist(Dictionary<string, object?>? input = null) =>
        _svc.LoglistAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 post。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Post"), HttpPost]
    [DisplayName("post")]
    public Task<object?> Post(Dictionary<string, object?>? input = null) =>
        _svc.PostAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 query。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Query"), HttpPost]
    [DisplayName("query")]
    public Task<object?> Query(Dictionary<string, object?>? input = null) =>
        _svc.QueryAsync(input, App.HttpContext?.RequestAborted ?? default);
}
