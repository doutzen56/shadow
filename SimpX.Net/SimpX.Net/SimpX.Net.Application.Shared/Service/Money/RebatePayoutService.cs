using System.Globalization;
using System.Text.Json;
using SimpX.Net.Data.Entity.Reports;
using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 返水计划列表与发放。对齐旧 rebate/planlist 主列表；发放对待审日志逐笔走账本。
/// 佣金/返点本刀共用本服务壳（计划表先走返水计划；无独立计划实体时列表为空）。
/// </summary>
public sealed partial class RebatePayoutService : ITransient
{
    /// <summary>待发放。</summary>
    public const int LogStatusNone = 0;

    /// <summary>已发放。</summary>
    public const int LogStatusSuccess = 2;

    /// <summary>手动返水账变类型（对齐旧 MoneyType.RebateAdd=61）。</summary>
    public const int MoneyTypeRebate = 61;

    /// <summary>会员返水日志类型（生成计划时默认）。</summary>
    public const string LogTypeMember = "Member";

    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;
    private readonly IMoneyLedger _money;

    /// <summary>注入。</summary>
    public RebatePayoutService(IBizDbAccessor biz, ICurrentSiteAccessor site, IMoneyLedger money)
    {
        _db = biz.Db;
        _site = site;
        _money = money;
    }

    /// <summary>返水计划列表。</summary>
    public async Task<List<RebatePlanListItemDto>> ListPlansAsync(CancellationToken cancellationToken = default)
    {
        var page = await PlanlistPageAsync(new Dictionary<string, object?>(), 1, 200, cancellationToken);
        return page.Items;
    }

    /// <summary>
    /// 返水计划分页（对齐旧 planlist：默认按 CreateAt 日切；可按 Name/Currency/Status）。
    /// </summary>
    public async Task<RebatePlanPageOutput> PlanlistPageAsync(
        Dictionary<string, object?> input,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        var siteId = _site.RequireSiteId();
        var query = _db.Queryable<UserRebatePlan>().Where(p => p.SiteID == siteId);

        var name = ReadString(input, "Name");
        if (!string.IsNullOrWhiteSpace(name))
            query = query.Where(p => p.Name.Contains(name.Trim()));

        var currencyLocked = false;
        if (TryReadInt(input, out var currency, "Currency", "currency") && currency > 0)
        {
            query = query.Where(p => p.Currency == currency);
            currencyLocked = true;
        }

        var statusText = ReadString(input, "Status");
        if (!string.IsNullOrWhiteSpace(statusText))
        {
            var pendingPlanIds = await _db.Queryable<UserRebateLog>()
                .Where(l => l.SiteID == siteId && l.Status == LogStatusNone && l.PlanID > 0)
                .Select(l => l.PlanID)
                .Distinct()
                .ToListAsync(cancellationToken);
            query = query.Where(p => pendingPlanIds.Contains(p.ID));
        }
        else
        {
            var date = ReadDate(input, "Date") ?? DateTime.Now.Date;
            var endDate = (ReadDate(input, "EndDate") ?? DateTime.Now.Date).Date.AddDays(1);
            query = query.Where(p => p.CreateAt >= date && p.CreateAt < endDate);
        }

        var index = pageIndex < 1 ? 1 : pageIndex;
        var size = pageSize < 1 ? 20 : Math.Min(pageSize, 5000);
        RefAsync<int> total = 0;
        var rows = await query
            .OrderBy(p => p.CreateAt, OrderByType.Desc)
            .ToPageListAsync(index, size, total, cancellationToken);

        var baseCurrency = await SiteCurrencyFx.GetBaseCurrencyAsync(_db, siteId, cancellationToken);
        var points = await SiteCurrencyFx.LoadPointsAsync(_db, siteId, baseCurrency, cancellationToken);

        // data 汇总：对本页过滤条件下全量合计（对齐旧 list.GroupBy 后再 ShowResult）
        var allForSum = await query.ToListAsync(cancellationToken);
        decimal moneySum = 0, paidSum = 0;
        foreach (var p in allForSum)
        {
            moneySum += SiteCurrencyFx.ToBase(p.Money, p.FromCurrency, baseCurrency, points, currencyLocked);
            paidSum += SiteCurrencyFx.ToBase(p.Paid, p.Currency, baseCurrency, points, currencyLocked);
        }

        var adminIds = rows.Select(r => r.AdminID).Where(id => id > 0).Distinct().ToList();
        var adminNames = new Dictionary<int, string>();
        if (adminIds.Count > 0)
        {
            var admins = await _db.Queryable<SiteAdmin>()
                .Where(a => a.SiteID == siteId && adminIds.Contains(a.ID))
                .Select(a => new { a.ID, a.AdminName })
                .ToListAsync(cancellationToken);
            foreach (var a in admins)
                adminNames[a.ID] = a.AdminName ?? string.Empty;
        }

        return new RebatePlanPageOutput
        {
            RecordCount = total.Value,
            DataMoney = moneySum,
            DataPaid = paidSum,
            Items = rows.Select(p => new RebatePlanListItemDto
            {
                ID = p.ID,
                Name = p.Name ?? string.Empty,
                Money = p.Money,
                Paid = p.Paid,
                Total = p.Total,
                Success = p.Success,
                Faild = p.Faild,
                FlowRate = p.FlowRate,
                StartAt = p.StartAt,
                EndAt = p.EndAt,
                CreateAt = p.CreateAt,
                AdminID = p.AdminID,
                AdminName = adminNames.GetValueOrDefault(p.AdminID, string.Empty),
                Currency = p.Currency,
                FromCurrency = p.FromCurrency,
                MoneyT = SiteCurrencyFx.ToBase(p.Money, p.FromCurrency, baseCurrency, points, currencyLocked),
                PaidT = SiteCurrencyFx.ToBase(p.Paid, p.Currency, baseCurrency, points, currencyLocked)
            }).ToList()
        };
    }

    /// <summary>
    /// 旧版 loglist 全量出参（含 data.Money 折算、MoneyT、Agent、Status/Currency 文案）。
    /// </summary>
    public async Task<object> LoglistForOldApiAsync(
        Dictionary<string, object?> input,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        var siteId = _site.RequireSiteId();
        var query = BuildLoglistQuery(siteId, input);

        var agentName = ReadString(input, "Agent");
        var userName = ReadString(input, "User", "UserName");
        if (!string.IsNullOrWhiteSpace(agentName))
        {
            var name = agentName.Trim();
            List<int> agentIds;
            if (int.TryParse(name, out var aidExact))
                agentIds = [aidExact];
            else
                agentIds = await _db.Queryable<User>()
                    .Where(u => u.SiteID == siteId && u.UserName.Contains(name))
                    .Select(u => u.ID)
                    .Take(500)
                    .ToListAsync(cancellationToken);
            // 上级代理：筛其直属会员的返水日志（完整下级树后续接 UserDepth）
            var memberIds = agentIds.Count == 0
                ? new List<int>()
                : await _db.Queryable<User>()
                    .Where(u => u.SiteID == siteId && agentIds.Contains(u.AgentID))
                    .Select(u => u.ID)
                    .Take(5000)
                    .ToListAsync(cancellationToken);
            query = memberIds.Count == 0
                ? query.Where(l => false)
                : query.Where(l => memberIds.Contains(l.UserID));
        }
        if (!string.IsNullOrWhiteSpace(userName))
        {
            var name = userName.Trim();
            List<int> named;
            if (int.TryParse(name, out var uidExact))
                named = [uidExact];
            else
                named = await _db.Queryable<User>()
                    .Where(u => u.SiteID == siteId && u.UserName.Contains(name))
                    .Select(u => u.ID)
                    .Take(500)
                    .ToListAsync(cancellationToken);
            query = named.Count == 0
                ? query.Where(l => false)
                : query.Where(l => named.Contains(l.UserID));
        }

        // 对齐旧 GetLogDetail：先全量取再 ShowResult 分页；本站日志量级可接受
        var rows = await query
            .OrderBy(l => l.CreateAt, OrderByType.Desc)
            .ToListAsync(cancellationToken);

        var currencyLocked = !string.IsNullOrWhiteSpace(ReadString(input, "Currency", "currency"));
        var baseCurrency = await SiteCurrencyFx.GetBaseCurrencyAsync(_db, siteId, cancellationToken);
        var points = await SiteCurrencyFx.LoadPointsAsync(_db, siteId, baseCurrency, cancellationToken);

        decimal moneySum = 0;
        foreach (var t in rows.Where(t => t.Status == LogStatusSuccess))
            moneySum += SiteCurrencyFx.ToBase(t.Money, t.Currency, baseCurrency, points, currencyLocked);

        var userIds = rows.Select(r => r.UserID).Distinct().ToList();
        var nameMap = await LoadNamesAsync(siteId, userIds, cancellationToken);
        var agentMap = await LoadAgentAsync(siteId, userIds, cancellationToken);

        var projected = rows.Select(t =>
        {
            var moneyT = SiteCurrencyFx.ToBase(t.Money, t.Currency, baseCurrency, points, currencyLocked);
            agentMap.TryGetValue(t.UserID, out var agent);
            // 金额按旧 ToJson 习惯输出字符串，避免 number/string 不一致
            return new
            {
                t.ID,
                t.UserID,
                t.PlanID,
                UserName = nameMap.TryGetValue(t.UserID, out var n) ? n : string.Empty,
                Money = t.Money.ToString("0.0000", CultureInfo.InvariantCulture),
                MoneyT = moneyT.ToString(CultureInfo.InvariantCulture),
                StartAt = NormalizeRebateLogStartAtForDisplay(t.Type, t.StartAt, t.EndAt),
                t.EndAt,
                t.CreateAt,
                AgentID = agent.AgentID,
                AgentName = agent.AgentName ?? string.Empty,
                FaildMsg = string.Equals(t.Type, "V2", StringComparison.OrdinalIgnoreCase)
                    ? string.Empty
                    : (t.FaildMsg ?? string.Empty),
                ClaimNo = string.Equals(t.Type, "V2", StringComparison.OrdinalIgnoreCase)
                    ? (t.FaildMsg ?? string.Empty)
                    : null,
                Type = FormatRebateType(t.Type),
                Status = FormatCheckStatus(t.Status),
                Currency = FormatCurrency(t.Currency)
            };
        }).ToList();

        return OldApiShowResult.SlicePage(
            projected,
            pageIndex,
            pageSize,
            data: new { Money = moneySum.ToString(CultureInfo.InvariantCulture) });
    }

    private ISugarQueryable<UserRebateLog> BuildLoglistQuery(int siteId, Dictionary<string, object?> input)
    {
        // 对齐旧 GetLogDetail 过滤
        var query = _db.Queryable<UserRebateLog>().Where(l => l.SiteID == siteId);
        var planId = ReadInt(input, "PlanID", "planId");
        if (planId > 0)
            query = query.Where(l => l.PlanID == planId);

        var startAtText = ReadString(input, "StartAt");
        var endAtText = ReadString(input, "EndAt");
        var dateText = ReadString(input, "Date");
        if (!string.IsNullOrWhiteSpace(startAtText))
        {
            var startDate = (ReadDate(input, "StartAt") ?? DateTime.Now.Date).Date;
            var endDate = (ReadDate(input, "EndAt") ?? startDate).Date.AddDays(1);
            query = query.Where(l => l.CreateAt >= startDate && l.CreateAt < endDate);
        }
        else if (!string.IsNullOrWhiteSpace(endAtText))
        {
            var endDate = (ReadDate(input, "EndAt") ?? DateTime.Now.Date).Date.AddDays(1);
            query = query.Where(l => l.CreateAt < endDate);
        }

        if (!string.IsNullOrWhiteSpace(dateText))
        {
            var date = (ReadDate(input, "Date") ?? DateTime.Now.Date).Date;
            query = query.Where(l => l.CreateAt >= date && l.CreateAt < date.AddDays(1));
        }

        // User/Agent 过滤在 LoglistForOldApiAsync 里异步补，避免这里同步 First

        var statusText = ReadString(input, "Status");
        if (!string.IsNullOrWhiteSpace(statusText))
        {
            if (int.TryParse(statusText, out var st))
                query = query.Where(l => l.Status == st);
            else if (string.Equals(statusText, "Success", StringComparison.OrdinalIgnoreCase))
                query = query.Where(l => l.Status == LogStatusSuccess);
            else if (string.Equals(statusText, "None", StringComparison.OrdinalIgnoreCase))
                query = query.Where(l => l.Status == LogStatusNone);
            else if (string.Equals(statusText, "Failed", StringComparison.OrdinalIgnoreCase)
                     || string.Equals(statusText, "Faild", StringComparison.OrdinalIgnoreCase))
                query = query.Where(l => l.Status == 1);
        }

        if (TryReadInt(input, out var currency, "Currency", "currency") && currency > 0)
            query = query.Where(l => l.Currency == currency);

        var typeText = ReadString(input, "Type", "type");
        if (!string.IsNullOrWhiteSpace(typeText))
        {
            var t = typeText.Trim();
            // 前端可能传展示名或库内码
            if (string.Equals(t, "自助", StringComparison.Ordinal) || string.Equals(t, "v2", StringComparison.OrdinalIgnoreCase))
                query = query.Where(l => l.Type == "V2" || l.Type == "v2");
            else if (string.Equals(t, "综合", StringComparison.Ordinal) || string.Equals(t, "aa", StringComparison.OrdinalIgnoreCase))
                query = query.Where(l => l.Type == "AA" || l.Type == "aa");
            else
                query = query.Where(l => l.Type == t);
        }

        return query;
    }

    private async Task<Dictionary<int, (int AgentID, string AgentName)>> LoadAgentAsync(
        int siteId,
        List<int> userIds,
        CancellationToken cancellationToken)
    {
        var map = new Dictionary<int, (int, string)>();
        if (userIds.Count == 0)
            return map;
        var users = await _db.Queryable<User>()
            .Where(u => u.SiteID == siteId && userIds.Contains(u.ID))
            .Select(u => new { u.ID, u.AgentID })
            .ToListAsync(cancellationToken);
        var agentIds = users.Select(u => u.AgentID).Where(id => id > 0).Distinct().ToList();
        var agentNames = new Dictionary<int, string>();
        if (agentIds.Count > 0)
        {
            var agents = await _db.Queryable<User>()
                .Where(u => u.SiteID == siteId && agentIds.Contains(u.ID))
                .Select(u => new { u.ID, u.UserName })
                .ToListAsync(cancellationToken);
            foreach (var a in agents)
                agentNames[a.ID] = a.UserName ?? string.Empty;
        }

        foreach (var u in users)
            map[u.ID] = (u.AgentID, agentNames.GetValueOrDefault(u.AgentID, string.Empty));
        return map;
    }

    private static string FormatCheckStatus(int status) => OldApiEnumDisplay.CheckStatus(status);

    private static string FormatCurrency(int currency) => OldApiEnumDisplay.Currency(currency);

    /// <summary>对齐旧 NormalizeRebateLogStartAtForDisplay：V2 且 StartAt&gt;EndAt 时钳到 EndAt。</summary>
    private static DateTime NormalizeRebateLogStartAtForDisplay(string type, DateTime startAt, DateTime endAt)
    {
        if (string.Equals(type, "V2", StringComparison.OrdinalIgnoreCase) && startAt > endAt && endAt != default)
            return endAt;
        return startAt;
    }

    /// <summary>
    /// 把时间段内、尚未挂计划的待发返水日志收成一份计划（简化对齐旧 ExecuteRebateLog 建计划）。
    /// </summary>
    /// <param name="input">时间段与标题</param>
    /// <param name="adminId">操作员</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <param name="includeCalculatedTypes">为 true 时连同 AA/分类待发一起收，仍排除自助 V2</param>
    /// <returns>计划编号</returns>
    public async Task<int> GeneratePlanAsync(
        RebateGeneratePlanInput input,
        int adminId,
        CancellationToken cancellationToken = default,
        bool includeCalculatedTypes = false)
    {
        ArgumentNullException.ThrowIfNull(input);
        var title = (input.Name ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(title))
            throw Oops.Oh("#请输入反水标识#");
        if (input.StartAt == default || input.EndAt == default || input.EndAt < input.StartAt)
            throw Oops.Oh("#时间范围错误#");

        var siteId = _site.RequireSiteId();
        var endExclusive = input.EndAt.Date.AddDays(1);
        var typeFilter = string.IsNullOrWhiteSpace(input.Type) ? LogTypeMember : input.Type.Trim();
        var pendingQuery = _db.Queryable<UserRebateLog>()
            .Where(l =>
                l.SiteID == siteId
                && l.PlanID == 0
                && l.Status == LogStatusNone
                && l.Money > 0
                && l.StartAt >= input.StartAt
                && l.StartAt < endExclusive);
        if (includeCalculatedTypes
            || string.IsNullOrEmpty(typeFilter)
            || string.Equals(typeFilter, LogTypeMember, StringComparison.OrdinalIgnoreCase))
        {
            // 发放页默认类型是 Member，重算实际写成 AA 或 A+分类。
            // 只按 Member 精确匹配时，待发日志收不进计划，计划列表会一直是空的。自助 V2 仍排除。
            pendingQuery = pendingQuery.Where(l => l.Type == null || l.Type == "" || l.Type != "V2");
        }
        else
        {
            pendingQuery = pendingQuery.Where(l => l.Type == typeFilter);
        }

        if (input.Currency.HasValue)
            pendingQuery = pendingQuery.Where(l => l.Currency == input.Currency.Value);

        // 执行时若勾选了日志编号，只收这些待发行（对齐旧 checked / ID）
        if (input is RebateExecuteInput exec)
        {
            var ids = new HashSet<int>();
            if (exec.Checked is { Length: > 0 })
                foreach (var id in exec.Checked.Where(x => x > 0))
                    ids.Add(id);
            if (exec.ID is { Length: > 0 })
                foreach (var id in exec.ID.Where(x => x > 0))
                    ids.Add(id);
            if (ids.Count > 0)
                pendingQuery = pendingQuery.Where(l => ids.Contains(l.ID));
        }

        var pending = await pendingQuery.ToListAsync(cancellationToken);
        if (pending.Count == 0)
            throw Oops.Oh("#没有可生成的返水记录#");

        var plan = new UserRebatePlan
        {
            SiteID = siteId,
            Name = title,
            AdminID = adminId,
            CreateAt = DateTime.Now,
            Total = pending.Count,
            StartAt = input.StartAt,
            EndAt = input.EndAt,
            Money = pending.Sum(p => p.Money),
            FlowRate = input.FlowRate,
            Paid = 0,
            Success = 0,
            Faild = 0,
            Currency = pending[0].Currency,
            FromCurrency = pending[0].Currency
        };
        var planId = await _db.Insertable(plan).ExecuteReturnIdentityAsync(cancellationToken);
        foreach (var log in pending)
        {
            log.PlanID = planId;
            // 库里 Type 只有 2 个字符，现网写 AA/A2/V2；不能写 Member
            if (string.IsNullOrEmpty(log.Type))
                log.Type = "AA";
        }

        await _db.Updateable(pending)
            .UpdateColumns(l => new { l.PlanID, l.Type })
            .ExecuteCommandAsync(cancellationToken);
        return planId;
    }

    /// <summary>
    /// 发放计划下待审日志。返回成功笔数。
    /// </summary>
    public async Task<int> PayPlanAsync(int planId, CancellationToken cancellationToken = default)
    {
        if (planId <= 0)
            throw Oops.Oh("#计划错误#");
        var siteId = _site.RequireSiteId();
        var plans = await _db.Queryable<UserRebatePlan>()
            .Where(p => p.SiteID == siteId && p.ID == planId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var plan = plans.FirstOrDefault() ?? throw Oops.Oh("#计划不存在#");

        var logs = await _db.Queryable<UserRebateLog>()
            .Where(l => l.SiteID == siteId && l.PlanID == planId && l.Status == LogStatusNone && l.Money > 0)
            .ToListAsync(cancellationToken);

        return await PayLogsAsync(siteId, plan, logs, cancellationToken);
    }

    /// <summary>
    /// 建计划并立刻发放待审日志。先按日投注和返水设定重算未发放日志，再入账。
    /// </summary>
    /// <param name="input">时间段与标题</param>
    /// <param name="adminId">操作员</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>计划号与成功笔数</returns>
    public async Task<RebateExecuteResultDto> ExecuteAsync(
        RebateExecuteInput input,
        int adminId,
        CancellationToken cancellationToken = default)
    {
        input ??= new RebateExecuteInput();
        if (!string.IsNullOrWhiteSpace(input.Title))
            input.Name = input.Title.Trim();
        // 与旧 execute 一致：开了自助反水则禁止手工发放
        await EnsureManualRebateAllowedAsync(cancellationToken);
        var written = await RecalculateCoreAsync(input, cancellationToken);
        var hasSelection = (input.Checked is { Length: > 0 }) || (input.ID is { Length: > 0 });
        if (!hasSelection && written.Count > 0)
            input.Checked = written.ToArray();
        var planId = await GeneratePlanAsync(
            input,
            adminId,
            cancellationToken,
            includeCalculatedTypes: written.Count > 0 || hasSelection);
        var siteId = _site.RequireSiteId();
        var plans = await _db.Queryable<UserRebatePlan>()
            .Where(p => p.SiteID == siteId && p.ID == planId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var plan = plans.FirstOrDefault() ?? throw Oops.Oh("#计划不存在#");
        var logs = await _db.Queryable<UserRebateLog>()
            .Where(l => l.SiteID == siteId && l.PlanID == planId && l.Status == LogStatusNone && l.Money > 0)
            .ToListAsync(cancellationToken);
        var success = await PayLogsAsync(siteId, plan, logs, cancellationToken);
        var faild = logs.Count(l => l.Status == 1);
        return new RebateExecuteResultDto
        {
            PlanID = planId,
            Success = success,
            Faild = faild
        };
    }

    /// <summary>
    /// 拒绝一笔待发返水。对齐旧 reject。
    /// </summary>
    /// <param name="planId">计划编号</param>
    /// <param name="logId">日志编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task RejectAsync(int planId, int logId, CancellationToken cancellationToken = default)
    {
        if (planId <= 0 || logId <= 0)
            throw Oops.Oh("#反水不存在#");
        var siteId = _site.RequireSiteId();
        var plans = await _db.Queryable<UserRebatePlan>()
            .Where(p => p.SiteID == siteId && p.ID == planId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var plan = plans.FirstOrDefault() ?? throw Oops.Oh("#计划不存在#");
        var logs = await _db.Queryable<UserRebateLog>()
            .Where(l => l.SiteID == siteId && l.PlanID == planId && l.ID == logId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var log = logs.FirstOrDefault() ?? throw Oops.Oh("#反水不存在#");
        if (log.Status != LogStatusNone)
            throw Oops.Oh("#未发放状态才可以拒绝#");

        log.Status = 1;
        log.CheckAt = DateTime.Now;
        await _db.Updateable(log)
            .UpdateColumns(l => new { l.Status, l.CheckAt })
            .ExecuteCommandAsync(cancellationToken);
        plan.Faild += 1;
        await _db.Updateable(plan)
            .UpdateColumns(p => new { p.Faild })
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 重发待审返水。对齐旧 reextcute（本刀只重发 Status=0，避免重复入账）。
    /// </summary>
    /// <param name="planId">计划编号</param>
    /// <param name="logId">日志编号；0=整计划待审</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>成功笔数</returns>
    public async Task<int> ReExecuteAsync(int planId, int logId, CancellationToken cancellationToken = default)
    {
        if (planId <= 0)
            throw Oops.Oh("#计划错误#");
        var siteId = _site.RequireSiteId();
        var plans = await _db.Queryable<UserRebatePlan>()
            .Where(p => p.SiteID == siteId && p.ID == planId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var plan = plans.FirstOrDefault() ?? throw Oops.Oh("#计划不存在#");

        var query = _db.Queryable<UserRebateLog>()
            .Where(l => l.SiteID == siteId && l.PlanID == planId && l.Status == LogStatusNone && l.Money > 0);
        if (logId > 0)
            query = query.Where(l => l.ID == logId);
        var logs = await query.ToListAsync(cancellationToken);
        return await PayLogsAsync(siteId, plan, logs, cancellationToken);
    }

    /// <summary>
    /// 按日游戏汇总和已启用返水设定，重写未发放日志。不入账。
    /// 同一会员、币种、类型、日期已经发放或已进计划的不覆盖。
    /// </summary>
    /// <param name="input">时间段；可指定用户名和分组</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>新增或更新的日志笔数</returns>
    public async Task<int> RecalculateAsync(
        RebateGeneratePlanInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await EnsureManualRebateAllowedAsync(cancellationToken);
        var ids = await RecalculateCoreAsync(input, cancellationToken);
        return ids.Count;
    }

    /// <summary>
    /// 重算未发放返水日志。没有启用设定时返回空列表，不挡原有待发日志。
    /// </summary>
    /// <param name="input">时间段</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>新增或更新的日志编号</returns>
    private async Task<List<int>> RecalculateCoreAsync(
        RebateGeneratePlanInput input,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.StartAt == default || input.EndAt == default || input.EndAt < input.StartAt)
            throw Oops.Oh("#时间范围错误#");

        var siteId = _site.RequireSiteId();
        var settings = await _db.Queryable<RebateSetting>()
            .Where(s => s.SiteID == siteId && s.Enabled)
            .ToListAsync(cancellationToken);
        if (settings.Count == 0)
            return new List<int>();

        // 当天数据还没结完，不按当天重算
        if (input.EndAt.Date >= DateTime.Today)
            throw Oops.Oh("#当前不能发放当天的反水#");

        var start = input.StartAt.Date;
        var end = input.EndAt.Date;
        var gameQuery = _db.Queryable<UserDateGame>()
            .Where(g => g.SiteID == siteId && g.Date >= start && g.Date <= end);
        if (input.Currency.HasValue)
            gameQuery = gameQuery.Where(g => g.Currency == input.Currency.Value);
        var games = await gameQuery.ToListAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(input.User))
        {
            var names = input.User.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (names.Length == 0)
                return new List<int>();
            var named = await _db.Queryable<User>()
                .Where(u => u.SiteID == siteId && names.Contains(u.UserName))
                .Select(u => u.ID)
                .ToListAsync(cancellationToken);
            if (named.Count == 0)
                return new List<int>();
            var only = named.ToHashSet();
            games = games.Where(g => only.Contains(g.UserID)).ToList();
        }

        if (games.Count == 0)
            return new List<int>();

        var userIds = games.Select(g => g.UserID).Distinct().ToList();
        var userRows = await _db.Queryable<User>()
            .Where(u => u.SiteID == siteId && userIds.Contains(u.ID))
            .Select(u => new RebateUserSnap { ID = u.ID, GroupID = u.GroupID, Level = u.Level })
            .ToListAsync(cancellationToken);
        var userMap = userRows.ToDictionary(u => u.ID);
        if (input.GroupID is { Length: > 0 })
        {
            var groups = input.GroupID.ToHashSet();
            userMap = userMap.Where(kv => groups.Contains(kv.Value.GroupID))
                .ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        var existing = await _db.Queryable<UserRebateLog>()
            .Where(l => l.SiteID == siteId && l.StartAt >= start && l.StartAt < end.AddDays(1))
            .ToListAsync(cancellationToken);
        var written = new List<int>();
        var sort = existing.Count;
        foreach (var typeGroup in settings.GroupBy(s => string.IsNullOrWhiteSpace(s.Type) ? "AA" : s.Type.Trim()))
        {
            var typeKey = typeGroup.Key;
            var typed = games.Where(g => RebateSettingCovers(typeKey, g.Category));
            foreach (var grp in typed.GroupBy(g => (g.UserID, g.Currency)))
            {
                if (!userMap.TryGetValue(grp.Key.UserID, out var user))
                    continue;
                var buckets = grp.GroupBy(g => (g.Category, g.Type))
                    .Select(g => new { g.Key.Category, g.Key.Type, Bet = g.Sum(x => x.BetMoney) })
                    .Where(x => x.Bet > 0)
                    .ToList();
                var betTotal = buckets.Sum(x => x.Bet);
                var setting = typeGroup
                    .Where(s => RebateGroupHit(s.Groups, user.GroupID)
                                && RebateLevelHit(s.Levels, user.Level)
                                && betTotal >= s.BetMoney)
                    .OrderByDescending(s => s.BetMoney)
                    .FirstOrDefault();
                if (setting == null)
                    continue;

                var rates = ParseRebateRates(setting.SettingString);
                decimal money = 0;
                var parts = new List<object>();
                foreach (var bucket in buckets)
                {
                    var rate = MatchRebateRate(rates, bucket.Category, bucket.Type);
                    if (rate == 0)
                        continue;
                    var part = Math.Round(bucket.Bet * rate / 100m, 2, MidpointRounding.AwayFromZero);
                    if (part == 0)
                        continue;
                    money += part;
                    parts.Add(new { bucket.Category, bucket.Type, Bet = bucket.Bet, Money = part, Rate = rate });
                }

                if (setting.MaxMoney > 0 && money > setting.MaxMoney)
                    money = setting.MaxMoney;
                if (money <= 0)
                    continue;
                if (setting.MinMoney > 0 && money < setting.MinMoney)
                    continue;

                var hit = existing.FirstOrDefault(l =>
                    l.UserID == user.ID
                    && l.Currency == grp.Key.Currency
                    && string.Equals(l.Type ?? string.Empty, typeKey, StringComparison.OrdinalIgnoreCase)
                    && l.StartAt.Date == start
                    && l.EndAt.Date == end);
                // 已进计划或已发放的不再改金额，避免重复入账
                if (hit != null && (hit.Status != LogStatusNone || hit.PlanID != 0))
                    continue;

                var detail = JsonSerializer.Serialize(parts);
                if (hit != null)
                {
                    hit.Money = money;
                    hit.Detail = detail;
                    hit.RebateID = setting.ID;
                    await _db.Updateable(hit)
                        .UpdateColumns(l => new { l.Money, l.Detail, l.RebateID })
                        .ExecuteCommandAsync(cancellationToken);
                    written.Add(hit.ID);
                    continue;
                }

                sort++;
                var log = new UserRebateLog
                {
                    SiteID = siteId,
                    PlanID = 0,
                    UserID = user.ID,
                    StartAt = start,
                    EndAt = end,
                    Money = money,
                    CreateAt = DateTime.Now,
                    CheckAt = DateTime.MinValue,
                    Detail = detail,
                    Type = typeKey,
                    Status = LogStatusNone,
                    FaildMsg = string.Empty,
                    RebateID = setting.ID,
                    Currency = grp.Key.Currency,
                    SortID = sort
                };
                var id = await _db.Insertable(log).ExecuteReturnIdentityAsync(cancellationToken);
                log.ID = id;
                existing.Add(log);
                written.Add(id);
            }
        }

        return written;
    }

    /// <summary>分组名单里有该会员分组才适用。空名单不适用。</summary>
    private static bool RebateGroupHit(string groups, int groupId)
    {
        // 空名单=不限分组，和等级名单空表示不限一致
        if (string.IsNullOrWhiteSpace(groups))
            return true;
        foreach (var part in groups.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (int.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) && id == groupId)
                return true;
        }

        return false;
    }

    /// <summary>等级名单为空表示不限；否则必须包含会员等级。</summary>
    private static bool RebateLevelHit(string levels, int level)
    {
        if (string.IsNullOrWhiteSpace(levels))
            return true;
        foreach (var part in levels.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (int.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) && id == level)
                return true;
        }

        return false;
    }

    /// <summary>AA、空、Member 覆盖全部分类；A 加数字只覆盖该分类。</summary>
    private static bool RebateSettingCovers(string settingType, int category)
    {
        var text = (settingType ?? string.Empty).Trim();
        if (text.Length == 0
            || text.Equals("AA", StringComparison.OrdinalIgnoreCase)
            || text.Equals(LogTypeMember, StringComparison.OrdinalIgnoreCase))
            return true;
        if (text.Length > 1
            && (text[0] == 'A' || text[0] == 'a')
            && int.TryParse(text.AsSpan(1), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            return parsed == category;
        return false;
    }

    /// <summary>设定原文里的比例。键可以是「分类.类型」、类型数字或 Rate。</summary>
    private static Dictionary<string, decimal> ParseRebateRates(string raw)
    {
        var map = SiteSettingQuery.Parse(raw ?? string.Empty);
        var rates = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in map)
        {
            if (decimal.TryParse(pair.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var rate) && rate != 0)
                rates[pair.Key] = rate;
        }

        return rates;
    }

    /// <summary>按「分类.类型」、类型数字、分类名前缀、Rate 的顺序取比例。</summary>
    private static decimal MatchRebateRate(Dictionary<string, decimal> rates, int category, int type)
    {
        if (rates.TryGetValue($"{category}.{type}", out var paired))
            return paired;
        if (rates.TryGetValue(type.ToString(CultureInfo.InvariantCulture), out var byType))
            return byType;
        // 设定原文是 Lottery.CB / Slot.CQ9；日汇总 Type 常是游戏类型数字，对不上后缀时用同分类任意一档
        if (CategoryNameById.TryGetValue(category, out var catName))
        {
            var prefix = catName + ".";
            foreach (var kv in rates)
            {
                if (kv.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return kv.Value;
            }
        }

        if (rates.TryGetValue("Rate", out var rate))
            return rate;
        return 0;
    }

    /// <summary>日汇总 Category 到设定键前缀（对齐旧 GameCategory 名）。</summary>
    private static readonly Dictionary<int, string> CategoryNameById = new()
    {
        [0] = "Lottery",
        [1] = "ESport",
        [2] = "Slot",
        [3] = "Live",
        [4] = "Chess",
        [5] = "Sport",
        [6] = "Fish",
        [7] = "Animal",
        [8] = "MiniGame",
        [9] = "Battle"
    };

    /// <summary>
    /// 站点开启自助反水时阻断手工返水。对齐旧 BlockWhenAutoReturnV2Enabled。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    private async Task EnsureManualRebateAllowedAsync(CancellationToken cancellationToken)
    {
        var siteId = _site.RequireSiteId();
        var sites = await _db.Queryable<SimpX.Net.Data.Entity.Sites.Site>()
            .Where(s => s.ID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var map = SiteSettingQuery.Parse(sites.FirstOrDefault()?.SettingString ?? string.Empty);
        if (SiteSettingQuery.GetBool(map, "IsAutoReturn"))
            throw Oops.Oh("#当前站点已启用自助反水，禁止手动反水#");
    }

    /// <summary>逐笔发放并回写计划汇总。</summary>
    private async Task<int> PayLogsAsync(
        int siteId,
        UserRebatePlan plan,
        List<UserRebateLog> logs,
        CancellationToken cancellationToken)
    {
        var ok = 0;
        foreach (var log in logs)
        {
            try
            {
                await _money.AddMoneyLogAsync(
                    siteId,
                    log.UserID,
                    log.Money,
                    MoneyTypeRebate,
                    $"RB{log.ID}",
                    $"返水发放 Plan={plan.ID}",
                    log.Currency,
                    cancellationToken,
                    flowRate: plan.FlowRate);
                log.Status = LogStatusSuccess;
                log.CheckAt = DateTime.Now;
                await _db.Updateable(log)
                    .UpdateColumns(l => new { l.Status, l.CheckAt })
                    .ExecuteCommandAsync(cancellationToken);
                ok++;
            }
            catch
            {
                log.Status = 1;
                log.FaildMsg = "发放失败";
                log.CheckAt = DateTime.Now;
                await _db.Updateable(log)
                    .UpdateColumns(l => new { l.Status, l.FaildMsg, l.CheckAt })
                    .ExecuteCommandAsync(cancellationToken);
            }
        }

        if (ok > 0 || logs.Any(l => l.Status == 1))
        {
            plan.Success += ok;
            plan.Paid += logs.Where(l => l.Status == LogStatusSuccess).Sum(l => l.Money);
            plan.Faild += logs.Count(l => l.Status == 1);
            await _db.Updateable(plan)
                .UpdateColumns(p => new { p.Success, p.Paid, p.Faild })
                .ExecuteCommandAsync(cancellationToken);
        }

        return ok;
    }

    /// <summary>重算时用的会员分组和等级。</summary>
    private sealed class RebateUserSnap
    {
        /// <summary>会员编号。</summary>
        public int ID { get; set; }

        /// <summary>分组。</summary>
        public int GroupID { get; set; }

        /// <summary>等级。</summary>
        public byte Level { get; set; }
    }

    // --- merged from RebatePayoutService.WaveGaps.cs ---


    /// <summary>
    /// 对齐旧 BlockWhenAutoReturnV2Enabled：站点开了自助返水则阻断手工。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> BlockWhenAutoReturnV2EnabledAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return BlockWhenAutoReturnV2EnabledCoreAsync(input, cancellationToken);
    }

    /// <summary>
    /// 对齐旧 exportlogdetail。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> ExportlogdetailAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return ExportlogdetailCoreAsync(input, cancellationToken);
    }

    /// <summary>
    /// 对齐旧 grouplist。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> GrouplistAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return GrouplistCoreAsync(input, cancellationToken);
    }

    /// <summary>
    /// 对齐旧 logdetail。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> LogdetailAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return LogdetailCoreAsync(input, cancellationToken);
    }

    /// <summary>
    /// 对齐旧 loglist。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> LoglistAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return LoglistCoreAsync(input, cancellationToken);
    }

    /// <summary>
    /// 对齐旧 updateenabled（本仓无独立开关表时明确提示）。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> UpdateenabledAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return UpdateenabledCoreAsync(input, cancellationToken);
    }

    /// <summary>读站点 IsAutoReturn；开启则 blocked=true。</summary>
    private async Task<object?> BlockWhenAutoReturnV2EnabledCoreAsync(
        Dictionary<string, object?> input,
        CancellationToken cancellationToken)
    {
        _ = input;
        var siteId = _site.RequireSiteId();
        var sites = await _db.Queryable<Site>()
            .Where(s => s.ID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var setting = sites.FirstOrDefault()?.SettingString;
        var map = SiteSettingQuery.Parse(setting ?? string.Empty);
        var isAuto = SiteSettingQuery.GetBool(map, "IsAutoReturn");
        if (isAuto)
            throw Oops.Oh("#当前站点已启用自助反水，禁止手动反水#");
        return new { blocked = false };
    }

    /// <summary>导出详情：复用 logdetail 列表结构。</summary>
    private Task<object?> ExportlogdetailCoreAsync(
        Dictionary<string, object?> input,
        CancellationToken cancellationToken) =>
        LogdetailCoreAsync(input, cancellationToken);

    /// <summary>会员分组下拉。</summary>
    private async Task<object?> GrouplistCoreAsync(
        Dictionary<string, object?> input,
        CancellationToken cancellationToken)
    {
        _ = input;
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<UserGroup>()
            .Where(g => g.SiteID == siteId)
            .OrderBy(g => g.Sort, OrderByType.Desc)
            .OrderBy(g => g.ID, OrderByType.Desc)
            .ToListAsync(cancellationToken);
        return rows.Select(t => new
        {
            t.ID,
            t.Name,
            t.CreateAt,
            t.Member,
            t.Sort,
            t.IsUpdate,
            t.IsDefault
        }).ToList();
    }

    /// <summary>计划下返水明细；无 PlanID 时按日志编号。</summary>
    private async Task<object?> LogdetailCoreAsync(
        Dictionary<string, object?> input,
        CancellationToken cancellationToken)
    {
        var siteId = _site.RequireSiteId();
        var planId = ReadInt(input, "PlanID", "planId", "ID", "Id");
        var logId = ReadInt(input, "LogID", "logId");
        var query = _db.Queryable<UserRebateLog>().Where(l => l.SiteID == siteId);
        if (planId > 0)
            query = query.Where(l => l.PlanID == planId);
        if (logId > 0)
            query = query.Where(l => l.ID == logId);
        var rows = await query
            .OrderBy(l => l.ID, OrderByType.Desc)
            .Take(500)
            .ToListAsync(cancellationToken);
        var nameMap = await LoadNamesAsync(siteId, rows.Select(r => r.UserID).Distinct().ToList(), cancellationToken);
        return rows.Select(t => new
        {
            t.ID,
            t.UserID,
            UserName = nameMap.TryGetValue(t.UserID, out var n) ? n : string.Empty,
            t.PlanID,
            t.StartAt,
            t.EndAt,
            Money = t.Money.ToString("F2", CultureInfo.InvariantCulture),
            t.Currency,
            t.Type,
            t.Status,
            t.Detail,
            t.SortID,
            t.FaildMsg
        }).ToList();
    }

    /// <summary>
    /// 返水日志列表。对齐旧 loglist：查 usr_RebateLog，不把计划列表再返回一遍。
    /// 以前这里直接返回计划，日志页没有行，看起来像没数据。
    /// </summary>
    private async Task<object?> LoglistCoreAsync(
        Dictionary<string, object?> input,
        CancellationToken cancellationToken)
    {
        var siteId = _site.RequireSiteId();
        var query = _db.Queryable<UserRebateLog>().Where(l => l.SiteID == siteId);
        var planId = ReadInt(input, "PlanID", "planId");
        if (planId > 0)
            query = query.Where(l => l.PlanID == planId);
        var logId = ReadInt(input, "LogID", "ID", "Id");
        if (logId > 0)
            query = query.Where(l => l.ID == logId);
        if (TryReadInt(input, out var status, "Status", "status"))
            query = query.Where(l => l.Status == status);
        if (TryReadInt(input, out var currency, "Currency", "currency"))
            query = query.Where(l => l.Currency == currency);

        var userName = ReadString(input, "User", "UserName");
        if (!string.IsNullOrWhiteSpace(userName))
        {
            var name = userName.Trim();
            List<int> named;
            if (int.TryParse(name, out var uidExact))
                named = [uidExact];
            else
                named = await _db.Queryable<User>()
                    .Where(u => u.SiteID == siteId && u.UserName.Contains(name))
                    .Select(u => u.ID)
                    .Take(500)
                    .ToListAsync(cancellationToken);
            if (named.Count == 0)
                return Array.Empty<object>();
            query = query.Where(l => named.Contains(l.UserID));
        }

        var start = ReadDate(input, "StartAt", "Date");
        if (start.HasValue)
            query = query.Where(l => l.CreateAt >= start.Value);
        var end = ReadDate(input, "EndAt", "EndDate");
        if (end.HasValue)
        {
            var endExclusive = end.Value.TimeOfDay == TimeSpan.Zero ? end.Value.Date.AddDays(1) : end.Value;
            query = query.Where(l => l.CreateAt < endExclusive);
        }

        var rows = await query
            .OrderBy(l => l.CreateAt, OrderByType.Desc)
            .Take(500)
            .ToListAsync(cancellationToken);
        var nameMap = await LoadNamesAsync(siteId, rows.Select(r => r.UserID).Distinct().ToList(), cancellationToken);
        return rows.Select(t => new
        {
            t.ID,
            t.UserID,
            UserName = nameMap.TryGetValue(t.UserID, out var n) ? n : string.Empty,
            t.PlanID,
            t.StartAt,
            t.EndAt,
            t.CreateAt,
            Money = t.Money.ToString("F2", CultureInfo.InvariantCulture),
            t.Currency,
            Type = FormatRebateType(t.Type),
            RawType = t.Type ?? string.Empty,
            t.Status,
            FaildMsg = string.Equals(t.Type, "V2", StringComparison.OrdinalIgnoreCase) ? string.Empty : (t.FaildMsg ?? string.Empty),
            ClaimNo = string.Equals(t.Type, "V2", StringComparison.OrdinalIgnoreCase) ? (t.FaildMsg ?? string.Empty) : string.Empty
        }).ToList();
    }

    /// <summary>日志类型给后台看的短文案。库内仍是 AA / A+分类 / V2。</summary>
    private static string FormatRebateType(string type)
    {
        if (string.IsNullOrWhiteSpace(type))
            return string.Empty;
        if (string.Equals(type, "V2", StringComparison.OrdinalIgnoreCase))
            return "自助";
        if (string.Equals(type, "AA", StringComparison.OrdinalIgnoreCase))
            return "综合";
        if (type.Length > 1
            && (type[0] == 'A' || type[0] == 'a')
            && int.TryParse(type.AsSpan(1), NumberStyles.Integer, CultureInfo.InvariantCulture, out var cate))
            return $"分类{cate}";
        return type;
    }

    /// <summary>切换 site_RebateSetting.Enabled。对齐旧按编号取反。</summary>
    private async Task<object?> UpdateenabledCoreAsync(
        Dictionary<string, object?> input,
        CancellationToken cancellationToken)
    {
        var id = ReadInt(input, "ID", "Id");
        if (id <= 0)
            throw Oops.Oh("#改信息不存在或已删除#");
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<RebateSetting>()
            .Where(s => s.SiteID == siteId && s.ID == id)
            .Take(1)
            .ToListAsync(cancellationToken);
        var model = rows.FirstOrDefault() ?? throw Oops.Oh("#改信息不存在或已删除#");
        model.Enabled = !model.Enabled;
        await _db.Updateable(model)
            .UpdateColumns(s => new { s.Enabled })
            .ExecuteCommandAsync(cancellationToken);
        return new { ID = model.ID, Enabled = model.Enabled };
    }

    /// <summary>取用户名。</summary>
    private async Task<Dictionary<int, string>> LoadNamesAsync(
        int siteId,
        List<int> ids,
        CancellationToken cancellationToken)
    {
        var map = new Dictionary<int, string>();
        if (ids.Count == 0)
            return map;
        var users = await _db.Queryable<User>()
            .Where(u => u.SiteID == siteId && ids.Contains(u.ID))
            .Select(u => new { u.ID, u.UserName })
            .ToListAsync(cancellationToken);
        foreach (var u in users)
            map[u.ID] = u.UserName ?? string.Empty;
        return map;
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

    /// <summary>表单里明确带了整数才算筛选项。缺省不当 0。</summary>
    private static bool TryReadInt(Dictionary<string, object?> input, out int value, params string[] keys)
    {
        foreach (var k in keys)
        {
            if (!input.TryGetValue(k, out var v) || v is null) continue;
            if (v is int i)
            {
                value = i;
                return true;
            }

            if (int.TryParse(v.ToString(), out var n))
            {
                value = n;
                return true;
            }
        }

        value = 0;
        return false;
    }

    /// <summary>按键读字符串；都空则返回空串。</summary>
    private static string ReadString(Dictionary<string, object?> input, params string[] keys)
    {
        foreach (var k in keys)
        {
            if (!input.TryGetValue(k, out var v) || v is null) continue;
            var s = v.ToString();
            if (!string.IsNullOrWhiteSpace(s))
                return s.Trim();
        }

        return string.Empty;
    }

    /// <summary>按键读时间；解析不了当没传。</summary>
    private static DateTime? ReadDate(Dictionary<string, object?> input, params string[] keys)
    {
        foreach (var k in keys)
        {
            if (!input.TryGetValue(k, out var v) || v is null) continue;
            if (v is DateTime dt)
                return dt;
            if (DateTime.TryParse(v.ToString(), out var parsed))
                return parsed;
        }

        return null;
    }
}
