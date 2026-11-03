using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 提现订单审核与出款。人工（SysID=0）当场账变；代付（SysID≠0）走网关→付款中，再回调确认。
/// </summary>
public sealed class WithdrawOrderOperateService : ITransient
{
    /// <summary>待审核。</summary>
    public const int StatusNone = 0;

    /// <summary>审核失败。</summary>
    public const int StatusFailed = 1;

    /// <summary>付款中（代付已提交）。</summary>
    public const int StatusInPayment = 2;

    /// <summary>已到账。</summary>
    public const int StatusFinish = 3;

    /// <summary>待处理（审核通过等出款）。</summary>
    public const int StatusWait = 10;

    /// <summary>提现账变类型。</summary>
    public const int MoneyTypeWithdraw = 2;

    /// <summary>提现手续费账变类型。</summary>
    public const int MoneyTypeWithdrawFee = 4;

    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;
    private readonly IMoneyLedger _money;
    private readonly IWithdrawGateway _gateway;

    /// <summary>注入。</summary>
    public WithdrawOrderOperateService(
        IBizDbAccessor biz,
        ICurrentSiteAccessor site,
        IMoneyLedger money,
        IWithdrawGateway gateway)
    {
        _db = biz.Db;
        _site = site;
        _money = money;
        _gateway = gateway;
    }

    /// <summary>
    /// 审核。Agree→待处理；Reject→失败并解锁提现锁定。
    /// </summary>
    public async Task CheckAsync(
        WithdrawOrderCheckInput input,
        int adminId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.OrderID <= 0)
            throw Oops.Oh("#订单编号错误#");
        var action = (input.Action ?? string.Empty).Trim();
        if (action is not ("Agree" or "Reject"))
            throw Oops.Oh("#未指定的审核动作#");

        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<WithdrawOrder>()
            .Where(o => o.SiteID == siteId && o.ID == input.OrderID)
            .Take(1)
            .ToListAsync(cancellationToken);
        var order = rows.FirstOrDefault() ?? throw Oops.Oh("#订单编号错误#");
        if (order.Status != StatusNone)
            throw Oops.Oh("#订单已被处理#");

        var desc = $"{input.Description ?? string.Empty} | {input.DescriptionA ?? string.Empty}".Trim();
        order.AdminID = adminId;
        order.AuditAt = DateTime.Now;
        order.Description = desc;
        order.Mark = input.Mark ?? string.Empty;

        await _db.Ado.BeginTranAsync();
        try
        {
            if (action == "Agree")
            {
                order.Status = StatusWait;
            }
            else
            {
                order.Status = StatusFailed;
                order.PayAt = DateTime.Now;
                await _money.UnlockWithdrawAsync(
                    siteId,
                    order.UserID,
                    order.ID,
                    "#拒绝审核#",
                    order.Currency,
                    cancellationToken);
            }

            await _db.Updateable(order)
                .UpdateColumns(o => new { o.AdminID, o.Description, o.Mark, o.Status, o.PayAt, o.AuditAt })
                .ExecuteCommandAsync(cancellationToken);
            await InsertLogAsync(siteId, order.ID, order.Status, adminId, cancellationToken);
            await _db.Ado.CommitTranAsync();
        }
        catch
        {
            await _db.Ado.RollbackTranAsync();
            throw;
        }
    }

    /// <summary>
    /// 出款。SysID=0 人工当场账变；SysID≠0 调代付网关，成功则改付款中（资金仍锁着，等回调确认）。
    /// </summary>
    public async Task ExecAsync(
        WithdrawOrderExecInput input,
        int adminId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.OrderID <= 0)
            throw Oops.Oh("#订单编号错误#");
        var action = (input.Action ?? string.Empty).Trim();
        if (action is not ("Agree" or "Reject"))
            throw Oops.Oh("#未指定的处理动作#");
        if (input.Fee < 0)
            throw Oops.Oh("#手续费错误#");

        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<WithdrawOrder>()
            .Where(o => o.SiteID == siteId && o.ID == input.OrderID)
            .Take(1)
            .ToListAsync(cancellationToken);
        var order = rows.FirstOrDefault() ?? throw Oops.Oh("#订单编号错误#");
        if (order.Status != StatusWait)
            throw Oops.Oh("#订单已被处理或未被审核#");

        var fee = Math.Round(input.Fee, 2, MidpointRounding.AwayFromZero);
        var desc = $"{input.Description ?? string.Empty} | {input.DescriptionA ?? string.Empty}".Trim();
        order.CashierID = adminId;
        order.Description = Truncate(desc, 45);
        order.Mark = Truncate(input.Mark ?? string.Empty, 45);

        await _db.Ado.BeginTranAsync();
        try
        {
            if (action == "Reject")
            {
                order.Status = StatusFailed;
                order.PayAt = DateTime.Now;
                await _money.UnlockWithdrawAsync(
                    siteId,
                    order.UserID,
                    order.ID,
                    "#出款拒绝#",
                    order.Currency,
                    cancellationToken);
            }
            else
            {
                if (input.ChannelID <= 0)
                    throw Oops.Oh("#渠道编号错误或维护中#");
                var channels = await _db.Queryable<WithdrawChannel>()
                    .Where(c => c.SiteID == siteId && c.ID == input.ChannelID && c.IsOpen && !c.IsDeleted)
                    .Take(1)
                    .ToListAsync(cancellationToken);
                var channel = channels.FirstOrDefault() ?? throw Oops.Oh("#渠道编号错误或维护中#");
                order.Fee = fee;
                order.ChannelID = channel.ID;

                if (channel.SysID == 0)
                {
                    await ApplyManualPayoutAsync(siteId, order, fee, cancellationToken);
                }
                else
                {
                    await ApplyGatewaySubmitAsync(siteId, order, channel, fee, cancellationToken);
                }
            }

            await _db.Updateable(order)
                .UpdateColumns(o => new
                {
                    o.CashierID,
                    o.Description,
                    o.Mark,
                    o.Fee,
                    o.Paid,
                    o.ChannelID,
                    o.Status,
                    o.PayAt,
                    o.SystemID
                })
                .ExecuteCommandAsync(cancellationToken);
            await InsertLogAsync(siteId, order.ID, order.Status, adminId, cancellationToken);
            await _db.Ado.CommitTranAsync();
        }
        catch
        {
            await _db.Ado.RollbackTranAsync();
            throw;
        }
    }

    /// <summary>
    /// 代付回调确认到账：付款中→解锁+扣款→已到账。
    /// </summary>
    /// <param name="orderId">订单号</param>
    /// <param name="adminId">操作人；网关匿名回调传 0</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <param name="siteIdOverride">按订单/渠道定站时传入，跳过当前站访问器</param>
    public async Task ConfirmGatewayAsync(
        int orderId,
        int adminId,
        CancellationToken cancellationToken = default,
        int? siteIdOverride = null)
    {
        if (orderId <= 0)
            throw Oops.Oh("#订单编号错误#");
        var siteId = siteIdOverride ?? _site.RequireSiteId();
        var rows = await _db.Queryable<WithdrawOrder>()
            .Where(o => o.SiteID == siteId && o.ID == orderId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var order = rows.FirstOrDefault() ?? throw Oops.Oh("#订单编号错误#");
        if (order.Status != StatusInPayment)
            throw Oops.Oh("#订单不是付款中#");

        var fee = order.Fee;
        order.CashierID = adminId;
        await _db.Ado.BeginTranAsync();
        try
        {
            await ApplyManualPayoutAsync(siteId, order, fee, cancellationToken);
            await _db.Updateable(order)
                .UpdateColumns(o => new { o.CashierID, o.Paid, o.Status, o.PayAt })
                .ExecuteCommandAsync(cancellationToken);
            await InsertLogAsync(siteId, order.ID, order.Status, adminId, cancellationToken);
            await _db.Ado.CommitTranAsync();
        }
        catch
        {
            await _db.Ado.RollbackTranAsync();
            throw;
        }
    }

    /// <summary>
    /// 代付查单。付款中订单调网关查单；若 Stub/供应商回报可完结则不自动扣款，只回查结果给运营。
    /// </summary>
    public async Task<WithdrawGatewayResult> QueryGatewayAsync(
        int orderId,
        CancellationToken cancellationToken = default)
    {
        if (orderId <= 0)
            throw Oops.Oh("#订单编号错误#");
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<WithdrawOrder>()
            .Where(o => o.SiteID == siteId && o.ID == orderId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var order = rows.FirstOrDefault() ?? throw Oops.Oh("#订单编号错误#");
        if (order.Status != StatusInPayment)
            throw Oops.Oh("#仅付款中可查单#");

        var channels = await _db.Queryable<WithdrawChannel>()
            .Where(c => c.SiteID == siteId && c.ID == order.ChannelID)
            .Take(1)
            .ToListAsync(cancellationToken);
        var channel = channels.FirstOrDefault();
        return await _gateway.QueryAsync(
            new WithdrawGatewayQueryRequest
            {
                ChannelID = order.ChannelID,
                OrderID = order.ID,
                Money = order.Paid > 0 ? order.Paid : order.Money - order.Fee,
                SystemID = order.SystemID ?? string.Empty,
                SysID = channel?.SysID ?? 0,
                ChannelSetting = channel?.Setting,
                ChannelQuerySetting = channel?.QuerySetting
            },
            cancellationToken);
    }

    /// <summary>
    /// 处理代付回调：验签 → 校验渠道 → 成功确认到账 / 失败回待处理（对齐旧 1012：不解锁，资金仍锁着）。
    /// </summary>
    /// <param name="raw">回调原文包</param>
    /// <param name="adminId">操作人；网关匿名回调传 0</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <param name="siteIdOverride">按渠道/订单定站时传入，跳过当前站访问器</param>
    public async Task HandleGatewayCallbackAsync(
        WithdrawGatewayCallbackRaw raw,
        int adminId,
        CancellationToken cancellationToken = default,
        int? siteIdOverride = null)
    {
        ArgumentNullException.ThrowIfNull(raw);
        var payload = _gateway.VerifyCallback(raw);
        var siteId = siteIdOverride ?? _site.RequireSiteId();
        var rows = await _db.Queryable<WithdrawOrder>()
            .Where(o => o.SiteID == siteId && o.ID == payload.OrderID)
            .Take(1)
            .ToListAsync(cancellationToken);
        var order = rows.FirstOrDefault() ?? throw Oops.Oh("#提现订单不存在#");
        if (order.ChannelID != payload.ChannelID && payload.ChannelID > 0)
            throw Oops.Oh("#提现渠道不匹配#");
        if (order.Status != StatusInPayment)
            throw Oops.Oh("#订单不是付款中#");

        if (payload.Success)
        {
            if (payload.Money > 0)
            {
                order.Paid = payload.Money;
                await _db.Updateable(order)
                    .UpdateColumns(o => new { o.Paid })
                    .ExecuteCommandAsync(cancellationToken);
            }

            await ConfirmGatewayAsync(order.ID, adminId, cancellationToken, siteId);
        }
        else
        {
            await FailGatewayAsync(order.ID, adminId, "#订单回调失败#", cancellationToken, siteId);
        }
    }

    /// <summary>
    /// 代付失败回待处理：付款中→Wait，不解锁（对齐旧核对失败 1012；解锁代码在旧版已注释掉）。
    /// </summary>
    /// <param name="orderId">订单号</param>
    /// <param name="adminId">操作人；网关匿名回调传 0</param>
    /// <param name="reason">失败原因</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <param name="siteIdOverride">按渠道/订单定站时传入，跳过当前站访问器</param>
    public async Task FailGatewayAsync(
        int orderId,
        int adminId,
        string reason,
        CancellationToken cancellationToken = default,
        int? siteIdOverride = null)
    {
        if (orderId <= 0)
            throw Oops.Oh("#订单编号错误#");
        var siteId = siteIdOverride ?? _site.RequireSiteId();
        var rows = await _db.Queryable<WithdrawOrder>()
            .Where(o => o.SiteID == siteId && o.ID == orderId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var order = rows.FirstOrDefault() ?? throw Oops.Oh("#订单编号错误#");
        if (order.Status != StatusInPayment)
            throw Oops.Oh("#订单不是付款中#");

        order.CashierID = adminId;
        order.Status = StatusWait;
        order.PayAt = DateTime.Now;
        order.Description = Truncate(
            string.IsNullOrWhiteSpace(reason) ? "#代付失败#" : reason,
            45);
        await _db.Ado.BeginTranAsync();
        try
        {
            // 旧版此处不解锁，资金继续锁着，运营可再出款或人工拒绝解锁。
            await _db.Updateable(order)
                .UpdateColumns(o => new { o.CashierID, o.Status, o.PayAt, o.Description })
                .ExecuteCommandAsync(cancellationToken);
            await InsertLogAsync(siteId, order.ID, order.Status, adminId, cancellationToken);
            await _db.Ado.CommitTranAsync();
        }
        catch
        {
            await _db.Ado.RollbackTranAsync();
            throw;
        }
    }

    /// <summary>人工出款：解锁并扣本金/手续费，置已到账。</summary>
    private async Task ApplyManualPayoutAsync(
        int siteId,
        WithdrawOrder order,
        decimal fee,
        CancellationToken cancellationToken)
    {
        await _money.UnlockWithdrawAsync(
            siteId,
            order.UserID,
            order.ID,
            "#审核通过#",
            order.Currency,
            cancellationToken);

        var principal = order.Money - fee;
        await _money.AddMoneyLogAsync(
            siteId,
            order.UserID,
            -Math.Abs(principal),
            MoneyTypeWithdraw,
            order.ID.ToString(),
            "提现",
            order.Currency,
            cancellationToken);
        if (fee != 0)
        {
            await _money.AddMoneyLogAsync(
                siteId,
                order.UserID,
                -Math.Abs(fee),
                MoneyTypeWithdrawFee,
                order.ID.ToString(),
                "提现",
                order.Currency,
                cancellationToken);
        }

        order.Paid = order.Money - fee;
        order.Status = StatusFinish;
        order.PayAt = DateTime.Now;
    }

    /// <summary>代付提交：调网关；仅 Paymenting/Repeat 改付款中，Failed 整笔回滚提示。</summary>
    private async Task ApplyGatewaySubmitAsync(
        int siteId,
        WithdrawOrder order,
        WithdrawChannel channel,
        decimal fee,
        CancellationToken cancellationToken)
    {
        var paid = order.Money - fee;
        var result = await _gateway.SubmitAsync(
            new WithdrawGatewayRequest
            {
                SiteID = siteId,
                ChannelID = channel.ID,
                SysID = channel.SysID,
                OrderID = order.ID,
                Bank = order.Bank,
                AccountName = order.AccountName ?? string.Empty,
                AccountNumber = order.AccountNumber ?? string.Empty,
                Money = paid,
                Currency = order.Currency,
                ChannelSetting = channel.Setting,
                ChannelQuerySetting = channel.QuerySetting
            },
            cancellationToken);

        if (result.Status == WithdrawGatewayStatus.Failed)
            throw Oops.Oh(string.IsNullOrWhiteSpace(result.Message) ? "#代付提交失败#" : result.Message);

        order.Paid = paid;
        order.Status = StatusInPayment;
        order.SystemID = string.IsNullOrWhiteSpace(result.SystemID)
            ? $"GW{order.ID}"
            : result.SystemID;
        if (!string.IsNullOrWhiteSpace(result.Message))
            order.Description = Truncate(result.Message, 45);
    }

    private async Task InsertLogAsync(int siteId, int orderId, int status, int adminId, CancellationToken ct)
    {
        await _db.Insertable(new WithdrawOrderLog
        {
            SiteID = siteId,
            OrderID = orderId,
            Status = status,
            CreateAt = DateTime.Now,
            AdminID = adminId,
            IP = string.Empty
        }).ExecuteCommandAsync(ct);
    }

    private static string Truncate(string s, int max) =>
        string.IsNullOrEmpty(s) || s.Length <= max ? (s ?? string.Empty) : s[..max];
}

/// <summary>提现审核入参。</summary>
public sealed class WithdrawOrderCheckInput
{
    /// <summary>订单号。</summary>
    public int OrderID { get; set; }

    /// <summary>Agree 或 Reject。</summary>
    public string Action { get; set; }

    /// <summary>备注前段。</summary>
    public string Description { get; set; }

    /// <summary>备注后段。</summary>
    public string DescriptionA { get; set; }

    /// <summary>内部标注。</summary>
    public string Mark { get; set; }
}

/// <summary>提现处理入参。</summary>
public sealed class WithdrawOrderExecInput
{
    /// <summary>订单号。</summary>
    public int OrderID { get; set; }

    /// <summary>渠道。</summary>
    public int ChannelID { get; set; }

    /// <summary>Agree 或 Reject。</summary>
    public string Action { get; set; }

    /// <summary>手续费。</summary>
    public decimal Fee { get; set; }

    /// <summary>备注前段。</summary>
    public string Description { get; set; }

    /// <summary>备注后段。</summary>
    public string DescriptionA { get; set; }

    /// <summary>内部标注。</summary>
    public string Mark { get; set; }
}
