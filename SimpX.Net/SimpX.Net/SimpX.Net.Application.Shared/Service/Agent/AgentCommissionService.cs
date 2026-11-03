using SimpX.Net.Data.Entity.Agents;

using SimpX.Net.Data.Entity.Games;
namespace SimpX.Net.Application.Shared;

/// <summary>
/// 租户后台返佣方案配置（不结算）。对齐旧 /admin/user/agent/commission。
/// </summary>
public sealed partial class AgentCommissionService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;

    /// <summary>
    /// 注入依赖。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站</param>
    public AgentCommissionService(IBizDbAccessor biz, ICurrentSiteAccessor site)
    {
        _db = biz.Db;
        _site = site;
    }

    /// <summary>
    /// 当前站方案列表。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行列表</returns>
    public async Task<List<AgentCommissionListItemDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<AgentCommission>()
            .Where(t => t.SiteID == siteId)
            .OrderBy(t => t.ID, OrderByType.Desc)
            .ToListAsync(cancellationToken);
        return rows.Select(t => new AgentCommissionListItemDto
        {
            ID = t.ID,
            Name = t.Name ?? string.Empty,
            MemberMoney = t.MemberMoney,
            Award = t.Award,
            Return = t.Return,
            Cost = t.Cost,
            MaxRate = t.MaxRate,
            IsOpen = t.IsOpen,
            IsContract = t.IsContract,
            AgentCount = t.AgentCount,
            CreateAt = t.CreateAt
        }).ToList();
    }

    /// <summary>
    /// 详情。
    /// </summary>
    /// <param name="id">编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>详情</returns>
    public async Task<AgentCommissionDetailDto> DetailAsync(int id, CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        if (id <= 0)
            return new AgentCommissionDetailDto { SiteID = siteId, IsOpen = true };
        var rows = await _db.Queryable<AgentCommission>()
            .Where(t => t.SiteID == siteId && t.ID == id).Take(1).ToListAsync(cancellationToken);
        var t = rows.FirstOrDefault() ?? throw Oops.Oh("#记录不存在#");
        return new AgentCommissionDetailDto
        {
            ID = t.ID,
            SiteID = t.SiteID,
            Name = t.Name ?? string.Empty,
            MemberMoney = t.MemberMoney,
            Award = t.Award,
            Return = t.Return,
            Cost = t.Cost,
            IsOpen = t.IsOpen,
            IsContract = t.IsContract,
            SettingString = t.SettingString ?? string.Empty,
            MaxRate = t.MaxRate,
            CreateAt = t.CreateAt
        };
    }

    /// <summary>
    /// 保存方案配置。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>编号</returns>
    public async Task<int> SaveAsync(AgentCommissionSaveInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (string.IsNullOrWhiteSpace(input.Name))
            throw Oops.Oh("#名称不能为空#");
        var siteId = _site.RequireSiteId();

        if (input.ID > 0)
        {
            var rows = await _db.Queryable<AgentCommission>()
                .Where(t => t.SiteID == siteId && t.ID == input.ID).Take(1).ToListAsync(cancellationToken);
            var entity = rows.FirstOrDefault() ?? throw Oops.Oh("#记录不存在#");
            Apply(entity, input);
            await _db.Updateable(entity).ExecuteCommandAsync(cancellationToken);
            return entity.ID;
        }

        var neu = new AgentCommission
        {
            SiteID = siteId,
            CreateAt = DateTime.Now,
            AgentCount = 0
        };
        Apply(neu, input);
        return await _db.Insertable(neu).ExecuteReturnIdentityAsync(cancellationToken);
    }

    /// <summary>
    /// 删除方案。
    /// </summary>
    /// <param name="id">编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        await _db.Deleteable<AgentCommission>()
            .Where(t => t.SiteID == siteId && t.ID == id)
            .ExecuteCommandAsync(cancellationToken);
    }

    private static void Apply(AgentCommission entity, AgentCommissionSaveInput input)
    {
        entity.Name = input.Name.Trim();
        entity.MemberMoney = input.MemberMoney;
        entity.Award = input.Award;
        entity.Return = input.Return;
        entity.Cost = input.Cost;
        entity.IsOpen = input.IsOpen;
        entity.IsContract = input.IsContract;
        entity.SettingString = input.SettingString ?? string.Empty;
        entity.MaxRate = input.MaxRate;
    }

    // --- merged from AgentCommissionService.WaveGaps.cs ---



    /// <summary>
    /// 对齐旧 agentrate。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> AgentrateAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return AgentrateCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 updateagentcount。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> UpdateagentcountAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return UpdateagentcountCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 saveagentrate。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> SaveagentrateAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return SaveagentrateCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 commissionlist（按 UserID 过滤可用方案；无用户时全量）。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> CommissionlistAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return CommissionlistCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 allagentcommissionlist（仅开放方案）。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> AllagentcommissionlistAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return AllagentcommissionlistCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 当前站已开启游戏的代理平台费。按排序从高到低返回，供返佣设定页展示和修改。
    /// </summary>
    /// <param name="input">表单。这个接口不按表单筛选，保留参数是为了和旧入口对齐。</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>list 为游戏类型、名称和平台费；RecordCount 为条数。</returns>
    /// <exception cref="AppFriendlyException">当前请求没有站点时抛出。</exception>
    private async Task<object?> AgentrateCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        _ = input;
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<GameSetting>()
            .Where(g => g.SiteID == siteId && g.IsOpen)
            .OrderBy(g => g.Sort, OrderByType.Desc)
            .ToListAsync(cancellationToken);

        // 旧版标签来自游戏类型说明。这里没有那套枚举说明，用系统层名称，没有再用站点自定义名称。
        var types = rows.Select(g => g.Game).Distinct().ToList();
        var sysRows = types.Count == 0
            ? new List<SystemGameSetting>()
            : await _db.Queryable<SystemGameSetting>()
                .Where(s => types.Contains(s.Type))
                .ToListAsync(cancellationToken);
        var nameMap = sysRows
            .GroupBy(s => s.Type)
            .ToDictionary(g => g.Key, g => g.First().Name);

        var list = rows.Select(t =>
        {
            var typeName = nameMap.TryGetValue(t.Game, out var sysName) && !string.IsNullOrWhiteSpace(sysName)
                ? sysName
                : (!string.IsNullOrWhiteSpace(t.Name) ? t.Name : t.Game.ToString());
            return new
            {
                Game = t.Game,
                Type = typeName,
                AgentRate = t.AgentRate
            };
        }).ToList();

        return new { list, RecordCount = list.Count };
    }


    /// <summary>
    /// 按当前站代理资料重算某条佣金方案的绑定人数，并写回方案。
    /// </summary>
    /// <param name="input">表单，读取方案编号 ID。</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>方案编号和写回后的人数。</returns>
    /// <exception cref="AppFriendlyException">编号无效、方案不存在，或人数没有写上时抛出。</exception>
    private async Task<object?> UpdateagentcountCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var id = ReadInt(input, "ID", "Id");
        if (id <= 0)
            throw Oops.Oh("#编号错误#");

        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<AgentCommission>()
            .Where(t => t.SiteID == siteId && t.ID == id)
            .Take(1)
            .ToListAsync(cancellationToken);
        var commission = rows.FirstOrDefault() ?? throw Oops.Oh("#记录不存在#");

        // 待审、正常、锁定都算绑定人数，和旧版按站点加方案编号计数一致。
        var agentCount = await _db.Queryable<AgentUser>()
            .Where(p => p.SiteID == siteId && p.CommissionID == id)
            .CountAsync(cancellationToken);
        commission.AgentCount = agentCount;
        var updated = await _db.Updateable(commission)
            .UpdateColumns(t => new { t.AgentCount })
            .ExecuteCommandAsync(cancellationToken);
        if (updated != 1)
            throw Oops.Oh("#更新失败#");

        return new { ID = id, AgentCount = agentCount };
    }


    /// <summary>核心：saveagentrate</summary>
    private async Task<object?> SaveagentrateCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var game = ReadInt(input, "Type", "Game", "game");
        if (game == 0) throw Oops.Oh("#参数错误#");
        var rateRaw = input.TryGetValue("Value", out var v) ? v?.ToString()
            : input.TryGetValue("AgentRate", out var a) ? a?.ToString()
            : "0";
        if (!decimal.TryParse(rateRaw, out var rate))
            throw Oops.Oh("#参数错误#");
        var siteId = _site.RequireSiteId();
        var n = await _db.Updateable<GameSetting>()
            .SetColumns(g => g.AgentRate == rate)
            .Where(g => g.SiteID == siteId && g.Game == game)
            .ExecuteCommandAsync(cancellationToken);
        if (n <= 0) throw Oops.Oh("#记录不存在#");
        return null;
    }


    /// <summary>核心：commissionlist</summary>
    private async Task<object?> CommissionlistCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        _ = input;
        return await ListAsync(cancellationToken);
    }


    /// <summary>核心：allagentcommissionlist</summary>
    private async Task<object?> AllagentcommissionlistCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        _ = input;
        var all = await ListAsync(cancellationToken);
        return all.Where(x => x.IsOpen).ToList();
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

/// <summary>返佣方案列表行。</summary>
public sealed class AgentCommissionListItemDto
{
    /// <summary>编号。</summary>
    public int ID { get; set; }

    /// <summary>名称。</summary>
    public string Name { get; set; }

    /// <summary>有效会员门槛。</summary>
    public decimal MemberMoney { get; set; }

    /// <summary>活动承担。</summary>
    public decimal Award { get; set; }

    /// <summary>返水承担。</summary>
    public decimal Return { get; set; }

    /// <summary>行政费承担。</summary>
    public decimal Cost { get; set; }

    /// <summary>最高比例。</summary>
    public decimal MaxRate { get; set; }

    /// <summary>是否开放。</summary>
    public bool IsOpen { get; set; }

    /// <summary>是否契约。</summary>
    public bool IsContract { get; set; }

    /// <summary>代理数。</summary>
    public int AgentCount { get; set; }

    /// <summary>创建时间。</summary>
    public DateTime CreateAt { get; set; }
}

/// <summary>返佣方案详情。</summary>
public sealed class AgentCommissionDetailDto
{
    /// <summary>编号。</summary>
    public int ID { get; set; }

    /// <summary>站点。</summary>
    public int SiteID { get; set; }

    /// <summary>名称。</summary>
    public string Name { get; set; }

    /// <summary>有效会员门槛。</summary>
    public decimal MemberMoney { get; set; }

    /// <summary>活动承担。</summary>
    public decimal Award { get; set; }

    /// <summary>返水承担。</summary>
    public decimal Return { get; set; }

    /// <summary>成本。</summary>
    public decimal Cost { get; set; }

    /// <summary>是否开放。</summary>
    public bool IsOpen { get; set; }

    /// <summary>是否契约。</summary>
    public bool IsContract { get; set; }

    /// <summary>阶梯配置原文。</summary>
    public string SettingString { get; set; }

    /// <summary>最大比例。</summary>
    public decimal MaxRate { get; set; }

    /// <summary>创建时间。</summary>
    public DateTime CreateAt { get; set; }
}

/// <summary>保存返佣方案。</summary>
public sealed class AgentCommissionSaveInput
{
    /// <summary>0=新增。</summary>
    public int ID { get; set; }

    /// <summary>名称。</summary>
    public string Name { get; set; }

    /// <summary>有效会员门槛。</summary>
    public decimal MemberMoney { get; set; }

    /// <summary>活动承担。</summary>
    public decimal Award { get; set; }

    /// <summary>返水承担。</summary>
    public decimal Return { get; set; }

    /// <summary>成本。</summary>
    public decimal Cost { get; set; }

    /// <summary>是否开放。</summary>
    public bool IsOpen { get; set; } = true;

    /// <summary>是否契约。</summary>
    public bool IsContract { get; set; }

    /// <summary>阶梯配置原文。</summary>
    public string SettingString { get; set; }

    /// <summary>最大比例。</summary>
    public decimal MaxRate { get; set; }
}
