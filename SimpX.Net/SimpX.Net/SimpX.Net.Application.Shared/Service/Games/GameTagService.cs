using SimpX.Net.Data.Entity.Games;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 总后台游戏标签。对齐旧 /system/setting/config/gametag*。
/// </summary>
public sealed class GameTagService : ITransient
{
    /// <summary>旧 OpenStatus.Open。</summary>
    public const int StatusOpen = 1;

    private readonly ISqlSugarClient _db;

    /// <summary>
    /// 注入业务库。游戏标签表在 BetWin30。
    /// </summary>
    /// <param name="biz">业务库</param>
    public GameTagService(IBizDbAccessor biz) => _db = biz.Db;

    /// <summary>
    /// 列表。
    /// </summary>
    /// <param name="tagName">名称模糊</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行列表</returns>
    public async Task<List<GameTagListItemDto>> ListAsync(
        string tagName = null,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Queryable<GameTag>();
        if (!string.IsNullOrWhiteSpace(tagName))
            query = query.Where(t => t.TagName.Contains(tagName.Trim()));
        var rows = await query.OrderBy(t => t.Sort).ToListAsync(cancellationToken);
        return rows.Select(t => new GameTagListItemDto
        {
            ID = t.ID,
            TagName = t.TagName ?? string.Empty,
            Sort = t.Sort,
            Status = t.Status == StatusOpen,
            CreateAt = t.CreateAt
        }).ToList();
    }

    /// <summary>
    /// 详情。
    /// </summary>
    /// <param name="id">编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>详情</returns>
    public async Task<GameTagDetailDto> DetailAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            return new GameTagDetailDto { Status = true };
        var rows = await _db.Queryable<GameTag>().Where(t => t.ID == id).Take(1).ToListAsync(cancellationToken);
        var t = rows.FirstOrDefault() ?? throw Oops.Oh("#记录不存在#");
        return new GameTagDetailDto
        {
            ID = t.ID,
            TagName = t.TagName ?? string.Empty,
            Sort = t.Sort,
            Status = t.Status == StatusOpen
        };
    }

    /// <summary>
    /// 保存。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>编号</returns>
    public async Task<int> SaveAsync(GameTagSaveInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (string.IsNullOrWhiteSpace(input.TagName))
            throw Oops.Oh("#标签名称不能为空#");

        if (input.ID > 0)
        {
            var rows = await _db.Queryable<GameTag>().Where(t => t.ID == input.ID).Take(1).ToListAsync(cancellationToken);
            var entity = rows.FirstOrDefault() ?? throw Oops.Oh("#记录不存在#");
            entity.TagName = input.TagName.Trim();
            entity.Sort = input.Sort;
            entity.Status = input.Status ? StatusOpen : 0;
            await _db.Updateable(entity).ExecuteCommandAsync(cancellationToken);
            return entity.ID;
        }

        return await _db.Insertable(new GameTag
        {
            TagName = input.TagName.Trim(),
            Sort = input.Sort,
            Status = input.Status ? StatusOpen : 0,
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
        await _db.Deleteable<GameTag>().In(id).ExecuteCommandAsync(cancellationToken);
    }
}

/// <summary>游戏标签列表行。</summary>
public sealed class GameTagListItemDto
{
    /// <summary>编号。</summary>
    public int ID { get; set; }

    /// <summary>名称原文。</summary>
    public string TagName { get; set; }

    /// <summary>排序。</summary>
    public int Sort { get; set; }

    /// <summary>是否开放。</summary>
    public bool Status { get; set; }

    /// <summary>创建时间。</summary>
    public DateTime CreateAt { get; set; }
}

/// <summary>游戏标签详情。</summary>
public sealed class GameTagDetailDto
{
    /// <summary>编号。</summary>
    public int ID { get; set; }

    /// <summary>名称原文。</summary>
    public string TagName { get; set; }

    /// <summary>排序。</summary>
    public int Sort { get; set; }

    /// <summary>是否开放。</summary>
    public bool Status { get; set; }
}

/// <summary>保存游戏标签。</summary>
public sealed class GameTagSaveInput
{
    /// <summary>0=新增。</summary>
    public int ID { get; set; }

    /// <summary>名称（可多语 JSON）。</summary>
    public string TagName { get; set; }

    /// <summary>排序。</summary>
    public int Sort { get; set; }

    /// <summary>是否开放。</summary>
    public bool Status { get; set; } = true;
}
