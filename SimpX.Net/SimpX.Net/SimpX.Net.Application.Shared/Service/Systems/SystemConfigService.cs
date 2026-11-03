using SimpX.Net.Data.Entity.Systems;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 总后台参数配置（sys_Config 键值）。对齐旧 /setting/config。
/// </summary>
public sealed class SystemConfigService : ITransient
{
    private readonly ISqlSugarClient _db;

    /// <summary>
    /// 注入业务库。总后台参数在 BetWin30。
    /// </summary>
    /// <param name="biz">业务库</param>
    public SystemConfigService(IBizDbAccessor biz) => _db = biz.Db;

    /// <summary>
    /// 列表（可按名称模糊）。
    /// </summary>
    /// <param name="name">名称模糊</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行列表</returns>
    public async Task<List<SystemConfigItemDto>> ListAsync(
        string name = null,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Queryable<SystemConfigEntry>();
        if (!string.IsNullOrWhiteSpace(name))
            query = query.Where(c => c.Name.Contains(name.Trim()));
        var rows = await query.OrderBy(c => c.Name).Take(500).ToListAsync(cancellationToken);
        return rows.Select(c => new SystemConfigItemDto
        {
            Name = c.Name ?? string.Empty,
            Value = Truncate(c.Value, 200)
        }).ToList();
    }

    /// <summary>
    /// 按名读取完整值。
    /// </summary>
    /// <param name="name">键名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>配置</returns>
    public async Task<SystemConfigItemDto> GetAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw Oops.Oh("#名称不能为空#");
        var rows = await _db.Queryable<SystemConfigEntry>()
            .Where(c => c.Name == name.Trim()).Take(1).ToListAsync(cancellationToken);
        var c = rows.FirstOrDefault() ?? throw Oops.Oh("#记录不存在#");
        return new SystemConfigItemDto { Name = c.Name, Value = c.Value ?? string.Empty };
    }

    /// <summary>
    /// 保存（无则插入）。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SaveAsync(SystemConfigSaveInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (string.IsNullOrWhiteSpace(input.Name))
            throw Oops.Oh("#名称不能为空#");
        var name = input.Name.Trim();
        var rows = await _db.Queryable<SystemConfigEntry>()
            .Where(c => c.Name == name).Take(1).ToListAsync(cancellationToken);
        var row = rows.FirstOrDefault();
        if (row is null)
        {
            await _db.Insertable(new SystemConfigEntry { Name = name, Value = input.Value ?? string.Empty })
                .ExecuteCommandAsync(cancellationToken);
            return;
        }

        row.Value = input.Value ?? string.Empty;
        await _db.Updateable(row).ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 删除键。
    /// </summary>
    /// <param name="name">键名</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task DeleteAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw Oops.Oh("#名称不能为空#");
        await _db.Deleteable<SystemConfigEntry>()
            .Where(c => c.Name == name.Trim())
            .ExecuteCommandAsync(cancellationToken);
    }

    private static string Truncate(string s, int max)
    {
        if (string.IsNullOrEmpty(s) || s.Length <= max)
            return s ?? string.Empty;
        return s[..max] + "…";
    }
}

/// <summary>配置项。</summary>
public sealed class SystemConfigItemDto
{
    /// <summary>键名。</summary>
    public string Name { get; set; }

    /// <summary>值。</summary>
    public string Value { get; set; }
}

/// <summary>保存配置。</summary>
public sealed class SystemConfigSaveInput
{
    /// <summary>键名。</summary>
    public string Name { get; set; }

    /// <summary>值。</summary>
    public string Value { get; set; }
}
