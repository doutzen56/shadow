using SimpX.Net.Data.Entity.Games;
using SqlSugar;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 游戏转账编排。对齐旧 CreateTransferOrder：
/// IN（中心→厂商）先建单、锁钱、调厂商，成功解锁并扣中心钱包（MoneyType=8 负数）；失败解锁。
/// OUT（厂商→中心）建单、调厂商，成功加中心钱包（MoneyType=5 正数）。
/// </summary>
public sealed class GameTransferService : ITransient
{
    /// <summary>DirectionType.None。</summary>
    public const int ActionNone = 0;

    /// <summary>DirectionType.IN：转入游戏（中心→厂商）。</summary>
    public const int ActionIn = 1;

    /// <summary>DirectionType.OUT：游戏转出（厂商→中心）。</summary>
    public const int ActionOut = 2;

    /// <summary>CheckStatus.None 待处理。</summary>
    public const int StatusNone = 0;

    /// <summary>CheckStatus.Faild 失败。</summary>
    public const int StatusFail = 1;

    /// <summary>CheckStatus.Success 成功。</summary>
    public const int StatusSuccess = 2;

    private readonly ISqlSugarClient _db;
    private readonly IMoneyLedger _money;
    private readonly StubGameVendorTransfer _stub;
    private readonly IEnumerable<IGameVendorTransfer> _vendors;

    /// <summary>
    /// 注入业务库、账本、Stub 与全部厂商转账适配。
    /// 按 GameType 选 CanHandle 命中者；未命中回落 Stub。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="money">资金账本</param>
    /// <param name="stub">未认领时的 Stub</param>
    /// <param name="vendors">已登记的厂商转账适配</param>
    public GameTransferService(
        IBizDbAccessor biz,
        IMoneyLedger money,
        StubGameVendorTransfer stub,
        IEnumerable<IGameVendorTransfer> vendors)
    {
        _db = biz.Db;
        _money = money;
        _stub = stub ?? throw new ArgumentNullException(nameof(stub));
        _vendors = vendors ?? Array.Empty<IGameVendorTransfer>();
    }

    /// <summary>
    /// 按 GameType 选厂商；无人认领则 Stub。
    /// </summary>
    /// <param name="gameType">游戏类型</param>
    /// <returns>适配</returns>
    private IGameVendorTransfer ResolveVendor(int gameType) =>
        _vendors.FirstOrDefault(v => v != null && v.CanHandle(gameType)) ?? _stub;

    /// <summary>
    /// 中心钱包转入游戏。Action=IN；先锁钱再调厂商。
    /// 成功：解锁 + 账变 type=GameTransferIn(8)、金额为负；订单 Success。
    /// 失败：解锁；订单 Fail。
    /// </summary>
    /// <param name="siteId">站点</param>
    /// <param name="userId">会员</param>
    /// <param name="gameType">游戏类型</param>
    /// <param name="money">金额（正数）</param>
    /// <param name="currency">币种</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>旧版返回</returns>
    public async Task<OldApiResult> TransferToGameAsync(
        int siteId,
        int userId,
        int gameType,
        decimal money,
        int currency,
        CancellationToken cancellationToken = default)
    {
        if (siteId <= 0 || userId <= 0)
            return OldApiResult.Fail("#参数错误#");
        if (money <= 0)
            return OldApiResult.Fail("#金额错误#");

        var order = new GameTransferOrder
        {
            SiteID = siteId,
            UserID = userId,
            Type = gameType,
            CreateAt = DateTime.Now,
            CheckAt = DateTime.MinValue,
            Money = money,
            Status = StatusNone,
            Action = ActionIn,
            Description = string.Empty,
            ResultID = string.Empty,
            Currency = currency
        };
        var orderId = await _db.Insertable(order).ExecuteReturnIdentityAsync(cancellationToken);
        if (orderId <= 0)
            return OldApiResult.Fail("#转账编号错误#");
        order.ID = orderId;

        try
        {
            await _money.LockGameTransferAsync(
                siteId,
                userId,
                money,
                orderId,
                $"#转入#:{gameType}",
                currency,
                cancellationToken);
        }
        catch (Exception ex)
        {
            await MarkOrderAsync(order, StatusFail, ex.Message, null, cancellationToken);
            return OldApiResult.Fail(string.IsNullOrWhiteSpace(ex.Message) ? "#可用余额不足#" : ex.Message);
        }

        var vendor = await ResolveVendor(gameType).TransferAsync(
            new GameVendorTransferContext
            {
                SiteId = siteId,
                UserId = userId,
                GameType = gameType,
                OrderId = orderId,
                Money = money,
                Currency = currency,
                Action = ActionIn
            },
            cancellationToken);

        if (vendor == null || !vendor.Ok)
        {
            var failMsg = vendor?.Message;
            if (string.IsNullOrWhiteSpace(failMsg))
                failMsg = "#转账失败#";
            try
            {
                await _money.UnlockGameTransferAsync(
                    siteId,
                    userId,
                    orderId,
                    $"#转入游戏# {gameType} #失败#",
                    currency,
                    cancellationToken);
            }
            catch
            {
                // 解锁失败仍把订单标失败，避免一直挂着
            }

            await MarkOrderAsync(order, StatusFail, failMsg, vendor?.ResultId, cancellationToken);
            return OldApiResult.Fail(failMsg);
        }

        var desc = $"{gameType}转入";
        await _money.UnlockGameTransferAsync(siteId, userId, orderId, desc, currency, cancellationToken);
        // 旧 UpdateTransferOrderMoney：IN 成功 → AddMoneyLog(money*-1, GameTransferIn=8)
        await _money.AddMoneyLogAsync(
            siteId,
            userId,
            money * -1m,
            MoneyLedgerService.MoneyTypeGameTransferIn,
            orderId.ToString(),
            desc,
            currency,
            cancellationToken);
        await MarkOrderAsync(order, StatusSuccess, vendor.Message ?? "#成功#", vendor.ResultId, cancellationToken);
        return OldApiResult.Ok("#成功#");
    }

    /// <summary>
    /// 从游戏转回中心钱包。Action=OUT；旧版不锁中心钱包。
    /// 成功：账变 type=GameTransferOut(5)、金额为正；订单 Success。
    /// </summary>
    /// <param name="siteId">站点</param>
    /// <param name="userId">会员</param>
    /// <param name="gameType">游戏类型</param>
    /// <param name="money">金额（正数）</param>
    /// <param name="currency">币种</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>旧版返回</returns>
    public async Task<OldApiResult> TransferToCenterAsync(
        int siteId,
        int userId,
        int gameType,
        decimal money,
        int currency,
        CancellationToken cancellationToken = default)
    {
        if (siteId <= 0 || userId <= 0)
            return OldApiResult.Fail("#参数错误#");
        if (money <= 0)
            return OldApiResult.Fail("#金额错误#");

        var order = new GameTransferOrder
        {
            SiteID = siteId,
            UserID = userId,
            Type = gameType,
            CreateAt = DateTime.Now,
            CheckAt = DateTime.MinValue,
            Money = money,
            Status = StatusNone,
            Action = ActionOut,
            Description = string.Empty,
            ResultID = string.Empty,
            Currency = currency
        };
        var orderId = await _db.Insertable(order).ExecuteReturnIdentityAsync(cancellationToken);
        if (orderId <= 0)
            return OldApiResult.Fail("#转账编号错误#");
        order.ID = orderId;

        var vendor = await ResolveVendor(gameType).TransferAsync(
            new GameVendorTransferContext
            {
                SiteId = siteId,
                UserId = userId,
                GameType = gameType,
                OrderId = orderId,
                Money = money,
                Currency = currency,
                Action = ActionOut
            },
            cancellationToken);

        if (vendor == null || !vendor.Ok)
        {
            var failMsg = vendor?.Message;
            if (string.IsNullOrWhiteSpace(failMsg))
                failMsg = "#转账失败#";
            await MarkOrderAsync(order, StatusFail, failMsg, vendor?.ResultId, cancellationToken);
            return OldApiResult.Fail(failMsg);
        }

        var desc = $"{gameType}转出";
        // 旧 UpdateTransferOrderMoney：OUT 成功 → AddMoneyLog(+money, GameTransferOut=5)
        await _money.AddMoneyLogAsync(
            siteId,
            userId,
            money,
            MoneyLedgerService.MoneyTypeGameTransferOut,
            orderId.ToString(),
            desc,
            currency,
            cancellationToken);
        await MarkOrderAsync(order, StatusSuccess, vendor.Message ?? "#成功#", vendor.ResultId, cancellationToken);
        return OldApiResult.Ok("#成功#");
    }

    /// <summary>
    /// 更新订单状态与备注。
    /// </summary>
    private async Task MarkOrderAsync(
        GameTransferOrder order,
        int status,
        string description,
        string resultId,
        CancellationToken cancellationToken)
    {
        order.Status = status;
        order.CheckAt = DateTime.Now;
        order.Description = description ?? string.Empty;
        if (!string.IsNullOrEmpty(resultId))
            order.ResultID = resultId;
        await _db.Updateable(order)
            .UpdateColumns(o => new { o.Status, o.CheckAt, o.Description, o.ResultID })
            .ExecuteCommandAsync(cancellationToken);
    }
}
