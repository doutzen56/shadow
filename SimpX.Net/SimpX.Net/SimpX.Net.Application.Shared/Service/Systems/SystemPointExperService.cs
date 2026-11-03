using SimpX.Net.Data.Entity.Systems;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 总后台积分/经验项配置。对齐旧 /setting/points/point。
/// </summary>
public sealed class SystemPointExperService : ITransient
{
    private readonly ISqlSugarClient _db;

    /// <summary>
    /// 注入业务库。积分经验配置在 BetWin30。
    /// </summary>
    /// <param name="biz">业务库</param>
    public SystemPointExperService(IBizDbAccessor biz) => _db = biz.Db;

    /// <summary>
    /// 读取当前配置（取第一行；没有则空壳）。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>配置</returns>
    public async Task<SystemPointExperDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _db.Queryable<SystemPointExper>().OrderBy(t => t.ID).Take(1)
            .ToListAsync(cancellationToken);
        var t = rows.FirstOrDefault();
        return new SystemPointExperDto
        {
            ID = t?.ID ?? 0,
            PointItemSetting = t?.PointItemSetting ?? string.Empty,
            ExperItemSetting = t?.ExperItemSetting ?? string.Empty
        };
    }

    /// <summary>
    /// 保存配置（无行则插入）。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>编号</returns>
    public async Task<int> SaveAsync(SystemPointExperSaveInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var rows = await _db.Queryable<SystemPointExper>().OrderBy(t => t.ID).Take(1)
            .ToListAsync(cancellationToken);
        var entity = rows.FirstOrDefault();
        if (entity is null)
        {
            return await _db.Insertable(new SystemPointExper
            {
                PointItemSetting = input.PointItemSetting ?? string.Empty,
                ExperItemSetting = input.ExperItemSetting ?? string.Empty
            }).ExecuteReturnIdentityAsync(cancellationToken);
        }

        entity.PointItemSetting = input.PointItemSetting ?? string.Empty;
        entity.ExperItemSetting = input.ExperItemSetting ?? string.Empty;
        await _db.Updateable(entity).ExecuteCommandAsync(cancellationToken);
        return entity.ID;
    }
}

/// <summary>积分经验项配置。</summary>
public sealed class SystemPointExperDto
{
    /// <summary>编号。</summary>
    public int ID { get; set; }

    /// <summary>积分项原文。</summary>
    public string PointItemSetting { get; set; }

    /// <summary>经验项原文。</summary>
    public string ExperItemSetting { get; set; }
}

/// <summary>保存积分经验项。</summary>
public sealed class SystemPointExperSaveInput
{
    /// <summary>积分项原文。</summary>
    public string PointItemSetting { get; set; }

    /// <summary>经验项原文。</summary>
    public string ExperItemSetting { get; set; }
}
