using SimpX.Net.Data.Entity.Systems;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 总后台系统充值接口只读。对齐旧 Web.System setting/payment/list 主字段。
/// </summary>
public sealed class SystemPaymentService : ITransient
{
    private readonly ISqlSugarClient _db;

    /// <summary>
    /// 注入业务库。系统充值接口在 BetWin30。
    /// </summary>
    /// <param name="biz">业务库</param>
    public SystemPaymentService(IBizDbAccessor biz) => _db = biz.Db;

    /// <summary>
    /// 列出系统充值接口。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行列表</returns>
    public async Task<List<SystemPaymentListItemDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _db.Queryable<SystemPayment>()
            .OrderBy(p => p.ID, OrderByType.Desc)
            .ToListAsync(cancellationToken);
        return rows.Select(p => new SystemPaymentListItemDto
        {
            ID = p.ID,
            GroupID = p.GroupID,
            Name = p.Name ?? string.Empty,
            IsOpen = p.IsOpen,
            SpecialType = p.SpecialType
        }).ToList();
    }
}

/// <summary>
/// 系统充值接口列表行。
/// </summary>
public sealed class SystemPaymentListItemDto
{
    /// <summary>编号。</summary>
    public int ID { get; set; }

    /// <summary>分组。</summary>
    public int GroupID { get; set; }

    /// <summary>名称。</summary>
    public string Name { get; set; }

    /// <summary>是否开放。</summary>
    public bool IsOpen { get; set; }

    /// <summary>特殊类型。</summary>
    public int SpecialType { get; set; }
}
