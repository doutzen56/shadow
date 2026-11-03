using SimpX.Net.Data.Entity.Systems;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 总后台标签管理。对齐旧 /system/setting/tag/*（不含机器翻译）。
/// </summary>
public sealed class SystemTagService : ITransient
{
    private readonly ISqlSugarClient _db;

    /// <summary>
    /// 注入业务库。系统标签在 BetWin30。
    /// </summary>
    /// <param name="biz">业务库</param>
    public SystemTagService(IBizDbAccessor biz) => _db = biz.Db;

    /// <summary>
    /// 标签列表。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行列表</returns>
    public async Task<List<SystemTagListItemDto>> ListAsync(
        SystemTagListInput input,
        CancellationToken cancellationToken = default)
    {
        input ??= new SystemTagListInput();
        var query = _db.Queryable<SystemTag>();
        if (input.Type is > 0)
            query = query.Where(t => t.Type == input.Type.Value);
        if (!string.IsNullOrWhiteSpace(input.Code))
            query = query.Where(t => t.Code.Contains(input.Code.Trim()));
        if (!string.IsNullOrWhiteSpace(input.Name))
            query = query.Where(t => t.Name.Contains(input.Name.Trim()));

        var rows = await query.OrderBy(t => t.Sort, OrderByType.Desc).ToListAsync(cancellationToken);
        return rows.Select(t => new SystemTagListItemDto
        {
            Type = t.Type,
            Code = t.Code ?? string.Empty,
            Name = t.Name ?? string.Empty,
            Sort = t.Sort,
            Image = t.Image ?? string.Empty
        }).ToList();
    }

    /// <summary>
    /// 标签详情。
    /// </summary>
    /// <param name="type">类型</param>
    /// <param name="code">码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>详情；不存在时返回空壳</returns>
    public async Task<SystemTagDetailDto> DetailAsync(
        byte type,
        string code,
        CancellationToken cancellationToken = default)
    {
        code ??= string.Empty;
        if (code == "undefined")
            code = string.Empty;
        var rows = await _db.Queryable<SystemTag>()
            .Where(t => t.Type == type && t.Code == code)
            .Take(1)
            .ToListAsync(cancellationToken);
        var tag = rows.FirstOrDefault();
        if (tag is null)
        {
            return new SystemTagDetailDto
            {
                Type = type,
                Code = code,
                Name = string.Empty,
                Setting = string.Empty,
                Image = string.Empty,
                Sort = 0
            };
        }

        return new SystemTagDetailDto
        {
            Type = tag.Type,
            Code = tag.Code ?? string.Empty,
            Name = tag.Name ?? string.Empty,
            Setting = tag.Setting ?? string.Empty,
            Image = tag.Image ?? string.Empty,
            Sort = tag.Sort
        };
    }

    /// <summary>
    /// 保存标签（复合主键 Type+Code；存在则更新）。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SaveAsync(SystemTagSaveInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.Type == 0 || string.IsNullOrWhiteSpace(input.Code))
            throw Oops.Oh("#参数错误#");

        var code = input.Code.Trim();
        var rows = await _db.Queryable<SystemTag>()
            .Where(t => t.Type == input.Type && t.Code == code)
            .Take(1)
            .ToListAsync(cancellationToken);
        var existing = rows.FirstOrDefault();
        if (existing is null)
        {
            await _db.Insertable(new SystemTag
            {
                Type = input.Type,
                Code = code,
                Name = input.Name ?? string.Empty,
                Setting = input.Setting ?? string.Empty,
                Image = input.Image ?? string.Empty,
                Sort = input.Sort
            }).ExecuteCommandAsync(cancellationToken);
            return;
        }

        existing.Name = input.Name ?? string.Empty;
        existing.Setting = input.Setting ?? string.Empty;
        existing.Image = input.Image ?? string.Empty;
        existing.Sort = input.Sort;
        await _db.Updateable(existing).ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 删除标签。
    /// </summary>
    /// <param name="type">类型</param>
    /// <param name="code">码</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task DeleteAsync(byte type, string code, CancellationToken cancellationToken = default)
    {
        if (type == 0 || string.IsNullOrWhiteSpace(code))
            throw Oops.Oh("#参数错误#");
        await _db.Deleteable<SystemTag>()
            .Where(t => t.Type == type && t.Code == code.Trim())
            .ExecuteCommandAsync(cancellationToken);
    }
}

/// <summary>标签列表筛选。</summary>
public sealed class SystemTagListInput
{
    /// <summary>类型；空或 0 不限。</summary>
    public byte? Type { get; set; }

    /// <summary>码模糊。</summary>
    public string Code { get; set; }

    /// <summary>名称模糊。</summary>
    public string Name { get; set; }
}

/// <summary>标签列表行。</summary>
public sealed class SystemTagListItemDto
{
    /// <summary>类型。</summary>
    public byte Type { get; set; }

    /// <summary>码。</summary>
    public string Code { get; set; }

    /// <summary>名称原文。</summary>
    public string Name { get; set; }

    /// <summary>排序。</summary>
    public short Sort { get; set; }

    /// <summary>图片。</summary>
    public string Image { get; set; }
}

/// <summary>标签详情。</summary>
public sealed class SystemTagDetailDto
{
    /// <summary>类型。</summary>
    public byte Type { get; set; }

    /// <summary>码。</summary>
    public string Code { get; set; }

    /// <summary>名称原文。</summary>
    public string Name { get; set; }

    /// <summary>参数。</summary>
    public string Setting { get; set; }

    /// <summary>图片。</summary>
    public string Image { get; set; }

    /// <summary>排序。</summary>
    public short Sort { get; set; }
}

/// <summary>保存标签。</summary>
public sealed class SystemTagSaveInput
{
    /// <summary>类型。</summary>
    public byte Type { get; set; }

    /// <summary>码。</summary>
    public string Code { get; set; }

    /// <summary>名称（多语 JSON 原文）。</summary>
    public string Name { get; set; }

    /// <summary>参数。</summary>
    public string Setting { get; set; }

    /// <summary>图片。</summary>
    public string Image { get; set; }

    /// <summary>排序。</summary>
    public short Sort { get; set; }
}

/// <summary>删除标签。</summary>
public sealed class SystemTagDeleteInput
{
    /// <summary>类型。</summary>
    public byte Type { get; set; }

    /// <summary>码。</summary>
    public string Code { get; set; }
}
