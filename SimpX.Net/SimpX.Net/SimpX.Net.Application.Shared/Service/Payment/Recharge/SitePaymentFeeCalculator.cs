namespace SimpX.Net.Application.Shared;

/// <summary>
/// 站点支付手续费/成本。对齐旧 SitePayment.GetFee / GetCost。
/// </summary>
public static class SitePaymentFeeCalculator
{
    /// <summary>
    /// 按比例算手续费，受最低/封顶约束。
    /// </summary>
    /// <param name="payment">通道</param>
    /// <param name="money">到账金额</param>
    /// <returns>手续费</returns>
    public static decimal GetFee(Data.Entity.Sites.SitePayment payment, decimal money)
    {
        ArgumentNullException.ThrowIfNull(payment);
        var fee = money * payment.Fee;
        if (fee < payment.MinFee && payment.MinFee != 0)
            return payment.MinFee;
        if (fee > payment.MaxFee && payment.MaxFee != 0)
            return payment.MaxFee;
        return fee;
    }

    /// <summary>
    /// 按比例算行政成本，受最低/封顶约束。
    /// </summary>
    /// <param name="payment">通道</param>
    /// <param name="money">到账金额</param>
    /// <returns>成本</returns>
    public static decimal GetCost(Data.Entity.Sites.SitePayment payment, decimal money)
    {
        ArgumentNullException.ThrowIfNull(payment);
        var cost = money * payment.Cost;
        if (cost < payment.MinCost && payment.MinCost != 0)
            return payment.MinCost;
        if (cost > payment.MaxCost && payment.MaxCost != 0)
            return payment.MaxCost;
        return cost;
    }
}
