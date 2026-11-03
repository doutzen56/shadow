using SimpX.Net.Data.Entity.Systems;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 总后台提现黑名单。对齐旧 /system/setting/black/*。
/// </summary>
public sealed class SystemBankBlackService : ITransient
{
    private readonly ISqlSugarClient _db;

    /// <summary>
    /// 注入业务库。银行卡黑名单在 BetWin30。
    /// </summary>
    /// <param name="biz">业务库</param>
    public SystemBankBlackService(IBizDbAccessor biz) => _db = biz.Db;

    /// <summary>
    /// 列出黑名单（卡号脱敏）。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行列表</returns>
    public async Task<List<SystemBankBlackListItemDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _db.Queryable<SystemBankAccountBlackList>()
            .OrderBy(p => p.CreateAt, OrderByType.Desc)
            .Take(500)
            .ToListAsync(cancellationToken);
        return rows.Select(p => new SystemBankBlackListItemDto
        {
            ID = p.ID,
            AccountName = p.AccountName ?? string.Empty,
            AccountNumber = Mask(p.AccountNumber),
            Remark = p.Remark ?? string.Empty,
            CreateAt = p.CreateAt
        }).ToList();
    }

    /// <summary>
    /// 新增黑名单。
    /// </summary>
    /// <param name="input">户名与卡号</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task CreateAsync(SystemBankBlackCreateInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (string.IsNullOrWhiteSpace(input.AccountName))
            throw Oops.Oh("#请输入姓名#");
        if (string.IsNullOrWhiteSpace(input.AccountNumber))
            throw Oops.Oh("#请输入银行卡号#");

        var number = input.AccountNumber.Trim();
        if (await _db.Queryable<SystemBankAccountBlackList>()
                .AnyAsync(p => p.AccountNumber == number, cancellationToken))
            throw Oops.Oh("#卡号已在黑名单#");

        await _db.Insertable(new SystemBankAccountBlackList
        {
            AccountName = input.AccountName.Trim(),
            AccountNumber = number,
            Remark = input.Remark ?? string.Empty,
            CreateAt = DateTime.Now
        }).ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 删除黑名单。
    /// </summary>
    /// <param name="id">编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw Oops.Oh("#参数错误#");
        await _db.Deleteable<SystemBankAccountBlackList>().In(id).ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 卡号是否在黑名单（给业务侧校验用；不脱敏）。
    /// </summary>
    /// <param name="accountNumber">卡号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否存在</returns>
    public Task<bool> ExistsAsync(string accountNumber, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(accountNumber))
            return Task.FromResult(false);
        var num = accountNumber.Trim();
        return _db.Queryable<SystemBankAccountBlackList>()
            .AnyAsync(p => p.AccountNumber == num, cancellationToken);
    }

    private static string Mask(string account)
    {
        if (string.IsNullOrEmpty(account) || account.Length < 8)
            return account ?? string.Empty;
        return account[..4] + "****" + account[^4..];
    }
}

/// <summary>提现黑名单行。</summary>
public sealed class SystemBankBlackListItemDto
{
    /// <summary>编号。</summary>
    public int ID { get; set; }

    /// <summary>户名。</summary>
    public string AccountName { get; set; }

    /// <summary>卡号（已脱敏）。</summary>
    public string AccountNumber { get; set; }

    /// <summary>备注。</summary>
    public string Remark { get; set; }

    /// <summary>创建时间。</summary>
    public DateTime CreateAt { get; set; }
}

/// <summary>新增提现黑名单。</summary>
public sealed class SystemBankBlackCreateInput
{
    /// <summary>户名。</summary>
    public string AccountName { get; set; }

    /// <summary>卡号（明文入库）。</summary>
    public string AccountNumber { get; set; }

    /// <summary>备注。</summary>
    public string Remark { get; set; }
}
