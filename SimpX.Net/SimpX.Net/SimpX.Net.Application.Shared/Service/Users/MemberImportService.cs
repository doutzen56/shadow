using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 会员导入三页暂存：注册会员 / 证件 / 银行。对齐旧 site/admin/import*。
/// 注册会员申请不落导入额度（加款仍归资金模块）。
/// </summary>
public sealed class MemberImportService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;
    private readonly BankAccountService _banks;

    /// <summary>
    /// 注入库、当前站与银行写服务。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站</param>
    /// <param name="banks">银行帐户</param>
    public MemberImportService(IBizDbAccessor biz, ICurrentSiteAccessor site, BankAccountService banks)
    {
        _db = biz.Db;
        _site = site;
        _banks = banks;
    }

    /// <summary>注册会员导入分页。</summary>
    /// <param name="input">筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页</returns>
    public async Task<MemberImportPageOutput<UserImportItemDto>> PageUsersAsync(
        MemberImportPageInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var siteId = _site.RequireSiteId();
        var pageSize = Math.Clamp(input.PageSize <= 0 ? 20 : input.PageSize, 1, 100);
        var page = Math.Max(1, input.Page <= 0 ? 1 : input.Page);
        var query = _db.Queryable<UserImport>().Where(t => t.SiteID == siteId);
        if (!string.IsNullOrWhiteSpace(input.UserName))
            query = query.Where(t => t.UserName.Contains(input.UserName.Trim()));
        if (!string.IsNullOrWhiteSpace(input.Status) && byte.TryParse(input.Status.Trim(), out var st))
            query = query.Where(t => t.Status == st);

        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderBy(t => t.ID, OrderByType.Desc)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new MemberImportPageOutput<UserImportItemDto>
        {
            Total = total,
            Items = rows.Select(MapUser).ToList()
        };
    }

    /// <summary>保存注册会员导入行。</summary>
    /// <param name="input">行</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SaveUserAsync(UserImportSaveInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (string.IsNullOrWhiteSpace(input.UserName))
            throw Oops.Oh("#用户名错误#");
        var siteId = _site.RequireSiteId();
        if (input.ID > 0)
        {
            var rows = await _db.Queryable<UserImport>()
                .Where(t => t.SiteID == siteId && t.ID == input.ID).Take(1).ToListAsync(cancellationToken);
            var row = rows.FirstOrDefault() ?? throw Oops.Oh("#记录不存在#");
            FillUser(row, input);
            await _db.Updateable(row).ExecuteCommandAsync(cancellationToken);
            return;
        }

        var neu = new UserImport { SiteID = siteId, Status = 0, CreateTime = DateTime.Now, Remark = string.Empty };
        FillUser(neu, input);
        await _db.Insertable(neu).ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>删除注册会员导入行。</summary>
    /// <param name="id">编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task DeleteUserAsync(int id, CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        await _db.Deleteable<UserImport>().Where(t => t.SiteID == siteId && t.ID == id).ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 把未成功的注册导入行写成正式会员（不含额度加款）。
    /// </summary>
    /// <param name="input">编号；空则处理全部未成功</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>成功数</returns>
    public async Task<MemberImportApplyOutput> ApplyUsersAsync(
        MemberImportApplyInput input,
        CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var query = _db.Queryable<UserImport>().Where(t => t.SiteID == siteId && t.Status != 1);
        if (input?.Ids is { Count: > 0 })
            query = query.Where(t => input.Ids.Contains(t.ID));
        var rows = await query.ToListAsync(cancellationToken);
        if (rows.Count == 0)
            return new MemberImportApplyOutput { SuccessCount = 0, Message = "#没有要增加的记录#" };

        var success = 0;
        foreach (var item in rows)
        {
            try
            {
                await CreateUserFromImportAsync(siteId, item, cancellationToken);
                item.Status = 1;
                item.Remark = item.Balance > 0
                    ? "成功（额度未落账，请走资金加款）"
                    : "成功";
                success++;
            }
            catch (Exception ex)
            {
                item.Status = 2;
                var msg = ex.Message ?? string.Empty;
                item.Remark = msg.Length > 50 ? msg[..49] : msg;
            }

            await _db.Updateable(item).UpdateColumns(t => new { t.Status, t.Remark }).ExecuteCommandAsync(cancellationToken);
        }

        return new MemberImportApplyOutput { SuccessCount = success, Message = $"#操作完成#{success}" };
    }

    /// <summary>证件导入分页。</summary>
    /// <param name="input">筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页</returns>
    public async Task<MemberImportPageOutput<IdCardImportItemDto>> PageIdCardsAsync(
        MemberImportPageInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var siteId = _site.RequireSiteId();
        var pageSize = Math.Clamp(input.PageSize <= 0 ? 20 : input.PageSize, 1, 100);
        var page = Math.Max(1, input.Page <= 0 ? 1 : input.Page);
        var query = _db.Queryable<UserIdCardImport>().Where(t => t.SiteID == siteId);
        if (!string.IsNullOrWhiteSpace(input.UserName))
            query = query.Where(t => t.UserName.Contains(input.UserName.Trim()));
        if (!string.IsNullOrWhiteSpace(input.Status) && int.TryParse(input.Status.Trim(), out var st))
        {
            var flag = st != 0;
            query = query.Where(t => t.Status == flag);
        }

        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderBy(t => t.ID, OrderByType.Desc)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new MemberImportPageOutput<IdCardImportItemDto>
        {
            Total = total,
            Items = rows.Select(t => new IdCardImportItemDto
            {
                ID = t.ID,
                UserName = t.UserName,
                CardNumber = t.CardNumber,
                CardFront = t.CardFront,
                CardBack = t.CardBack,
                CardCity = t.CardCity,
                CardCreateAt = t.CardCreateAt,
                CardType = t.CardType,
                CardBirthday = t.CardBirthday,
                Status = t.Status,
                CreateTime = t.CreateTime
            }).ToList()
        };
    }

    /// <summary>保存证件导入。</summary>
    /// <param name="input">行</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SaveIdCardAsync(IdCardImportSaveInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (string.IsNullOrWhiteSpace(input.UserName) || string.IsNullOrWhiteSpace(input.CardNumber))
            throw Oops.Oh("#参数错误#");
        var siteId = _site.RequireSiteId();
        if (input.ID > 0)
        {
            var rows = await _db.Queryable<UserIdCardImport>()
                .Where(t => t.SiteID == siteId && t.ID == input.ID).Take(1).ToListAsync(cancellationToken);
            var row = rows.FirstOrDefault() ?? throw Oops.Oh("#记录不存在#");
            FillIdCard(row, input);
            await _db.Updateable(row).ExecuteCommandAsync(cancellationToken);
            return;
        }

        var neu = new UserIdCardImport { SiteID = siteId, Status = false, CreateTime = DateTime.Now };
        FillIdCard(neu, input);
        await _db.Insertable(neu).ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>删除证件导入。</summary>
    /// <param name="id">编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task DeleteIdCardAsync(int id, CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        await _db.Deleteable<UserIdCardImport>().Where(t => t.SiteID == siteId && t.ID == id)
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>把未导入证件写入正式表。</summary>
    /// <param name="input">编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>成功数</returns>
    public async Task<MemberImportApplyOutput> ApplyIdCardsAsync(
        MemberImportApplyInput input,
        CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var query = _db.Queryable<UserIdCardImport>().Where(t => t.SiteID == siteId && !t.Status);
        if (input?.Ids is { Count: > 0 })
            query = query.Where(t => input.Ids.Contains(t.ID));
        var rows = await query.ToListAsync(cancellationToken);
        if (rows.Count == 0)
            return new MemberImportApplyOutput { SuccessCount = 0, Message = "#没有要增加的记录#" };

        var success = 0;
        foreach (var item in rows)
        {
            var userId = await ResolveUserIdAsync(siteId, item.UserName, cancellationToken);
            if (userId <= 0)
                continue;

            await _db.Insertable(new UserIdCard
            {
                SiteID = siteId,
                UserID = userId,
                UserName = item.UserName ?? string.Empty,
                CardName = string.Empty,
                CardNumber = item.CardNumber ?? string.Empty,
                CardFront = item.CardFront ?? string.Empty,
                CardBack = item.CardBack ?? string.Empty,
                CardCity = item.CardCity ?? string.Empty,
                CardCreateAt = item.CardCreateAt,
                CardType = item.CardType,
                CardBirthday = item.CardBirthday,
                Status = BankAccountService.StatusNone,
                Remark = string.Empty,
                CreateAt = DateTime.Now
            }).ExecuteCommandAsync(cancellationToken);
            item.Status = true;
            await _db.Updateable(item).UpdateColumns(t => new { t.Status }).ExecuteCommandAsync(cancellationToken);
            success++;
        }

        return new MemberImportApplyOutput { SuccessCount = success, Message = $"#操作完成#{success}" };
    }

    /// <summary>银行导入分页。</summary>
    /// <param name="input">筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页</returns>
    public async Task<MemberImportPageOutput<BankImportItemDto>> PageBanksAsync(
        MemberImportPageInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var siteId = _site.RequireSiteId();
        var pageSize = Math.Clamp(input.PageSize <= 0 ? 20 : input.PageSize, 1, 100);
        var page = Math.Max(1, input.Page <= 0 ? 1 : input.Page);
        var query = _db.Queryable<UserBankImport>().Where(t => t.SiteID == siteId);
        if (!string.IsNullOrWhiteSpace(input.UserName))
            query = query.Where(t => t.UserName.Contains(input.UserName.Trim()));
        if (!string.IsNullOrWhiteSpace(input.Status) && int.TryParse(input.Status.Trim(), out var st))
        {
            var flag = st != 0;
            query = query.Where(t => t.Status == flag);
        }

        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderBy(t => t.ID, OrderByType.Desc)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new MemberImportPageOutput<BankImportItemDto>
        {
            Total = total,
            Items = rows.Select(t => new BankImportItemDto
            {
                ID = t.ID,
                UserName = t.UserName,
                Type = t.Type,
                BankName = t.BankName,
                Name = t.Name,
                Account = t.Account,
                AccountDetail = t.AccountDetail,
                IsVir = t.IsVir,
                BankAddress = t.BankAddress,
                BranchCode = t.BranchCode,
                BankInfo = t.BankInfo,
                Status = t.Status,
                CreateTime = t.CreateTime
            }).ToList()
        };
    }

    /// <summary>保存银行导入。</summary>
    /// <param name="input">行</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SaveBankAsync(BankImportSaveInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (string.IsNullOrWhiteSpace(input.UserName) || string.IsNullOrWhiteSpace(input.Account))
            throw Oops.Oh("#参数错误#");
        var siteId = _site.RequireSiteId();
        if (input.ID > 0)
        {
            var rows = await _db.Queryable<UserBankImport>()
                .Where(t => t.SiteID == siteId && t.ID == input.ID).Take(1).ToListAsync(cancellationToken);
            var row = rows.FirstOrDefault() ?? throw Oops.Oh("#记录不存在#");
            FillBank(row, input);
            await _db.Updateable(row).ExecuteCommandAsync(cancellationToken);
            return;
        }

        var neu = new UserBankImport { SiteID = siteId, Status = false, CreateTime = DateTime.Now };
        FillBank(neu, input);
        await _db.Insertable(neu).ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>删除银行导入。</summary>
    /// <param name="id">编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task DeleteBankAsync(int id, CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        await _db.Deleteable<UserBankImport>().Where(t => t.SiteID == siteId && t.ID == id)
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>把未导入银行写入正式银行帐户。</summary>
    /// <param name="input">编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>成功数</returns>
    public async Task<MemberImportApplyOutput> ApplyBanksAsync(
        MemberImportApplyInput input,
        CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var query = _db.Queryable<UserBankImport>().Where(t => t.SiteID == siteId && !t.Status);
        if (input?.Ids is { Count: > 0 })
            query = query.Where(t => input.Ids.Contains(t.ID));
        var rows = await query.ToListAsync(cancellationToken);
        if (rows.Count == 0)
            return new MemberImportApplyOutput { SuccessCount = 0, Message = "#没有要增加的记录#" };

        var success = 0;
        foreach (var item in rows)
        {
            var userId = await ResolveUserIdAsync(siteId, item.UserName, cancellationToken);
            if (userId <= 0)
                continue;
            try
            {
                await _banks.CreateAsync(new BankAccountCreateInput
                {
                    ID = userId,
                    UserName = item.UserName,
                    IsVir = string.IsNullOrWhiteSpace(item.IsVir) ? "0" : item.IsVir.Trim(),
                    Type = item.Type,
                    Name = item.Name,
                    Account = item.Account,
                    AccountDetail = item.AccountDetail,
                    BankAddress = item.BankAddress,
                    BranchCode = item.BranchCode,
                    BankInfo = item.BankInfo
                }, cancellationToken);
                item.Status = true;
                await _db.Updateable(item).UpdateColumns(t => new { t.Status }).ExecuteCommandAsync(cancellationToken);
                success++;
            }
            catch
            {
                // 单行失败继续其它行
            }
        }

        return new MemberImportApplyOutput { SuccessCount = success, Message = $"#操作完成#{success}" };
    }

    /// <summary>按用户名取会员编号。</summary>
    private async Task<int> ResolveUserIdAsync(int siteId, string userName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userName))
            return 0;
        var ids = await _db.Queryable<User>()
            .Where(u => u.SiteID == siteId && u.UserName == userName.Trim())
            .Select(u => u.ID).Take(1).ToListAsync(cancellationToken);
        return ids.FirstOrDefault();
    }

    /// <summary>从导入行创建会员。</summary>
    private async Task CreateUserFromImportAsync(int siteId, UserImport item, CancellationToken cancellationToken)
    {
        var userName = (item.UserName ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(item.Password))
            throw new InvalidOperationException("#用户名/密码错误#");
        if (await _db.Queryable<User>().AnyAsync(u => u.SiteID == siteId && u.UserName == userName, cancellationToken))
            throw new InvalidOperationException("#该用户名已被注册#");

        var agentId = 0;
        if (!string.IsNullOrWhiteSpace(item.AgentName))
        {
            var agents = await _db.Queryable<User>()
                .Where(u => u.SiteID == siteId && u.UserName == item.AgentName.Trim())
                .Select(u => u.ID).Take(1).ToListAsync(cancellationToken);
            agentId = agents.FirstOrDefault();
        }

        var groups = await _db.Queryable<UserGroup>()
            .Where(g => g.SiteID == siteId && g.IsDefault)
            .OrderBy(g => g.ID).Take(1).ToListAsync(cancellationToken);
        var group = groups.FirstOrDefault() ?? throw new InvalidOperationException("#系统错误,未配置默认分组#");

        var now = DateTime.Now;
        var empty = string.Empty;
        var accountName = (item.AccountName ?? empty).Replace(" ", "");
        if (accountName.Length > 15)
            accountName = accountName[..15];

        await _db.Insertable(new User
        {
            SiteID = siteId,
            UserName = userName,
            NickName = userName,
            AccountName = accountName,
            Password = Md5PasswordHasher.Hash(item.Password),
            PayPassword = empty,
            Face = empty,
            Email = empty,
            Mobile = item.Mobile ?? empty,
            IsMobile = !string.IsNullOrWhiteSpace(item.Mobile),
            Telegram = empty,
            Line = empty,
            Google = empty,
            Answer = empty,
            Info = empty,
            DeviceId = empty,
            PixelID = empty,
            Address = empty,
            WashingRatio = empty,
            DisableCurrency = empty,
            BlockGame = empty,
            RegisteredFrom = empty,
            Notes = empty,
            CreateAt = now,
            LoginAt = now,
            ActiveAt = now,
            UpdateAt = now,
            Birthday = new DateTime(1970, 1, 1),
            RegIP = empty,
            LoginIP = empty,
            Lock = 0,
            Function = 0,
            AgentID = agentId,
            InviteID = 0,
            Depth = 0,
            GroupID = group.ID,
            Type = 0,
            Currency = item.Currency,
            Money = 0,
            LockMoney = 0,
            GameMoney = 0,
            IsOnline = false,
            Flag = 0,
            Level = 0,
            InitLevel = 0,
            Exper = 0,
            RegFlag = 0,
            RegDomain = empty,
            LoginPlatform = 0,
            RegPlatform = 0,
            Unbalanced = 0,
            ProxyRate = 0,
            LastReturn = now.AddDays(-1),
            Coin = 0,
            SecretKey = Guid.Empty,
            Question = 0,
            IsEmail = false,
            OldGroupID = 0
        }).ExecuteCommandAsync(cancellationToken);
    }

    private static UserImportItemDto MapUser(UserImport t) => new()
    {
        ID = t.ID,
        UserName = t.UserName,
        AgentName = t.AgentName,
        Password = t.Password,
        Mobile = t.Mobile,
        Currency = t.Currency,
        Balance = t.Balance,
        AccountName = t.AccountName,
        Status = t.Status,
        Remark = t.Remark,
        CreateTime = t.CreateTime
    };

    private static void FillUser(UserImport row, UserImportSaveInput input)
    {
        row.UserName = input.UserName.Trim();
        row.AgentName = input.AgentName ?? string.Empty;
        row.Password = input.Password ?? string.Empty;
        row.Mobile = input.Mobile ?? string.Empty;
        row.Currency = input.Currency;
        row.Balance = input.Balance;
        row.AccountName = input.AccountName ?? string.Empty;
    }

    private static void FillIdCard(UserIdCardImport row, IdCardImportSaveInput input)
    {
        row.UserName = input.UserName.Trim();
        row.CardNumber = input.CardNumber.Trim();
        row.CardFront = input.CardFront ?? string.Empty;
        row.CardBack = input.CardBack ?? string.Empty;
        row.CardCity = input.CardCity ?? string.Empty;
        row.CardCreateAt = input.CardCreateAt ?? DateTime.Now;
        row.CardType = input.CardType;
        row.CardBirthday = input.CardBirthday ?? new DateTime(1970, 1, 1);
    }

    private static void FillBank(UserBankImport row, BankImportSaveInput input)
    {
        row.UserName = input.UserName.Trim();
        row.Type = input.Type;
        row.BankName = input.BankName ?? string.Empty;
        row.Name = input.Name ?? string.Empty;
        row.Account = input.Account.Trim();
        row.AccountDetail = input.AccountDetail ?? string.Empty;
        row.IsVir = string.IsNullOrWhiteSpace(input.IsVir) ? "0" : input.IsVir.Trim();
        row.BankAddress = input.BankAddress ?? string.Empty;
        row.BranchCode = input.BranchCode ?? string.Empty;
        row.BankInfo = input.BankInfo ?? string.Empty;
    }
}
