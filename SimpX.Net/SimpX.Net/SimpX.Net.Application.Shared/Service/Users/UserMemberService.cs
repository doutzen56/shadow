using SimpX.Net.Data;
using SimpX.Net.Data.Entity.Games;
using SimpX.Net.Data.Entity.Reports;
using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Users;
using SimpX.Net.Feature.Auth;
using SqlSugar;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 租户后台会员：列表/详情/改锁/改等级/重置字段。业务字段对齐旧版 /admin/user/member/*。
/// </summary>
public sealed partial class UserMemberService : ITransient
{
    /// <summary>查看联系方式按钮权限（种子里仍是旧 GUID 码）。</summary>
    public const string ViewContactPermission = MemberProfileService.ViewContactPermissionId;

    /// <summary>会员列表「其他功能」权限（改锁需要）。</summary>
    public const string OtherFeaturePermissionId = "d7d1323ff76427d0eaa3a189fef234a7";

    /// <summary>「修改会员等级」权限（改 Level / InitLevel）。</summary>
    public const string ModifyLevelPermissionId = "306c74fa7303704a8bdc2780cf8c167d";

    /// <summary>允许的锁定位（跟旧版锁定位一样）。</summary>
    private static readonly HashSet<int> AllowedLockBits =
    [
        1, 2, 4, 8, 16, 32, 64, 128, 256
    ];

    /// <summary>允许的功能位（跟旧版 FunctionType 一样）。</summary>
    private static readonly HashSet<int> AllowedFunctionBits =
    [
        1, 2, 4, 8, 16, 32, 64, 128, 256
    ];

    /// <summary>旧 FunctionType.CloseSub：禁止下级登录。</summary>
    public const int FunctionCloseSub = 32;

    /// <summary>旧 LockStatus.Login。</summary>
    public const int LockLogin = 1;

    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;
    private readonly SiteAdminAuthService _adminAuth;
    private readonly IPermissionChecker _permissionChecker;

    /// <summary>
    /// 注入业务库、当前站、后台会话、权限检查。会员表在 BetWin30 业务库，不能走主库 Sqlite。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站</param>
    /// <param name="adminAuth">后台会话（OldApi）</param>
    /// <param name="permissionChecker">按钮权限（JWT）</param>
    public UserMemberService(
        IBizDbAccessor biz,
        ICurrentSiteAccessor site,
        SiteAdminAuthService adminAuth,
        IPermissionChecker permissionChecker)
    {
        _db = biz.Db;
        _site = site;
        _adminAuth = adminAuth;
        _permissionChecker = permissionChecker;
    }

    /// <summary>
    /// 当前站会员分页。对齐旧版 list 筛选与行字段。
    /// </summary>
    /// <param name="input">分页与用户名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页结果</returns>
    public async Task<UserMemberPageOutput> PageAsync(
        UserMemberPageInput input,
        CancellationToken cancellationToken = default)
    {
        return await PageBySiteAsync(_site.RequireSiteId(), input, maskContact: true, cancellationToken);
    }

    /// <summary>
    /// 按显式站点分页（总后台跨站；不定站）。
    /// </summary>
    /// <param name="siteId">站点编号，必须大于 0</param>
    /// <param name="input">筛选</param>
    /// <param name="maskContact">是否脱敏联系方式</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页</returns>
    public async Task<UserMemberPageOutput> PageBySiteAsync(
        int siteId,
        UserMemberPageInput input,
        bool maskContact = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (siteId <= 0)
            throw Oops.Oh("#请指定站点#");
        var pageSize = Math.Clamp(input.PageSize <= 0 ? 20 : input.PageSize, 1, 100);
        var page = Math.Max(1, input.Page <= 0 ? 1 : input.Page);

        var query = _db.Queryable<User>().Where(u => u.SiteID == siteId);
        var keyword = (input.User ?? input.UserName ?? string.Empty).Trim();
        var searchType = string.IsNullOrWhiteSpace(input.SearchType) ? "UserName" : input.SearchType.Trim();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            switch (searchType)
            {
                case "UserID":
                    if (int.TryParse(keyword, out var uid) && uid > 0)
                        query = query.Where(u => u.ID == uid);
                    else
                        query = query.Where(u => u.UserName != null && u.UserName.Contains(keyword));
                    break;
                case "NickName":
                    query = query.Where(u => u.NickName != null && u.NickName.Contains(keyword));
                    break;
                case "Mobile":
                    query = query.Where(u => u.Mobile != null && u.Mobile.Contains(keyword));
                    break;
                case "Email":
                    query = query.Where(u => u.Email != null && u.Email.Contains(keyword));
                    break;
                case "AccountName":
                    query = query.Where(u => u.AccountName != null && u.AccountName.Contains(keyword));
                    break;
                case "RegIP":
                    query = query.Where(u => u.RegIP != null && u.RegIP.Contains(keyword));
                    break;
                case "LoginIP":
                    query = query.Where(u => u.LoginIP != null && u.LoginIP.Contains(keyword));
                    break;
                case "DeviceId":
                    query = query.Where(u => u.DeviceId != null && u.DeviceId.Contains(keyword));
                    break;
                case "Account":
                    {
                        // 银行卡号：usr_BankAccount.Account → 会员编号
                        var bankUserIds = await _db.Queryable<BankAccount>()
                            .Where(b => b.SiteID == siteId && b.Account != null && b.Account.Contains(keyword))
                            .Select(b => b.UserID)
                            .Distinct()
                            .Take(2000)
                            .ToListAsync(cancellationToken);
                        query = bankUserIds.Count > 0
                            ? query.Where(u => bankUserIds.Contains(u.ID))
                            : query.Where(u => false);
                        break;
                    }
                case "GameUser":
                    {
                        // 游戏帐号：game_User.PlayerName → 会员编号
                        var gameUserIds = await _db.Queryable<GameUser>()
                            .Where(g => g.SiteID == siteId && g.PlayerName != null && g.PlayerName.Contains(keyword))
                            .Select(g => g.UserID)
                            .Distinct()
                            .Take(2000)
                            .ToListAsync(cancellationToken);
                        query = gameUserIds.Count > 0
                            ? query.Where(u => gameUserIds.Contains(u.ID))
                            : query.Where(u => false);
                        break;
                    }
                case "KYCId":
                    {
                        // 证件号码：usr_IdCard.CardNumber → 会员编号
                        var kycUserIds = await _db.Queryable<UserIdCard>()
                            .Where(c => c.SiteID == siteId && c.CardNumber != null && c.CardNumber.Contains(keyword))
                            .Select(c => c.UserID)
                            .Distinct()
                            .Take(2000)
                            .ToListAsync(cancellationToken);
                        query = kycUserIds.Count > 0
                            ? query.Where(u => kycUserIds.Contains(u.ID))
                            : query.Where(u => false);
                        break;
                    }
                default:
                    query = query.Where(u => u.UserName != null && u.UserName.Contains(keyword));
                    break;
            }
        }

        if (!string.IsNullOrWhiteSpace(input.Agent))
        {
            var agentName = input.Agent.Trim();
            if (agentName == "0")
            {
                query = query.Where(u => u.AgentID == 0);
            }
            else
            {
                List<int> agentIds;
                if (int.TryParse(agentName, out var aidExact))
                    agentIds = [aidExact];
                else
                    agentIds = await _db.Queryable<User>()
                        .Where(u => u.SiteID == siteId && u.UserName.Contains(agentName))
                        .Select(u => u.ID)
                        .Take(500)
                        .ToListAsync(cancellationToken);
                if (agentIds.Count == 0)
                {
                    query = query.Where(u => false);
                }
                else if (input.BelongUser == 1)
                {
                    var childIds = await _db.Queryable<UserDepth>()
                        .Where(d => d.SiteID == siteId && agentIds.Contains(d.UserID))
                        .Select(d => d.ChildID)
                        .ToListAsync(cancellationToken);
                    var scope = agentIds.Concat(childIds).Distinct().ToList();
                    query = query.Where(u => scope.Contains(u.ID) || agentIds.Contains(u.AgentID));
                }
                else
                {
                    query = query.Where(u => agentIds.Contains(u.AgentID));
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(input.Type))
        {
            var typeText = input.Type.Trim();
            if (int.TryParse(typeText, out var typeInt))
                query = query.Where(u => u.Type == typeInt);
            else if (string.Equals(typeText, "Member", StringComparison.OrdinalIgnoreCase))
                query = query.Where(u => u.Type == 0);
            else if (string.Equals(typeText, "Agent", StringComparison.OrdinalIgnoreCase))
                query = query.Where(u => u.Type != 0);
        }

        if (input.GroupID is > 0)
            query = query.Where(u => u.GroupID == input.GroupID.Value);
        if (input.Level.HasValue)
            query = query.Where(u => u.Level == input.Level.Value);
        if (input.RegMethod.HasValue)
            query = query.Where(u => u.RegFlag == input.RegMethod.Value);
        if (input.Depth.HasValue)
            query = query.Where(u => u.Depth == input.Depth.Value);
        if (!string.IsNullOrWhiteSpace(input.Domain))
            query = query.Where(u => u.RegDomain != null && u.RegDomain.Contains(input.Domain.Trim()));

        if (!string.IsNullOrWhiteSpace(input.InviterName))
        {
            var inviter = input.InviterName.Trim();
            if (int.TryParse(inviter, out var inviteIdExact) && inviteIdExact > 0)
                query = query.Where(u => u.InviteID == inviteIdExact);
            else
            {
                var inviteIds = await _db.Queryable<User>()
                    .Where(u => u.SiteID == siteId && u.UserName.Contains(inviter))
                    .Select(u => u.ID)
                    .Take(500)
                    .ToListAsync(cancellationToken);
                if (inviteIds.Count == 0)
                    query = query.Where(u => false);
                else
                    query = query.Where(u => inviteIds.Contains(u.InviteID));
            }
        }

        var timeType = (input.TimeType ?? "Reg").Trim();
        if (input.StartAt is { } startAt)
        {
            query = timeType switch
            {
                "Login" => query.Where(u => u.LoginAt >= startAt),
                "Active" => query.Where(u => u.ActiveAt >= startAt),
                _ => query.Where(u => u.CreateAt >= startAt)
            };
        }

        if (input.EndAt is { } endAt)
        {
            query = timeType switch
            {
                "Login" => query.Where(u => u.LoginAt <= endAt),
                "Active" => query.Where(u => u.ActiveAt <= endAt),
                _ => query.Where(u => u.CreateAt <= endAt)
            };
        }

        if (input.Active is > 0)
        {
            var since = DateTime.Now.AddMinutes(-input.Active.Value);
            query = query.Where(u => u.ActiveAt >= since);
        }

        if (input.MinMoney.HasValue || input.MaxMoney.HasValue)
        {
            // 余额筛在钱包表上做；先取符合金额的 UserID 再收窄
            var moneyQ = _db.Queryable<UserMoney>().Where(w => w.SiteID == siteId);
            if (input.Currency is > 0)
                moneyQ = moneyQ.Where(w => w.Currency == input.Currency.Value);
            if (input.MinMoney.HasValue)
                moneyQ = moneyQ.Where(w => w.TotalMoney >= input.MinMoney.Value);
            if (input.MaxMoney.HasValue)
                moneyQ = moneyQ.Where(w => w.TotalMoney <= input.MaxMoney.Value);
            var moneyUserIds = await moneyQ.Select(w => w.UserID).Distinct().ToListAsync(cancellationToken);
            query = query.Where(u => moneyUserIds.Contains(u.ID));
        }

        var total = await query.CountAsync(cancellationToken);
        var canViewContact = !maskContact || await CanViewContactAsync(cancellationToken);

        var rows = await query
            .OrderBy(u => u.ActiveAt, OrderByType.Desc)
            .Select(u => new
            {
                u.ID,
                u.UserName,
                u.NickName,
                u.AccountName,
                u.CreateAt,
                u.Lock,
                u.AgentID,
                u.InviteID,
                u.GroupID,
                u.Level,
                u.LoginAt,
                u.LoginIP,
                u.ActiveAt,
                u.Email,
                u.Mobile,
                u.Currency,
                u.Money,
                u.Type,
                u.Depth,
                u.RegFlag
            })
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var now = DateTime.Now;
        // Users.Money 靠旁路队列回写，本仓队列消费未齐；列表余额改读 usr_Money（会员主币种那一行）
        var walletMap = await LoadMainWalletMoneyAsync(
            siteId,
            rows.Select(u => (u.ID, u.Currency)).ToList(),
            cancellationToken);

        var relatedIds = rows.Select(u => u.AgentID).Concat(rows.Select(u => u.InviteID)).Where(id => id > 0).Distinct().ToList();
        var nameMap = relatedIds.Count == 0
            ? new Dictionary<int, string>()
            : (await _db.Queryable<User>()
                .Where(u => u.SiteID == siteId && relatedIds.Contains(u.ID))
                .Select(u => new { u.ID, u.UserName })
                .ToListAsync(cancellationToken))
            .ToDictionary(x => x.ID, x => x.UserName ?? string.Empty);

        var groupIds = rows.Select(u => u.GroupID).Where(id => id > 0).Distinct().ToList();
        var groupMap = groupIds.Count == 0
            ? new Dictionary<int, string>()
            : (await _db.Queryable<UserGroup>()
                .Where(g => g.SiteID == siteId && groupIds.Contains(g.ID))
                .Select(g => new { g.ID, g.Name })
                .ToListAsync(cancellationToken))
            .ToDictionary(x => x.ID, x => x.Name ?? string.Empty);

        var agentRowIds = rows.Where(u => u.Type != 0).Select(u => u.ID).ToList();
        var childCountMap = new Dictionary<int, int>();
        if (agentRowIds.Count > 0)
        {
            var counts = await _db.Queryable<User>()
                .Where(u => u.SiteID == siteId && agentRowIds.Contains(u.AgentID))
                .GroupBy(u => u.AgentID)
                .Select(u => new { AgentID = u.AgentID, Cnt = SqlFunc.AggregateCount(u.ID) })
                .ToListAsync(cancellationToken);
            childCountMap = counts.ToDictionary(x => x.AgentID, x => x.Cnt);
        }

        // 对齐旧 list：从 rpt_UserDateMoney 汇总充/提类型（失败不影响列表主数据）。
        var pageUserIds = rows.Select(u => u.ID).ToList();
        var rechargeByUser = new Dictionary<int, decimal>();
        var withdrawByUser = new Dictionary<int, decimal>();
        if (pageUserIds.Count > 0)
        {
            try
            {
                int[] rechargeTypes = [1, 111, 9, 44]; // Recharge / ProxyRecharge / RechargeDue / RechargeDueReverse
                int[] withdrawTypes = [2, 11, 10]; // Withdraw / WithdrawMoney / WithdrawReturn
                var typeIds = rechargeTypes.Concat(withdrawTypes).ToArray();
                var moneyStats = await _db.Queryable<UserDateMoney>()
                    .Where(t => t.SiteID == siteId && pageUserIds.Contains(t.UserID) && typeIds.Contains(t.Type))
                    .Select(t => new { t.UserID, t.Type, t.Money })
                    .ToListAsync(cancellationToken);
                foreach (var g in moneyStats.GroupBy(x => x.UserID))
                {
                    rechargeByUser[g.Key] = g.Where(x => rechargeTypes.Contains(x.Type)).Sum(x => x.Money);
                    withdrawByUser[g.Key] = g.Where(x => withdrawTypes.Contains(x.Type)).Sum(x => x.Money);
                }
            }
            catch
            {
                // 报表表异常时充提合计为 0，不拖垮会员列表。
            }
        }

        var data = rows.Select(u =>
        {
            var money = walletMap.TryGetValue(u.ID, out var walletMoney) ? walletMoney : u.Money;
            return new MemberListItemDto
            {
                ID = u.ID,
                UserID = u.ID,
                UserName = u.UserName,
                NickName = u.NickName,
                AccountName = u.AccountName,
                CreateAt = u.CreateAt,
                Lock = u.Lock,
                AgentID = u.AgentID,
                InviteID = u.InviteID,
                GroupID = u.GroupID,
                Level = u.Level,
                LoginAt = u.LoginAt,
                LoginIP = u.LoginIP,
                ActiveAt = u.ActiveAt,
                Email = canViewContact ? (u.Email ?? string.Empty) : ContactMask.MaskEmail(u.Email),
                Mobile = canViewContact ? (u.Mobile ?? string.Empty) : ContactMask.MaskMobile(u.Mobile),
                Currency = u.Currency,
                Money = money,
                TotalMoney = money,
                TotalRechargeMoney = rechargeByUser.TryGetValue(u.ID, out var tr) ? tr : 0m,
                TotalWithdrawMoney = withdrawByUser.TryGetValue(u.ID, out var tw) ? tw : 0m,
                Type = u.Type,
                TypeName = u.Type == 0 ? "会员" : "代理",
                Depth = u.Depth,
                RegFlag = u.RegFlag,
                IsOnline = u.ActiveAt.Year >= 2000 && (now - u.ActiveAt).TotalMinutes < 10,
                AgentName = u.AgentID > 0 && nameMap.TryGetValue(u.AgentID, out var an) ? an : string.Empty,
                InviterName = u.InviteID > 0 && nameMap.TryGetValue(u.InviteID, out var inv) ? inv : string.Empty,
                Group = u.GroupID > 0 && groupMap.TryGetValue(u.GroupID, out var gn) ? gn : string.Empty,
                LevelName = u.Level.ToString(),
                Count = childCountMap.TryGetValue(u.ID, out var cc) ? cc : 0
            };
        }).ToList();

        return new UserMemberPageOutput
        {
            RecordCount = total,
            Total = total,
            Page = page,
            PageSize = pageSize,
            Data = data
        };
    }

    /// <summary>
    /// 按会员主币种批量读 usr_Money.TotalMoney；没有钱包行就跳过，仍用 Users.Money。
    /// </summary>
    /// <param name="siteId">站点</param>
    /// <param name="users">会员编号与主币种</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>UserID → 余额</returns>
    private async Task<Dictionary<int, decimal>> LoadMainWalletMoneyAsync(
        int siteId,
        IReadOnlyList<(int UserId, int Currency)> users,
        CancellationToken cancellationToken)
    {
        var map = new Dictionary<int, decimal>();
        if (users == null || users.Count == 0)
            return map;

        var userIds = users.Select(u => u.UserId).Distinct().ToList();
        var wallets = await _db.Queryable<UserMoney>()
            .Where(w => w.SiteID == siteId && userIds.Contains(w.UserID))
            .Select(w => new { w.UserID, w.Currency, w.TotalMoney, w.IsBase })
            .ToListAsync(cancellationToken);

        var currencyByUser = users.ToDictionary(u => u.UserId, u => u.Currency);
        foreach (var group in wallets.GroupBy(w => w.UserID))
        {
            var prefer = currencyByUser.TryGetValue(group.Key, out var cur) ? cur : 0;
            var hit = group.FirstOrDefault(w => w.Currency == prefer)
                      ?? group.FirstOrDefault(w => w.IsBase)
                      ?? group.FirstOrDefault();
            if (hit != null)
                map[group.Key] = hit.TotalMoney;
        }

        return map;
    }

    /// <summary>
    /// 旧版兼容会员分页。返回 OldApiResult，info 字段名与旧版 list 一致。
    /// </summary>
    /// <param name="input">分页与用户名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>旧版返回格式</returns>
    public async Task<OldApiResult> PageForOldApiAsync(
        UserMemberPageInput input,
        CancellationToken cancellationToken = default)
    {
        input ??= new UserMemberPageInput();
        if (!TryValidateListFilter(input, out var fail))
            return fail!;

        var page = await PageAsync(input, cancellationToken);
        return OldApiResult.Ok(
            string.Empty,
            OldApiShowResult.OfPage(page.Data ?? [], page.RecordCount, page.Page, page.PageSize));
    }

    /// <summary>
    /// 会员导出（旧版 Form）。对齐 /admin/user/member/info/export。
    /// </summary>
    /// <param name="input">筛选（强制 PageSize≤5000）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>旧版返回格式；info.list 为导出行</returns>
    public async Task<OldApiResult> ExportForOldApiAsync(
        UserMemberPageInput input,
        CancellationToken cancellationToken = default)
    {
        input ??= new UserMemberPageInput();
        if (!TryValidateListFilter(input, out var fail))
            return fail!;

        var export = await ExportAsync(input, cancellationToken);
        var rows = export.Data ?? [];
        return OldApiResult.Ok(
            string.Empty,
            OldApiShowResult.OfPage(rows, export.RecordCount, 1, Math.Max(rows.Count, 1)));
    }

    /// <summary>
    /// 对齐旧 list/export：默认要有条件；IsLimit=0（查看全体会员）放行。
    /// </summary>
    private static bool TryValidateListFilter(UserMemberPageInput input, out OldApiResult? fail)
    {
        fail = null;
        var keyword = (input.User ?? input.UserName ?? string.Empty).Trim();
        var hasFilter = !string.IsNullOrWhiteSpace(keyword)
            || !string.IsNullOrWhiteSpace(input.Agent)
            || !string.IsNullOrWhiteSpace(input.Type)
            || input.GroupID.HasValue
            || input.Level.HasValue
            || input.StartAt.HasValue
            || input.EndAt.HasValue
            || input.Active.HasValue
            || !string.IsNullOrWhiteSpace(input.Domain)
            || !string.IsNullOrWhiteSpace(input.InviterName)
            || input.MinMoney.HasValue
            || input.MaxMoney.HasValue
            || input.Depth.HasValue
            || input.RegMethod.HasValue
            || input.CommissionID.HasValue
            || input.ReturnID.HasValue
            || input.RechargeCount.HasValue
            || input.Currency.HasValue;
        if (input.IsLimit != 0 && !hasFilter)
        {
            fail = OldApiResult.Fail("#请输入查询条件#");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 当前站会员详情。对齐旧版 info/detail 精简字段。
    /// </summary>
    /// <param name="userId">会员编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>详情</returns>
    public async Task<UserMemberDetailDto> DetailAsync(int userId, CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
            throw Oops.Oh("#用户不存在#");

        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<User>()
            .Where(u => u.ID == userId && u.SiteID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var user = rows.FirstOrDefault() ?? throw Oops.Oh("#用户不存在#");

        var canViewContact = await _permissionChecker.IsGrantedAsync(ViewContactPermission);
        var email = user.Email ?? string.Empty;
        var mobile = user.Mobile ?? string.Empty;
        if (!canViewContact)
        {
            email = ContactMask.MaskEmail(email);
            mobile = ContactMask.MaskMobile(mobile);
        }

        var walletMap = await LoadMainWalletMoneyAsync(
            siteId,
            [(user.ID, user.Currency)],
            cancellationToken);
        return new UserMemberDetailDto
        {
            ID = user.ID,
            UserName = user.UserName,
            NickName = user.NickName,
            AccountName = user.AccountName,
            CreateAt = user.CreateAt,
            RegIP = user.RegIP,
            RegDomain = user.RegDomain,
            LoginAt = user.LoginAt,
            LoginIP = user.LoginIP,
            ActiveAt = user.ActiveAt,
            Lock = user.Lock,
            Function = user.Function,
            Currency = user.Currency,
            Money = walletMap.TryGetValue(user.ID, out var walletMoney) ? walletMoney : user.Money,
            AgentID = user.AgentID,
            InviteID = user.InviteID,
            GroupID = user.GroupID,
            Level = user.Level,
            InitLevel = user.InitLevel,
            Type = user.Type,
            Depth = user.Depth,
            Email = email,
            Mobile = mobile
        };
    }

    /// <summary>
    /// 设置/取消某一锁定位。对应旧版 /admin/user/member/info/updatelock。
    /// </summary>
    /// <param name="userId">会员编号</param>
    /// <param name="lockBit">锁定位数值（如 1=登录）</param>
    /// <param name="isLock">是否锁定</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>旧版返回格式</returns>
    public async Task<OldApiResult> UpdateLockAsync(
        int userId,
        int lockBit,
        bool isLock,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
            return OldApiResult.Fail("#未找到用户信息#");
        if (!AllowedLockBits.Contains(lockBit))
            return OldApiResult.Fail("#参数错误#");

        var siteId = _site.RequireSiteId();
        var denied = await EnsureAdminPermissionAsync(OtherFeaturePermissionId, cancellationToken);
        if (denied != null)
            return denied;

        var rows = await _db.Queryable<User>()
            .Where(u => u.ID == userId && u.SiteID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var user = rows.FirstOrDefault();
        if (user == null)
            return OldApiResult.Fail("#未找到用户信息#");

        var current = user.Lock;
        var hasBit = (current & lockBit) == lockBit;
        var next = current;
        var loginAt = user.LoginAt;
        if (isLock && !hasBit)
            next = current | lockBit;
        else if (!isLock && hasBit)
        {
            next = current ^ lockBit;
            if (lockBit == 1)
                loginAt = DateTime.Now;
        }
        else
            return OldApiResult.Fail("#没有状态变更#");

        await _db.Updateable<User>()
            .SetColumns(u => new User { Lock = next, LoginAt = loginAt })
            .Where(u => u.ID == userId && u.SiteID == siteId)
            .ExecuteCommandAsync(cancellationToken);

        return OldApiResult.Ok("#操作成功#");
    }

    /// <summary>
    /// 开启/关闭某一功能位。对应旧版 /admin/user/member/info/updatefunction。
    /// </summary>
    /// <param name="userId">会员编号</param>
    /// <param name="functionBit">功能位数值（如 64=关闭下级活动）</param>
    /// <param name="isOpen">是否打开该位</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>旧版返回格式</returns>
    public async Task<OldApiResult> UpdateFunctionAsync(
        int userId,
        int functionBit,
        bool isOpen,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
            return OldApiResult.Fail("#未找到用户信息#");
        if (!AllowedFunctionBits.Contains(functionBit))
            return OldApiResult.Fail("#参数错误#");

        var siteId = _site.RequireSiteId();
        var denied = await EnsureAdminPermissionAsync(OtherFeaturePermissionId, cancellationToken);
        if (denied != null)
            return denied;

        var rows = await _db.Queryable<User>()
            .Where(u => u.ID == userId && u.SiteID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var user = rows.FirstOrDefault();
        if (user == null)
            return OldApiResult.Fail("#未找到用户信息#");

        var current = user.Function;
        var hasBit = (current & functionBit) == functionBit;
        int next;
        if (isOpen && !hasBit)
            next = current | functionBit;
        else if (!isOpen && hasBit)
            next = current ^ functionBit;
        else
            return OldApiResult.Fail("#没有状态变更#");

        await _db.Updateable<User>()
            .SetColumns(u => new User { Function = next })
            .Where(u => u.ID == userId && u.SiteID == siteId)
            .ExecuteCommandAsync(cancellationToken);

        // 对齐旧 UpdateUserFunctionStatus：打开 CloseSub 时给下级加登录锁；关闭则去掉。
        if (functionBit == FunctionCloseSub)
            await CascadeCloseSubLoginLockAsync(siteId, userId, isOpen, cancellationToken);

        return OldApiResult.Ok("#操作成功#");
    }

    /// <summary>
    /// CloseSub 打开时给 usr_Depth 下级加登录锁；关闭时去掉登录锁。
    /// </summary>
    /// <param name="siteId">站点</param>
    /// <param name="parentUserId">上级会员</param>
    /// <param name="closeSubOpen">是否打开「禁止下级登录」</param>
    /// <param name="cancellationToken">取消令牌</param>
    private async Task CascadeCloseSubLoginLockAsync(
        int siteId,
        int parentUserId,
        bool closeSubOpen,
        CancellationToken cancellationToken)
    {
        var childIds = await _db.Queryable<UserDepth>()
            .Where(d => d.SiteID == siteId && d.UserID == parentUserId)
            .Select(d => d.ChildID)
            .ToListAsync(cancellationToken);
        if (childIds.Count == 0)
            return;

        var children = await _db.Queryable<User>()
            .Where(u => u.SiteID == siteId && childIds.Contains(u.ID))
            .ToListAsync(cancellationToken);
        foreach (var child in children)
        {
            var hasLoginLock = (child.Lock & LockLogin) == LockLogin;
            int nextLock;
            if (closeSubOpen && !hasLoginLock)
                nextLock = child.Lock | LockLogin;
            else if (!closeSubOpen && hasLoginLock)
                nextLock = child.Lock ^ LockLogin;
            else
                continue;

            await _db.Updateable<User>()
                .SetColumns(u => new User { Lock = nextLock })
                .Where(u => u.ID == child.ID && u.SiteID == siteId)
                .ExecuteCommandAsync(cancellationToken);
        }
    }

    /// <summary>
    /// 改会员状态字段。对应旧版 /admin/user/member/info/updatestatus：GroupID / Level / InitLevel。
    /// </summary>
    /// <param name="userId">会员编号</param>
    /// <param name="field">字段名：GroupID、Level、InitLevel</param>
    /// <param name="value">新值</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>旧版返回格式</returns>
    public async Task<OldApiResult> UpdateStatusAsync(
        int userId,
        string field,
        string value,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
            return OldApiResult.Fail("#未找到用户信息#");

        field = (field ?? string.Empty).Trim();
        if (string.Equals(field, "GroupID", StringComparison.OrdinalIgnoreCase))
            return await UpdateGroupAsync(userId, value, cancellationToken);
        if (string.Equals(field, "Level", StringComparison.OrdinalIgnoreCase))
            return await UpdateLevelAsync(userId, value, initLevel: false, cancellationToken);
        if (string.Equals(field, "InitLevel", StringComparison.OrdinalIgnoreCase))
            return await UpdateLevelAsync(userId, value, initLevel: true, cancellationToken);

        return OldApiResult.Fail("#暂不支持该字段#");
    }

    /// <summary>
    /// 重置会员资料字段。对齐旧版 ResetUserInfo：Mobile / Email / PayPassword / Password。
    /// </summary>
    /// <param name="userId">会员编号</param>
    /// <param name="field">字段：Mobile、Email、PayPassword、Password</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>旧版返回格式</returns>
    public async Task<OldApiResult> ResetAsync(
        int userId,
        string field,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
            return OldApiResult.Fail("#未找到用户信息#");

        var denied = await EnsureAdminPermissionAsync(OtherFeaturePermissionId, cancellationToken);
        if (denied != null)
            return denied;

        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<User>()
            .Where(u => u.ID == userId && u.SiteID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var user = rows.FirstOrDefault();
        if (user == null)
            return OldApiResult.Fail("#未找到用户信息#");

        field = (field ?? string.Empty).Trim();
        if (string.Equals(field, "Mobile", StringComparison.OrdinalIgnoreCase))
        {
            await _db.Updateable<User>()
                .SetColumns(u => new User { Mobile = string.Empty, IsMobile = false })
                .Where(u => u.ID == userId && u.SiteID == siteId)
                .ExecuteCommandAsync(cancellationToken);
            return OldApiResult.Ok("#操作成功#");
        }

        if (string.Equals(field, "Email", StringComparison.OrdinalIgnoreCase))
        {
            await _db.Updateable<User>()
                .SetColumns(u => new User { Email = string.Empty, IsEmail = false })
                .Where(u => u.ID == userId && u.SiteID == siteId)
                .ExecuteCommandAsync(cancellationToken);
            return OldApiResult.Ok("#操作成功#");
        }

        if (string.Equals(field, "PayPassword", StringComparison.OrdinalIgnoreCase))
        {
            await _db.Updateable<User>()
                .SetColumns(u => new User { PayPassword = string.Empty })
                .Where(u => u.ID == userId && u.SiteID == siteId)
                .ExecuteCommandAsync(cancellationToken);
            return OldApiResult.Ok("#操作成功#");
        }

        if (string.Equals(field, "Password", StringComparison.OrdinalIgnoreCase))
        {
            var sites = await _db.Queryable<Site>()
                .Where(s => s.ID == siteId)
                .Take(1)
                .ToListAsync(cancellationToken);
            var site = sites.FirstOrDefault();
            if (site == null)
                return OldApiResult.Fail("#未设置默认密码#");

            var map = SiteSettingQuery.Parse(site.SettingString ?? string.Empty);
            var defaultPassword = SiteSettingQuery.Get(map, "Password");
            if (string.IsNullOrEmpty(defaultPassword))
                return OldApiResult.Fail("#未设置默认密码#");

            var hash = Md5PasswordHasher.Hash(defaultPassword);
            await _db.Updateable<User>()
                .SetColumns(u => new User
                {
                    Password = hash,
                    Is_Temporary_Password = true
                })
                .Where(u => u.ID == userId && u.SiteID == siteId)
                .ExecuteCommandAsync(cancellationToken);
            return OldApiResult.Ok("#操作成功#");
        }

        return OldApiResult.Fail("#暂不支持该字段#");
    }

    /// <summary>
    /// 改会员分组。
    /// </summary>
    private async Task<OldApiResult> UpdateGroupAsync(
        int userId,
        string value,
        CancellationToken cancellationToken)
    {
        if (!int.TryParse((value ?? string.Empty).Trim(), out var newGroupId) || newGroupId <= 0)
            return OldApiResult.Fail("#分组编号错误#");

        var siteId = _site.RequireSiteId();
        var denied = await EnsureAdminPermissionAsync(UserGroupService.GroupEditPermissionId, cancellationToken);
        if (denied != null)
            return denied;

        var rows = await _db.Queryable<User>()
            .Where(u => u.ID == userId && u.SiteID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var user = rows.FirstOrDefault();
        if (user == null)
            return OldApiResult.Fail("#未找到用户信息#");

        var groups = await _db.Queryable<UserGroup>()
            .Where(g => g.ID == newGroupId && g.SiteID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        if (groups.Count == 0)
            return OldApiResult.Fail("#分组编号错误#");

        var oldGroupId = user.GroupID;
        if (oldGroupId == newGroupId)
            return OldApiResult.Fail("#没有状态变更#");

        await _db.Ado.BeginTranAsync();
        try
        {
            await _db.Updateable<User>()
                .SetColumns(u => new User { GroupID = newGroupId, OldGroupID = oldGroupId })
                .Where(u => u.ID == userId && u.SiteID == siteId)
                .ExecuteCommandAsync(cancellationToken);

            if (oldGroupId > 0)
            {
                await _db.Updateable<UserGroup>()
                    .SetColumns(g => g.Member == g.Member - 1)
                    .Where(g => g.ID == oldGroupId && g.SiteID == siteId && g.Member > 0)
                    .ExecuteCommandAsync(cancellationToken);
            }

            await _db.Updateable<UserGroup>()
                .SetColumns(g => g.Member == g.Member + 1)
                .Where(g => g.ID == newGroupId && g.SiteID == siteId)
                .ExecuteCommandAsync(cancellationToken);

            await _db.Ado.CommitTranAsync();
        }
        catch
        {
            await _db.Ado.RollbackTranAsync();
            throw;
        }

        return OldApiResult.Ok("#操作成功#");
    }

    /// <summary>
    /// 改会员等级或初始等级；初始等级要求尚无投注记录。
    /// </summary>
    private async Task<OldApiResult> UpdateLevelAsync(
        int userId,
        string value,
        bool initLevel,
        CancellationToken cancellationToken)
    {
        if (!byte.TryParse((value ?? string.Empty).Trim(), out var newLevel))
            return OldApiResult.Fail("#参数错误#");

        var siteId = _site.RequireSiteId();
        var denied = await EnsureAdminPermissionAsync(ModifyLevelPermissionId, cancellationToken);
        if (denied != null)
            return denied;

        var rows = await _db.Queryable<User>()
            .Where(u => u.ID == userId && u.SiteID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var user = rows.FirstOrDefault();
        if (user == null)
            return OldApiResult.Fail("#未找到用户信息#");

        if (initLevel)
        {
            var betMoney = await _db.Queryable<UserDateGame>()
                .Where(g => g.SiteID == siteId && g.UserID == userId)
                .SumAsync(g => g.BetMoney);
            if (betMoney > 0)
                return OldApiResult.Fail("#新会员才可以操作#");

            if (user.InitLevel == newLevel)
                return OldApiResult.Fail("#没有状态变更#");

            await _db.Updateable<User>()
                .SetColumns(u => new User { InitLevel = newLevel })
                .Where(u => u.ID == userId && u.SiteID == siteId)
                .ExecuteCommandAsync(cancellationToken);

            return OldApiResult.Ok("#操作成功#");
        }

        var oldLevel = user.Level;
        if (oldLevel == newLevel)
            return OldApiResult.Fail("#没有状态变更#");

        await _db.Updateable<User>()
            .SetColumns(u => new User { Level = newLevel })
            .Where(u => u.ID == userId && u.SiteID == siteId)
            .ExecuteCommandAsync(cancellationToken);

        await _db.Insertable(new SiteVipUpGradeLog
        {
            SiteID = siteId,
            UserID = userId,
            UserName = user.UserName ?? string.Empty,
            OldLevel = oldLevel,
            NewLevel = newLevel,
            Type = newLevel > oldLevel ? 1 : 2,
            Status = true,
            Remark = string.Empty,
            CreateAt = DateTime.Now
        }).ExecuteCommandAsync(cancellationToken);

        return OldApiResult.Ok("#操作成功#");
    }

    /// <summary>
    /// 当前管理员是否可看联系方式。
    /// 先认 OldApi 站点管理员；没有会话再认 JWT 菜单按钮码。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>有权限为 true</returns>
    private async Task<bool> CanViewContactAsync(CancellationToken cancellationToken)
    {
        var admin = await _adminAuth.TryGetCurrentAdminAsync(cancellationToken);
        if (admin != null)
        {
            if (string.Equals(admin.AdminName, "admin", StringComparison.OrdinalIgnoreCase) || admin.GroupID == 0)
                return true;

            var csv = await LoadGroupPermissionCsvAsync(admin, cancellationToken);
            return OldApiPermissionEvaluator.SiteAdminHasAll(
                admin.AdminName,
                admin.GroupID,
                csv,
                [ViewContactPermission]);
        }

        return await _permissionChecker.IsGrantedAsync(ViewContactPermission);
    }

    /// <summary>
    /// 当前管理员是否具备指定权限；否则返回拒绝结果。
    /// 先认 OldApi 站点管理员；没有会话再认 JWT 菜单按钮码。
    /// </summary>
    private async Task<OldApiResult> EnsureAdminPermissionAsync(
        string permissionId,
        CancellationToken cancellationToken)
    {
        var admin = await _adminAuth.TryGetCurrentAdminAsync(cancellationToken);
        if (admin != null)
        {
            var csv = await LoadGroupPermissionCsvAsync(admin, cancellationToken);
            if (!OldApiPermissionEvaluator.SiteAdminHasAll(admin.AdminName, admin.GroupID, csv, [permissionId]))
                return OldApiResult.NeedPermission(
                    OldApiPermissionEvaluator.BuildPermissionDeniedMessage(permissionId));
            return null;
        }

        if (await _permissionChecker.IsGrantedAsync(permissionId))
            return null;

        return OldApiResult.NeedPermission(
            OldApiPermissionEvaluator.BuildPermissionDeniedMessage(permissionId));
    }

    /// <summary>
    /// 读取管理员分组权限串。
    /// </summary>
    private async Task<string> LoadGroupPermissionCsvAsync(SiteAdmin admin, CancellationToken cancellationToken)
    {
        if (string.Equals(admin.AdminName, "admin", StringComparison.OrdinalIgnoreCase) || admin.GroupID == 0)
            return string.Empty;

        var groups = await _db.Queryable<SiteAdminGroup>()
            .Where(g => g.SiteID == admin.SiteID && g.ID == admin.GroupID)
            .Take(1)
            .ToListAsync(cancellationToken);
        return groups.FirstOrDefault()?.PermissionString ?? string.Empty;
    }
}
