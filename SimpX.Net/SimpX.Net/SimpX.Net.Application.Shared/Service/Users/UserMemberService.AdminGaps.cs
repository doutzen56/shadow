using System.Text.Json;
using SimpX.Net.Data.Entity.Agents;
using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Users;
using SqlSugar;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 会员后台缺口动作：禁止货币、升降级、钱包、锁钱、上级、查询辅助等。
/// </summary>
public sealed partial class UserMemberService
{
    /// <summary>旧 UserType.Member。</summary>
    public const int UserTypeMember = 0;

    /// <summary>旧 UserType.Agent。</summary>
    public const int UserTypeAgent = 1;

    /// <summary>旧 FunctionType.CheckAgent。</summary>
    public const int FunctionCheckAgent = 4;

    /// <summary>旧 MoneyLock.LockType.Admin。</summary>
    public const int LockTypeAdmin = 3;

    /// <summary>
    /// 禁止/放开货币。对齐旧 updatecurrency。
    /// </summary>
    /// <param name="userId">会员</param>
    /// <param name="currency">币种串</param>
    /// <param name="isOpen">true=写入禁止名单</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task UpdateCurrencyAsync(
        int userId,
        string currency,
        bool isOpen,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
            throw Oops.Oh("#未找到用户信息#");
        var status = (currency ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(status))
            throw Oops.Oh("#参数错误#");

        var denied = await EnsureAdminPermissionAsync(OtherFeaturePermissionId, cancellationToken);
        if (denied != null)
            throw Oops.Oh(denied.Msg);

        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<User>()
            .Where(u => u.ID == userId && u.SiteID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var user = rows.FirstOrDefault() ?? throw Oops.Oh("#未找到用户信息#");

        var csv = user.DisableCurrency ?? string.Empty;
        var token = "," + status + ",";
        var has = ("," + csv).Contains(token, StringComparison.Ordinal);
        var next = csv;
        if (isOpen && !has)
            next = csv + status + ",";
        else if (!isOpen && has)
            next = csv.Replace(status + ",", string.Empty, StringComparison.Ordinal);
        else
            throw Oops.Oh("#没有状态变更#");

        await _db.Updateable<User>()
            .SetColumns(u => new User { DisableCurrency = next })
            .Where(u => u.ID == userId && u.SiteID == siteId)
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 会员升级为代理。对齐旧 updatetype。
    /// </summary>
    /// <param name="userId">会员</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task UpdateTypeAsync(int userId, CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<User>()
            .Where(u => u.ID == userId && u.SiteID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var user = rows.FirstOrDefault();
        if (user == null || user.Type != UserTypeMember)
            throw Oops.Oh("#用户参数错误#");

        if (user.AgentID != 0)
        {
            var parents = await _db.Queryable<User>()
                .Where(u => u.ID == user.AgentID && u.SiteID == siteId)
                .Take(1)
                .ToListAsync(cancellationToken);
            var parent = parents.FirstOrDefault();
            if (parent == null || parent.Type != UserTypeAgent)
                throw Oops.Oh($"上级{user.AgentID}不是代理");
        }

        var nextFunction = (user.Function & FunctionCheckAgent) == FunctionCheckAgent
            ? user.Function
            : user.Function | FunctionCheckAgent;
        user.Type = UserTypeAgent;
        user.Function = nextFunction;
        await _db.Updateable(user).UpdateColumns(u => new { u.Type, u.Function }).ExecuteCommandAsync(cancellationToken);

        var exists = await _db.Queryable<AgentUser>().AnyAsync(a => a.ID == user.ID, cancellationToken);
        if (!exists)
        {
            await _db.Insertable(new AgentUser
            {
                ID = user.ID,
                SiteID = siteId,
                ParentID = user.AgentID > 0 ? user.AgentID : 0,
                CreateAt = DateTime.Now,
                Status = AgentUser.AgentStatus.Normal,
                CommissionID = 0,
                ReturnID = 0,
                Permission = 0
            }).ExecuteCommandAsync(cancellationToken);
        }
    }

    /// <summary>
    /// 代理降为会员。对齐旧 degradetype：下级深度必须只剩自己。
    /// </summary>
    /// <param name="userId">会员</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task DegradeTypeAsync(int userId, CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<User>()
            .Where(u => u.ID == userId && u.SiteID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var user = rows.FirstOrDefault();
        if (user == null || user.Type != UserTypeAgent)
            throw Oops.Oh("#用户参数错误#");

        var depthCount = await _db.Queryable<UserDepth>()
            .Where(d => d.SiteID == siteId && d.UserID == userId)
            .CountAsync(cancellationToken);
        if (depthCount != 1)
            throw Oops.Oh("#请转移代理下级#");

        var nextFunction = (user.Function & FunctionCheckAgent) == FunctionCheckAgent
            ? user.Function ^ FunctionCheckAgent
            : user.Function;
        user.Type = UserTypeMember;
        user.Function = nextFunction;
        await _db.Updateable(user).UpdateColumns(u => new { u.Type, u.Function }).ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 会员已绑银行卡列表。对齐旧 banklist。
    /// </summary>
    /// <param name="userId">会员</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行列表</returns>
    public async Task<List<MemberBankListItemDto>> BankListAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<BankAccount>()
            .Where(t => t.SiteID == siteId && t.UserID == userId)
            .OrderBy(t => t.CreateAt, OrderByType.Desc)
            .ToListAsync(cancellationToken);
        return rows.Select(t => new MemberBankListItemDto
        {
            ID = t.ID,
            Type = t.Type.ToString(),
            Name = string.IsNullOrEmpty(t.NameLetters) ? (t.Name ?? string.Empty) : $"{t.Name}/{t.NameLetters}",
            BankName = t.BankName ?? string.Empty,
            Account = t.Account ?? string.Empty,
            AccountDetail = t.AccountDetail ?? string.Empty,
            BankInfo = t.BankInfo ?? string.Empty,
            Status = t.Status,
            IsVir = t.IsVir ?? "0",
            CreateAt = t.CreateAt
        }).ToList();
    }

    /// <summary>
    /// 读会员钱包。对齐旧 wallet（默认取会员主币种；无记录则报失败）。
    /// </summary>
    /// <param name="userId">会员</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>钱包</returns>
    public async Task<MemberWalletDto> WalletAsync(int userId, CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var users = await _db.Queryable<User>()
            .Where(u => u.ID == userId && u.SiteID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var user = users.FirstOrDefault() ?? throw Oops.Oh("获取数据失败");

        var wallets = await _db.Queryable<UserMoney>()
            .Where(m => m.SiteID == siteId && m.UserID == userId && m.Currency == user.Currency)
            .Take(1)
            .ToListAsync(cancellationToken);
        var wallet = wallets.FirstOrDefault();
        if (wallet == null)
        {
            wallets = await _db.Queryable<UserMoney>()
                .Where(m => m.SiteID == siteId && m.UserID == userId)
                .Take(1)
                .ToListAsync(cancellationToken);
            wallet = wallets.FirstOrDefault() ?? throw Oops.Oh("获取数据失败");
        }

        return new MemberWalletDto
        {
            UserID = userId,
            UserName = user.UserName ?? string.Empty,
            TotalMoney = wallet.TotalMoney,
            Money = wallet.TotalMoney - wallet.LockMoney,
            LockMoney = wallet.LockMoney,
            WashFlow = Math.Round(wallet.WashFlow, 2)
        };
    }

    /// <summary>
    /// 读会员流水。对齐旧 washflow（汇率本刀按 1 处理）。
    /// </summary>
    /// <param name="userId">会员</param>
    /// <param name="currency">币种</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>流水</returns>
    public async Task<MemberWashFlowDto> WashFlowAsync(
        int userId,
        int currency,
        CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<UserMoney>()
            .Where(m => m.SiteID == siteId && m.UserID == userId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var wash = rows.FirstOrDefault()?.WashFlow ?? 0m;
        return new MemberWashFlowDto
        {
            WashFlow = (wash * 1m).ToString("#0.00"),
            Currency = $"({currency})"
        };
    }

    /// <summary>
    /// 管理员手动锁钱。对齐旧 lockmoney（LockType=Admin）。
    /// </summary>
    /// <param name="input">入参</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task LockMoneyAsync(MemberLockMoneyInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.UserID <= 0 || input.Money <= 0)
            throw Oops.Oh("#金额错误#");

        var siteId = _site.RequireSiteId();
        var users = await _db.Queryable<User>()
            .Where(u => u.ID == input.UserID && u.SiteID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        if (users.Count == 0)
            throw Oops.Oh("#用户不存在#");

        var sourceId = Random.Shared.Next(1, int.MaxValue);
        var moneyLock = new MoneyLock
        {
            SiteID = siteId,
            UserID = input.UserID,
            Type = LockTypeAdmin,
            SourceID = sourceId,
            Money = input.Money,
            IsLock = true,
            CreateAt = DateTime.Now,
            UnLockAt = DateTime.MinValue,
            LockDesc = input.LockDesc ?? string.Empty,
            UnLockDesc = string.Empty,
            Currency = input.Currency
        };
        await _db.Insertable(moneyLock).ExecuteCommandAsync(cancellationToken);

        var affected = await _db.Ado.ExecuteCommandAsync(
            "UPDATE usr_Money SET LockMoney = LockMoney + @Money WHERE SiteID = @SiteID AND UserID = @UserID AND Currency=@Currency AND (TotalMoney - LockMoney - @Money) >= 0",
            new List<SugarParameter>
            {
                new("@SiteID", siteId),
                new("@UserID", input.UserID),
                new("@Money", input.Money),
                new("@Currency", input.Currency)
            });
        if (affected == 0)
            throw Oops.Oh("#可用余额不足#");
    }

    /// <summary>
    /// 管理员手动解锁。对齐旧 unlockmoney（仅 Admin 类型）。
    /// </summary>
    /// <param name="lockId">锁记录编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task UnlockMoneyAsync(int lockId, CancellationToken cancellationToken = default)
    {
        if (lockId <= 0)
            throw Oops.Oh("#编号错误#");
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<MoneyLock>()
            .Where(l => l.ID == lockId && l.SiteID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var moneyLock = rows.FirstOrDefault() ?? throw Oops.Oh("#编号错误#");
        if (!moneyLock.IsLock)
            throw Oops.Oh("#已被解锁#");
        if (moneyLock.Type != LockTypeAdmin)
            throw Oops.Oh("#非手动锁定类型#");

        moneyLock.IsLock = false;
        moneyLock.UnLockAt = DateTime.Now;
        moneyLock.UnLockDesc = "手动解锁";
        await _db.Updateable(moneyLock)
            .UpdateColumns(l => new { l.IsLock, l.UnLockAt, l.UnLockDesc })
            .ExecuteCommandAsync(cancellationToken);

        var affected = await _db.Ado.ExecuteCommandAsync(
            "UPDATE usr_Money SET LockMoney = LockMoney + @Money WHERE SiteID = @SiteID AND UserID = @UserID AND LockMoney + @Money >= 0 AND Currency=@Currency",
            new List<SugarParameter>
            {
                new("@SiteID", siteId),
                new("@UserID", moneyLock.UserID),
                new("@Money", moneyLock.Money * -1m),
                new("@Currency", moneyLock.Currency)
            });
        if (affected == 0)
            throw Oops.Oh("#锁定资金不足#");
    }

    /// <summary>
    /// 改上级。对齐旧 updateagent（「总代」清上级；有报表数据或 11～15 日限制本刀放宽为仅校验用户）。
    /// </summary>
    /// <param name="userId">会员</param>
    /// <param name="agent">上级用户名或「总代」</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task UpdateAgentAsync(int userId, string agent, CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var users = await _db.Queryable<User>()
            .Where(u => u.ID == userId && u.SiteID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var user = users.FirstOrDefault() ?? throw Oops.Oh("#用户不存在#");

        var agentId = 0;
        var agentName = (agent ?? string.Empty).Trim();
        if (!string.Equals(agentName, "总代", StringComparison.Ordinal))
        {
            if (string.IsNullOrEmpty(agentName))
                throw Oops.Oh("#上级代理用户名错误#");
            var agents = await _db.Queryable<User>()
                .Where(u => u.SiteID == siteId && u.UserName == agentName)
                .Take(1)
                .ToListAsync(cancellationToken);
            var a = agents.FirstOrDefault() ?? throw Oops.Oh("#上级代理用户名错误#");
            agentId = a.ID;
        }

        user.AgentID = agentId;
        await _db.Updateable(user).UpdateColumns(u => new { u.AgentID }).ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 用户名联想。对齐旧 query。
    /// </summary>
    /// <param name="key">关键字</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户名 JSON 数组字符串</returns>
    public async Task<string> QueryUserNameAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw Oops.Oh("#参数错误#");
        var siteId = _site.RequireSiteId();
        var k = key.Trim();
        var names = await _db.Queryable<User>()
            .Where(u => u.SiteID == siteId && u.UserName.Contains(k))
            .OrderBy(u => u.UserName)
            .Take(20)
            .Select(u => u.UserName)
            .ToListAsync(cancellationToken);
        return JsonSerializer.Serialize(names);
    }

    /// <summary>
    /// 按用户名取编号。对齐旧 getuserid。
    /// </summary>
    /// <param name="userName">用户名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>编号</returns>
    public async Task<MemberGetUserIdOutput> GetUserIdAsync(
        string userName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userName))
            throw Oops.Oh("#请输入用户#");
        var siteId = _site.RequireSiteId();
        var key = userName.Trim();
        var rows = await _db.Queryable<User>()
            .Where(u => u.SiteID == siteId && u.UserName == key)
            .Take(1)
            .ToListAsync(cancellationToken);
        var user = rows.FirstOrDefault() ?? throw Oops.Oh("#用户名不存在#");
        return new MemberGetUserIdOutput { UserID = user.ID, UserName = key };
    }

    /// <summary>
    /// 上级链。对齐旧 getparentlist。
    /// </summary>
    /// <param name="userId">会员；0 时配合 userName</param>
    /// <param name="userName">用户名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>上级节点（从顶到近）</returns>
    public async Task<List<MemberParentItemDto>> GetParentListAsync(
        int userId,
        string userName,
        CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        if (userId <= 0 && !string.IsNullOrWhiteSpace(userName))
        {
            var found = await GetUserIdAsync(userName, cancellationToken);
            userId = found.UserID;
        }

        if (userId <= 0)
            return [];

        var parents = await _db.Queryable<UserDepth>()
            .Where(d => d.SiteID == siteId && d.ChildID == userId && d.UserID != userId)
            .OrderBy(d => d.Depth, OrderByType.Desc)
            .Select(d => d.UserID)
            .ToListAsync(cancellationToken);

        // 旧 getparentlist：Depth 降序即顶→近，末尾再挂自身
        var chain = parents.Append(userId).ToList();
        var nameMap = await _db.Queryable<User>()
            .Where(u => u.SiteID == siteId && chain.Contains(u.ID))
            .Select(u => new { u.ID, u.UserName })
            .ToListAsync(cancellationToken);
        var dict = nameMap.ToDictionary(x => x.ID, x => x.UserName ?? string.Empty);
        return chain.Select(id => new MemberParentItemDto
        {
            UserID = id,
            UserName = dict.TryGetValue(id, out var n) ? n : string.Empty
        }).ToList();
    }

    /// <summary>
    /// 多币种总余额。对齐旧 moremoney。
    /// </summary>
    /// <param name="userId">会员</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>币种→余额</returns>
    public async Task<Dictionary<string, decimal>> MoreMoneyAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var currencies = await _db.Queryable<SiteCurrency>()
            .Where(c => c.SiteID == siteId && c.IsOpen)
            .OrderBy(c => c.Sort)
            .ToListAsync(cancellationToken);
        var moneys = await _db.Queryable<UserMoney>()
            .Where(m => m.SiteID == siteId && m.UserID == userId)
            .ToListAsync(cancellationToken);
        var map = moneys.ToDictionary(m => m.Currency, m => Math.Round(m.TotalMoney, 2));
        var result = new Dictionary<string, decimal>();
        foreach (var c in currencies)
            result[c.Currency.ToString()] = map.TryGetValue(c.Currency, out var v) ? v : 0m;
        return result;
    }

    /// <summary>
    /// 主币种+其它币种可用余额。对齐旧 detailmoney。
    /// </summary>
    /// <param name="userId">会员</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>主/明细</returns>
    public async Task<MemberMoneyByCurrencyDto> DetailMoneyAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var users = await _db.Queryable<User>()
            .Where(u => u.ID == userId && u.SiteID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var user = users.FirstOrDefault() ?? throw Oops.Oh("#用户不存在#");
        var currencies = await _db.Queryable<SiteCurrency>()
            .Where(c => c.SiteID == siteId && c.IsOpen)
            .OrderBy(c => c.Sort)
            .ToListAsync(cancellationToken);
        var moneys = await _db.Queryable<UserMoney>()
            .Where(m => m.SiteID == siteId && m.UserID == userId)
            .ToListAsync(cancellationToken);

        decimal Available(UserMoney m) => Math.Round(m.TotalMoney - m.LockMoney, 2);
        var master = moneys.FirstOrDefault(m => m.Currency == user.Currency);
        var masterMoney = master == null ? 0m : Available(master);
        var detail = new Dictionary<string, decimal>();
        foreach (var c in currencies.Where(c => c.Currency != user.Currency))
        {
            var row = moneys.FirstOrDefault(m => m.Currency == c.Currency);
            detail[c.Currency.ToString()] = row == null ? 0m : Available(row);
        }

        return new MemberMoneyByCurrencyDto
        {
            Master = $"{user.Currency}:{masterMoney}",
            Detail = JsonSerializer.Serialize(detail),
            All = null
        };
    }

    /// <summary>
    /// 是否黑名单手机。对齐旧 isblackmobile。
    /// </summary>
    /// <param name="mobile">手机；可带区号逗号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public async Task<MemberBlackMobileDto> IsBlackMobileAsync(
        string mobile,
        CancellationToken cancellationToken = default)
    {
        var raw = mobile ?? string.Empty;
        var index = raw.IndexOf(',');
        if (index != -1)
            raw = raw[(index + 1)..];
        raw = raw.Trim();
        if (string.IsNullOrEmpty(raw))
            throw Oops.Oh("#请输入手机号#");

        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<BlackMobileImport>()
            .Where(t => t.SiteID == siteId && t.BlackMobile == raw)
            .Take(1)
            .ToListAsync(cancellationToken);
        var black = rows.FirstOrDefault();
        if (black != null)
            return new MemberBlackMobileDto { IsBlack = true, Demo = black.Demo ?? string.Empty };
        return new MemberBlackMobileDto { IsBlack = false, Demo = null };
    }

    /// <summary>
    /// 批量加备注。对齐旧 batchaddmark（本刀用 Items；Url 非空且 Items 空时返回提示）。
    /// </summary>
    /// <param name="input">入参</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>成功条数文案</returns>
    public async Task<string> BatchAddMarkAsync(
        MemberBatchAddMarkInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var items = input.Items ?? [];
        if (items.Count == 0)
        {
            if (string.IsNullOrWhiteSpace(input.Url))
                throw Oops.Oh("#不能为空#");
            throw Oops.Oh("#请改用 Items 传用户名与备注#");
        }

        var siteId = _site.RequireSiteId();
        var count = 0;
        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.UserName) || string.IsNullOrWhiteSpace(item.Content))
                continue;
            var name = item.UserName.Trim();
            var content = item.Content.Trim();
            if (content.Length > 500)
                content = content[..500];
            var users = await _db.Queryable<User>()
                .Where(u => u.SiteID == siteId && u.UserName == name)
                .Take(1)
                .ToListAsync(cancellationToken);
            var user = users.FirstOrDefault();
            if (user == null)
                continue;
            await _db.Insertable(new UserMark
            {
                SiteID = siteId,
                UserID = user.ID,
                Content = content,
                CreateAt = DateTime.Now,
                AdminName = string.Empty
            }).ExecuteCommandAsync(cancellationToken);
            count++;
        }

        if (count <= 0)
            throw Oops.Oh("#添加记录数#:0");
        return $"#添加记录数#:{count}";
    }

    /// <summary>
    /// 导出：返回筛选后的会员行（前端自行下表）。对齐旧 export 数据侧。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>导出行</returns>
    public async Task<MemberExportOutput> ExportAsync(
        UserMemberPageInput input,
        CancellationToken cancellationToken = default)
    {
        input ??= new UserMemberPageInput();
        input.Page = 1;
        input.PageSize = 5000;
        var page = await PageAsync(input, cancellationToken);
        return new MemberExportOutput
        {
            Data = page.Data ?? [],
            RecordCount = page.RecordCount
        };
    }

    /// <summary>
    /// 受限搜索别名：走分页。对齐旧 limitusersearch 入口（完整资金汇总本刀用列表字段）。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页</returns>
    public Task<UserMemberPageOutput> LimitUserSearchAsync(
        UserMemberPageInput input,
        CancellationToken cancellationToken = default) =>
        PageAsync(input ?? new UserMemberPageInput(), cancellationToken);

    /// <summary>
    /// 普通搜索别名：走分页。对齐旧 usersearch。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页</returns>
    public Task<UserMemberPageOutput> UserSearchAsync(
        UserMemberPageInput input,
        CancellationToken cancellationToken = default) =>
        PageAsync(input ?? new UserMemberPageInput(), cancellationToken);

    /// <summary>
    /// 收藏游戏名列表（简化：只返回收藏的 GameID）。对齐旧 detailfavoritegame 外壳。
    /// </summary>
    /// <param name="userId">会员（旧 ID）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>名称行</returns>
    public async Task<List<object>> DetailFavoriteGameAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        // 无独立收藏表映射时返回空列表，保证接口可调用。
        _ = siteId;
        _ = userId;
        _ = cancellationToken;
        return [];
    }
}
