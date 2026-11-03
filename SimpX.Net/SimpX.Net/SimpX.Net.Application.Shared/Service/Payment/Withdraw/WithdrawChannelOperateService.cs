using SimpX.Net.Data.Entity.Sites;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 站点提现渠道增删改。对齐旧 WithdrawAgent.SaveChannel / DeleteChannel；
/// 删除改为本站软删（旧版硬删），避免误删后订单/缓存对不上。
/// </summary>
public sealed class WithdrawChannelOperateService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;

    /// <summary>注入库与当前站。</summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站</param>
    public WithdrawChannelOperateService(IBizDbAccessor biz, ICurrentSiteAccessor site)
    {
        _db = biz.Db;
        _site = site;
    }

    /// <summary>
    /// 详情；编号 0 或不存在返回空壳。
    /// </summary>
    /// <param name="id">渠道编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>详情</returns>
    public async Task<WithdrawChannelDetailDto> InfoAsync(int id, CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        if (id <= 0)
        {
            return new WithdrawChannelDetailDto
            {
                Name = string.Empty,
                Setting = string.Empty,
                QuerySetting = string.Empty,
                IsOpen = true
            };
        }

        var rows = await _db.Queryable<WithdrawChannel>()
            .Where(c => c.ID == id && c.SiteID == siteId && !c.IsDeleted)
            .Take(1)
            .ToListAsync(cancellationToken);
        var c = rows.FirstOrDefault() ?? throw Oops.Oh("#渠道编号错误#");
        return new WithdrawChannelDetailDto
        {
            ID = c.ID,
            Name = c.Name ?? string.Empty,
            SysID = c.SysID,
            IsOpen = c.IsOpen,
            Cost = c.Cost,
            MinCost = c.MinCost,
            MaxCost = c.MaxCost,
            Setting = c.Setting ?? string.Empty,
            QuerySetting = c.QuerySetting ?? string.Empty,
            Currency = c.Currency,
            Sort = c.Sort
        };
    }

    /// <summary>
    /// 保存。名称必填；编号 0 新增（本站、CreateAt=Now、Sort=本站条数+1、未删），否则只改本站未删行的指定字段。
    /// </summary>
    /// <param name="input">入参</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SaveAsync(WithdrawChannelSaveInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (string.IsNullOrWhiteSpace(input.Name))
            throw Oops.Oh("#请输入名称#");

        var siteId = _site.RequireSiteId();
        var name = input.Name.Trim();

        if (input.ID == 0)
        {
            var count = await _db.Queryable<WithdrawChannel>()
                .Where(c => c.SiteID == siteId)
                .CountAsync(cancellationToken);
            var entity = new WithdrawChannel
            {
                SiteID = siteId,
                Name = name,
                SysID = input.SysID,
                IsOpen = input.IsOpen,
                Cost = input.Cost,
                MinCost = input.MinCost,
                MaxCost = input.MaxCost,
                Setting = input.Setting ?? string.Empty,
                QuerySetting = input.QuerySetting ?? string.Empty,
                Currency = input.Currency,
                CreateAt = DateTime.Now,
                Sort = (short)(count + 1),
                IsDeleted = false
            };
            await _db.Insertable(entity).ExecuteCommandAsync(cancellationToken);
            return;
        }

        var rows = await _db.Queryable<WithdrawChannel>()
            .Where(c => c.ID == input.ID && c.SiteID == siteId && !c.IsDeleted)
            .Take(1)
            .ToListAsync(cancellationToken);
        var row = rows.FirstOrDefault() ?? throw Oops.Oh("#渠道编号错误#");
        row.Name = name;
        row.IsOpen = input.IsOpen;
        row.SysID = input.SysID;
        row.Cost = input.Cost;
        row.MinCost = input.MinCost;
        row.MaxCost = input.MaxCost;
        row.Setting = input.Setting ?? string.Empty;
        row.QuerySetting = input.QuerySetting ?? string.Empty;
        row.Currency = input.Currency;
        row.Sort = input.Sort;
        await _db.Updateable(row)
            .UpdateColumns(c => new
            {
                c.Name,
                c.IsOpen,
                c.SysID,
                c.Cost,
                c.MinCost,
                c.MaxCost,
                c.Setting,
                c.QuerySetting,
                c.Currency,
                c.Sort
            })
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 软删本站渠道（IsDeleted=true）。旧版是硬删；软删更安全，历史订单还能对上渠道。
    /// </summary>
    /// <param name="id">渠道编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw Oops.Oh("#渠道编号错误#");

        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<WithdrawChannel>()
            .Where(c => c.ID == id && c.SiteID == siteId && !c.IsDeleted)
            .Take(1)
            .ToListAsync(cancellationToken);
        var row = rows.FirstOrDefault() ?? throw Oops.Oh("#渠道编号错误#");
        row.IsDeleted = true;
        await _db.Updateable(row).UpdateColumns(c => new { c.IsDeleted })
            .ExecuteCommandAsync(cancellationToken);
    }
}

/// <summary>提现渠道保存入参。</summary>
public sealed class WithdrawChannelSaveInput
{
    /// <summary>编号；0 表示新增。</summary>
    public int ID { get; set; }

    /// <summary>名称。</summary>
    public string Name { get; set; }

    /// <summary>系统代付编号。</summary>
    public int SysID { get; set; }

    /// <summary>是否开放。</summary>
    public bool IsOpen { get; set; }

    /// <summary>成本比例。</summary>
    public decimal Cost { get; set; }

    /// <summary>最低成本。</summary>
    public decimal MinCost { get; set; }

    /// <summary>最高成本。</summary>
    public decimal MaxCost { get; set; }

    /// <summary>接口配置原文。</summary>
    public string Setting { get; set; }

    /// <summary>查单配置原文。</summary>
    public string QuerySetting { get; set; }

    /// <summary>币种。</summary>
    public int Currency { get; set; }

    /// <summary>排序；更新时可改，新增忽略（自动 count+1）。</summary>
    public short Sort { get; set; }
}

/// <summary>提现渠道详情。</summary>
public sealed class WithdrawChannelDetailDto
{
    /// <summary>编号。</summary>
    public int ID { get; set; }

    /// <summary>名称。</summary>
    public string Name { get; set; }

    /// <summary>系统代付编号。</summary>
    public int SysID { get; set; }

    /// <summary>是否开放。</summary>
    public bool IsOpen { get; set; }

    /// <summary>成本比例。</summary>
    public decimal Cost { get; set; }

    /// <summary>最低成本。</summary>
    public decimal MinCost { get; set; }

    /// <summary>最高成本。</summary>
    public decimal MaxCost { get; set; }

    /// <summary>接口配置原文。</summary>
    public string Setting { get; set; }

    /// <summary>查单配置原文。</summary>
    public string QuerySetting { get; set; }

    /// <summary>币种。</summary>
    public int Currency { get; set; }

    /// <summary>排序。</summary>
    public short Sort { get; set; }
}
