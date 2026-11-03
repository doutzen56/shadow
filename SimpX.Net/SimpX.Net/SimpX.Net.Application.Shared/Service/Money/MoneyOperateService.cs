using System.Text.Json;
using SimpX.Net.Data.Entity.Games;
using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Users;

using System.Globalization;
using System.Text.RegularExpressions;
using SimpX.Net.Data.Entity.Reports;
namespace SimpX.Net.Application.Shared;

/// <summary>
/// 租户后台人工加款/扣款。对齐旧 MoneyReviewAgent.SaveMoneyReview：
/// 站点开启审核则写 usr_MoneyReview（扣款另锁钱）；否则直接走 <see cref="IMoneyLedger.AddMoneyLogAsync"/>。
/// </summary>
public sealed partial class MoneyOperateService : ITransient
{
    /// <summary>业务动作：加款。</summary>
    public const int ActionIn = 1;

    /// <summary>业务动作：扣款。</summary>
    public const int ActionOut = 2;

    /// <summary>审核状态：待审。</summary>
    public const int StatusNone = 0;

    /// <summary>审核状态：通过（免审直接入账时写入）。</summary>
    public const int StatusSuccess = 2;

    /// <summary>禁止登录锁定位（对齐旧 LockStatus.Login）。</summary>
    public const int LockLogin = 1;

    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;
    private readonly IMoneyLedger _money;
    private readonly IHttpContextAccessor _http;

    /// <summary>
    /// 注入库、定站、账本与请求上下文。
    /// </summary>
    /// <param name="db">库</param>
    /// <param name="site">当前站</param>
    /// <param name="money">账本</param>
    /// <param name="http">HTTP（取管理员 Id，可空）</param>
    public MoneyOperateService(
        IBizDbAccessor biz,
        ICurrentSiteAccessor site,
        IMoneyLedger money,
        IHttpContextAccessor http)
    {
        _db = biz.Db;
        _site = site;
        _money = money;
        _http = http;
    }

    /// <summary>
    /// 会员加款。对齐旧 addmoney（批量用户名逗号分隔）。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>逐用户结果</returns>
    public Task<MoneyOperateSubmitResult> GrantAsync(
        MoneyOperateSubmitInput input,
        CancellationToken cancellationToken = default) =>
        SubmitAsync(input, ActionIn, cancellationToken);

    /// <summary>
    /// 会员扣款。对齐旧 redeductmoney。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>逐用户结果</returns>
    public Task<MoneyOperateSubmitResult> DeductAsync(
        MoneyOperateSubmitInput input,
        CancellationToken cancellationToken = default) =>
        SubmitAsync(input, ActionOut, cancellationToken);

    /// <summary>
    /// 制表符批量加款。对齐旧 addmoneybatch（按用户编号逐行）。
    /// </summary>
    /// <param name="input">Batch 与 SourceID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>逐用户结果</returns>
    public async Task<MoneyOperateSubmitResult> GrantBatchTabAsync(
        MoneyOperateBatchTabInput input,
        CancellationToken cancellationToken = default)
    {
        input ??= new MoneyOperateBatchTabInput();
        var batch = input.Batch ?? string.Empty;
        var lines = batch.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0)
            throw Oops.Oh("#请输入用户名#");

        var siteId = _site.RequireSiteId();
        var items = new List<MoneyOperateUserResult>();
        var i = 0;
        foreach (var line in lines)
        {
            i++;
            var cols = line.Split('\t');
            if (cols.Length < 8 || !int.TryParse(cols[0].Trim(), out var userId) || userId <= 0)
            {
                items.Add(new MoneyOperateUserResult
                {
                    UserName = $"行{i}",
                    Success = false,
                    Message = "用户编号错误"
                });
                continue;
            }

            var users = await _db.Queryable<User>()
                .Where(u => u.SiteID == siteId && u.ID == userId)
                .Take(1)
                .ToListAsync(cancellationToken);
            var user = users.FirstOrDefault();
            if (user == null)
            {
                items.Add(new MoneyOperateUserResult
                {
                    UserName = userId.ToString(),
                    Success = false,
                    Message = "用户不存在"
                });
                continue;
            }

            if (!int.TryParse(cols[1].Trim(), out var type))
                type = 0;
            if (!decimal.TryParse(cols[3].Trim(), out var money))
                money = 0;
            if (!decimal.TryParse(cols[5].Trim(), out var flow))
                flow = 1;
            if (!decimal.TryParse(cols[6].Trim(), out var minFlow))
                minFlow = 0;
            if (!int.TryParse(cols[7].Trim(), out var currency))
                currency = 0;

            var source = string.IsNullOrWhiteSpace(cols[2])
                ? (string.IsNullOrWhiteSpace(input.SourceID) ? $"B{DateTime.Now:HHmmss}{i:000}" : input.SourceID.Trim())
                : cols[2].Trim();
            var one = new MoneyOperateSubmitInput
            {
                User = user.UserName,
                Type = type,
                Currency = currency,
                Money = money,
                SourceID = TruncateSource(source),
                Description = cols.Length > 4 ? cols[4] : string.Empty,
                Flow = flow,
                MinFlow = minFlow
            };
            try
            {
                var result = await GrantAsync(one, cancellationToken);
                if (result?.Items != null)
                    items.AddRange(result.Items);
            }
            catch (Exception ex)
            {
                items.Add(new MoneyOperateUserResult
                {
                    UserName = user.UserName,
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        return new MoneyOperateSubmitResult
        {
            Items = items,
            Summary = items.Count == 0
                ? "#所有添加成功#"
                : string.Join(",<br>", items.Select(x => x.Message))
        };
    }

    /// <summary>
    /// 加款页游戏过滤下拉。对齐旧 filtergame（开放厂商列表）。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>开放厂商与初始勾选</returns>
    public async Task<MoneyOperateFilterGameDto> FilterGameAsync(CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<GameSetting>()
            .Where(g => g.SiteID == siteId && g.IsOpen && !g.IsDisable)
            .OrderBy(g => g.Name, OrderByType.Asc)
            .ToListAsync(cancellationToken);

        // 旧版 openPlatforms 带 children（二级分类）；openTransfer 只有厂商一层。
        var platforms = new List<object>();
        var transfers = new List<object>();
        foreach (var g in rows)
        {
            var value = string.IsNullOrWhiteSpace(g.Name) ? g.Game.ToString() : g.Name.Trim();
            var name = value;
            var children = await LoadGameChildrenAsync(g.Game, value, cancellationToken);
            platforms.Add(new { value, name, children });
            transfers.Add(new { value, name });
        }

        return new MoneyOperateFilterGameDto
        {
            OpenPlatforms = JsonSerializer.Serialize(platforms),
            OpenTransfer = JsonSerializer.Serialize(transfers),
            InitPlatform = "[\"\"]"
        };
    }

    /// <summary>
    /// 厂商二级分类（对齐旧 GetGameSubCategories；无分类表数据时返回空数组）。
    /// </summary>
    private async Task<List<object>> LoadGameChildrenAsync(
        int gameType,
        string gameCode,
        CancellationToken cancellationToken)
    {
        var rows = await _db.Queryable<SiteGameSubAssort>()
            .Where(s => s.Platform == gameType || s.GameID == gameType)
            .OrderBy(s => s.Sort, OrderByType.Desc)
            .Take(50)
            .ToListAsync(cancellationToken);
        if (rows.Count == 0)
            return [];

        return rows.Select(s =>
        {
            var code = string.IsNullOrWhiteSpace(s.Code) ? s.Name : s.Code;
            var value = string.IsNullOrWhiteSpace(code)
                ? $"{gameCode}-{s.Category}"
                : (code.Contains('-', StringComparison.Ordinal) ? code : $"{gameCode}-{code}");
            var name = string.IsNullOrWhiteSpace(s.Name) ? value : s.Name;
            return (object)new { value, name };
        }).ToList();
    }

    /// <summary>
    /// 回显活动已选排除游戏。对齐旧 initplatform。
    /// </summary>
    /// <param name="id">活动编号</param>
    /// <param name="type">活动类型（新建壳用）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>已选 JSON 数组</returns>
    public Task<MoneyOperateInitPlatformDto> InitPlatformAsync(
        int id,
        int type,
        CancellationToken cancellationToken = default) =>
        InitFilterFieldAsync(id, type, useFilterGame: true, cancellationToken);

    /// <summary>
    /// 回显活动已选排除转入。对齐旧 inittransfer。
    /// </summary>
    /// <param name="id">活动编号</param>
    /// <param name="type">活动类型（新建壳用）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>已选 JSON 数组</returns>
    public Task<MoneyOperateInitPlatformDto> InitTransferAsync(
        int id,
        int type,
        CancellationToken cancellationToken = default) =>
        InitFilterFieldAsync(id, type, useFilterGame: false, cancellationToken);

    /// <summary>
    /// 手工活动下拉（有排除游戏）。对齐旧 show0。
    /// </summary>
    /// <param name="type">活动类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>选项</returns>
    public Task<List<MoneyOperateEventOptionDto>> Show0Async(
        int type,
        CancellationToken cancellationToken = default) =>
        ListOpenEventsAsync(type, requireFilterGame: true, cancellationToken);

    /// <summary>
    /// 手工活动下拉（有排除转入）。对齐旧 showtransfer0。
    /// </summary>
    /// <param name="type">活动类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>选项</returns>
    public Task<List<MoneyOperateEventOptionDto>> ShowTransfer0Async(
        int type,
        CancellationToken cancellationToken = default) =>
        ListOpenEventsAsync(type, requireFilterGame: false, cancellationToken);

    /// <summary>
    /// 按动作提交加/扣款。
    /// </summary>
    private async Task<MoneyOperateSubmitResult> SubmitAsync(
        MoneyOperateSubmitInput input,
        int action,
        CancellationToken cancellationToken)
    {
        input ??= new MoneyOperateSubmitInput();
        var siteId = _site.RequireSiteId();
        var rows = ParseBatchRows(input);
        if (rows.Count == 0)
            throw Oops.Oh("#请输入用户名#");
        if (rows.Count > 1000)
            throw Oops.Oh("#一次最多1000个#");
        if (string.IsNullOrWhiteSpace(input.SourceID))
            throw Oops.Oh("#流水号错误#");
        // 类型必须是旧版已登记的 MoneyType；未登记直接拦，避免乱类型入账
        if (!MoneyTypeNameResolver.IsKnownType(input.Type))
            throw Oops.Oh("#类型错误#");
        // 仅「充值=1」禁止走会员加款；其它合法类型（如手工加款 250）正常发放，不要误逼代客充值
        if (action == ActionIn && input.Type == MoneyTypeNameResolver.MoneyTypeRecharge)
            throw Oops.Oh("#请走[代客充值]流程#");

        var needCheck = await ReadMoneyChangedNeedCheckAsync(siteId, cancellationToken);
        var desc = $"{input.Description ?? string.Empty} | {input.DescriptionA ?? string.Empty}".Trim();
        var creatorId = TryGetAdminId();
        var items = new List<MoneyOperateUserResult>();
        var baseSource = input.SourceID.Trim();

        foreach (var row in rows)
        {
            var name = row.UserName;
            try
            {
                if (row.Money <= 0 && input.Money <= 0)
                    throw Oops.Oh("#资金错误#");
                var one = CloneInput(input);
                one.User = name;
                one.Money = row.Money > 0 ? row.Money : input.Money;
                // 批量时流水号按用户追加尾缀，避免撞唯一键
                one.SourceID = rows.Count == 1
                    ? baseSource
                    : TruncateSource($"{baseSource}_{name}");
                await ApplyOneAsync(
                    siteId,
                    name,
                    action,
                    one,
                    desc,
                    needCheck,
                    creatorId,
                    cancellationToken);
                items.Add(new MoneyOperateUserResult
                {
                    UserName = name,
                    Success = true,
                    Message = $"#成功#:{name}"
                });
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                if (msg.StartsWith("Oops:", StringComparison.OrdinalIgnoreCase))
                    msg = msg["Oops:".Length..].Trim();
                items.Add(new MoneyOperateUserResult
                {
                    UserName = name,
                    Success = false,
                    Message = $"#失败#: {name},#原因#: {msg}"
                });
            }
        }

        return new MoneyOperateSubmitResult
        {
            Items = items,
            Summary = items.Count == 0
                ? "#所有添加成功#"
                : string.Join(",<br>", items.Select(i => i.Message))
        };
    }

    /// <summary>
    /// 处理单个用户。
    /// </summary>
    private async Task ApplyOneAsync(
        int siteId,
        string userName,
        int action,
        MoneyOperateSubmitInput input,
        string description,
        bool needCheck,
        int creatorId,
        CancellationToken cancellationToken)
    {
        var users = await _db.Queryable<User>()
            .Where(u => u.SiteID == siteId && u.UserName == userName)
            .Take(1)
            .ToListAsync(cancellationToken);
        var user = users.FirstOrDefault() ?? throw Oops.Oh("#用户错误#");
        if ((user.Lock & LockLogin) == LockLogin)
            throw Oops.Oh("禁止登录");

        var signedMoney = action == ActionOut ? -Math.Abs(input.Money) : Math.Abs(input.Money);
        var sourceId = input.SourceID.Trim();
        if (sourceId.Length > 20)
            sourceId = sourceId[..20];

        var tableId = user.ID % 10;
        var existsLog = await _db.Queryable<MoneyLog>()
            .Where(l => l.TableID == tableId && l.UserID == user.ID && l.Type == input.Type && l.SourceID == sourceId && l.SubType == 0)
            .AnyAsync(cancellationToken);
        if (existsLog)
            throw Oops.Oh("#流水号重复#");

        if (needCheck)
        {
            var review = new MoneyReview
            {
                SiteID = siteId,
                UserID = user.ID,
                Money = signedMoney,
                Type = input.Type,
                Flow = input.Flow,
                MinFlow = input.MinFlow,
                CreateAt = DateTime.Now,
                CreatorID = creatorId,
                SourceID = sourceId,
                Action = action,
                ReviewerID = 0,
                ReviewAt = DateTime.MinValue,
                Status = StatusNone,
                Currency = input.Currency,
                Description = description,
                EventID = 0,
                EventIDNoTrans = 0,
                PayID = input.PayID
            };
            // 开审核：写审核单；扣款还要按审核单号锁可用余额（对齐旧 SaveMoneyReview + LockMoney）。
            await _db.Ado.BeginTranAsync();
            try
            {
                var newId = await _db.Insertable(review).ExecuteReturnIdentityAsync(cancellationToken);
                review.ID = Convert.ToInt32(newId);
                if (action == ActionOut)
                {
                    await _money.LockReviewAsync(
                        siteId,
                        user.ID,
                        Math.Abs(input.Money),
                        review.ID,
                        description,
                        input.Currency,
                        cancellationToken);
                }

                await _db.Ado.CommitTranAsync();
            }
            catch
            {
                await _db.Ado.RollbackTranAsync();
                throw;
            }

            return;
        }

        await _money.AddMoneyLogAsync(
            siteId,
            user.ID,
            signedMoney,
            input.Type,
            sourceId,
            description,
            input.Currency,
            cancellationToken,
            flowRate: action == ActionIn ? input.Flow : 0,
            minFlow: action == ActionIn ? input.MinFlow : 0);
    }

    /// <summary>
    /// 读站点 Setting 里是否「加扣款需审核」。读不到当 false（免审直入账）。
    /// 真库 Setting 是 query 串（如 MoneyChangedNeedCheck=True）；也兼容 JSON。
    /// </summary>
    private async Task<bool> ReadMoneyChangedNeedCheckAsync(int siteId, CancellationToken cancellationToken)
    {
        var sites = await _db.Queryable<Site>()
            .Where(s => s.ID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var setting = sites.FirstOrDefault()?.SettingString;
        if (string.IsNullOrWhiteSpace(setting))
            return false;

        var trimmed = setting.TrimStart();
        if (trimmed.StartsWith('{'))
        {
            try
            {
                using var doc = JsonDocument.Parse(setting);
                if (doc.RootElement.TryGetProperty("MoneyChangedNeedCheck", out var p))
                {
                    if (p.ValueKind == JsonValueKind.True) return true;
                    if (p.ValueKind == JsonValueKind.False) return false;
                    if (p.ValueKind == JsonValueKind.Number) return p.GetInt32() != 0;
                    if (p.ValueKind == JsonValueKind.String && bool.TryParse(p.GetString(), out var b))
                        return b;
                }
            }
            catch (JsonException)
            {
                // JSON 坏了再走 query 串
            }
        }

        var map = SiteSettingQuery.Parse(setting);
        return SiteSettingQuery.GetBool(map, "MoneyChangedNeedCheck");
    }

    /// <summary>
    /// 尝试取当前后台用户 Id；没有则 0。
    /// </summary>
    private int TryGetAdminId()
    {
        var user = _http.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
            return 0;
        var id = user.FindFirst("UserId")?.Value
            ?? user.FindFirst("userid")?.Value
            ?? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(id, out var n) ? n : 0;
    }

    /// <summary>解析 User 逗号列表或 CsvText 行。</summary>
    private static List<(string UserName, decimal Money)> ParseBatchRows(MoneyOperateSubmitInput input)
    {
        var list = new List<(string, decimal)>();
        if (!string.IsNullOrWhiteSpace(input.CsvText))
        {
            foreach (var line in input.CsvText.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
            {
                var t = line.Trim();
                if (t.Length == 0 || t.StartsWith('#'))
                    continue;
                var parts = t.Split([',', '\t', ';'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0)
                    continue;
                var money = 0m;
                if (parts.Length > 1)
                    decimal.TryParse(parts[1], out money);
                list.Add((parts[0], money));
            }

            return list;
        }

        if (string.IsNullOrWhiteSpace(input.User))
            return list;
        foreach (var name in input.User.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            list.Add((name, 0m));
        return list;
    }

    /// <summary>浅拷贝加扣款入参。</summary>
    private static MoneyOperateSubmitInput CloneInput(MoneyOperateSubmitInput src) => new()
    {
        User = src.User,
        Type = src.Type,
        Currency = src.Currency,
        Money = src.Money,
        SourceID = src.SourceID,
        Description = src.Description,
        DescriptionA = src.DescriptionA,
        Flow = src.Flow,
        MinFlow = src.MinFlow,
        PayID = src.PayID,
        CsvText = null
    };

    /// <summary>流水号截到 20。</summary>
    private static string TruncateSource(string s) =>
        string.IsNullOrEmpty(s) || s.Length <= 20 ? (s ?? string.Empty) : s[..20];

    /// <summary>
    /// 读活动 FilterGame 或 FilterPlatform，拼成 JSON 数组回显。
    /// </summary>
    private async Task<MoneyOperateInitPlatformDto> InitFilterFieldAsync(
        int id,
        int type,
        bool useFilterGame,
        CancellationToken cancellationToken)
    {
        var siteId = _site.RequireSiteId();
        var raw = string.Empty;
        if (id > 0)
        {
            var rows = await _db.Queryable<SiteEvent>()
                .Where(e => e.SiteID == siteId && e.ID == id)
                .Take(1)
                .ToListAsync(cancellationToken);
            var ev = rows.FirstOrDefault();
            if (ev != null)
                raw = useFilterGame ? (ev.FilterGame ?? string.Empty) : (ev.FilterPlatform ?? string.Empty);
        }

        var parts = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(p => p.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var json = JsonSerializer.Serialize(parts);
        return new MoneyOperateInitPlatformDto { InitPlatform = json };
    }

    /// <summary>
    /// 当前站进行中、已开、且带过滤字段的活动下拉。
    /// </summary>
    private async Task<List<MoneyOperateEventOptionDto>> ListOpenEventsAsync(
        int type,
        bool requireFilterGame,
        CancellationToken cancellationToken)
    {
        var siteId = _site.RequireSiteId();
        var now = DateTime.Now;
        var query = _db.Queryable<SiteEvent>()
            .Where(e => e.SiteID == siteId
                        && e.Type == type
                        && e.IsOpen
                        && e.StartAt <= now
                        && e.ExpireAt >= now);
        query = requireFilterGame
            ? query.Where(e => e.FilterGame != null && e.FilterGame != "")
            : query.Where(e => e.FilterPlatform != null && e.FilterPlatform != "");

        var rows = await query
            .OrderBy(e => e.Sort, OrderByType.Desc)
            .ToListAsync(cancellationToken);
        return rows.Select(e => new MoneyOperateEventOptionDto
        {
            ID = e.ID,
            Title = e.Title ?? string.Empty
        }).ToList();
    }

    // --- merged from MoneyOperateService.WaveGaps.cs ---

    /// <summary>旧 EventType.Rescue。</summary>
    public const int EventTypeRescue = 5;

    /// <summary>
    /// 对齐旧 loglist（救援金发放记录）。
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
    /// 对齐旧 post（救援金发放入 EventLog）。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> PostAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return PostCoreAsync(input, cancellationToken);
    }

    /// <summary>
    /// 对齐旧 query（按日亏损预览；无活动设定时金额按简化比例）。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> QueryAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return QueryCoreAsync(input, cancellationToken);
    }

    /// <summary>救援金发放记录：读 usr_EventLog（Type=Rescue）。有用户、活动、状态、日期时再收窄。</summary>
    private async Task<object?> LoglistCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var siteId = _site.RequireSiteId();
        var query = _db.Queryable<EventLog>()
            .Where(l => l.SiteID == siteId && l.Type == EventTypeRescue);
        var eventId = ReadInt(input, "EventID", "eventId");
        if (eventId > 0)
            query = query.Where(l => l.EventID == eventId);
        var userId = ReadInt(input, "UserID", "userId");
        var userName = ReadString(input, "User", "UserName");
        if (userId > 0)
            query = query.Where(l => l.UserID == userId);
        else if (!string.IsNullOrWhiteSpace(userName))
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
        if (TryReadInt(input, out var status, "Status", "status"))
            query = query.Where(l => l.Status == status);
        else
        {
            var statusText = ReadString(input, "Status", "status");
            if (!string.IsNullOrWhiteSpace(statusText)
                && OldApiMoneyDisplay.TryParseCheckStatus(statusText, out var statusEnum))
                query = query.Where(l => l.Status == statusEnum);
        }
        if (TryReadInt(input, out var currency, "Currency", "currency") && currency > 0)
            query = query.Where(l => l.Currency == currency);
        var day = ReadDate(input, "Date", "date");
        if (day.HasValue)
        {
            var start = day.Value.Date;
            var end = start.AddDays(1);
            query = query.Where(l => l.CreateAt >= start && l.CreateAt < end);
        }

        var rows = await query
            .OrderBy(l => l.CreateAt, OrderByType.Desc)
            .Take(500)
            .ToListAsync(cancellationToken);
        var nameMap = await LoadUserNamesAsync(siteId, rows.Select(r => r.UserID).Distinct().ToList(), cancellationToken);
        return rows.Select(l => new
        {
            l.ID,
            l.UserID,
            UserName = nameMap.TryGetValue(l.UserID, out var n) ? n : string.Empty,
            l.CreateAt,
            l.EventID,
            l.ExpectedAt,
            l.ErrorMsg,
            l.Reward,
            l.Flow,
            Status = OldApiMoneyDisplay.FormatCheckStatus(l.Status),
            Currency = OldApiMoneyDisplay.FormatCurrency(l.Currency)
        }).ToList();
    }

    /// <summary>
    /// 发放救援金：表单里数字键对应 JSON 行，写入 EventLog（待审，后续活动审核链路入账）。
    /// </summary>
    private async Task<object?> PostCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var siteId = _site.RequireSiteId();
        var models = new List<RescueGrantRow>();
        foreach (var kv in input)
        {
            if (!Regex.IsMatch(kv.Key ?? string.Empty, @"^\d+$") || kv.Value is null)
                continue;
            var json = kv.Value switch
            {
                string s => s,
                JsonElement je => je.GetRawText(),
                _ => kv.Value.ToString() ?? string.Empty
            };
            if (string.IsNullOrWhiteSpace(json))
                continue;
            try
            {
                var row = JsonSerializer.Deserialize<RescueGrantRow>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                if (row is { UserID: > 0 })
                    models.Add(row);
            }
            catch (JsonException)
            {
                // 跳过坏 JSON
            }
        }

        if (models.Count == 0)
            throw Oops.Oh("#没有要发放的记录#");

        var userIds = models.Select(m => m.UserID).Distinct().ToList();
        var knownUsers = await _db.Queryable<User>()
            .Where(u => u.SiteID == siteId && userIds.Contains(u.ID))
            .Select(u => u.ID)
            .ToListAsync(cancellationToken);
        var known = knownUsers.ToHashSet();

        var success = 0;
        var faild = 0;
        foreach (var model in models)
        {
            try
            {
                // 金额无效或会员不在本站：记失败，不写活动日志
                if (model.Money <= 0 || !known.Contains(model.UserID))
                {
                    faild++;
                    continue;
                }

                var sourceId = model.Date == default
                    ? DateTime.Today.ToString("yyyyMMdd", CultureInfo.InvariantCulture)
                    : model.Date.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
                // 同站同人同活动同来源日不可重复发
                var exists = await _db.Queryable<EventLog>()
                    .AnyAsync(
                        l => l.SiteID == siteId
                             && l.UserID == model.UserID
                             && l.EventID == model.EventID
                             && l.Type == EventTypeRescue
                             && l.SourceID == sourceId,
                        cancellationToken);
                if (exists)
                {
                    faild++;
                    continue;
                }

                var log = new EventLog
                {
                    SiteID = siteId,
                    UserID = model.UserID,
                    CreateAt = DateTime.Now,
                    EventID = model.EventID,
                    Put = 1,
                    Reward = model.Money,
                    IP = "0.0.0.0",
                    Type = EventTypeRescue,
                    Status = 0,
                    SourceID = sourceId,
                    ExpectedAt = DateTime.Now,
                    Flow = model.Flow,
                    Currency = model.Currency,
                    ErrorMsg =
                        $"#游戏#:{model.GameMoney:n},活动优惠: {model.EventMoney:n},返点: {model.RebateMoney:n}",
                    ReleaseAt = DateTime.MinValue,
                    CheckMessage = string.Empty
                };
                await _db.Insertable(log).ExecuteCommandAsync(cancellationToken);
                success++;
            }
            catch
            {
                faild++;
            }
        }

        return new { Success = success, Faild = faild, Message = $"成功{success}笔,失败{faild}笔" };
    }

    /// <summary>
    /// 按日预览：亏损会员 × 开启中的救援活动。活动优惠和返水读日资金表；RealWinLose 时从亏损里扣掉。
    /// </summary>
    private async Task<object?> QueryCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var siteId = _site.RequireSiteId();
        var date = ReadDate(input, "Date") ?? DateTime.Today;
        if (date.Date == DateTime.Today)
            throw Oops.Oh("#请输入日期进行发放#");

        // 现网开启中的救援活动 Status 多为 1；原先只认 0 会误报「没有开启救援金活动」。
        var events = await _db.Queryable<SiteEvent>()
            .Where(e => e.SiteID == siteId && e.Type == EventTypeRescue && e.IsOpen)
            .ToListAsync(cancellationToken);
        if (events.Count == 0)
            throw Oops.Oh("#当前没有开启救援金活动#");

        var userId = 0;
        var userName = ReadString(input, "User");
        if (!string.IsNullOrWhiteSpace(userName))
        {
            var users = await _db.Queryable<User>()
                .Where(u => u.SiteID == siteId && u.UserName == userName.Trim())
                .Take(1)
                .ToListAsync(cancellationToken);
            userId = users.FirstOrDefault()?.ID ?? 0;
            if (userId == 0)
                throw Oops.Oh("#用户名不存在#");
        }

        // Sqlite 业务日列常是 TEXT，用闭开区间比 == 更稳
        var dayStart = date.Date;
        var dayEnd = dayStart.AddDays(1);
        var gameQuery = _db.Queryable<UserDateGame>()
            .Where(g => g.SiteID == siteId && g.Date >= dayStart && g.Date < dayEnd);
        if (userId > 0)
            gameQuery = gameQuery.Where(g => g.UserID == userId);
        var games = await gameQuery.ToListAsync(cancellationToken);
        if (games.Count == 0)
            throw Oops.Oh("#当日没有游戏记录#");

        var loseUserIds = games.Where(g => g.Money < 0).Select(g => g.UserID).Distinct().ToList();
        if (loseUserIds.Count == 0)
            return Array.Empty<object>();

        var moneyRows = await _db.Queryable<UserDateMoney>()
            .Where(m => m.SiteID == siteId && m.Date >= dayStart && m.Date < dayEnd && loseUserIds.Contains(m.UserID))
            .ToListAsync(cancellationToken);
        var nameMap = await LoadUserNamesAsync(siteId, loseUserIds, cancellationToken);
        var results = new List<object>();
        foreach (var uid in loseUserIds)
        {
            foreach (var ev in events)
            {
                var rule = ParseRescueSetting(ev.SettingString);
                var userGames = games.Where(g => g.UserID == uid).ToList();
                // FilterGame 有值时按「分类_类型」粗过滤；空则算全部亏损
                var filter = (ev.FilterGame ?? string.Empty)
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                IEnumerable<UserDateGame> matched = userGames;
                if (filter.Length > 0)
                {
                    matched = userGames.Where(g =>
                        filter.Contains($"{g.Category}_{g.Type}", StringComparer.OrdinalIgnoreCase));
                }

                foreach (var grp in matched.GroupBy(g => g.Currency))
                {
                    var gameMoney = grp.Sum(g => g.Money);
                    if (gameMoney >= 0)
                        continue;
                    var eventMoney = moneyRows
                        .Where(m => m.UserID == uid && m.Currency == grp.Key && IsRescueEventMoneyType(m.Type))
                        .Sum(m => m.Money);
                    var rebateMoney = moneyRows
                        .Where(m => m.UserID == uid && m.Currency == grp.Key && IsRescueRebateMoneyType(m.Type))
                        .Sum(m => m.Money);
                    var absLoss = Math.Abs(gameMoney);
                    // RealWinLose：亏损先扣掉当天活动优惠和返水；WinLose 只看游戏盈亏
                    var net = string.Equals(rule.Statistics, "RealWinLose", StringComparison.OrdinalIgnoreCase)
                        ? absLoss - eventMoney - rebateMoney
                        : absLoss;
                    if (net <= 0)
                        continue;
                    var money = ComputeRescueMoney(net, rule);
                    if (money <= 0)
                        continue;
                    var flow = Math.Round(money * rule.Flow, 2, MidpointRounding.AwayFromZero);
                    results.Add(new
                    {
                        Date = date.Date,
                        EventID = ev.ID,
                        EventName = ev.Title ?? string.Empty,
                        UserID = uid,
                        UserName = nameMap.TryGetValue(uid, out var n) ? n : string.Empty,
                        GameMoney = gameMoney,
                        EventMoney = eventMoney,
                        RebateMoney = rebateMoney,
                        Money = money,
                        Flow = flow,
                        Currency = grp.Key
                    });
                }
            }
        }

        return results;
    }

    /// <summary>活动类资金类型（旧分类 Event，含 200 段和少量分散值）。</summary>
    private static bool IsRescueEventMoneyType(int type) =>
        type is 3 or 31 or 32 or 91 or 246 or 248 or 249 || (type >= 200 && type <= 224);

    /// <summary>返水类资金类型：60 返水、61 手动返水、62 返水扣除。</summary>
    private static bool IsRescueRebateMoneyType(int type) => type is 60 or 61 or 62;

    /// <summary>按净亏损和活动设定算出应发金额。有亏损阶梯时优先阶梯，否则用比例。</summary>
    private static decimal ComputeRescueMoney(decimal net, RescuePayRule rule)
    {
        RescueLossStep hit = null;
        foreach (var step in rule.Steps)
        {
            if (step.Need <= net && (hit == null || step.Need > hit.Need))
                hit = step;
        }

        if (hit != null)
        {
            if (rule.ReturnByRate)
                return Math.Round(net * hit.Money / 100m, 2, MidpointRounding.AwayFromZero);
            if (hit.Money > 0)
                return Math.Round(hit.Money, 2, MidpointRounding.AwayFromZero);
        }

        return Math.Round(net * rule.Rate, 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>读活动设定里的比例、流水倍数、统计方式和亏损阶梯（query 或 JSON）。</summary>
    private static RescuePayRule ParseRescueSetting(string? setting)
    {
        var rule = new RescuePayRule();
        if (string.IsNullOrWhiteSpace(setting))
            return rule;
        var trimmed = setting.TrimStart();
        if (trimmed.StartsWith('{'))
        {
            try
            {
                using var doc = JsonDocument.Parse(setting);
                ApplyRescueJson(rule, doc.RootElement);
                return rule;
            }
            catch (JsonException)
            {
                // 再走 query
            }
        }

        var map = SiteSettingQuery.Parse(setting);
        ApplyRescueMap(rule, map);
        return rule;
    }

    /// <summary>从 JSON 活动设定填比例、流水和阶梯。</summary>
    private static void ApplyRescueJson(RescuePayRule rule, JsonElement root)
    {
        if (root.TryGetProperty("Rate", out var rateEl)
            && decimal.TryParse(rateEl.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var rate))
            rule.Rate = rate;
        var flowSet = false;
        if (root.TryGetProperty("Flow", out var flowEl)
            && decimal.TryParse(flowEl.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var flow))
        {
            rule.Flow = flow;
            flowSet = true;
        }

        if (!flowSet
            && root.TryGetProperty("WashFlow", out var washEl)
            && decimal.TryParse(washEl.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var wash))
            rule.Flow = wash;
        if (root.TryGetProperty("Statistics", out var statEl))
            rule.Statistics = statEl.ToString();
        if (root.TryGetProperty("ReturnWay", out var wayEl)
            && string.Equals(wayEl.ToString(), "Rate", StringComparison.OrdinalIgnoreCase))
            rule.ReturnByRate = true;
        if (!root.TryGetProperty("Items", out var items) || items.ValueKind != JsonValueKind.Array)
            return;
        foreach (var item in items.EnumerateArray())
        {
            if (item.TryGetProperty("Type", out var typeEl) && !IsLossItemType(typeEl))
                continue;
            if (!item.TryGetProperty("needMoney", out var needEl) && !item.TryGetProperty("NeedMoney", out needEl))
                continue;
            if (!item.TryGetProperty("Money", out var moneyEl))
                continue;
            if (!decimal.TryParse(needEl.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var need))
                continue;
            if (!decimal.TryParse(moneyEl.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var money))
                continue;
            rule.Steps.Add(new RescueLossStep { Need = need, Money = money });
        }
    }

    /// <summary>从 query 活动设定填比例和流水。没有 Flow 时用 WashFlow。</summary>
    private static void ApplyRescueMap(RescuePayRule rule, Dictionary<string, string> map)
    {
        if (decimal.TryParse(SiteSettingQuery.Get(map, "Rate"), NumberStyles.Any, CultureInfo.InvariantCulture, out var rate))
            rule.Rate = rate;
        var flowSet = decimal.TryParse(SiteSettingQuery.Get(map, "Flow"), NumberStyles.Any, CultureInfo.InvariantCulture, out var flow);
        if (flowSet)
            rule.Flow = flow;
        else if (decimal.TryParse(SiteSettingQuery.Get(map, "WashFlow"), NumberStyles.Any, CultureInfo.InvariantCulture, out var wash))
            rule.Flow = wash;
        var statistics = SiteSettingQuery.Get(map, "Statistics");
        if (!string.IsNullOrWhiteSpace(statistics))
            rule.Statistics = statistics;
        if (string.Equals(SiteSettingQuery.Get(map, "ReturnWay"), "Rate", StringComparison.OrdinalIgnoreCase))
            rule.ReturnByRate = true;
    }

    /// <summary>阶梯项是否按亏损（2 或 Loss）。没写类型也当亏损阶梯。</summary>
    private static bool IsLossItemType(JsonElement typeEl)
    {
        var text = typeEl.ToString();
        if (string.IsNullOrWhiteSpace(text))
            return true;
        if (string.Equals(text, "Loss", StringComparison.OrdinalIgnoreCase))
            return true;
        return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) && n == 2;
    }

    /// <summary>批量取用户名。</summary>
    private async Task<Dictionary<int, string>> LoadUserNamesAsync(
        int siteId,
        List<int> userIds,
        CancellationToken cancellationToken)
    {
        var map = new Dictionary<int, string>();
        if (userIds.Count == 0)
            return map;
        var users = await _db.Queryable<User>()
            .Where(u => u.SiteID == siteId && userIds.Contains(u.ID))
            .Select(u => new { u.ID, u.UserName })
            .ToListAsync(cancellationToken);
        foreach (var u in users)
            map[u.ID] = u.UserName ?? string.Empty;
        return map;
    }

    /// <summary>读整数；键不存在返回 false，避免把「没传」和 0 混在一起。</summary>
    private static bool TryReadInt(Dictionary<string, object?> input, out int value, params string[] keys)
    {
        value = 0;
        foreach (var k in keys)
        {
            if (!input.TryGetValue(k, out var v) || v is null)
                continue;
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

        return false;
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

    private static string ReadString(Dictionary<string, object?> input, params string[] keys)
    {
        foreach (var k in keys)
        {
            if (!input.TryGetValue(k, out var v) || v is null) continue;
            var s = v.ToString();
            if (!string.IsNullOrWhiteSpace(s))
                return s;
        }
        return string.Empty;
    }

    private static DateTime? ReadDate(Dictionary<string, object?> input, params string[] keys)
    {
        foreach (var k in keys)
        {
            if (!input.TryGetValue(k, out var v) || v is null) continue;
            if (v is DateTime dt) return dt;
            if (DateTime.TryParse(v.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed))
                return parsed;
        }
        return null;
    }

}

/// <summary>救援金发放行（对齐旧 RescueModel JSON）。</summary>
internal sealed class RescueGrantRow
{
    /// <summary>业务日。</summary>
    public DateTime Date { get; set; }

    /// <summary>活动编号。</summary>
    public int EventID { get; set; }

    /// <summary>会员。</summary>
    public int UserID { get; set; }

    /// <summary>发放金额。</summary>
    public decimal Money { get; set; }

    /// <summary>流水要求。</summary>
    public decimal Flow { get; set; }

    /// <summary>游戏盈亏。</summary>
    public decimal GameMoney { get; set; }

    /// <summary>活动优惠合计（预览字段）。</summary>
    public decimal EventMoney { get; set; }

    /// <summary>返点合计（预览字段）。</summary>
    public decimal RebateMoney { get; set; }

    /// <summary>币种。</summary>
    public int Currency { get; set; }
}

/// <summary>救援金预览用的活动设定。</summary>
internal sealed class RescuePayRule
{
    /// <summary>没写比例时按 10%。</summary>
    public decimal Rate { get; set; } = 0.1m;

    /// <summary>流水倍数。没写时按 1。</summary>
    public decimal Flow { get; set; } = 1m;

    /// <summary>WinLose 只看游戏盈亏；RealWinLose 再扣活动和返水。</summary>
    public string Statistics { get; set; } = "WinLose";

    /// <summary>阶梯金额按比例（净亏损 × 阶梯值 / 100）。</summary>
    public bool ReturnByRate { get; set; }

    /// <summary>亏损阶梯。Need 为门槛，Money 为固定额或比例。</summary>
    public List<RescueLossStep> Steps { get; } = new();
}

/// <summary>救援金亏损阶梯一档。</summary>
internal sealed class RescueLossStep
{
    /// <summary>净亏损达到该值才用这一档。</summary>
    public decimal Need { get; set; }

    /// <summary>固定发放额，或比例（ReturnByRate 时）。</summary>
    public decimal Money { get; set; }
}

