using SimpX.Net.Data.Entity.Systems;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 总后台代付接口。对齐旧 /system/setting/withdraw/*。
/// </summary>
public sealed class SystemWithdrawService : ITransient
{
    private readonly ISqlSugarClient _db;

    /// <summary>
    /// 注入业务库。系统代付接口在 BetWin30。
    /// </summary>
    /// <param name="biz">业务库</param>
    public SystemWithdrawService(IBizDbAccessor biz) => _db = biz.Db;

    /// <summary>
    /// 列出代付接口。
    /// </summary>
    /// <param name="name">名称模糊；空不限</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行列表</returns>
    public async Task<List<SystemWithdrawListItemDto>> ListAsync(
        string name = null,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Queryable<SystemWithdraw>();
        if (!string.IsNullOrWhiteSpace(name))
            query = query.Where(p => p.Name.Contains(name.Trim()));
        var rows = await query.OrderBy(p => p.ID, OrderByType.Desc).ToListAsync(cancellationToken);
        return rows.Select(p => new SystemWithdrawListItemDto
        {
            ID = p.ID,
            Name = p.Name ?? string.Empty,
            IsOpen = p.IsOpen
        }).ToList();
    }

    /// <summary>
    /// 代付接口详情（含配置字符串；不含运行时解析）。
    /// </summary>
    /// <param name="id">编号；0 返回空壳</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>详情</returns>
    public async Task<SystemWithdrawDetailDto> DetailAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            return new SystemWithdrawDetailDto();

        var rows = await _db.Queryable<SystemWithdraw>()
            .Where(p => p.ID == id).Take(1).ToListAsync(cancellationToken);
        var w = rows.FirstOrDefault() ?? throw Oops.Oh("#编号错误#");
        return MapDetail(w);
    }

    /// <summary>
    /// 保存代付接口（新增或更新已有字段）。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>编号</returns>
    public async Task<int> SaveAsync(SystemWithdrawSaveInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (string.IsNullOrWhiteSpace(input.Name))
            throw Oops.Oh("#名称不能为空#");

        SystemWithdraw entity;
        if (input.ID > 0)
        {
            var rows = await _db.Queryable<SystemWithdraw>()
                .Where(p => p.ID == input.ID).Take(1).ToListAsync(cancellationToken);
            entity = rows.FirstOrDefault() ?? throw Oops.Oh("#编号错误#");
            entity.Name = input.Name.Trim();
            entity.IsOpen = input.IsOpen;
            if (input.FieldString != null) entity.FieldString = input.FieldString;
            if (input.SignString != null) entity.SignString = input.SignString;
            if (input.SubmitString != null) entity.SubmitString = input.SubmitString;
            if (input.CallbackString != null) entity.CallbackString = input.CallbackString;
            if (input.BankString != null) entity.BankString = input.BankString;
            await _db.Updateable(entity).ExecuteCommandAsync(cancellationToken);
            return entity.ID;
        }

        entity = new SystemWithdraw
        {
            Name = input.Name.Trim(),
            IsOpen = input.IsOpen,
            FieldString = input.FieldString ?? string.Empty,
            SignString = input.SignString ?? string.Empty,
            SubmitString = input.SubmitString ?? string.Empty,
            CallbackString = input.CallbackString ?? string.Empty,
            BankString = input.BankString ?? string.Empty
        };
        return await _db.Insertable(entity).ExecuteReturnIdentityAsync(cancellationToken);
    }

    /// <summary>
    /// 改名称。
    /// </summary>
    /// <param name="id">编号</param>
    /// <param name="name">名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task UpdateNameAsync(int id, string name, CancellationToken cancellationToken = default)
    {
        if (id <= 0 || string.IsNullOrWhiteSpace(name))
            throw Oops.Oh("#参数错误#");
        var n = await _db.Updateable<SystemWithdraw>()
            .SetColumns(p => p.Name == name.Trim())
            .Where(p => p.ID == id)
            .ExecuteCommandAsync(cancellationToken);
        if (n <= 0)
            throw Oops.Oh("#编号错误#");
    }

    /// <summary>
    /// 改是否开放。
    /// </summary>
    /// <param name="id">编号</param>
    /// <param name="isOpen">是否开放</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task UpdateIsOpenAsync(int id, bool isOpen, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw Oops.Oh("#参数错误#");
        var n = await _db.Updateable<SystemWithdraw>()
            .SetColumns(p => p.IsOpen == isOpen)
            .Where(p => p.ID == id)
            .ExecuteCommandAsync(cancellationToken);
        if (n <= 0)
            throw Oops.Oh("#编号错误#");
    }

    private static SystemWithdrawDetailDto MapDetail(SystemWithdraw w) => new()
    {
        ID = w.ID,
        Name = w.Name ?? string.Empty,
        IsOpen = w.IsOpen,
        FieldString = w.FieldString ?? string.Empty,
        SignString = w.SignString ?? string.Empty,
        SubmitString = w.SubmitString ?? string.Empty,
        CallbackString = w.CallbackString ?? string.Empty,
        BankString = w.BankString ?? string.Empty
    };
}

/// <summary>代付接口列表行。</summary>
public sealed class SystemWithdrawListItemDto
{
    /// <summary>编号。</summary>
    public int ID { get; set; }

    /// <summary>名称。</summary>
    public string Name { get; set; }

    /// <summary>是否开放。</summary>
    public bool IsOpen { get; set; }
}

/// <summary>代付接口详情。</summary>
public sealed class SystemWithdrawDetailDto
{
    /// <summary>编号。</summary>
    public int ID { get; set; }

    /// <summary>名称。</summary>
    public string Name { get; set; }

    /// <summary>是否开放。</summary>
    public bool IsOpen { get; set; }

    /// <summary>字段配置。</summary>
    public string FieldString { get; set; }

    /// <summary>签名配置。</summary>
    public string SignString { get; set; }

    /// <summary>提交配置。</summary>
    public string SubmitString { get; set; }

    /// <summary>回调配置。</summary>
    public string CallbackString { get; set; }

    /// <summary>银行配置。</summary>
    public string BankString { get; set; }
}

/// <summary>保存代付接口。</summary>
public sealed class SystemWithdrawSaveInput
{
    /// <summary>0=新增。</summary>
    public int ID { get; set; }

    /// <summary>名称。</summary>
    public string Name { get; set; }

    /// <summary>是否开放。</summary>
    public bool IsOpen { get; set; }

    /// <summary>字段配置；null 表示不改。</summary>
    public string FieldString { get; set; }

    /// <summary>签名配置；null 表示不改。</summary>
    public string SignString { get; set; }

    /// <summary>提交配置；null 表示不改。</summary>
    public string SubmitString { get; set; }

    /// <summary>回调配置；null 表示不改。</summary>
    public string CallbackString { get; set; }

    /// <summary>银行配置；null 表示不改。</summary>
    public string BankString { get; set; }
}

/// <summary>改开放状态。</summary>
public sealed class SystemWithdrawUpdateOpenInput
{
    /// <summary>编号。</summary>
    public int ID { get; set; }

    /// <summary>是否开放。</summary>
    public bool IsOpen { get; set; }
}

/// <summary>改名称。</summary>
public sealed class SystemWithdrawUpdateNameInput
{
    /// <summary>编号。</summary>
    public int ID { get; set; }

    /// <summary>名称。</summary>
    public string Name { get; set; }
}

/// <summary>代付列表筛选。</summary>
public sealed class SystemWithdrawListInput
{
    /// <summary>名称模糊。</summary>
    public string Name { get; set; }
}
