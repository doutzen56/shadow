using System.Globalization;
using SimpX.Net.Data.Entity.Games;
using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 游戏会员/转账缺口。对齐旧 /admin/game/user/*、/admin/game/transfer/*。
/// </summary>
public sealed partial class GameUserQueryService
{
    /// <summary>
    /// 游戏会员列表（兼容旧 userlist 入参）。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页</returns>
    public async Task<GameUserPageOutput> UserListAsync(
        GameUserListInput input,
        CancellationToken cancellationToken = default)
    {
        input ??= new GameUserListInput();
        var siteId = _site.RequireSiteId();
        var pageSize = Math.Clamp(input.PageSize <= 0 ? 20 : input.PageSize, 1, 100);
        var page = Math.Max(1, input.Page <= 0 ? 1 : input.Page);
        var query = _db.Queryable<GameUser>().Where(g => g.SiteID == siteId);

        if (input.UserID > 0)
            query = query.Where(g => g.UserID == input.UserID);
        if (input.Type > 0)
            query = query.Where(g => g.Game == input.Type);
        if (!string.IsNullOrWhiteSpace(input.GameUser))
            query = query.Where(g => g.PlayerName == input.GameUser.Trim());
        if (!string.IsNullOrWhiteSpace(input.User))
        {
            var name = input.User.Trim();
            var ids = await _db.Queryable<User>()
                .Where(u => u.SiteID == siteId && u.UserName == name)
                .Select(u => u.ID)
                .Take(1)
                .ToListAsync(cancellationToken);
            var uid = ids.FirstOrDefault();
            query = query.Where(g => g.UserID == uid);
        }

        if (input.StartAt.HasValue)
            query = query.Where(g => g.CreateAt >= input.StartAt.Value);
        if (input.EndAt.HasValue)
            query = query.Where(g => g.CreateAt < input.EndAt.Value);
        if (input.MinMoney != 0)
            query = query.Where(g => g.Balance >= input.MinMoney);
        if (input.MaxMoney != 0)
            query = query.Where(g => g.Balance <= input.MaxMoney);
        if (input.Currency >= 0)
            query = query.Where(g => g.Currency == input.Currency);

        var total = await query.CountAsync(cancellationToken);
        var ordered = input.UserID > 0
            ? query.OrderBy(g => g.Game)
            : query.OrderBy(g => g.CreateAt, OrderByType.Desc);
        var rows = await ordered.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        var nameMap = await LoadNamesAsync(siteId, rows.Select(r => r.UserID).Distinct().ToList(), cancellationToken);

        return new GameUserPageOutput
        {
            Total = total,
            Items = rows.Select(g => new GameUserItemDto
            {
                UserID = g.UserID,
                UserName = nameMap.TryGetValue(g.UserID, out var un) ? un : string.Empty,
                Game = g.Game,
                Currency = g.Currency,
                PlayerName = g.PlayerName,
                PlayerPass = g.PlayerPass,
                Balance = g.Balance,
                Withdraw = g.Withdraw,
                CreateAt = g.CreateAt,
                UpdateAt = g.UpdateAt,
                Lock = g.Lock,
                Limits = g.Limits,
                LimitType = g.LimitType,
                IsTest = g.IsTest
            }).ToList()
        };
    }

    /// <summary>
    /// 读库余额（不调厂商）。对齐旧 getbalance 的本站落库值。
    /// </summary>
    /// <param name="userId">会员</param>
    /// <param name="type">游戏类型</param>
    /// <param name="currency">币种</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>余额</returns>
    public async Task<GameBalanceDto> GetBalanceAsync(
        int userId,
        int type,
        int currency,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0 || type == 0)
            throw Oops.Oh("#参数错误#");
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<GameUser>()
            .Where(g => g.SiteID == siteId && g.UserID == userId && g.Game == type && g.Currency == currency)
            .Take(1)
            .ToListAsync(cancellationToken);
        var g = rows.FirstOrDefault() ?? throw Oops.Oh("#未找到游戏帐号#");
        return new GameBalanceDto { Balance = g.Balance, UserID = userId, Type = type, Currency = currency };
    }

    /// <summary>
    /// 会员各游戏余额摘要。对齐旧 GetUserBalanceList。
    /// </summary>
    /// <param name="userId">会员</param>
    /// <param name="currency">币种；-1=全部</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行</returns>
    public async Task<List<GameBalanceDto>> GetUserBalanceListAsync(
        int userId,
        int currency = -1,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
            throw Oops.Oh("#参数错误#");
        var siteId = _site.RequireSiteId();
        var q = _db.Queryable<GameUser>().Where(g => g.SiteID == siteId && g.UserID == userId);
        if (currency >= 0)
            q = q.Where(g => g.Currency == currency);
        var rows = await q.OrderBy(g => g.Game).ToListAsync(cancellationToken);
        return rows.Select(g => new GameBalanceDto
        {
            UserID = g.UserID,
            Type = g.Game,
            Currency = g.Currency,
            Balance = g.Balance
        }).ToList();
    }

    /// <summary>
    /// 改游戏锁定位。对齐旧 updatelock（位运算；Name=锁枚举值）。
    /// </summary>
    /// <param name="input">入参</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task UpdateLockAsync(GameUserLockInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.UserID <= 0 || input.Type == 0)
            throw Oops.Oh("#参数错误#");
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<GameUser>()
            .Where(g => g.SiteID == siteId && g.UserID == input.UserID && g.Game == input.Type && g.Currency == input.Currency)
            .Take(1)
            .ToListAsync(cancellationToken);
        var g = rows.FirstOrDefault() ?? throw Oops.Oh("#未找到游戏帐号#");
        var bit = input.Name;
        if (bit == 0)
            throw Oops.Oh("#锁类型错误#");
        g.Lock = input.IsLock ? (g.Lock | bit) : (g.Lock & ~bit);
        await _db.Updateable(g).UpdateColumns(t => new { t.Lock }).ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 标记测试号（只能开不能关）。对齐旧 updatetest。
    /// </summary>
    /// <param name="userId">会员</param>
    /// <param name="type">游戏</param>
    /// <param name="isTest">是否测试</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task UpdateTestAsync(
        int userId,
        int type,
        bool isTest,
        CancellationToken cancellationToken = default)
    {
        if (!isTest)
            throw Oops.Oh("#开启测试标记后不可关闭#");
        if (userId <= 0 || type == 0)
            throw Oops.Oh("#参数错误#");
        var siteId = _site.RequireSiteId();
        var n = await _db.Updateable<GameUser>()
            .SetColumns(g => new GameUser { IsTest = true })
            .Where(g => g.SiteID == siteId && g.UserID == userId && g.Game == type)
            .ExecuteCommandAsync(cancellationToken);
        if (n <= 0)
            throw Oops.Oh("#未找到游戏帐号#");
    }

    /// <summary>
    /// 更新限红字段（不调厂商）。对齐旧 updateLimit 的落库部分。
    /// </summary>
    /// <param name="input">入参</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task UpdateLimitAsync(GameUserLimitInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.UserID <= 0 || input.Type == 0)
            throw Oops.Oh("#参数错误#");
        if (input.LimitType == 2)
        {
            var limits = input.Limits ?? string.Empty;
            if (!System.Text.RegularExpressions.Regex.IsMatch(limits, @"^MinBetLimit=\d+&MaxBetLimit=\d+$"))
                throw Oops.Oh("#限红格式不正确，请使用格式：MinBetLimit=100&MaxBetLimit=200#");
        }

        var siteId = _site.RequireSiteId();
        var n = await _db.Updateable<GameUser>()
            .SetColumns(g => new GameUser { Limits = input.Limits ?? string.Empty, LimitType = input.LimitType })
            .Where(g => g.SiteID == siteId && g.UserID == input.UserID && g.Game == input.Type && g.Currency == input.Currency)
            .ExecuteCommandAsync(cancellationToken);
        if (n != 1)
            throw Oops.Oh("#未找到游戏帐号#");
    }

    /// <summary>
    /// 注册游戏帐号占位（只写本地行，不调厂商）。对齐旧 register。
    /// </summary>
    /// <param name="input">入参</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task RegisterAsync(GameUserRegisterInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.UserID <= 0 || input.Type == 0)
            throw Oops.Oh("#平台不能为空#");
        var siteId = _site.RequireSiteId();
        var exists = await _db.Queryable<GameUser>()
            .AnyAsync(
                g => g.SiteID == siteId && g.UserID == input.UserID && g.Game == input.Type && g.Currency == input.Currency,
                cancellationToken);
        if (exists)
            throw Oops.Oh("#已存在#");

        await _db.Insertable(new GameUser
        {
            SiteID = siteId,
            UserID = input.UserID,
            Game = input.Type,
            Currency = input.Currency,
            PlayerName = string.Empty,
            PlayerPass = string.Empty,
            CreateAt = DateTime.Now,
            UpdateAt = DateTime.Now,
            Balance = 0,
            Withdraw = 0,
            Lock = 0,
            IsTest = false,
            Limits = string.Empty,
            LimitType = 0
        }).ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 代理侧会员游戏账户（排除免转）。对齐旧 /admin/proxy/gameuser、/request/proxy/gameuser。
    /// </summary>
    public async Task<object> ProxyGameUserAsync(
        string? userName,
        int currency,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userName))
            throw Oops.Oh("#用户名不能为空#");
        if (currency < 0)
            throw Oops.Oh("#货币不能为空#");

        var siteId = _site.RequireSiteId();
        var name = userName.Trim();
        var userIds = await _db.Queryable<User>()
            .Where(u => u.SiteID == siteId && u.UserName == name)
            .Select(u => u.ID)
            .Take(1)
            .ToListAsync(cancellationToken);
        var userId = userIds.FirstOrDefault();
        if (userId <= 0)
            throw Oops.Oh("#用户名不存在#");

        var freeGames = await _db.Queryable<GameSetting>()
            .Where(g => g.SiteID == siteId && g.IsFree)
            .Select(g => g.Game)
            .ToListAsync(cancellationToken);
        var freeSet = freeGames.ToHashSet();

        var list = await _db.Queryable<GameUser>()
            .Where(g => g.SiteID == siteId && g.UserID == userId)
            .ToListAsync(cancellationToken);
        var rows = list
            .Where(g => !freeSet.Contains(g.Game))
            .Select(t => new
            {
                Type = t.Game,
                Game = t.Game.ToString(CultureInfo.InvariantCulture),
                t.Balance,
                t.Withdraw
            })
            .ToList();

        var wallets = await _db.Queryable<UserMoney>()
            .Where(m => m.SiteID == siteId && m.UserID == userId && m.Currency == currency)
            .Take(1)
            .ToListAsync(cancellationToken);
        var wallet = wallets.FirstOrDefault();
        var walletDto = wallet == null
            ? new { Currency = currency, Money = 0m, LockMoney = 0m, TotalMoney = 0m }
            : new
            {
                Currency = wallet.Currency,
                Money = wallet.TotalMoney - wallet.LockMoney,
                wallet.LockMoney,
                wallet.TotalMoney
            };

        return OldApiShowResult.OfList(rows, walletDto);
    }

    /// <summary>
    /// 转账订单详情。
    /// </summary>
    /// <param name="id">订单号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行</returns>
    public async Task<GameTransferItemDto> TransferInfoAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw Oops.Oh("#编号错误#");
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<GameTransferOrder>()
            .Where(o => o.SiteID == siteId && o.ID == id)
            .Take(1)
            .ToListAsync(cancellationToken);
        var o = rows.FirstOrDefault() ?? throw Oops.Oh("#编号错误#");
        var names = await LoadNamesAsync(siteId, [o.UserID], cancellationToken);
        return new GameTransferItemDto
        {
            ID = o.ID,
            UserID = o.UserID,
            UserName = names.TryGetValue(o.UserID, out var un) ? un : string.Empty,
            Type = o.Type,
            Money = o.Money,
            Status = o.Status,
            Action = o.Action,
            Currency = o.Currency,
            Description = o.Description,
            ResultID = o.ResultID,
            CreateAt = o.CreateAt,
            CheckAt = o.CheckAt
        };
    }

    private async Task<Dictionary<int, string>> LoadNamesAsync(
        int siteId,
        List<int> ids,
        CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
            return new Dictionary<int, string>();
        var names = await _db.Queryable<User>()
            .Where(u => u.SiteID == siteId && ids.Contains(u.ID))
            .Select(u => new { u.ID, u.UserName })
            .ToListAsync(cancellationToken);
        return names.ToDictionary(n => n.ID, n => n.UserName ?? string.Empty);
    }
}

/// <summary>旧 userlist 筛选。</summary>
public sealed class GameUserListInput
{
    /// <summary>页码。</summary>
    public int Page { get; set; } = 1;

    /// <summary>每页。</summary>
    public int PageSize { get; set; } = 20;

    /// <summary>会员编号。</summary>
    public int UserID { get; set; }

    /// <summary>用户名。</summary>
    public string User { get; set; }

    /// <summary>游戏类型。</summary>
    public int Type { get; set; }

    /// <summary>第三方帐号。</summary>
    public string GameUser { get; set; }

    /// <summary>开始。</summary>
    public DateTime? StartAt { get; set; }

    /// <summary>结束。</summary>
    public DateTime? EndAt { get; set; }

    /// <summary>最小余额。</summary>
    public decimal MinMoney { get; set; }

    /// <summary>最大余额。</summary>
    public decimal MaxMoney { get; set; }

    /// <summary>币种；-1=全部。</summary>
    public int Currency { get; set; } = -1;
}

/// <summary>余额。</summary>
public sealed class GameBalanceDto
{
    /// <summary>会员。</summary>
    public int UserID { get; set; }

    /// <summary>游戏。</summary>
    public int Type { get; set; }

    /// <summary>币种。</summary>
    public int Currency { get; set; }

    /// <summary>余额。</summary>
    public decimal Balance { get; set; }
}

/// <summary>锁定位入参。</summary>
public sealed class GameUserLockInput
{
    /// <summary>会员。</summary>
    public int UserID { get; set; }

    /// <summary>游戏类型。</summary>
    public int Type { get; set; }

    /// <summary>币种。</summary>
    public int Currency { get; set; }

    /// <summary>锁枚举值。</summary>
    public int Name { get; set; }

    /// <summary>是否加锁。</summary>
    public bool IsLock { get; set; }
}

/// <summary>限红入参。</summary>
public sealed class GameUserLimitInput
{
    /// <summary>会员。</summary>
    public int UserID { get; set; }

    /// <summary>游戏。</summary>
    public int Type { get; set; }

    /// <summary>币种。</summary>
    public int Currency { get; set; }

    /// <summary>限红串。</summary>
    public string Limits { get; set; }

    /// <summary>限红类型。</summary>
    public int LimitType { get; set; }

    /// <summary>第三方帐号（兼容旧字段）。</summary>
    public string PlayerName { get; set; }
}

/// <summary>注册入参。</summary>
public sealed class GameUserRegisterInput
{
    /// <summary>会员。</summary>
    public int UserID { get; set; }

    /// <summary>游戏。</summary>
    public int Type { get; set; }

    /// <summary>币种。</summary>
    public int Currency { get; set; }
}

/// <summary>转账动作入参。</summary>
public sealed class GameTransferActionInput
{
    /// <summary>会员。</summary>
    public int UserID { get; set; }

    /// <summary>游戏。</summary>
    public int Type { get; set; }

    /// <summary>金额。</summary>
    public decimal Money { get; set; }

    /// <summary>方向；1=IN 2=OUT。</summary>
    public int Action { get; set; }

    /// <summary>币种。</summary>
    public int Currency { get; set; }
}
