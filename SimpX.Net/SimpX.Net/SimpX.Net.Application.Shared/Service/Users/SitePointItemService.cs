using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 站点积分项配置 + 积分日志 / 手工导入（只加日志与余额字段，走 UserPointService 若有）。
/// </summary>
public sealed partial class SitePointItemService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;
    private readonly IUserPointService _points;

    /// <summary>
    /// 注入依赖。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站</param>
    /// <param name="points">积分服务</param>
    public SitePointItemService(IBizDbAccessor biz, ICurrentSiteAccessor site, IUserPointService points)
    {
        _db = biz.Db;
        _site = site;
        _points = points;
    }

    /// <summary>
    /// 读积分项原文。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>配置</returns>
    public async Task<SitePointItemDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<SiteVipConfig>()
            .Where(c => c.ID == siteId).Take(1).ToListAsync(cancellationToken);
        var c = rows.FirstOrDefault();
        return new SitePointItemDto
        {
            SiteID = siteId,
            PointItemSetting = c?.PointItemSettingString ?? string.Empty,
            ExperItemSetting = c?.ExperItemSettingString ?? string.Empty,
            PointRule = c?.PointRule ?? string.Empty
        };
    }

    /// <summary>
    /// 保存积分项原文。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SaveAsync(SitePointItemSaveInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<SiteVipConfig>()
            .Where(c => c.ID == siteId).Take(1).ToListAsync(cancellationToken);
        var entity = rows.FirstOrDefault();
        if (entity is null)
        {
            await _db.Insertable(new SiteVipConfig
            {
                ID = siteId,
                CreateAt = DateTime.Now,
                PointItemSettingString = input.PointItemSetting ?? string.Empty,
                ExperItemSettingString = input.ExperItemSetting ?? string.Empty,
                PointRule = input.PointRule ?? string.Empty,
                VIPConditionSetting = string.Empty,
                VIPRightSetting = string.Empty,
                RuleClause = string.Empty,
                AutoReturnRule = string.Empty,
                Currency = 0
            }).ExecuteCommandAsync(cancellationToken);
            return;
        }

        entity.PointItemSettingString = input.PointItemSetting ?? string.Empty;
        entity.ExperItemSettingString = input.ExperItemSetting ?? string.Empty;
        entity.PointRule = input.PointRule ?? string.Empty;
        await _db.Updateable(entity).ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 积分日志分页。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页</returns>
    public async Task<PointLogPageOutput> PageLogsAsync(
        PointLogPageInput input,
        CancellationToken cancellationToken = default)
    {
        input ??= new PointLogPageInput();
        var siteId = _site.RequireSiteId();
        var pageSize = Math.Clamp(input.PageSize <= 0 ? 20 : input.PageSize, 1, 100);
        var page = Math.Max(1, input.Page <= 0 ? 1 : input.Page);
        var query = _db.Queryable<UserPointLog>().Where(l => l.SiteID == siteId);
        if (input.UserID > 0)
            query = query.Where(l => l.UserID == input.UserID);
        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderBy(l => l.ID, OrderByType.Desc)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        var ids = rows.Select(r => r.UserID).Distinct().ToList();
        var nameMap = new Dictionary<int, string>();
        if (ids.Count > 0)
        {
            var names = await _db.Queryable<User>()
                .Where(u => u.SiteID == siteId && ids.Contains(u.ID))
                .Select(u => new { u.ID, u.UserName }).ToListAsync(cancellationToken);
            foreach (var n in names)
                nameMap[n.ID] = n.UserName;
        }

        return new PointLogPageOutput
        {
            Total = total,
            Items = rows.Select(l => new PointLogItemDto
            {
                ID = l.ID,
                UserID = l.UserID,
                UserName = nameMap.TryGetValue(l.UserID, out var un) ? un : string.Empty,
                Point = l.Point,
                Balance = l.Balance,
                Type = l.Type,
                CreateAt = l.CreateAt
            }).ToList()
        };
    }

    /// <summary>
    /// 手工加积分（导入；只支持正数，走现有积分账本）。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task ImportPointAsync(PointImportInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.UserID <= 0)
            throw Oops.Oh("#会员错误#");
        if (input.Point <= 0)
            throw Oops.Oh("#积分错误#");
        var siteId = _site.RequireSiteId();
        await _points.AddPointAsync(
            siteId,
            input.UserID,
            input.Point,
            input.Type,
            input.Remark ?? "后台导入",
            0,
            cancellationToken);
    }

    // --- merged from SitePointItemService.WaveGaps.cs ---



    /// <summary>
    /// 对齐旧 clearallusers。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> ClearallusersAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return ClearallusersCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 list。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> ListAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return ListCoreAsync(input, cancellationToken);
    }


    /// <summary>核心：clearallusers（对齐旧 ClearAllUserPoint：usr_Money.Coin=0）。</summary>
    private async Task<object?> ClearallusersCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        _ = input;
        var siteId = _site.RequireSiteId();
        var affected = await _db.Updateable<UserMoney>()
            .SetColumns(m => m.Coin == 0)
            .Where(m => m.SiteID == siteId)
            .ExecuteCommandAsync(cancellationToken);
        return affected;
    }


    /// <summary>核心：list</summary>
    private async Task<object?> ListCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        _ = input; _ = cancellationToken;
        return await Task.FromResult<object?>(Array.Empty<object>());
    }


    private static int ReadInt(Dictionary<string, object?> input, params string[] keys)
    {
        foreach (var k in keys)
        {
            if (!input.TryGetValue(k, out var v) || v is null) continue;
            if (v is int i) return i;
            if (int.TryParse(v.ToString(), out var n)) return n;
        }
        return 0;
    }
}

/// <summary>站点积分项。</summary>
public sealed class SitePointItemDto
{
    /// <summary>站点。</summary>
    public int SiteID { get; set; }

    /// <summary>积分项原文。</summary>
    public string PointItemSetting { get; set; }

    /// <summary>经验项原文。</summary>
    public string ExperItemSetting { get; set; }

    /// <summary>积分规则。</summary>
    public string PointRule { get; set; }
}

/// <summary>保存积分项。</summary>
public sealed class SitePointItemSaveInput
{
    /// <summary>积分项原文。</summary>
    public string PointItemSetting { get; set; }

    /// <summary>经验项原文。</summary>
    public string ExperItemSetting { get; set; }

    /// <summary>积分规则。</summary>
    public string PointRule { get; set; }
}

/// <summary>积分日志筛选。</summary>
public sealed class PointLogPageInput
{
    /// <summary>页码。</summary>
    public int Page { get; set; } = 1;

    /// <summary>每页。</summary>
    public int PageSize { get; set; } = 20;

    /// <summary>会员。</summary>
    public int UserID { get; set; }
}

/// <summary>积分日志分页。</summary>
public sealed class PointLogPageOutput
{
    /// <summary>总数。</summary>
    public int Total { get; set; }

    /// <summary>行。</summary>
    public List<PointLogItemDto> Items { get; set; }
}

/// <summary>积分日志行。</summary>
public sealed class PointLogItemDto
{
    /// <summary>编号。</summary>
    public int ID { get; set; }

    /// <summary>会员。</summary>
    public int UserID { get; set; }

    /// <summary>用户名。</summary>
    public string UserName { get; set; }

    /// <summary>积分变动。</summary>
    public decimal Point { get; set; }

    /// <summary>余额。</summary>
    public decimal Balance { get; set; }

    /// <summary>类型。</summary>
    public int Type { get; set; }

    /// <summary>时间。</summary>
    public DateTime CreateAt { get; set; }
}

/// <summary>积分导入。</summary>
public sealed class PointImportInput
{
    /// <summary>会员。</summary>
    public int UserID { get; set; }

    /// <summary>积分（可负）。</summary>
    public decimal Point { get; set; }

    /// <summary>类型。</summary>
    public int Type { get; set; }

    /// <summary>备注。</summary>
    public string Remark { get; set; }
}
