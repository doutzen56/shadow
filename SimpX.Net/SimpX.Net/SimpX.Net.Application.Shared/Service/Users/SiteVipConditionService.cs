using SimpX.Net.Data.Entity.Sites;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 租户后台 VIP 申请资格配置。对齐旧 /site/vip/levels。
/// </summary>
public sealed partial class SiteVipConditionService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;

    /// <summary>
    /// 注入依赖。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站</param>
    public SiteVipConditionService(IBizDbAccessor biz, ICurrentSiteAccessor site)
    {
        _db = biz.Db;
        _site = site;
    }

    /// <summary>
    /// 读取当前站 VIP 资格相关原文。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>配置</returns>
    public async Task<SiteVipConditionDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<SiteVipConfig>()
            .Where(c => c.ID == siteId).Take(1).ToListAsync(cancellationToken);
        var c = rows.FirstOrDefault();
        return new SiteVipConditionDto
        {
            SiteID = siteId,
            VIPConditionSetting = c?.VIPConditionSetting ?? string.Empty,
            VIPRightSetting = c?.VIPRightSetting ?? string.Empty,
            RuleClause = c?.RuleClause ?? string.Empty,
            Currency = c?.Currency ?? 0
        };
    }

    /// <summary>
    /// 保存资格配置（无行则插入）。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SaveAsync(SiteVipConditionSaveInput input, CancellationToken cancellationToken = default)
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
                VIPConditionSetting = input.VIPConditionSetting ?? string.Empty,
                VIPRightSetting = input.VIPRightSetting ?? string.Empty,
                RuleClause = input.RuleClause ?? string.Empty,
                Currency = input.Currency,
                PointItemSettingString = string.Empty,
                ExperItemSettingString = string.Empty,
                PointRule = string.Empty,
                AutoReturnRule = string.Empty
            }).ExecuteCommandAsync(cancellationToken);
            return;
        }

        entity.VIPConditionSetting = input.VIPConditionSetting ?? string.Empty;
        entity.VIPRightSetting = input.VIPRightSetting ?? string.Empty;
        entity.RuleClause = input.RuleClause ?? string.Empty;
        entity.Currency = input.Currency;
        await _db.Updateable(entity).ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>积分规则原文（对齐 /request/site/vip/pointrule）。</summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>{ PointRule, AutoReturnRule }</returns>
    public async Task<object> PointRuleAsync(CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<SiteVipConfig>()
            .Where(c => c.ID == siteId).Take(1).ToListAsync(cancellationToken);
        var row = rows.FirstOrDefault();
        return new
        {
            PointRule = row?.PointRule ?? string.Empty,
            AutoReturnRule = row?.AutoReturnRule ?? string.Empty
        };
    }

    /// <summary>
    /// 保存自助反水规则原文。对齐旧 /admin/site/vip/savereturnrule。
    /// </summary>
    /// <param name="autoReturnRule">规则原文</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SaveReturnRuleAsync(string autoReturnRule, CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<SiteVipConfig>()
            .Where(c => c.ID == siteId).Take(1).ToListAsync(cancellationToken);
        var entity = rows.FirstOrDefault();
        if (entity == null)
        {
            await _db.Insertable(new SiteVipConfig
            {
                ID = siteId,
                CreateAt = DateTime.Now,
                VIPConditionSetting = string.Empty,
                VIPRightSetting = string.Empty,
                RuleClause = string.Empty,
                PointItemSettingString = string.Empty,
                ExperItemSettingString = string.Empty,
                PointRule = string.Empty,
                AutoReturnRule = autoReturnRule ?? string.Empty,
                Currency = 0
            }).ExecuteCommandAsync(cancellationToken);
            return;
        }

        entity.AutoReturnRule = autoReturnRule ?? string.Empty;
        await _db.Updateable(entity)
            .UpdateColumns(c => new { c.AutoReturnRule })
            .ExecuteCommandAsync(cancellationToken);
    }
}

/// <summary>VIP 资格配置。</summary>
public sealed class SiteVipConditionDto
{
    /// <summary>站点。</summary>
    public int SiteID { get; set; }

    /// <summary>申请资格原文。</summary>
    public string VIPConditionSetting { get; set; }

    /// <summary>特权原文。</summary>
    public string VIPRightSetting { get; set; }

    /// <summary>规则条款。</summary>
    public string RuleClause { get; set; }

    /// <summary>币种。</summary>
    public int Currency { get; set; }
}

/// <summary>保存 VIP 资格。</summary>
public sealed class SiteVipConditionSaveInput
{
    /// <summary>申请资格原文。</summary>
    public string VIPConditionSetting { get; set; }

    /// <summary>特权原文。</summary>
    public string VIPRightSetting { get; set; }

    /// <summary>规则条款。</summary>
    public string RuleClause { get; set; }

    /// <summary>币种。</summary>
    public int Currency { get; set; }
}
