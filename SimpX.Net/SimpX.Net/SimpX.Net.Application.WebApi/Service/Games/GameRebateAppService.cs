using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.WebApi;

/// <summary>
/// 会员返水设定只读：/request/game/rebate/*。
/// </summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "game-rebate", Order = 33)]
public class GameRebateAppService : IDynamicApiController, ITransient
{
    private readonly RebateSettingService _rebate;

    public GameRebateAppService(RebateSettingService rebate) => _rebate = rebate;

    [OldApiGuest, AllowAnonymous]
    [HttpPost("/request/game/rebate/returntype"), HttpGet("/request/game/rebate/returntype")]
    [NonUnify, DisplayName("game-rebate-returntype")]
    public Task<OldApiResult> Returntype() =>
        Run(_ => Task.FromResult<object>(_rebate.ReturnType()));

    [OldApiGuest, AllowAnonymous]
    [HttpPost("/request/game/rebate/list"), HttpGet("/request/game/rebate/list")]
    [NonUnify, DisplayName("game-rebate-list")]
    public Task<OldApiResult> List() =>
        Run(async ct =>
        {
            var items = await _rebate.ListAsync(ct);
            return (object)OldApiShowResult.OfList(items.Select(t => new
            {
                t.ID,
                t.IsAuto,
                t.BetMoney,
                t.MaxMoney,
                t.MinMoney,
                t.Name,
                t.Enabled,
                Group = t.Groups,
                Level = t.Levels,
                Game = t.Type
            }));
        });

    [OldApiGuest, AllowAnonymous]
    [HttpPost("/request/game/rebate/info"), HttpGet("/request/game/rebate/info")]
    [NonUnify, DisplayName("game-rebate-info")]
    public Task<OldApiResult> Info() =>
        Run(async ct =>
        {
            var id = OldApiFormReader.ReadInt(App.HttpContext, "ID", "Id");
            if (id <= 0)
                return (object)new { Games = "{}" };
            var detail = await _rebate.DetailAsync(id, ct);
            return (object)new
            {
                Games = detail.SettingString ?? detail.Type ?? "{}"
            };
        });

    private static async Task<OldApiResult> Run<T>(Func<CancellationToken, Task<T>> action) => await OldApiFormReader.RunAsync(action);
}
