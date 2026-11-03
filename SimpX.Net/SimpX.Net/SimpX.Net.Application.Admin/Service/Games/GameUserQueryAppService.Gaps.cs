using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 游戏会员/转账旧动作名。
/// </summary>
public partial class GameUserQueryAppService
{
    /// <summary>对齐旧 userlist。</summary>
    /// <param name="input">筛选</param>
    /// <returns>分页</returns>
    [ApiDescriptionSettings(Name = "UserList"), HttpPost]
    [DisplayName("游戏会员列表")]
    public Task<GameUserPageOutput> UserList(GameUserListInput input) =>
        _svc.UserListAsync(input ?? new GameUserListInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>对齐旧 getbalance。</summary>
    /// <param name="userId">会员</param>
    /// <param name="type">游戏</param>
    /// <param name="currency">币种</param>
    /// <returns>余额</returns>
    [ApiDescriptionSettings(Name = "GetBalance"), HttpPost]
    [DisplayName("游戏余额")]
    public Task<GameBalanceDto> GetBalance([FromQuery] int userId, [FromQuery] int type, [FromQuery] int currency) =>
        _svc.GetBalanceAsync(userId, type, currency, App.HttpContext?.RequestAborted ?? default);

    /// <summary>对齐旧 GetUserBalanceList。</summary>
    /// <param name="id">会员</param>
    /// <param name="currency">币种</param>
    /// <returns>行</returns>
    [ApiDescriptionSettings(Name = "GetUserBalanceList"), HttpPost]
    [DisplayName("会员游戏余额列表")]
    public Task<List<GameBalanceDto>> GetUserBalanceList([FromQuery] int id, [FromQuery] int currency = -1) =>
        _svc.GetUserBalanceListAsync(id, currency, App.HttpContext?.RequestAborted ?? default);

    /// <summary>对齐旧 updatelock。</summary>
    /// <param name="input">入参</param>
    [ApiDescriptionSettings(Name = "UpdateLock"), HttpPost]
    [DisplayName("游戏锁")]
    public Task UpdateLock(GameUserLockInput input) =>
        _svc.UpdateLockAsync(input ?? new GameUserLockInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>对齐旧 updatetest。</summary>
    /// <param name="userId">会员</param>
    /// <param name="type">游戏</param>
    /// <param name="isTest">测试</param>
    [ApiDescriptionSettings(Name = "UpdateTest"), HttpPost]
    [DisplayName("游戏测试标记")]
    public Task UpdateTest([FromQuery] int userId, [FromQuery] int type, [FromQuery] bool isTest) =>
        _svc.UpdateTestAsync(userId, type, isTest, App.HttpContext?.RequestAborted ?? default);

    /// <summary>对齐旧 updateLimit。</summary>
    /// <param name="input">入参</param>
    [ApiDescriptionSettings(Name = "UpdateLimit"), HttpPost]
    [DisplayName("游戏限红")]
    public Task UpdateLimit(GameUserLimitInput input) =>
        _svc.UpdateLimitAsync(input ?? new GameUserLimitInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>对齐旧 register。</summary>
    /// <param name="input">入参</param>
    [ApiDescriptionSettings(Name = "Register"), HttpPost]
    [DisplayName("注册游戏帐号")]
    public Task Register(GameUserRegisterInput input) =>
        _svc.RegisterAsync(input ?? new GameUserRegisterInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>转账列表。对齐旧 transfer/list。</summary>
    /// <param name="input">筛选</param>
    /// <returns>分页</returns>
    [ApiDescriptionSettings(Name = "List"), HttpPost]
    [DisplayName("转账列表")]
    public Task<GameTransferPageOutput> List(GameTransferPageInput input) =>
        _svc.PageTransfersAsync(input ?? new GameTransferPageInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>转账详情。对齐旧 transfer/info。</summary>
    /// <param name="id">订单</param>
    /// <returns>行</returns>
    [ApiDescriptionSettings(Name = "Info"), HttpPost]
    [DisplayName("转账详情")]
    public Task<GameTransferItemDto> Info([FromQuery] int id) =>
        _svc.TransferInfoAsync(id, App.HttpContext?.RequestAborted ?? default);

    /// <summary>转账动作。对齐旧 transfer/action。</summary>
    /// <param name="input">入参</param>
    /// <returns>结果</returns>
    [ApiDescriptionSettings(Name = "Action"), HttpPost]
    [DisplayName("游戏转账")]
    public async Task<object> Action(GameTransferActionInput input)
    {
        input ??= new GameTransferActionInput();
        var siteId = _site.RequireSiteId();
        OldApiResult result;
        if (input.Action == GameTransferService.ActionIn)
            result = await _transfer.TransferToGameAsync(siteId, input.UserID, input.Type, input.Money, input.Currency);
        else if (input.Action == GameTransferService.ActionOut)
            result = await _transfer.TransferToCenterAsync(siteId, input.UserID, input.Type, input.Money, input.Currency);
        else
            throw Oops.Oh("#参数错误#");
        if (result.Success != 1)
            throw Oops.Oh(result.Msg ?? "#失败#");
        return new { message = result.Msg ?? "#成功#" };
    }

    /// <summary>检查占位。对齐旧 check。</summary>
    /// <param name="id">订单</param>
    /// <returns>详情</returns>
    [ApiDescriptionSettings(Name = "Check"), HttpPost]
    [DisplayName("检查转账")]
    public Task<GameTransferItemDto> Check([FromQuery] int id) =>
        _svc.TransferInfoAsync(id, App.HttpContext?.RequestAborted ?? default);

    /// <summary>检查信息。对齐旧 checkinfo。</summary>
    /// <param name="id">订单</param>
    /// <returns>详情</returns>
    [ApiDescriptionSettings(Name = "CheckInfo"), HttpPost]
    [DisplayName("转账状态")]
    public async Task<object> CheckInfo([FromQuery] int id)
    {
        var row = await _svc.TransferInfoAsync(id, App.HttpContext?.RequestAborted ?? default);
        return new { msg = row.Description, status = row.Status };
    }

    /// <summary>一键转出：按库余额调 OUT。对齐旧 onetransferout。</summary>
    /// <param name="userId">会员</param>
    /// <param name="currency">币种；-1=全部</param>
    /// <returns>结果串</returns>
    [ApiDescriptionSettings(Name = "OneTransferOut"), HttpPost]
    [DisplayName("一键转出")]
    public async Task<object> OneTransferOut([FromQuery] int userId, [FromQuery] int currency = -1)
    {
        var balances = await _svc.GetUserBalanceListAsync(userId, currency, App.HttpContext?.RequestAborted ?? default);
        var siteId = _site.RequireSiteId();
        var msgs = new List<string>();
        foreach (var b in balances.Where(x => x.Balance > 0))
        {
            var money = Math.Floor(b.Balance);
            if (money <= 0)
                continue;
            var r = await _transfer.TransferToCenterAsync(siteId, userId, b.Type, money, b.Currency);
            msgs.Add($"{b.Type}:{(r.Success == 1 ? "成功" : r.Msg)}");
        }

        return new { data = msgs };
    }
}
