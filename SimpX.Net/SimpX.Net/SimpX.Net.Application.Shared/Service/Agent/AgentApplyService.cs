using SimpX.Net.Data.Entity.Agents;
using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 租户后台代理审核。对齐旧版 /admin/user/agent/info/applylist|execapply。
/// </summary>
public sealed class AgentApplyService : ITransient
{
    /// <summary>待审核。</summary>
    public const int StatusNone = 0;

    /// <summary>拒绝。</summary>
    public const int StatusReject = 1;

    /// <summary>通过。</summary>
    public const int StatusPass = 2;

    /// <summary>会员（User.Type）。</summary>
    public const int UserTypeMember = 0;

    /// <summary>代理（User.Type）。</summary>
    public const int UserTypeAgent = 1;

    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;
    private readonly IUserMessageNotifyService _messages;

    /// <summary>
    /// 注入业务库、当前站与站内信。代理申请和会员表在 BetWin30 业务库，不能走主库 Sqlite。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站</param>
    /// <param name="messages">站内信；可空，单测可省略</param>
    public AgentApplyService(
        IBizDbAccessor biz,
        ICurrentSiteAccessor site,
        IUserMessageNotifyService messages = null)
    {
        _db = biz.Db;
        _site = site;
        _messages = messages;
    }

    /// <summary>
    /// 当前站代理申请分页。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页</returns>
    public async Task<AgentApplyPageOutput> PageAsync(
        AgentApplyPageInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var siteId = _site.RequireSiteId();
        var pageSize = Math.Clamp(input.PageSize <= 0 ? 20 : input.PageSize, 1, 100);
        var page = Math.Max(1, input.Page <= 0 ? 1 : input.Page);

        var query = _db.Queryable<AgentApply>().Where(a => a.SiteID == siteId);
        if (!string.IsNullOrWhiteSpace(input.User))
        {
            var name = input.User.Trim();
            var userIds = await _db.Queryable<User>()
                .Where(u => u.SiteID == siteId && u.UserName != null && u.UserName.Contains(name))
                .Select(u => u.ID)
                .ToListAsync(cancellationToken);
            query = query.Where(a => userIds.Contains(a.UserID));
        }

        if (input.Status.HasValue)
            query = query.Where(a => a.Status == input.Status.Value);
        if (input.StartAt.HasValue && input.StartAt.Value != default)
            query = query.Where(a => a.CreateAt >= input.StartAt.Value);
        if (input.EndAt.HasValue && input.EndAt.Value != default)
            query = query.Where(a => a.CreateAt < input.EndAt.Value.Date.AddDays(1));

        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderBy(a => a.CreateAt, OrderByType.Desc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var userIdSet = rows.Select(r => r.UserID).Where(id => id > 0).Distinct().ToList();
        var users = userIdSet.Count == 0
            ? new List<User>()
            : await _db.Queryable<User>()
                .Where(u => u.SiteID == siteId && userIdSet.Contains(u.ID))
                .ToListAsync(cancellationToken);
        var nameMap = users.ToDictionary(u => u.ID, u => u.UserName ?? string.Empty);

        var data = rows.Select(a => new AgentApplyListItemDto
        {
            ID = a.ID,
            UserID = a.UserID,
            UserName = nameMap.TryGetValue(a.UserID, out var un) ? un : string.Empty,
            CreateAt = a.CreateAt,
            Status = a.Status,
            StatusValue = StatusText(a.Status),
            Skype = a.Skype ?? string.Empty,
            QQ = a.QQ ?? string.Empty,
            Area = a.Area ?? string.Empty,
            Phone = a.Phone ?? string.Empty,
            Telegram = a.Telegram ?? string.Empty,
            Line = a.Line ?? string.Empty,
            WhatsApp = a.WhatsApp ?? string.Empty,
            Content = a.Content ?? string.Empty
        }).ToList();

        return new AgentApplyPageOutput
        {
            RecordCount = total,
            Page = page,
            PageSize = pageSize,
            Data = data
        };
    }

    /// <summary>
    /// 审核代理申请。通过则改 User.Type=Agent 并写入 agent_User；拒绝只改状态。站内信失败不回滚。
    /// </summary>
    /// <param name="input">审核入参</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task CheckAsync(AgentApplyCheckInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.ID <= 0)
            throw Oops.Oh("#编号错误#");
        if (input.Status is not (StatusReject or StatusPass))
            throw Oops.Oh("#没有选择要处理的动作#");

        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<AgentApply>()
            .Where(a => a.ID == input.ID && a.SiteID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var apply = rows.FirstOrDefault() ?? throw Oops.Oh("#编号错误#");
        if (apply.Status != StatusNone)
            throw Oops.Oh("#该笔申请已被审核#");

        if (input.Status == StatusPass)
        {
            var users = await _db.Queryable<User>()
                .Where(u => u.ID == apply.UserID && u.SiteID == siteId)
                .Take(1)
                .ToListAsync(cancellationToken);
            var user = users.FirstOrDefault() ?? throw Oops.Oh("#用户参数错误#");
            if (user.Type != UserTypeMember)
                throw Oops.Oh("#用户参数错误#");

            // 有上级时上级必须已是代理（对齐旧 UpdateUserType）
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

            user.Type = UserTypeAgent;
            await _db.Updateable(user).UpdateColumns(u => new { u.Type }).ExecuteCommandAsync(cancellationToken);

            var exists = await _db.Queryable<AgentUser>()
                .AnyAsync(a => a.ID == user.ID, cancellationToken);
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

        apply.Status = input.Status;
        await _db.Updateable(apply).UpdateColumns(a => new { a.Status }).ExecuteCommandAsync(cancellationToken);

        await TrySendMessageAsync(
            siteId,
            apply.UserID,
            input.Status == StatusPass ? "#您的代理申请已经通过#" : "#您的代理申请被拒绝#",
            cancellationToken);
    }

    /// <summary>
    /// 状态文案。
    /// </summary>
    /// <param name="status">状态值</param>
    /// <returns>文案</returns>
    private static string StatusText(int status) => status switch
    {
        StatusNone => "#待审核#",
        StatusReject => "#拒绝#",
        StatusPass => "#通过#",
        _ => status.ToString()
    };

    /// <summary>
    /// 写站内信；失败只打日志，不回滚审核结果。
    /// </summary>
    /// <param name="siteId">站点</param>
    /// <param name="userId">会员</param>
    /// <param name="content">内容</param>
    /// <param name="cancellationToken">取消令牌</param>
    private async Task TrySendMessageAsync(
        int siteId,
        int userId,
        string content,
        CancellationToken cancellationToken)
    {
        if (_messages == null)
            return;
        try
        {
            await _messages.SendNotifyAsync(siteId, userId, content, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            Log.Warning($"代理审核站内信失败 ApplyUser={userId}：{ex.Message}");
        }
    }
}
