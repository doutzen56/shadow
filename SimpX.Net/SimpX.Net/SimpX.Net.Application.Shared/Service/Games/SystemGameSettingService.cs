using SimpX.Net.Data.Entity.Games;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 总后台系统层游戏配置只读。对齐旧 Web.System「游戏配置」列表主字段（不定站）。
/// </summary>
public sealed class SystemGameSettingService : ITransient
{
    private readonly ISqlSugarClient _db;

    /// <summary>
    /// 注入业务库。系统层游戏配置在 BetWin30。
    /// </summary>
    /// <param name="biz">业务库</param>
    public SystemGameSettingService(IBizDbAccessor biz) => _db = biz.Db;

    /// <summary>
    /// 列出系统游戏配置。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行列表</returns>
    public async Task<List<SystemGameSettingListItemDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _db.Queryable<SystemGameSetting>()
            .OrderBy(s => s.Type)
            .ToListAsync(cancellationToken);
        return rows.Select(s => new SystemGameSettingListItemDto
        {
            Type = s.Type,
            Name = s.Name ?? string.Empty,
            Status = s.Status,
            DefaultRate = s.DefaultRate,
            Currencys = s.Currencys ?? string.Empty,
            Languages = s.Languages ?? string.Empty,
            GameCategorys = s.GameCategorys ?? string.Empty,
            Embedded = s.Embedded
        }).ToList();
    }
}

/// <summary>
/// 系统游戏配置列表行。
/// </summary>
public sealed class SystemGameSettingListItemDto
{
    /// <summary>游戏类型。</summary>
    public int Type { get; set; }

    /// <summary>名称。</summary>
    public string Name { get; set; }

    /// <summary>状态。</summary>
    public int Status { get; set; }

    /// <summary>默认条件。</summary>
    public decimal DefaultRate { get; set; }

    /// <summary>支持币种。</summary>
    public string Currencys { get; set; }

    /// <summary>支持语言。</summary>
    public string Languages { get; set; }

    /// <summary>分类。</summary>
    public string GameCategorys { get; set; }

    /// <summary>是否支持内嵌。</summary>
    public bool Embedded { get; set; }
}
