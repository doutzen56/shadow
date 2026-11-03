using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 打码流水（JWT）。对齐旧 /admin/money/washflow/*。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "washFlow", Order = 462, Description = "打码流水")]
public class WashFlowAdminAppService : IDynamicApiController, ITransient
{
    private readonly WashFlowAdminService _svc;

    /// <summary>
    /// 注入。
    /// </summary>
    /// <param name="svc">Shared 服务</param>
    public WashFlowAdminAppService(WashFlowAdminService svc) => _svc = svc;

    /// <summary>
    /// 当前打码量清单。对齐旧 list。
    /// </summary>
    /// <param name="userId">会员</param>
    /// <param name="currency">币种</param>
    /// <param name="orderId">提现单</param>
    /// <param name="show">有值则只看未挂提现单</param>
    /// <param name="startAt">开始</param>
    /// <param name="endAt">结束</param>
    /// <returns>清单</returns>
    [ApiDescriptionSettings(Name = "List"), HttpPost]
    [DisplayName("打码清单")]
    public Task<WashFlowListResultDto> List(
        [FromQuery] int userId,
        [FromQuery] int currency,
        [FromQuery] int orderId = 0,
        [FromQuery] string? show = null,
        [FromQuery] DateTime? startAt = null,
        [FromQuery] DateTime? endAt = null) =>
        _svc.ListAsync(
            userId,
            currency,
            orderId,
            !string.IsNullOrWhiteSpace(show),
            startAt,
            endAt,
            App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 流水日志。对齐旧 log。
    /// </summary>
    /// <param name="userId">会员</param>
    /// <param name="id">流水编号</param>
    /// <param name="currency">币种</param>
    /// <returns>日志</returns>
    [ApiDescriptionSettings(Name = "Log"), HttpPost]
    [DisplayName("打码日志")]
    public Task<List<WashFlowLogItemDto>> Log(
        [FromQuery] int userId,
        [FromQuery] int id,
        [FromQuery] int currency) =>
        _svc.LogAsync(userId, id, currency, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 更新打码量。对齐旧 updatewashflow。
    /// </summary>
    /// <param name="userId">会员</param>
    /// <param name="money">金额</param>
    /// <param name="type">Bet/BetFull/Flow</param>
    /// <param name="description">备注</param>
    /// <param name="currency">币种</param>
    [ApiDescriptionSettings(Name = "UpdateWashFlow"), HttpPost]
    [DisplayName("更新打码量")]
    public Task UpdateWashFlow(
        [FromQuery] int userId,
        [FromQuery] decimal money,
        [FromQuery] string type,
        [FromQuery] string? description,
        [FromQuery] int currency) =>
        _svc.UpdateAsync(
            userId,
            money,
            type,
            description,
            currency,
            0,
            App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 按流水编号更新。对齐旧 updatewashflowsingle。
    /// </summary>
    /// <param name="userId">会员</param>
    /// <param name="money">金额</param>
    /// <param name="type">动作</param>
    /// <param name="description">备注</param>
    /// <param name="currency">币种</param>
    /// <param name="id">流水编号</param>
    [ApiDescriptionSettings(Name = "UpdateWashFlowSingle"), HttpPost]
    [DisplayName("按编号更新打码")]
    public Task UpdateWashFlowSingle(
        [FromQuery] int userId,
        [FromQuery] decimal money,
        [FromQuery] string type,
        [FromQuery] string? description,
        [FromQuery] int currency,
        [FromQuery] int id) =>
        _svc.UpdateAsync(
            userId,
            money,
            string.IsNullOrWhiteSpace(type) ? "BetID" : type,
            description,
            currency,
            id,
            App.HttpContext?.RequestAborted ?? default);
}
