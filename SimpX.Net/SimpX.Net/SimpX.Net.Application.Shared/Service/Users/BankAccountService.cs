using SimpX.Net.Data.Entity.Systems;
using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 租户后台银行信息。对齐旧 /admin/user/member/bank/*（列表/详情/审核/删/新增）。
/// </summary>
public sealed partial class BankAccountService : ITransient
{
    /// <summary>旧 CheckStatus.None。</summary>
    public const int StatusNone = 0;

    /// <summary>旧 CheckStatus.Faild。</summary>
    public const int StatusFailed = 1;

    /// <summary>旧 CheckStatus.Success。</summary>
    public const int StatusSuccess = 2;

    /// <summary>旧 WithdrawStatus.Faild。</summary>
    private const int WithdrawFailed = 1;

    /// <summary>旧 WithdrawStatus.Finish。</summary>
    private const int WithdrawFinished = 3;

    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;
    private readonly UserManager _userManager;

    /// <summary>
    /// 注入业务库、当前站与后台当前用户。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站</param>
    /// <param name="userManager">后台 JWT 用户</param>
    public BankAccountService(IBizDbAccessor biz, ICurrentSiteAccessor site, UserManager userManager)
    {
        _db = biz.Db;
        _site = site;
        _userManager = userManager;
    }

    /// <summary>
    /// 当前站银行帐户分页。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页</returns>
    public async Task<BankAccountAdminPageOutput> PageAsync(
        BankAccountAdminPageInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var siteId = _site.RequireSiteId();
        var pageSize = Math.Clamp(input.PageSize <= 0 ? 20 : input.PageSize, 1, 100);
        var page = Math.Max(1, input.Page <= 0 ? 1 : input.Page);

        var query = _db.Queryable<BankAccount>().Where(t => t.SiteID == siteId);

        if (!string.IsNullOrWhiteSpace(input.StartAt) && DateTime.TryParse(input.StartAt.Trim(), out var startAt))
            query = query.Where(t => t.CreateAt >= startAt);
        if (!string.IsNullOrWhiteSpace(input.EndAt) && DateTime.TryParse(input.EndAt.Trim(), out var endAt))
            query = query.Where(t => t.CreateAt <= endAt);
        if (!string.IsNullOrWhiteSpace(input.UserNumber))
            query = query.Where(t => t.Account != null && t.Account.Contains(input.UserNumber.Trim()));
        if (OldApiMoneyDisplay.TryParseCheckStatus(input.Status, out var status))
            query = query.Where(t => t.Status == status);
        if (!string.IsNullOrWhiteSpace(input.IsVir))
            query = query.Where(t => t.IsVir == input.IsVir.Trim());

        if (!string.IsNullOrWhiteSpace(input.UserName))
        {
            var userKey = input.UserName.Trim();
            List<int> ids;
            if (int.TryParse(userKey, out var uidExact))
                ids = [uidExact];
            else
                ids = await _db.Queryable<User>()
                    .Where(u => u.SiteID == siteId && u.UserName.Contains(userKey))
                    .Select(u => u.ID)
                    .Take(500)
                    .ToListAsync(cancellationToken);
            if (ids.Count == 0)
                return new BankAccountAdminPageOutput { Items = [], Total = 0 };
            query = query.Where(t => ids.Contains(t.UserID));
        }

        if (!string.IsNullOrWhiteSpace(input.Currency) && int.TryParse(input.Currency.Trim(), out var currencyFilter))
        {
            var currencyUserIds = await _db.Queryable<User>()
                .Where(u => u.SiteID == siteId && u.Currency == currencyFilter)
                .Select(u => u.ID)
                .Take(5000)
                .ToListAsync(cancellationToken);
            if (currencyUserIds.Count == 0)
                return new BankAccountAdminPageOutput { Items = [], Total = 0 };
            query = query.Where(t => currencyUserIds.Contains(t.UserID));
        }

        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderBy(t => t.CreateAt, OrderByType.Desc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var userIds = rows.Select(r => r.UserID).Distinct().ToList();
        var nameMap = new Dictionary<int, string>();
        var currencyMap = new Dictionary<int, int>();
        if (userIds.Count > 0)
        {
            var users = await _db.Queryable<User>()
                .Where(u => u.SiteID == siteId && userIds.Contains(u.ID))
                .Select(u => new { u.ID, u.UserName, u.Currency })
                .ToListAsync(cancellationToken);
            foreach (var u in users)
            {
                nameMap[u.ID] = u.UserName ?? string.Empty;
                currencyMap[u.ID] = u.Currency;
            }
        }

        return new BankAccountAdminPageOutput
        {
            Total = total,
            Items = rows.Select(t => new BankAccountAdminListItemDto
            {
                ID = t.ID,
                UserID = t.UserID,
                UserName = nameMap.TryGetValue(t.UserID, out var n) ? n : string.Empty,
                Name = t.Name ?? string.Empty,
                Account = "'" + (t.Account ?? string.Empty),
                AccountDetail = t.AccountDetail ?? string.Empty,
                BankName = string.IsNullOrWhiteSpace(t.BankName) ? t.Type.ToString() : t.BankName,
                Type = t.Type,
                IsVir = (t.IsVir ?? "0").Trim(),
                Status = t.Status,
                Remark = t.Remark ?? string.Empty,
                CreateAt = t.CreateAt,
                CheckDateTime = t.CheckDateTime,
                Currency = currencyMap.TryGetValue(t.UserID, out var c) ? c : 0
            }).ToList()
        };
    }

    /// <summary>
    /// 当前站银行帐户详情。
    /// </summary>
    /// <param name="id">帐户编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>详情</returns>
    public async Task<BankAccountAdminDetailDto> DetailAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw Oops.Oh("#参数错误#");
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<BankAccount>()
            .Where(t => t.SiteID == siteId && t.ID == id)
            .Take(1)
            .ToListAsync(cancellationToken);
        var model = rows.FirstOrDefault() ?? throw Oops.Oh("#记录不存在#");
        return new BankAccountAdminDetailDto
        {
            ID = model.ID,
            Account = model.Account ?? string.Empty,
            Name = model.Name ?? string.Empty,
            BankName = string.IsNullOrWhiteSpace(model.BankName) ? model.Type.ToString() : model.BankName,
            AccountDetail = model.AccountDetail ?? string.Empty,
            Status = model.Status,
            Remark = model.Remark ?? string.Empty,
            BankInfo = model.BankInfo ?? string.Empty
        };
    }

    /// <summary>
    /// 按用户名确认会员是否存在（新增前查询）。
    /// </summary>
    /// <param name="userName">用户名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>会员编号与开户名</returns>
    public async Task<BankAccountVerifyUserOutput> VerifyUserAsync(
        string userName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userName))
            throw Oops.Oh("#用户不存在#");
        var siteId = _site.RequireSiteId();
        var key = userName.Trim();
        var rows = await _db.Queryable<User>()
            .Where(u => u.SiteID == siteId && u.UserName == key)
            .Select(u => new { u.ID, u.AccountName })
            .Take(1)
            .ToListAsync(cancellationToken);
        var user = rows.FirstOrDefault() ?? throw Oops.Oh("#用户不存在#");
        return new BankAccountVerifyUserOutput
        {
            Id = user.ID,
            AccountName = user.AccountName ?? string.Empty
        };
    }

    /// <summary>
    /// 审核银行帐户状态与备注。
    /// </summary>
    /// <param name="input">编号、状态、备注</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task UpdateStatusAsync(
        BankAccountUpdateStatusInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.ID <= 0)
            throw Oops.Oh("#参数错误#");
        if (input.Status is not (StatusNone or StatusFailed or StatusSuccess))
            throw Oops.Oh("#参数错误#");

        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<BankAccount>()
            .Where(t => t.SiteID == siteId && t.ID == input.ID)
            .Take(1)
            .ToListAsync(cancellationToken);
        var bank = rows.FirstOrDefault() ?? throw Oops.Oh("#记录不存在#");

        bank.Status = input.Status;
        bank.Remark = input.Remark ?? string.Empty;
        bank.UpdateAt = DateTime.Now;
        if (input.Status != StatusNone)
        {
            bank.CheckDateTime = DateTime.Now;
            bank.Checker = _userManager.UserId > 0 ? (int)_userManager.UserId : null;
        }

        await _db.Updateable(bank)
            .UpdateColumns(t => new { t.Status, t.Remark, t.UpdateAt, t.CheckDateTime, t.Checker })
            .ExecuteCommandAsync(cancellationToken);

        // 审核成功：标记已绑卡（对齐旧 UpdateFlag IsCard）。
        if (input.Status == StatusSuccess)
            await EnsureFlagIsCardAsync(bank.UserID, true, cancellationToken);
    }

    /// <summary>
    /// 撤回审核：状态回到待审，清审核人/时间。对齐旧 bank/reset。
    /// </summary>
    /// <param name="id">帐户编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task ResetAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw Oops.Oh("#参数错误#");
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<BankAccount>()
            .Where(t => t.SiteID == siteId && t.ID == id)
            .Take(1)
            .ToListAsync(cancellationToken);
        var bank = rows.FirstOrDefault() ?? throw Oops.Oh("#记录不存在#");
        if (bank.Status == StatusNone)
            throw Oops.Oh("#当前已是待审状态#");

        var wasSuccess = bank.Status == StatusSuccess;
        bank.Status = StatusNone;
        bank.Checker = null;
        bank.CheckDateTime = null;
        bank.UpdateAt = DateTime.Now;
        await _db.Updateable(bank)
            .UpdateColumns(t => new { t.Status, t.Checker, t.CheckDateTime, t.UpdateAt })
            .ExecuteCommandAsync(cancellationToken);

        // 撤回通过：若该会员已无其它通过的银行卡，清绑卡标记。
        if (wasSuccess && bank.IsVir == "0")
        {
            var stillOk = await _db.Queryable<BankAccount>()
                .Where(t => t.SiteID == siteId && t.UserID == bank.UserID && t.IsVir == "0" && t.Status == StatusSuccess && t.ID != bank.ID)
                .AnyAsync(cancellationToken);
            if (!stillOk)
                await EnsureFlagIsCardAsync(bank.UserID, false, cancellationToken);
        }
    }

    /// <summary>
    /// 删除银行帐户；有未完结提现单时拒绝。
    /// </summary>
    /// <param name="id">帐户编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw Oops.Oh("#参数错误#");
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<BankAccount>()
            .Where(t => t.SiteID == siteId && t.ID == id)
            .Take(1)
            .ToListAsync(cancellationToken);
        var bank = rows.FirstOrDefault() ?? throw Oops.Oh("#参数错误#");

        var pending = await _db.Queryable<WithdrawOrder>()
            .Where(t =>
                t.SiteID == siteId
                && t.UserID == bank.UserID
                && t.AccountNumber == bank.Account
                && t.Status != WithdrawFailed
                && t.Status != WithdrawFinished)
            .Select(t => t.ID)
            .Take(1)
            .ToListAsync(cancellationToken);
        if (pending.Count > 0)
            throw Oops.Oh($"无法删除！该账户有关联的待处理提现订单（单号：{pending[0]}）。请先处理或取消该订单后，再进行删除操作。");

        await _db.Deleteable<BankAccount>().In(bank.ID).ExecuteCommandAsync(cancellationToken);

        // 没有成功银行卡时清掉绑卡标记。
        var stillOk = await _db.Queryable<BankAccount>()
            .AnyAsync(
                t => t.SiteID == siteId && t.UserID == bank.UserID && t.IsVir == "0" && t.Status == StatusSuccess,
                cancellationToken);
        if (!stillOk)
            await EnsureFlagIsCardAsync(bank.UserID, false, cancellationToken);
    }

    /// <summary>
    /// 后台新增银行帐户（按会员路径 isImport，跳过短信校验）。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task CreateAsync(BankAccountCreateInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.ID <= 0 || string.IsNullOrWhiteSpace(input.UserName))
            throw Oops.Oh("#请先查询用户是否存在#");

        var siteId = _site.RequireSiteId();
        var userName = input.UserName.Trim();
        var users = await _db.Queryable<User>()
            .Where(u => u.SiteID == siteId && u.ID == input.ID && u.UserName == userName)
            .Take(1)
            .ToListAsync(cancellationToken);
        var user = users.FirstOrDefault() ?? throw Oops.Oh("#用户不存在#");

        var isVir = string.IsNullOrWhiteSpace(input.IsVir) ? "0" : input.IsVir.Trim();
        var account = new BankAccount
        {
            SiteID = siteId,
            UserID = user.ID,
            IsVir = isVir,
            BankName = string.Empty,
            CreateAt = DateTime.Now,
            UpdateAt = DateTime.Now,
            Remark = string.Empty,
            Country = string.Empty,
            NameLetters = string.Empty,
            BankInfo = string.Empty,
            AccountDetail = string.Empty
        };

        switch (isVir)
        {
            case "0":
                account.Name = (input.Name ?? string.Empty).Trim();
                account.Account = (input.Account ?? string.Empty).Trim();
                account.AccountDetail = string.Join(",",
                    (input.AccountDetail ?? string.Empty).Trim(),
                    (input.BankAddress ?? string.Empty).Trim(),
                    (input.BranchCode ?? string.Empty).Trim());
                account.Type = input.Type;
                account.BankInfo = (input.BankInfo ?? string.Empty).Trim();
                break;
            case "1":
                account.Type = input.VCurrency;
                account.Name = (input.VAgreement ?? string.Empty).Trim();
                account.Account = (input.VAddress ?? string.Empty).Trim();
                break;
            case "2":
                account.Type = input.EType;
                account.Name = (input.EUserName ?? string.Empty).Trim();
                account.Account = (input.EAccount ?? string.Empty).Trim();
                account.Country = (input.EArea ?? string.Empty).Trim();
                break;
            case "4":
                // 超商：类型 + 县市/地区/门店写入地址明细；帐号优先门店编号。
                account.Type = input.CType > 0 ? input.CType : input.Type;
                account.Name = !string.IsNullOrWhiteSpace(input.CName)
                    ? input.CName.Trim()
                    : (!string.IsNullOrWhiteSpace(user.AccountName) ? user.AccountName.Trim() : userName);
                account.Account = !string.IsNullOrWhiteSpace(input.CAccount)
                    ? input.CAccount.Trim()
                    : (input.CBankAddress > 0 ? input.CBankAddress.ToString() : string.Empty);
                account.AccountDetail = string.Join(",",
                    (input.CCity ?? string.Empty).Trim(),
                    (input.CDistrict ?? string.Empty).Trim(),
                    (input.BankAddress ?? string.Empty).Trim());
                break;
            default:
                throw Oops.Oh("#参数错误#");
        }

        if (await _db.Queryable<SystemBankAccountBlackList>()
                .AnyAsync(b => b.AccountNumber == account.Account, cancellationToken))
            throw Oops.Oh("#该卡号已被冻结#");

        if (string.IsNullOrEmpty(account.Name))
            throw Oops.Oh("#提现账户名不能为空#");
        if (account.Name.Length > 50)
            throw Oops.Oh("#账户名太长#");
        if (string.IsNullOrEmpty(account.Account))
            throw Oops.Oh("#提现帐号不能为空#");

        // 同站重复卡号：银行卡按帐号；其它按帐号+类型。
        if (isVir == "0")
        {
            if (await _db.Queryable<BankAccount>()
                    .AnyAsync(t => t.SiteID == siteId && t.IsVir == "0" && t.Account == account.Account, cancellationToken))
                throw Oops.Oh($"#卡号# {account.Account} #已被绑定#");
        }
        else if (await _db.Queryable<BankAccount>()
                     .AnyAsync(
                         t => t.SiteID == siteId && t.Account == account.Account && t.Type == account.Type,
                         cancellationToken))
        {
            throw Oops.Oh($"#卡号# {account.Account} #已被绑定#");
        }

        // 后台导入：非银行卡默认直接成功；银行卡待审（对齐旧 IsKYC 路径简化）。
        if (isVir != "0")
        {
            account.Status = StatusSuccess;
            account.CheckDateTime = DateTime.Now;
            account.Checker = _userManager.UserId > 0 ? (int)_userManager.UserId : 0;
        }
        else
        {
            account.Status = StatusNone;
        }

        await _db.Insertable(account).ExecuteCommandAsync(cancellationToken);

        if (isVir == "0" && string.IsNullOrWhiteSpace(user.AccountName))
        {
            user.AccountName = account.Name;
            await _db.Updateable(user).UpdateColumns(u => new { u.AccountName }).ExecuteCommandAsync(cancellationToken);
        }

        if (account.Status == StatusSuccess)
            await EnsureFlagIsCardAsync(user.ID, true, cancellationToken);
    }

    /// <summary>
    /// 写入或更新 usr_Flag.IsCard。
    /// </summary>
    /// <param name="userId">会员</param>
    /// <param name="isCard">是否已绑卡</param>
    /// <param name="cancellationToken">取消令牌</param>
    private async Task EnsureFlagIsCardAsync(int userId, bool isCard, CancellationToken cancellationToken)
    {
        var flags = await _db.Queryable<UserFlag>()
            .Where(f => f.UserID == userId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var flag = flags.FirstOrDefault();
        if (flag is null)
        {
            await _db.Insertable(new UserFlag
            {
                UserID = userId,
                IsCard = isCard,
                IsMobile = false,
                IsEmail = false,
                IsKYCCard = false,
                KYCProcessString = string.Empty
            }).ExecuteCommandAsync(cancellationToken);
            return;
        }

        if (flag.IsCard == isCard)
            return;
        flag.IsCard = isCard;
        await _db.Updateable(flag).UpdateColumns(f => new { f.IsCard }).ExecuteCommandAsync(cancellationToken);
    }
}
