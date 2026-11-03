using SimpX.Net.Caching;
using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 代理制上下分与登录令牌。对齐旧 ProxyTransfer / GetUserToken。
/// </summary>
public sealed class ProxyMoneyService : ITransient
{
    public const int FunctionSubTransfer = 1;
    public const int FunctionFreeTransfer = 2;
    public const int MoneyTypeProxyIn = MoneyLedgerCallbackService.MoneyTypeProxyIn;
    public const int MoneyTypeProxyOut = MoneyLedgerCallbackService.MoneyTypeProxyOut;

    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;
    private readonly IMoneyLedger _money;
    private readonly ISimpXCache _cache;

    public ProxyMoneyService(
        IBizDbAccessor biz,
        ICurrentSiteAccessor site,
        IMoneyLedger money,
        ISimpXCache cache)
    {
        _db = biz.Db;
        _site = site;
        _money = money;
        _cache = cache;
    }

    /// <summary>签发/刷新 usr_Token，返回 Token=GuidN。</summary>
    public async Task<object> IssueTokenAsync(int userId, CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        if (userId <= 0)
            throw Oops.Oh("#用户不存在#");
        var exists = await _db.Queryable<User>()
            .AnyAsync(u => u.SiteID == siteId && u.ID == userId, cancellationToken);
        if (!exists)
            throw Oops.Oh("#用户不存在#");

        var token = Guid.NewGuid();
        var now = DateTime.Now;
        var hasRow = await _db.Queryable<UserToken>()
            .AnyAsync(t => t.SiteID == siteId && t.UserID == userId, cancellationToken);
        if (hasRow)
        {
            await _db.Updateable<UserToken>()
                .SetColumns(t => new UserToken { Token = token, UpdateAt = now, Type = 0 })
                .Where(t => t.SiteID == siteId && t.UserID == userId)
                .ExecuteCommandAsync(cancellationToken);
        }
        else
        {
            await _db.Insertable(new UserToken
            {
                SiteID = siteId,
                UserID = userId,
                Token = token,
                UpdateAt = now,
                Type = 0
            }).ExecuteCommandAsync(cancellationToken);
        }

        _cache.Add(SiteHostCacheKeys.UserToken, token.ToString("N"), (int?)userId, TimeSpan.FromDays(7));
        return new { Token = token.ToString("N") };
    }

    /// <summary>代理上分：代理 → 下级（ProxyIn 方向）。</summary>
    public Task TransferInAsync(
        int agentId,
        string userName,
        string payPassword,
        decimal money,
        int currency,
        CancellationToken cancellationToken = default) =>
        TransferCoreAsync(agentId, userName, payPassword, money, currency, toSub: true, cancellationToken);

    /// <summary>代理下分：下级 → 代理（ProxyOut 方向）。</summary>
    public Task TransferOutAsync(
        int agentId,
        string userName,
        string payPassword,
        decimal money,
        int currency,
        CancellationToken cancellationToken = default) =>
        TransferCoreAsync(agentId, userName, payPassword, money, currency, toSub: false, cancellationToken);

    private async Task TransferCoreAsync(
        int agentId,
        string userName,
        string payPassword,
        decimal money,
        int currency,
        bool toSub,
        CancellationToken cancellationToken)
    {
        var siteId = _site.RequireSiteId();
        money = Math.Abs(money);
        if (money < 1m)
            throw Oops.Oh("#转账金额必须大于等于1#");
        if (agentId <= 0)
            throw Oops.Oh("#用户不存在#");

        var agents = await _db.Queryable<User>()
            .Where(u => u.SiteID == siteId && u.ID == agentId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var agent = agents.FirstOrDefault() ?? throw Oops.Oh("#用户不存在#");
        if (!Md5PasswordHasher.Matches(payPassword, agent.PayPassword))
            throw Oops.Oh("#资金密码错误#");

        userName = (userName ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(userName))
            throw Oops.Oh("#收款人或者回款人用户名错误#");
        var targets = await _db.Queryable<User>()
            .Where(u => u.SiteID == siteId && u.UserName == userName)
            .Take(1)
            .ToListAsync(cancellationToken);
        var target = targets.FirstOrDefault();
        if (target == null || target.ID == agentId)
            throw Oops.Oh("#收款人或者回款人用户名错误#");

        var fn = agent.Function;
        var hasSub = (fn & FunctionSubTransfer) != 0;
        var hasFree = (fn & FunctionFreeTransfer) != 0;
        if (!hasSub && !hasFree)
            throw Oops.Oh("#没有转账权限#");

        var isChild = target.AgentID == agentId
            || await _db.Queryable<UserDepth>()
                .AnyAsync(d => d.SiteID == siteId && d.UserID == agentId && d.ChildID == target.ID, cancellationToken);
        if (!isChild && !hasFree)
            throw Oops.Oh("#您只能操作直属下级#");

        var sourceId = Guid.NewGuid().ToString("N");
        int outId, inId;
        string outDesc, inDesc;
        if (toSub)
        {
            outId = agentId;
            inId = target.ID;
            outDesc = $"#转给#{target.UserName},#代理上分#";
            inDesc = $"{agent.UserName}#转入#,#代理上分#";
        }
        else
        {
            outId = target.ID;
            inId = agentId;
            outDesc = $"#转给#{agent.UserName},#代理下分#";
            inDesc = $"{target.UserName}#转入#,#代理下分#";
        }

        await _money.AddMoneyLogAsync(
            siteId, outId, -money, MoneyTypeProxyOut, sourceId, outDesc, currency, cancellationToken);
        await _money.AddMoneyLogAsync(
            siteId, inId, money, MoneyTypeProxyIn, sourceId, inDesc, currency, cancellationToken);
    }
}
