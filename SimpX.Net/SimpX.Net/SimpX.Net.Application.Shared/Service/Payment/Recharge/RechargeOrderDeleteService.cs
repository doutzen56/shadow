using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 拒绝/删除未到账充值订单。对齐旧 UserAgent.DeleteRechargeOrder（含操作日志）。
/// </summary>
public sealed class RechargeOrderDeleteService : ITransient
{
    /// <summary>旧 SiteAdminLog.LogType.Money。</summary>
    public const int AdminLogTypeMoney = 4;

    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;

    /// <summary>
    /// 注入库与定站。
    /// </summary>
    public RechargeOrderDeleteService(IBizDbAccessor biz, ICurrentSiteAccessor site)
    {
        _db = biz.Db;
        _site = site;
    }

    /// <summary>
    /// 软删未到账单；已到账拒绝。有关联线下入款单时一并软删。
    /// </summary>
    /// <param name="orderId">订单号</param>
    /// <param name="adminId">操作管理员；0 表示系统/未知</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task DeleteAsync(
        string orderId,
        int adminId = 0,
        CancellationToken cancellationToken = default)
    {
        var id = (orderId ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(id))
            throw new InvalidOperationException("#订单号错误#");

        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<RechargeOrder>()
            .Where(o => o.SiteID == siteId && o.ID == id)
            .Take(1)
            .ToListAsync(cancellationToken);
        var order = rows.FirstOrDefault();
        if (order == null)
            throw new InvalidOperationException("#订单号错误#");
        if (order.IsPay)
            throw new InvalidOperationException("#该笔充值订单已经入账#");
        if (order.IsDeleted)
            return;

        await _db.Ado.BeginTranAsync();
        try
        {
            await _db.Updateable<BankRechargeOrder>()
                .SetColumns(b => new BankRechargeOrder { IsDeleted = true })
                .Where(b => b.SiteID == siteId && b.OrderID == id && !b.IsDeleted)
                .ExecuteCommandAsync(cancellationToken);

            var n = await _db.Updateable<RechargeOrder>()
                .SetColumns(o => new RechargeOrder { IsDeleted = true })
                .Where(o => o.SiteID == siteId && o.ID == id && !o.IsPay && !o.IsDeleted)
                .ExecuteCommandAsync(cancellationToken);
            if (n == 0)
                throw new InvalidOperationException("#该笔充值订单已经入账#");

            await _db.Insertable(new SiteAdminLog
            {
                SiteID = siteId,
                AdminID = adminId,
                Type = AdminLogTypeMoney,
                Content = "DeleteRechargeOrder:" + id,
                IP = string.Empty,
                IPAddress = string.Empty,
                CreateAt = DateTime.Now,
                RawData = id
            }).ExecuteCommandAsync(cancellationToken);

            await _db.Ado.CommitTranAsync();
        }
        catch
        {
            await _db.Ado.RollbackTranAsync();
            throw;
        }
    }
}
