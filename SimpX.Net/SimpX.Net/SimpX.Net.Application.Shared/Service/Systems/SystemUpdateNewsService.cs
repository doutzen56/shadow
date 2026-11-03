using SimpX.Net.Data.Entity.Systems;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 系统更新通知。对齐旧 /admin/system/updatenews 与总后台 /setting/update。
/// </summary>
public sealed class SystemUpdateNewsService : ITransient
{
    private readonly ISqlSugarClient _db;

    /// <summary>
    /// 注入业务库。更新公告在 BetWin30。
    /// </summary>
    /// <param name="biz">业务库</param>
    public SystemUpdateNewsService(IBizDbAccessor biz) => _db = biz.Db;

    /// <summary>
    /// 按语种/标题列出更新通知。
    /// </summary>
    /// <param name="language">语种；null=全部</param>
    /// <param name="title">标题模糊</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>列表</returns>
    public async Task<List<SystemUpdateNewsItemDto>> ListAsync(
        int? language = null,
        string title = null,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Queryable<SystemUpdateNews>();
        if (language.HasValue)
            query = query.Where(n => n.Language == language.Value);
        if (!string.IsNullOrWhiteSpace(title))
            query = query.Where(n => n.Title.Contains(title.Trim()));

        var rows = await query
            .OrderBy(n => n.CreateAt, OrderByType.Desc)
            .ToListAsync(cancellationToken);

        return rows.Select(Map).ToList();
    }

    /// <summary>
    /// 详情。
    /// </summary>
    /// <param name="id">编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>详情</returns>
    public async Task<SystemUpdateNewsItemDto> DetailAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            return new SystemUpdateNewsItemDto { Ident = Guid.NewGuid().ToString() };
        var rows = await _db.Queryable<SystemUpdateNews>().Where(n => n.ID == id).Take(1).ToListAsync(cancellationToken);
        var n = rows.FirstOrDefault() ?? throw Oops.Oh("#记录不存在#");
        return Map(n);
    }

    /// <summary>
    /// 保存。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>编号</returns>
    public async Task<int> SaveAsync(SystemUpdateNewsSaveInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (string.IsNullOrWhiteSpace(input.Title))
            throw Oops.Oh("#标题不能为空#");

        if (input.ID > 0)
        {
            var rows = await _db.Queryable<SystemUpdateNews>().Where(n => n.ID == input.ID).Take(1)
                .ToListAsync(cancellationToken);
            var entity = rows.FirstOrDefault() ?? throw Oops.Oh("#记录不存在#");
            entity.Title = input.Title.Trim();
            entity.Content = input.Content ?? string.Empty;
            entity.Language = input.Language;
            if (!string.IsNullOrWhiteSpace(input.Ident))
                entity.Ident = input.Ident.Trim();
            await _db.Updateable(entity).ExecuteCommandAsync(cancellationToken);
            return entity.ID;
        }

        return await _db.Insertable(new SystemUpdateNews
        {
            Title = input.Title.Trim(),
            Content = input.Content ?? string.Empty,
            Language = input.Language,
            Ident = string.IsNullOrWhiteSpace(input.Ident) ? Guid.NewGuid().ToString() : input.Ident.Trim(),
            CreateAt = DateTime.Now
        }).ExecuteReturnIdentityAsync(cancellationToken);
    }

    /// <summary>
    /// 删除。
    /// </summary>
    /// <param name="id">编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw Oops.Oh("#参数错误#");
        await _db.Deleteable<SystemUpdateNews>().In(id).ExecuteCommandAsync(cancellationToken);
    }

    private static SystemUpdateNewsItemDto Map(SystemUpdateNews n) => new()
    {
        ID = n.ID,
        Title = n.Title ?? string.Empty,
        Content = n.Content ?? string.Empty,
        Language = n.Language,
        CreateAt = n.CreateAt,
        Ident = n.Ident ?? string.Empty
    };
}
