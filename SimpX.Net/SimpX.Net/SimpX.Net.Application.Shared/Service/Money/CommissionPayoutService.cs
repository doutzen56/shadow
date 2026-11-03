using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using SimpX.Net.Data.Entity.Agents;
using SimpX.Net.Data.Entity.Reports;
using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 代理佣金发放。对齐旧佣金：生成待发日志 Status=0，发放时账变 Commission=120。
/// </summary>
public sealed class CommissionPayoutService : ITransient
{
    /// <summary>待发放。</summary>
    public const int StatusNone = 0;

    /// <summary>失败/拒绝（旧 CheckStatus.Faild）。重叠判断时忽略这类记录。</summary>
    public const int StatusFailed = 1;

    /// <summary>已发放。</summary>
    public const int StatusPaid = 2;

    /// <summary>代理返佣账变类型。</summary>
    public const int MoneyTypeCommission = 120;

    private static readonly Regex JsonRateRegex = new(
        "\"Rate\"\\s*:\\s*(-?\\d+(?:\\.\\d+)?)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;
    private readonly IMoneyLedger _money;

    /// <summary>注入。</summary>
    public CommissionPayoutService(IBizDbAccessor biz, ICurrentSiteAccessor site, IMoneyLedger money)
    {
        _db = biz.Db;
        _site = site;
        _money = money;
    }

    /// <summary>待发与近期佣金日志（最多 200）。</summary>
    public async Task<List<CommissionLogListItemDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<UserCommissionLog>()
            .Where(l => l.SiteID == siteId)
            .OrderBy(l => l.CreateAt, OrderByType.Desc)
            .Take(200)
            .ToListAsync(cancellationToken);

        var names = await LoadNamesAsync(siteId, rows.Select(r => r.UserID).Distinct().ToList(), cancellationToken);
        return rows.Select(l => new CommissionLogListItemDto
        {
            ID = l.ID,
            UserID = l.UserID,
            UserName = names.GetValueOrDefault(l.UserID, string.Empty),
            Money = l.Money,
            Status = l.Status,
            Currency = l.Currency,
            StartDate = l.StartDate,
            EndDate = l.EndDate,
            CreateAt = l.CreateAt
        }).ToList();
    }

    /// <summary>
    /// 按区间生成待发佣金日志（只插 usr_CommissionLog，不写账变）。
    /// </summary>
    /// <param name="start">统计起（含）。</param>
    /// <param name="end">统计止（含）；必须早于今天。</param>
    /// <param name="now">当前时间；单测可注入，默认 DateTime.Now。</param>
    /// <param name="cancellationToken">取消。</param>
    /// <returns>成功与失败条数。</returns>
    public async Task<CommissionCreateResultDto> CreateAsync(
        DateTime start,
        DateTime end,
        DateTime? now = null,
        CancellationToken cancellationToken = default)
    {
        var clock = now ?? DateTime.Now;
        var siteId = _site.RequireSiteId();
        start = start.Date;
        end = end.Date;

        if (start > end)
            throw Oops.Oh("#起始日期不能大于或等于截至日期#");
        if (end >= clock.Date)
            throw Oops.Oh("#不能生成大于或等于当天的佣金#");
        if (clock < clock.Date.AddHours(1))
            throw Oops.Oh("#当天凌晨1点后才能生成代理佣金#");

        var overlap = await _db.Queryable<UserCommissionLog>()
            .Where(l => l.SiteID == siteId
                        && l.Status != StatusFailed
                        && l.StartDate <= end
                        && start <= l.EndDate)
            .AnyAsync(cancellationToken);
        if (overlap)
            throw Oops.Oh("#该时间段已生成返佣记录,不可重复发放#");

        var agents = await _db.Queryable<AgentUser>()
            .Where(a => a.SiteID == siteId
                        && a.CommissionID > 0
                        && a.Status == AgentUser.AgentStatus.Normal)
            .ToListAsync(cancellationToken);
        if (agents.Count == 0)
            return new CommissionCreateResultDto { Success = 0, Failed = 0 };

        var commissionIds = agents.Select(a => a.CommissionID).Distinct().ToList();
        var commissions = await _db.Queryable<AgentCommission>()
            .Where(c => c.SiteID == siteId && commissionIds.Contains(c.ID))
            .ToListAsync(cancellationToken);
        var commissionMap = commissions.ToDictionary(c => c.ID);

        var success = 0;
        var failed = 0;
        foreach (var agent in agents)
        {
            if (!commissionMap.TryGetValue(agent.CommissionID, out var plan))
                continue;

            try
            {
                var childIds = await _db.Queryable<UserDepth>()
                    .Where(d => d.SiteID == siteId && d.UserID == agent.ID && d.Depth == 1)
                    .Select(d => d.ChildID)
                    .ToListAsync(cancellationToken);

                decimal sumGame = 0;
                decimal sumEvent = 0;
                decimal sumRebate = 0;
                decimal sumRechargeFee = 0;
                decimal sumWithdrawFee = 0;
                var currency = 0;
                if (childIds.Count > 0)
                {
                    var stats = await _db.Queryable<ReportAgentStatis>()
                        .Where(s => s.SiteID == siteId
                                    && childIds.Contains(s.UserID)
                                    && s.Date >= start
                                    && s.Date <= end)
                        .ToListAsync(cancellationToken);
                    if (stats.Count > 0)
                    {
                        // 无汇率时先按单币汇总：取出现最多的币种，只加该币行。
                        currency = stats
                            .GroupBy(s => s.Currency)
                            .OrderByDescending(g => g.Count())
                            .Select(g => g.Key)
                            .First();
                        foreach (var row in stats.Where(s => s.Currency == currency))
                        {
                            sumGame += row.GameMoney;
                            sumEvent += row.MoneyEvent;
                            sumRebate += row.MoneyRebate;
                            sumRechargeFee += row.RechargeFee;
                            sumWithdrawFee += row.WithdrawFee;
                        }
                    }
                }

                var detailGameMoney = -sumGame;
                var fees = sumRechargeFee + sumWithdrawFee;
                // 有效会员数本波先按 0；阶梯 Member 门槛靠单测/后续补齐投注人数统计。
                var validUserCount = 0;
                var money = ComputeCommissionMoney(plan, detailGameMoney, sumEvent, sumRebate, fees, validUserCount);
                if (money <= 0)
                    continue;

                var rate = TrySelectLadderRate(plan.SettingString, detailGameMoney, validUserCount) ?? 0m;
                var detailJson = JsonSerializer.Serialize(new
                {
                    GameMoney = detailGameMoney,
                    MoneyEvent = sumEvent,
                    MoneyRebate = sumRebate,
                    Cost = fees,
                    Award = plan.Award,
                    Return = plan.Return,
                    Rate = rate,
                    Money = money,
                    Currency = currency
                });

                var log = new UserCommissionLog
                {
                    CommID = plan.ID,
                    SiteID = siteId,
                    UserID = agent.ID,
                    StartDate = start,
                    EndDate = end,
                    ValidUserCount = validUserCount,
                    RechargeWithdrawFee = fees,
                    ActivityMoney = sumEvent * plan.Award,
                    Money = money,
                    CreateAt = clock,
                    CheckAt = DateTime.MinValue,
                    Detail = detailJson,
                    Status = StatusNone,
                    Currency = currency
                };
                await _db.Insertable(log).ExecuteCommandAsync(cancellationToken);
                success++;
            }
            catch
            {
                failed++;
            }
        }

        return new CommissionCreateResultDto { Success = success, Failed = failed };
    }

    /// <summary>发放一笔待审佣金。返回是否成功。</summary>
    public async Task<bool> PayAsync(int logId, CancellationToken cancellationToken = default)
    {
        if (logId <= 0)
            throw Oops.Oh("#佣金记录错误#");
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<UserCommissionLog>()
            .Where(l => l.SiteID == siteId && l.ID == logId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var log = rows.FirstOrDefault() ?? throw Oops.Oh("#佣金记录不存在#");
        if (log.Status != StatusNone)
            throw Oops.Oh("#已处理#");
        if (log.Money <= 0)
            throw Oops.Oh("#金额错误#");

        await _money.AddMoneyLogAsync(
            siteId,
            log.UserID,
            log.Money,
            MoneyTypeCommission,
            $"CM{log.ID}",
            "代理佣金发放",
            log.Currency,
            cancellationToken);
        log.Status = StatusPaid;
        log.CheckAt = DateTime.Now;
        await _db.Updateable(log)
            .UpdateColumns(l => new { l.Status, l.CheckAt })
            .ExecuteCommandAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// 拒绝一笔待发佣金。对齐旧 commission/logreject。
    /// </summary>
    /// <param name="logId">日志号</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task RejectAsync(int logId, CancellationToken cancellationToken = default)
    {
        if (logId <= 0)
            throw Oops.Oh("#佣金记录错误#");
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<UserCommissionLog>()
            .Where(l => l.SiteID == siteId && l.ID == logId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var log = rows.FirstOrDefault() ?? throw Oops.Oh("#佣金记录不存在#");
        if (log.Status != StatusNone)
            throw Oops.Oh("#已处理#");

        log.Status = StatusFailed;
        log.CheckAt = DateTime.Now;
        await _db.Updateable(log)
            .UpdateColumns(l => new { l.Status, l.CheckAt })
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 佣金详情 JSON。对齐旧 commission/logdetail。
    /// </summary>
    /// <param name="logId">日志号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>详情</returns>
    public async Task<CommissionLogDetailDto> DetailAsync(int logId, CancellationToken cancellationToken = default)
    {
        if (logId <= 0)
            throw Oops.Oh("#编号错误#");
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<UserCommissionLog>()
            .Where(l => l.SiteID == siteId && l.ID == logId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var log = rows.FirstOrDefault() ?? throw Oops.Oh("#编号错误#");
        return new CommissionLogDetailDto
        {
            ID = log.ID,
            Detail = log.Detail ?? string.Empty
        };
    }

    /// <summary>
    /// 对齐旧 GetCommission 简化版：选满足 Money/Member 门槛的最高档 Rate，
    /// 再算 Money = Rate×GameMoney − Event×Award − Rebate×Return − fees；平台费 SiteRate 本波按 0。
    /// </summary>
    /// <param name="plan">佣金方案。</param>
    /// <param name="detailGameMoney">平台视角游戏盈亏（正=代理侧可抽成基数）。</param>
    /// <param name="sumEvent">活动金额合计。</param>
    /// <param name="sumRebate">反水金额合计。</param>
    /// <param name="fees">充提手续费合计。</param>
    /// <param name="validUserCount">有效会员数（对阶梯 Member）。</param>
    /// <returns>佣金金额，已下限归零。</returns>
    public static decimal ComputeCommissionMoney(
        AgentCommission plan,
        decimal detailGameMoney,
        decimal sumEvent,
        decimal sumRebate,
        decimal fees,
        int validUserCount = 0)
    {
        var rate = TrySelectLadderRate(plan.SettingString, detailGameMoney, validUserCount);
        if (rate.HasValue)
        {
            var money = rate.Value * detailGameMoney
                        - sumEvent * plan.Award
                        - sumRebate * plan.Return
                        - fees;
            return Math.Max(0m, money);
        }

        // 无阶梯时：基数 − 活动/反水承担 − 手续费
        return Math.Max(0m, detailGameMoney - sumEvent * plan.Award - sumRebate * plan.Return - fees);
    }

    /// <summary>
    /// 解析 Setting 阶梯（JSON 数组/单对象或 XML），选出 Money≤gameMoney 且 Member≤userCount 中 Rate 最高的一档。
    /// </summary>
    /// <param name="setting">方案 Setting 原文。</param>
    /// <param name="gameMoney">盈亏门槛对照值。</param>
    /// <param name="userCount">有效人数门槛对照值。</param>
    /// <returns>选中档 Rate；无可用档返回空。</returns>
    public static decimal? TrySelectLadderRate(string setting, decimal gameMoney, int userCount)
    {
        var levels = ParseLadderLevels(setting);
        if (levels.Count == 0)
            return null;

        decimal? best = null;
        foreach (var level in levels)
        {
            if (level.Money > gameMoney || level.Member > userCount)
                continue;
            if (best == null || level.Rate > best.Value)
                best = level.Rate;
        }
        return best;
    }

    /// <summary>兼容旧入口：不看门槛时取 Setting 里出现过的最大 Rate。</summary>
    public static decimal? TryParseRate(string setting)
    {
        var levels = ParseLadderLevels(setting);
        if (levels.Count == 0)
            return null;
        return levels.Max(l => l.Rate);
    }

    /// <summary>把 Setting 解析成 Money/Member/Rate 列表（JSON 数组、单对象、XML、正则兜底）。</summary>
    private static List<CommissionLadderLevel> ParseLadderLevels(string setting)
    {
        var list = new List<CommissionLadderLevel>();
        if (string.IsNullOrWhiteSpace(setting))
            return list;

        var text = setting.Trim();
        try
        {
            using var doc = JsonDocument.Parse(text);
            if (doc.RootElement.ValueKind == JsonValueKind.Object)
            {
                if (TryReadLevel(doc.RootElement, out var one))
                    list.Add(one);
                return list;
            }

            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in doc.RootElement.EnumerateArray())
                {
                    if (TryReadLevel(item, out var level))
                        list.Add(level);
                }
                return list;
            }
        }
        catch (JsonException)
        {
            // 继续试 XML
        }

        try
        {
            var root = XElement.Parse(text);
            foreach (var el in root.Elements().DefaultIfEmpty(root))
            {
                var rateAttr = el.Attribute("Rate");
                if (rateAttr == null)
                    continue;
                if (!decimal.TryParse(rateAttr.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var rate))
                    continue;
                var money = 0m;
                var member = 0;
                var moneyAttr = el.Attribute("Money");
                if (moneyAttr != null)
                    decimal.TryParse(moneyAttr.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out money);
                var memberAttr = el.Attribute("Member");
                if (memberAttr != null)
                    int.TryParse(memberAttr.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out member);
                list.Add(new CommissionLadderLevel(money, member, rate));
            }
            if (list.Count > 0)
                return list;
        }
        catch
        {
            // 继续正则
        }

        var m = JsonRateRegex.Match(text);
        if (m.Success
            && decimal.TryParse(m.Groups[1].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var fallback))
            list.Add(new CommissionLadderLevel(0m, 0, fallback));

        return list;
    }

    /// <summary>从 JSON 对象读一档；至少要有 Rate。</summary>
    private static bool TryReadLevel(JsonElement item, out CommissionLadderLevel level)
    {
        level = default;
        if (item.ValueKind != JsonValueKind.Object)
            return false;
        if (!item.TryGetProperty("Rate", out var r) || !r.TryGetDecimal(out var rate))
            return false;

        var money = 0m;
        var member = 0;
        if (item.TryGetProperty("Money", out var moneyEl))
            moneyEl.TryGetDecimal(out money);
        if (item.TryGetProperty("Member", out var memberEl))
        {
            if (memberEl.ValueKind == JsonValueKind.Number && memberEl.TryGetInt32(out var m))
                member = m;
            else if (memberEl.TryGetDecimal(out var md))
                member = (int)md;
        }

        level = new CommissionLadderLevel(money, member, rate);
        return true;
    }

    /// <summary>阶梯一档：盈亏门槛、人数门槛、比例。</summary>
    private readonly struct CommissionLadderLevel
    {
        /// <summary>构造一档。</summary>
        public CommissionLadderLevel(decimal money, int member, decimal rate)
        {
            Money = money;
            Member = member;
            Rate = rate;
        }

        /// <summary>盈亏门槛。</summary>
        public decimal Money { get; }

        /// <summary>有效人数门槛。</summary>
        public int Member { get; }

        /// <summary>佣金比例。</summary>
        public decimal Rate { get; }
    }

    private async Task<Dictionary<int, string>> LoadNamesAsync(int siteId, List<int> ids, CancellationToken ct)
    {
        var map = new Dictionary<int, string>();
        if (ids.Count == 0) return map;
        var users = await _db.Queryable<User>()
            .Where(u => u.SiteID == siteId && ids.Contains(u.ID))
            .Select(u => new { u.ID, u.UserName })
            .ToListAsync(ct);
        foreach (var u in users)
            map[u.ID] = u.UserName ?? string.Empty;
        return map;
    }
}
