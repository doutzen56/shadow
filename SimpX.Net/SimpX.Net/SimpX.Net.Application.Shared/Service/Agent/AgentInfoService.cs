using SimpX.Net.Data.Entity.Agents;
using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 租户后台代理信息（方案绑定与状态）。对齐旧 /user/agent/usragentinfo。
/// </summary>
public sealed partial class AgentInfoService : ITransient
{
    /// <summary>旧 UserType.Member。</summary>
    public const int UserTypeMember = 0;

    /// <summary>旧 UserType.Agent。</summary>
    public const int UserTypeAgent = 1;

    /// <summary>旧 FunctionType.CheckAgent。</summary>
    public const int FunctionCheckAgent = 4;

    /// <summary>旧 RegMethod.AdminCreate。</summary>
    public const int RegFlagAdminCreate = 2;

    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;
    private readonly AgentApplyService _apply;

    /// <summary>
    /// 注入依赖。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站</param>
    /// <param name="apply">代理申请审核</param>
    public AgentInfoService(IBizDbAccessor biz, ICurrentSiteAccessor site, AgentApplyService apply)
    {
        _db = biz.Db;
        _site = site;
        _apply = apply ?? throw new ArgumentNullException(nameof(apply));
    }

    /// <summary>
    /// 代理分页列表。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页</returns>
    public async Task<AgentInfoPageOutput> PageAsync(
        AgentInfoPageInput input,
        CancellationToken cancellationToken = default)
    {
        input ??= new AgentInfoPageInput();
        var siteId = _site.RequireSiteId();
        var page = input.Page <= 0 ? 1 : input.Page;
        var pageSize = input.PageSize <= 0 ? 20 : Math.Min(input.PageSize, 100);

        var query = _db.Queryable<AgentUser, User>((a, u) => new JoinQueryInfos(
                JoinType.Inner, a.ID == u.ID && a.SiteID == u.SiteID))
            .Where((a, u) => a.SiteID == siteId);

        if (!string.IsNullOrWhiteSpace(input.UserName))
            query = query.Where((a, u) => u.UserName.Contains(input.UserName.Trim()));
        if (input.Status.HasValue)
            query = query.Where((a, u) => a.Status == (AgentUser.AgentStatus)input.Status.Value);
        if (input.StartAt.HasValue && input.StartAt.Value != default)
            query = query.Where((a, u) => a.CreateAt >= input.StartAt.Value);
        if (input.EndAt.HasValue && input.EndAt.Value != default)
            query = query.Where((a, u) => a.CreateAt < input.EndAt.Value.Date.AddDays(1));

        if (!string.IsNullOrWhiteSpace(input.Agent))
        {
            var agentName = input.Agent.Trim();
            var parentIds = await _db.Queryable<User>()
                .Where(u => u.SiteID == siteId && u.UserName != null && u.UserName.Contains(agentName))
                .Select(u => u.ID)
                .Take(500)
                .ToListAsync(cancellationToken);
            if (parentIds.Count == 0)
                return new AgentInfoPageOutput { Items = [], Total = 0 };

            if (input.BelongUser)
            {
                var childIds = await _db.Queryable<UserDepth>()
                    .Where(d => d.SiteID == siteId && parentIds.Contains(d.UserID))
                    .Select(d => d.ChildID)
                    .ToListAsync(cancellationToken);
                var scope = parentIds.Concat(childIds).Distinct().ToList();
                if (scope.Count == 0)
                    return new AgentInfoPageOutput { Items = [], Total = 0 };
                query = query.Where((a, u) => scope.Contains(a.ParentID) || parentIds.Contains(a.ParentID));
            }
            else
            {
                query = query.Where((a, u) => parentIds.Contains(a.ParentID));
            }
        }

        RefAsync<int> total = 0;
        var rows = await query
            .OrderBy((a, u) => a.CreateAt, OrderByType.Desc)
            .Select((a, u) => new AgentInfoListItemDto
            {
                ID = a.ID,
                UserName = u.UserName,
                ParentID = a.ParentID,
                Status = (byte)a.Status,
                CommissionID = a.CommissionID,
                ReturnID = a.ReturnID,
                Permission = a.Permission,
                CreateAt = a.CreateAt,
                BlockGame = u.BlockGame
            })
            .ToPageListAsync(page, pageSize, total, cancellationToken);

        await EnrichAgentInfoRowsAsync(siteId, rows, cancellationToken);
        return new AgentInfoPageOutput { Items = rows, Total = total };
    }

    /// <summary>补齐上级名/方案名/邀请码/域名/下级人数。</summary>
    private async Task EnrichAgentInfoRowsAsync(
        int siteId,
        List<AgentInfoListItemDto> rows,
        CancellationToken cancellationToken)
    {
        if (rows.Count == 0) return;

        var parentIds = rows.Select(r => r.ParentID).Where(id => id > 0).Distinct().ToList();
        var parentNames = parentIds.Count == 0
            ? new Dictionary<int, string>()
            : (await _db.Queryable<User>()
                .Where(u => u.SiteID == siteId && parentIds.Contains(u.ID))
                .Select(u => new { u.ID, u.UserName })
                .ToListAsync(cancellationToken))
            .ToDictionary(x => x.ID, x => x.UserName ?? string.Empty);

        var commissionIds = rows.Select(r => r.CommissionID).Where(id => id > 0).Distinct().ToList();
        var commissionNames = commissionIds.Count == 0
            ? new Dictionary<int, string>()
            : (await _db.Queryable<AgentCommission>()
                .Where(c => c.SiteID == siteId && commissionIds.Contains(c.ID))
                .Select(c => new { c.ID, c.Name })
                .ToListAsync(cancellationToken))
            .ToDictionary(x => x.ID, x => x.Name ?? string.Empty);

        var returnIds = rows.Select(r => r.ReturnID).Where(id => id > 0).Distinct().ToList();
        var returnNames = returnIds.Count == 0
            ? new Dictionary<int, string>()
            : (await _db.Queryable<AgentReturn>()
                .Where(r => r.SiteID == siteId && returnIds.Contains(r.ID))
                .Select(r => new { r.ID, r.Name })
                .ToListAsync(cancellationToken))
            .ToDictionary(x => x.ID, x => x.Name ?? string.Empty);

        var agentIds = rows.Select(r => r.ID).ToList();
        var memberCounts = (await _db.Queryable<User>()
                .Where(u => u.SiteID == siteId && agentIds.Contains(u.AgentID))
                .GroupBy(u => u.AgentID)
                .Select(u => new { AgentID = u.AgentID, Cnt = SqlFunc.AggregateCount(u.ID) })
                .ToListAsync(cancellationToken))
            .ToDictionary(x => x.AgentID, x => x.Cnt);

        var invites = await _db.Queryable<UserInvite>()
            .Where(i => i.SiteID == siteId && agentIds.Contains(i.UserID))
            .Select(i => new { i.UserID, i.ID })
            .ToListAsync(cancellationToken);
        var inviteMap = invites
            .GroupBy(i => i.UserID)
            .ToDictionary(g => g.Key, g => g.First().ID ?? string.Empty);

        var domains = await _db.Queryable<UserDomain>()
            .Where(d => d.SiteID == siteId && agentIds.Contains(d.UserID))
            .Select(d => new { d.UserID, d.Name })
            .ToListAsync(cancellationToken);
        var domainMap = domains
            .GroupBy(d => d.UserID)
            .ToDictionary(g => g.Key, g => g.First().Name ?? string.Empty);

        foreach (var row in rows)
        {
            row.AgentName = row.ParentID > 0 && parentNames.TryGetValue(row.ParentID, out var pn) ? pn : string.Empty;
            row.Mode = row.CommissionID > 0 && commissionNames.TryGetValue(row.CommissionID, out var cn) ? cn : string.Empty;
            row.ReturnMode = row.ReturnID > 0 && returnNames.TryGetValue(row.ReturnID, out var rn) ? rn : string.Empty;
            row.Member = memberCounts.GetValueOrDefault(row.ID, 0);
            row.Invite = inviteMap.GetValueOrDefault(row.ID, string.Empty);
            row.Domain = domainMap.GetValueOrDefault(row.ID, string.Empty);
            row.BlockGame ??= string.Empty;
        }
    }

    /// <summary>
    /// 详情。
    /// </summary>
    /// <param name="id">代理编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>详情</returns>
    public async Task<AgentInfoDetailDto> DetailAsync(int id, CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<AgentUser, User>((a, u) => new JoinQueryInfos(
                JoinType.Inner, a.ID == u.ID && a.SiteID == u.SiteID))
            .Where((a, u) => a.SiteID == siteId && a.ID == id)
            .Select((a, u) => new AgentInfoDetailDto
            {
                ID = a.ID,
                UserName = u.UserName,
                ParentID = a.ParentID,
                Status = (byte)a.Status,
                CommissionID = a.CommissionID,
                ReturnID = a.ReturnID,
                Permission = a.Permission,
                CreateAt = a.CreateAt
            }).Take(1).ToListAsync(cancellationToken);
        return rows.FirstOrDefault() ?? throw Oops.Oh("#记录不存在#");
    }

    /// <summary>
    /// 更新方案与状态（不改资金）。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SaveAsync(AgentInfoSaveInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<AgentUser>()
            .Where(a => a.SiteID == siteId && a.ID == input.ID).Take(1).ToListAsync(cancellationToken);
        var entity = rows.FirstOrDefault() ?? throw Oops.Oh("#记录不存在#");
        entity.CommissionID = input.CommissionID;
        entity.ReturnID = input.ReturnID;
        entity.Status = (AgentUser.AgentStatus)input.Status;
        entity.Permission = input.Permission;
        if (input.ParentID >= 0)
            entity.ParentID = input.ParentID;
        await _db.Updateable(entity).ExecuteCommandAsync(cancellationToken);
    }

    // --- merged from AgentInfoService.WaveGaps.cs ---



    /// <summary>
    /// 对齐旧 add。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> AddAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return AddCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 agentlist。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> AgentlistAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return AgentlistCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 applylist。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> ApplylistAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return ApplylistCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 check。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> CheckAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return CheckCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 delete。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> DeleteAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return DeleteCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 execapply。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> ExecapplyAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return ExecapplyCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 getblockgame。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> GetblockgameAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return GetblockgameCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 permission。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> PermissionAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return PermissionCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 updateagent。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> UpdateagentAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return UpdateagentCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 updatecommission。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> UpdatecommissionAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return UpdatecommissionCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 updateinviteopen。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> UpdateinviteopenAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return UpdateinviteopenCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 updatereturn。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> UpdatereturnAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return UpdatereturnCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 usragent/save：批量改会员上级为指定代理。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果文案</returns>
    public Task<object?> UsragentSaveAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return UsragentSaveCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 usragent/saveblockgame。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> SaveblockgameAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return SaveblockgameCoreAsync(input, cancellationToken);
    }


    /// <summary>核心：add —— 后台添加总代。</summary>
    private async Task<object?> AddCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var userName = ReadString(input, "UserName", "userName").Trim();
        var password = ReadString(input, "Password", "password");
        var currency = ReadInt(input, "Currency", "currency");
        if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))
            throw Oops.Oh("#参数错误#");

        var siteId = _site.RequireSiteId();
        var exists = await _db.Queryable<User>()
            .AnyAsync(u => u.SiteID == siteId && u.UserName == userName, cancellationToken);
        if (exists)
            throw Oops.Oh("#用户名已存在#");

        var groups = await _db.Queryable<UserGroup>()
            .Where(g => g.SiteID == siteId)
            .OrderBy(g => g.ID)
            .Take(1)
            .ToListAsync(cancellationToken);
        var group = groups.FirstOrDefault() ?? throw Oops.Oh("#未配置会员分组#");

        var now = DateTime.Now;
        var empty = string.Empty;
        var user = new User
        {
            SiteID = siteId,
            UserName = userName,
            NickName = userName,
            AccountName = empty,
            Password = Md5PasswordHasher.Hash(password),
            PayPassword = empty,
            Face = empty,
            Email = empty,
            Mobile = empty,
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
            CreateAt = now,
            LoginAt = now,
            ActiveAt = now,
            UpdateAt = now,
            Birthday = new DateTime(1970, 1, 1),
            RegIP = "0.0.0.0",
            LoginIP = "0.0.0.0",
            RegDomain = empty,
            RegFlag = RegFlagAdminCreate,
            LastReturn = now.Date.AddDays(-1),
            SecretKey = Guid.Empty,
            GroupID = group.ID,
            AgentID = -1,
            InviteID = 0,
            Depth = 0,
            Type = UserTypeAgent,
            Function = FunctionCheckAgent,
            Currency = currency
        };

        await _db.Ado.BeginTranAsync();
        try
        {
            var newId = await _db.Insertable(user).ExecuteReturnIdentityAsync(cancellationToken);
            if (newId <= 0)
            {
                await _db.Ado.RollbackTranAsync();
                throw Oops.Oh("#添加失败#");
            }

            await _db.Insertable(new AgentUser
            {
                ID = newId,
                SiteID = siteId,
                ParentID = 0,
                Status = AgentUser.AgentStatus.Normal,
                CommissionID = 0,
                ReturnID = 0,
                Permission = 0,
                CreateAt = now
            }).ExecuteCommandAsync(cancellationToken);

            await _db.Insertable(new UserDepth
            {
                SiteID = siteId,
                UserID = newId,
                ChildID = newId,
                Depth = 0
            }).ExecuteCommandAsync(cancellationToken);

            await _db.Updateable<UserGroup>()
                .SetColumns(g => g.Member == g.Member + 1)
                .Where(g => g.ID == group.ID && g.SiteID == siteId)
                .ExecuteCommandAsync(cancellationToken);

            await _db.Ado.CommitTranAsync();
            return new { ID = newId, UserName = userName };
        }
        catch
        {
            await _db.Ado.RollbackTranAsync();
            throw;
        }
    }


    /// <summary>核心：agentlist</summary>
    private async Task<object?> AgentlistCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        byte? status = null;
        var statusRaw = ReadString(input, "Status", "status");
        if (byte.TryParse(statusRaw, out var st)) status = st;
        var belongRaw = ReadString(input, "BelongUser", "belongUser");
        var belong = belongRaw is "1" or "true" or "True";
        DateTime? startAt = null;
        DateTime? endAt = null;
        if (DateTime.TryParse(ReadString(input, "StartAt", "startAt"), out var sa)) startAt = sa;
        if (DateTime.TryParse(ReadString(input, "EndAt", "endAt"), out var ea)) endAt = ea;
        var pageIndex = Math.Max(1, ReadInt(input, "PageIndex", "page", "Page"));
        var pageSize = Math.Clamp(ReadInt(input, "PageSize", "pagesize") is var ps && ps > 0 ? ps : 20, 1, 100);
        var page = await PageAsync(new AgentInfoPageInput
        {
            Page = pageIndex,
            PageSize = pageSize,
            UserName = ReadString(input, "UserName", "User", "user"),
            Agent = ReadString(input, "Agent", "AgentName", "ParentName"),
            BelongUser = belong,
            Status = status,
            StartAt = startAt,
            EndAt = endAt
        }, cancellationToken);
        return OldApiShowResult.OfPage(page.Items ?? [], page.Total, pageIndex, pageSize);
    }


    /// <summary>核心：applylist</summary>
    private async Task<object?> ApplylistCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        if (_apply == null)
            return Array.Empty<object>();
        int? status = null;
        var statusRaw = ReadString(input, "Status", "status");
        if (int.TryParse(statusRaw, out var st)) status = st;
        return await _apply.PageAsync(new AgentApplyPageInput
        {
            Page = Math.Max(1, ReadInt(input, "PageIndex", "page", "Page")),
            PageSize = Math.Clamp(ReadInt(input, "PageSize", "pagesize") is var ps && ps > 0 ? ps : 20, 1, 100),
            User = ReadString(input, "User", "UserName", "user"),
            Status = status
        }, cancellationToken);
    }


    /// <summary>核心：check —— 对齐旧 usragent/check。</summary>
    private async Task<object?> CheckCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var status = ReadInt(input, "Status", "status");
        var inputUsers = ReadString(input, "Users", "users");
        if (string.IsNullOrWhiteSpace(inputUsers))
            throw Oops.Oh("输入不能为空!");

        var names = inputUsers.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (names.Count == 0)
            throw Oops.Oh("输入不能为空!");

        var siteId = _site.RequireSiteId();
        var wantType = status == 1 ? UserTypeAgent : UserTypeMember;
        var found = await _db.Queryable<User>()
            .Where(u => u.SiteID == siteId && names.Contains(u.UserName) && u.Type == wantType)
            .Select(u => u.UserName)
            .ToListAsync(cancellationToken);
        var foundSet = found.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missing = names.Where(n => !foundSet.Contains(n)).ToList();
        if (missing.Count > 0)
            throw Oops.Oh($"{string.Join(",", missing)}不满足条件");
        return "检查完成";
    }


    /// <summary>核心：delete —— 锁定代理（无旧 delete 时作安全替代）。</summary>
    private async Task<object?> DeleteCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var id = ReadInt(input, "ID", "Id", "UserID");
        if (id <= 0) throw Oops.Oh("#参数错误#");
        var siteId = _site.RequireSiteId();
        var n = await _db.Updateable<AgentUser>()
            .SetColumns(a => a.Status == AgentUser.AgentStatus.Locked)
            .Where(a => a.SiteID == siteId && a.ID == id)
            .ExecuteCommandAsync(cancellationToken);
        if (n <= 0) throw Oops.Oh("#记录不存在#");
        return null;
    }


    /// <summary>核心：execapply</summary>
    private async Task<object?> ExecapplyCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        await _apply.CheckAsync(new AgentApplyCheckInput
        {
            ID = ReadInt(input, "ID", "Id"),
            Status = ReadInt(input, "Status", "status")
        }, cancellationToken);
        return null;
    }


    /// <summary>核心：getblockgame</summary>
    private async Task<object?> GetblockgameCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var agentId = ReadInt(input, "agentId", "AgentId", "AgentID", "ID", "Id");
        if (agentId <= 0) throw Oops.Oh("代理无效!");
        var siteId = _site.RequireSiteId();
        var users = await _db.Queryable<User>()
            .Where(u => u.SiteID == siteId && u.ID == agentId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var agent = users.FirstOrDefault() ?? throw Oops.Oh("代理无效!");

        var open = await _db.Queryable<SimpX.Net.Data.Entity.Games.GameSetting>()
            .Where(g => g.SiteID == siteId && g.IsOpen && !g.IsDisable)
            .OrderBy(g => g.Name)
            .Select(g => new { value = g.Game, name = g.Name })
            .ToListAsync(cancellationToken);

        var block = (agent.BlockGame ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new
        {
            agentId = agent.ID,
            openGames = open,
            blockGames = block
        };
    }


    /// <summary>核心：permission —— 位开关。</summary>
    private async Task<object?> PermissionCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var userId = ReadInt(input, "UserID", "ID", "Id");
        if (userId <= 0) throw Oops.Oh("#参数错误#");
        var bit = ParsePermissionBit(ReadString(input, "Permission", "Type"));
        if (bit == 0) throw Oops.Oh("#参数错误#");
        var selected = ReadInt(input, "Checked", "Selected") == 1
            || string.Equals(ReadString(input, "Checked"), "true", StringComparison.OrdinalIgnoreCase);
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<AgentUser>()
            .Where(a => a.SiteID == siteId && a.ID == userId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var row = rows.FirstOrDefault() ?? throw Oops.Oh("#记录不存在#");
        var next = selected ? (row.Permission | bit) : (row.Permission & ~bit);
        row.Permission = next;
        await _db.Updateable(row).UpdateColumns(a => a.Permission).ExecuteCommandAsync(cancellationToken);
        return null;
    }

    private static string ReadString(Dictionary<string, object?> input, params string[] keys)
    {
        foreach (var k in keys)
        {
            if (!input.TryGetValue(k, out var v) || v is null) continue;
            var s = v.ToString();
            if (!string.IsNullOrEmpty(s)) return s!;
        }

        return string.Empty;
    }


    /// <summary>核心：updateagent —— 会员升代理。</summary>
    private async Task<object?> UpdateagentCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var userId = ReadInt(input, "UserID", "ID", "Id");
        if (userId <= 0) throw Oops.Oh("#参数错误#");
        var siteId = _site.RequireSiteId();
        var users = await _db.Queryable<User>()
            .Where(u => u.SiteID == siteId && u.ID == userId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var user = users.FirstOrDefault() ?? throw Oops.Oh("#用户不存在#");

        if (user.Type != UserTypeAgent)
        {
            var nextFunction = (user.Function & FunctionCheckAgent) == FunctionCheckAgent
                ? user.Function
                : user.Function | FunctionCheckAgent;
            user.Type = UserTypeAgent;
            user.Function = nextFunction;
            await _db.Updateable(user).UpdateColumns(u => new { u.Type, u.Function }).ExecuteCommandAsync(cancellationToken);
        }

        var exists = await _db.Queryable<AgentUser>()
            .AnyAsync(a => a.SiteID == siteId && a.ID == userId, cancellationToken);
        if (!exists)
        {
            await _db.Insertable(new AgentUser
            {
                ID = userId,
                SiteID = siteId,
                ParentID = user.AgentID > 0 ? user.AgentID : 0,
                Status = AgentUser.AgentStatus.Normal,
                CommissionID = 0,
                ReturnID = 0,
                Permission = 0,
                CreateAt = DateTime.Now
            }).ExecuteCommandAsync(cancellationToken);
        }

        return null;
    }


    /// <summary>核心：updatecommission</summary>
    private async Task<object?> UpdatecommissionCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var userId = ReadInt(input, "UserID", "ID", "Id");
        var commissionId = ReadInt(input, "Value", "CommissionID");
        if (userId <= 0) throw Oops.Oh("#参数错误#");
        var siteId = _site.RequireSiteId();
        var n = await _db.Updateable<AgentUser>()
            .SetColumns(a => new AgentUser { CommissionID = commissionId })
            .Where(a => a.SiteID == siteId && a.ID == userId)
            .ExecuteCommandAsync(cancellationToken);
        if (n <= 0) throw Oops.Oh("#记录不存在#");
        return null;
    }


    /// <summary>核心：updateinviteopen</summary>
    private async Task<object?> UpdateinviteopenCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var inviteId = ReadString(input, "ID", "Invite", "InviteID").Trim();
        if (string.IsNullOrEmpty(inviteId))
            throw Oops.Oh("#参数错误#");
        var isOpen = ReadInt(input, "IsOpen", "Open") == 1
            || string.Equals(ReadString(input, "IsOpen"), "true", StringComparison.OrdinalIgnoreCase);
        var siteId = _site.RequireSiteId();
        var n = await _db.Updateable<UserInvite>()
            .SetColumns(i => i.IsOpen == isOpen)
            .Where(i => i.ID == inviteId && i.SiteID == siteId)
            .ExecuteCommandAsync(cancellationToken);
        if (n <= 0) throw Oops.Oh("#邀请码不存在#");
        return null;
    }


    /// <summary>核心：updatereturn</summary>
    private async Task<object?> UpdatereturnCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var userId = ReadInt(input, "UserID", "ID", "Id");
        var returnId = ReadInt(input, "Value", "ReturnID");
        if (userId <= 0) throw Oops.Oh("#参数错误#");
        var siteId = _site.RequireSiteId();
        var n = await _db.Updateable<AgentUser>()
            .SetColumns(a => new AgentUser { ReturnID = returnId })
            .Where(a => a.SiteID == siteId && a.ID == userId)
            .ExecuteCommandAsync(cancellationToken);
        if (n <= 0) throw Oops.Oh("#记录不存在#");
        return null;
    }


    /// <summary>核心：usragent/save</summary>
    private async Task<object?> UsragentSaveCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var agentId = ReadInt(input, "ID", "Id", "AgentID", "agentId");
        var inputUsers = ReadString(input, "Users", "users");
        if (string.IsNullOrWhiteSpace(inputUsers))
            throw Oops.Oh("输入不能为空!");
        if (agentId <= 0)
            throw Oops.Oh("代理无效!");

        var siteId = _site.RequireSiteId();
        var agentOk = await _db.Queryable<User>()
            .AnyAsync(u => u.SiteID == siteId && u.ID == agentId && u.Type == UserTypeAgent, cancellationToken);
        if (!agentOk)
            throw Oops.Oh("代理无效!");

        var names = inputUsers.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var count = 0;
        foreach (var name in names)
        {
            var rows = await _db.Queryable<User>()
                .Where(u => u.SiteID == siteId && u.UserName == name)
                .Take(1)
                .ToListAsync(cancellationToken);
            var user = rows.FirstOrDefault();
            if (user == null) continue;
            if (user.ID == agentId) continue;
            var isChildOfSelf = await _db.Queryable<UserDepth>()
                .AnyAsync(d => d.SiteID == siteId && d.UserID == user.ID && d.ChildID == agentId, cancellationToken);
            if (isChildOfSelf) continue;

            user.AgentID = agentId;
            await _db.Updateable(user).UpdateColumns(u => new { u.AgentID }).ExecuteCommandAsync(cancellationToken);
            count++;
        }

        return $"设置成功笔数{count}";
    }


    /// <summary>核心：saveblockgame</summary>
    private async Task<object?> SaveblockgameCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var agentId = ReadInt(input, "agentId", "AgentId", "AgentID", "ID", "Id");
        if (agentId <= 0) throw Oops.Oh("代理无效!");
        var games = ReadString(input, "Games", "games", "BlockGame");
        var siteId = _site.RequireSiteId();
        var n = await _db.Updateable<User>()
            .SetColumns(u => u.BlockGame == (games ?? string.Empty))
            .Where(u => u.SiteID == siteId && u.ID == agentId)
            .ExecuteCommandAsync(cancellationToken);
        if (n <= 0) throw Oops.Oh("代理无效!");
        return "设置成功";
    }


    private static long ParsePermissionBit(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return 0;
        if (long.TryParse(raw.Trim(), out var n)) return n;
        return raw.Trim() switch
        {
            "AccountName" => 1,
            "None" => 0,
            _ => 0
        };
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
}

/// <summary>代理信息分页入参。</summary>
public sealed class AgentInfoPageInput
{
    /// <summary>页码。</summary>
    public int Page { get; set; } = 1;

    /// <summary>页大小。</summary>
    public int PageSize { get; set; } = 20;

    /// <summary>用户名模糊。</summary>
    public string UserName { get; set; }

    /// <summary>上级代理用户名模糊。</summary>
    public string Agent { get; set; }

    /// <summary>上级筛选是否含其下级代理树。</summary>
    public bool BelongUser { get; set; }

    /// <summary>状态；null=全部。</summary>
    public byte? Status { get; set; }

    /// <summary>成为代理时间起。</summary>
    public DateTime? StartAt { get; set; }

    /// <summary>成为代理时间止（含当日）。</summary>
    public DateTime? EndAt { get; set; }
}

/// <summary>代理信息分页出参。</summary>
public sealed class AgentInfoPageOutput
{
    /// <summary>行。</summary>
    public List<AgentInfoListItemDto> Items { get; set; }

    /// <summary>总数。</summary>
    public int Total { get; set; }
}

/// <summary>代理信息列表行。</summary>
public class AgentInfoListItemDto
{
    /// <summary>编号。</summary>
    public int ID { get; set; }

    /// <summary>用户名。</summary>
    public string UserName { get; set; }

    /// <summary>上级。</summary>
    public int ParentID { get; set; }

    /// <summary>上级用户名。</summary>
    public string AgentName { get; set; }

    /// <summary>状态。</summary>
    public byte Status { get; set; }

    /// <summary>佣金方案。</summary>
    public int CommissionID { get; set; }

    /// <summary>佣金方案名。</summary>
    public string Mode { get; set; }

    /// <summary>返点方案。</summary>
    public int ReturnID { get; set; }

    /// <summary>返点方案名。</summary>
    public string ReturnMode { get; set; }

    /// <summary>下级会员数。</summary>
    public int Member { get; set; }

    /// <summary>邀请码。</summary>
    public string Invite { get; set; }

    /// <summary>域名。</summary>
    public string Domain { get; set; }

    /// <summary>禁入平台。</summary>
    public string BlockGame { get; set; }

    /// <summary>权限位。</summary>
    public long Permission { get; set; }

    /// <summary>成为代理时间。</summary>
    public DateTime CreateAt { get; set; }
}

/// <summary>代理信息详情。</summary>
public sealed class AgentInfoDetailDto : AgentInfoListItemDto
{
}

/// <summary>保存代理信息。</summary>
public sealed class AgentInfoSaveInput
{
    /// <summary>编号。</summary>
    public int ID { get; set; }

    /// <summary>上级；负数表示不改。</summary>
    public int ParentID { get; set; } = -1;

    /// <summary>状态。</summary>
    public byte Status { get; set; }

    /// <summary>佣金方案。</summary>
    public int CommissionID { get; set; }

    /// <summary>返点方案。</summary>
    public int ReturnID { get; set; }

    /// <summary>权限位。</summary>
    public long Permission { get; set; }
}
