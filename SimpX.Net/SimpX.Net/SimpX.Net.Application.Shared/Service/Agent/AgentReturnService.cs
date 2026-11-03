using SimpX.Net.Data.Entity.Agents;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 租户后台返点方案配置（不结算）。对齐旧 /admin/user/agent/return。
/// </summary>
public sealed partial class AgentReturnService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;

    /// <summary>
    /// 注入依赖。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站</param>
    public AgentReturnService(IBizDbAccessor biz, ICurrentSiteAccessor site)
    {
        _db = biz.Db;
        _site = site;
    }

    /// <summary>
    /// 当前站方案列表。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行列表</returns>
    public async Task<List<AgentReturnListItemDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<AgentReturn>()
            .Where(t => t.SiteID == siteId)
            .OrderBy(t => t.ID, OrderByType.Desc)
            .ToListAsync(cancellationToken);
        return rows.Select(t => new AgentReturnListItemDto
        {
            ID = t.ID,
            Name = t.Name ?? string.Empty,
            IsOpen = t.IsOpen,
            CreateAt = t.CreateAt
        }).ToList();
    }

    /// <summary>
    /// 详情。
    /// </summary>
    /// <param name="id">编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>详情</returns>
    public async Task<AgentReturnDetailDto> DetailAsync(int id, CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        if (id <= 0)
            return new AgentReturnDetailDto { SiteID = siteId, IsOpen = true };
        var rows = await _db.Queryable<AgentReturn>()
            .Where(t => t.SiteID == siteId && t.ID == id).Take(1).ToListAsync(cancellationToken);
        var t = rows.FirstOrDefault() ?? throw Oops.Oh("#记录不存在#");
        return new AgentReturnDetailDto
        {
            ID = t.ID,
            SiteID = t.SiteID,
            Name = t.Name ?? string.Empty,
            SettingString = t.SettingString ?? string.Empty,
            IsOpen = t.IsOpen,
            CreateAt = t.CreateAt
        };
    }

    /// <summary>
    /// 保存方案配置。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>编号</returns>
    public async Task<int> SaveAsync(AgentReturnSaveInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (string.IsNullOrWhiteSpace(input.Name))
            throw Oops.Oh("#名称不能为空#");
        var siteId = _site.RequireSiteId();

        if (input.ID > 0)
        {
            var rows = await _db.Queryable<AgentReturn>()
                .Where(t => t.SiteID == siteId && t.ID == input.ID).Take(1).ToListAsync(cancellationToken);
            var entity = rows.FirstOrDefault() ?? throw Oops.Oh("#记录不存在#");
            entity.Name = input.Name.Trim();
            entity.SettingString = input.SettingString ?? string.Empty;
            entity.IsOpen = input.IsOpen;
            await _db.Updateable(entity).ExecuteCommandAsync(cancellationToken);
            return entity.ID;
        }

        return await _db.Insertable(new AgentReturn
        {
            SiteID = siteId,
            Name = input.Name.Trim(),
            SettingString = input.SettingString ?? string.Empty,
            IsOpen = input.IsOpen,
            CreateAt = DateTime.Now
        }).ExecuteReturnIdentityAsync(cancellationToken);
    }

    /// <summary>
    /// 删除方案。
    /// </summary>
    /// <param name="id">编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        await _db.Deleteable<AgentReturn>()
            .Where(t => t.SiteID == siteId && t.ID == id)
            .ExecuteCommandAsync(cancellationToken);
    }

    // --- merged from AgentReturnService.WaveGaps.cs ---



    /// <summary>
    /// 对齐旧 reexereturnmoney。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> ReexereturnmoneyAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return ReexereturnmoneyCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 returnplan/loglist（实体未迁时返回空列表）。
    /// </summary>
    public Task<object?> LoglistAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        _ = input; _ = cancellationToken;
        return Task.FromResult<object?>(Array.Empty<object>());
    }


    /// <summary>
    /// 对齐旧 returnplan/logdetail（实体未迁时返回空列表）。
    /// </summary>
    public Task<object?> LogdetailAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        _ = input; _ = cancellationToken;
        return Task.FromResult<object?>(Array.Empty<object>());
    }


    /// <summary>核心：reexereturnmoney —— 重跑结算依赖旧返点流水表，暂未接通。</summary>
    private Task<object?> ReexereturnmoneyCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        var id = ReadInt(input, "ID", "Id");
        if (id <= 0) throw Oops.Oh("#参数错误#");
        throw Oops.Oh("#功能尚未接通#");
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

/// <summary>返点方案列表行。</summary>
public sealed class AgentReturnListItemDto
{
    /// <summary>编号。</summary>
    public int ID { get; set; }

    /// <summary>名称。</summary>
    public string Name { get; set; }

    /// <summary>是否开放。</summary>
    public bool IsOpen { get; set; }

    /// <summary>创建时间。</summary>
    public DateTime CreateAt { get; set; }
}

/// <summary>返点方案详情。</summary>
public sealed class AgentReturnDetailDto
{
    /// <summary>编号。</summary>
    public int ID { get; set; }

    /// <summary>站点。</summary>
    public int SiteID { get; set; }

    /// <summary>名称。</summary>
    public string Name { get; set; }

    /// <summary>游戏返点原文。</summary>
    public string SettingString { get; set; }

    /// <summary>是否开放。</summary>
    public bool IsOpen { get; set; }

    /// <summary>创建时间。</summary>
    public DateTime CreateAt { get; set; }
}

/// <summary>保存返点方案。</summary>
public sealed class AgentReturnSaveInput
{
    /// <summary>0=新增。</summary>
    public int ID { get; set; }

    /// <summary>名称。</summary>
    public string Name { get; set; }

    /// <summary>游戏返点原文。</summary>
    public string SettingString { get; set; }

    /// <summary>是否开放。</summary>
    public bool IsOpen { get; set; } = true;
}
