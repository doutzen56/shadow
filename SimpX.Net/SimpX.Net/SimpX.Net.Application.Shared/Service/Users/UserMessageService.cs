using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 租户后台站内信息。对齐旧 /user/customer/message。
/// </summary>
public sealed partial class UserMessageService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;
    private readonly UserManager _userManager;

    /// <summary>
    /// 注入依赖。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站</param>
    /// <param name="userManager">后台用户</param>
    public UserMessageService(IBizDbAccessor biz, ICurrentSiteAccessor site, UserManager userManager)
    {
        _db = biz.Db;
        _site = site;
        _userManager = userManager;
    }

    /// <summary>
    /// 分页列表。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页</returns>
    public async Task<UserMessagePageOutput> PageAsync(
        UserMessagePageInput input,
        CancellationToken cancellationToken = default)
    {
        input ??= new UserMessagePageInput();
        var siteId = _site.RequireSiteId();
        var page = input.Page <= 0 ? 1 : input.Page;
        var pageSize = input.PageSize <= 0 ? 20 : Math.Min(input.PageSize, 100);

        var query = _db.Queryable<UserMessage>().Where(m => m.SiteID == siteId);
        if (input.UserID > 0)
            query = query.Where(m => m.UserID == input.UserID);
        if (!string.IsNullOrWhiteSpace(input.User))
        {
            var name = input.User.Trim();
            var uidRows = await _db.Queryable<User>()
                .Where(u => u.SiteID == siteId && u.UserName.Contains(name))
                .Select(u => u.ID)
                .Take(500)
                .ToListAsync(cancellationToken);
            if (uidRows.Count == 0)
                return new UserMessagePageOutput { Items = [], Total = 0 };
            query = query.Where(m => uidRows.Contains(m.UserID));
        }
        if (!string.IsNullOrWhiteSpace(input.Title))
            query = query.Where(m => m.Title.Contains(input.Title.Trim()));
        if (input.IsRead.HasValue)
            query = query.Where(m => m.IsRead == input.IsRead.Value);
        if (input.StartAt.HasValue)
            query = query.Where(m => m.CreateAt >= input.StartAt.Value);
        if (input.EndAt.HasValue)
            query = query.Where(m => m.CreateAt <= input.EndAt.Value);

        RefAsync<int> total = 0;
        var rows = await query.OrderBy(m => m.CreateAt, OrderByType.Desc)
            .Select(m => new UserMessageListItemDto
            {
                ID = m.ID,
                UserID = m.UserID,
                SendID = m.SendID,
                Type = m.Type,
                Title = m.Title,
                IsRead = m.IsRead,
                CreateAt = m.CreateAt,
                ReadAt = m.ReadAt
            })
            .ToPageListAsync(page, pageSize, total, cancellationToken);

        if (rows.Count > 0)
        {
            var uids = rows.Select(r => r.UserID).Distinct().ToList();
            var nameMap = await _db.Queryable<User>()
                .Where(u => u.SiteID == siteId && uids.Contains(u.ID))
                .Select(u => new { u.ID, u.UserName })
                .ToListAsync(cancellationToken);
            var dict = nameMap.ToDictionary(x => x.ID, x => x.UserName ?? string.Empty);
            foreach (var r in rows)
                r.UserName = dict.TryGetValue(r.UserID, out var n) ? n : string.Empty;
        }

        return new UserMessagePageOutput { Items = rows, Total = total };
    }

    /// <summary>
    /// 详情。
    /// </summary>
    /// <param name="id">编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>详情</returns>
    public async Task<UserMessageDetailDto> DetailAsync(int id, CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<UserMessage>()
            .Where(m => m.SiteID == siteId && m.ID == id).Take(1).ToListAsync(cancellationToken);
        var m = rows.FirstOrDefault() ?? throw Oops.Oh("#记录不存在#");
        var nameRows = await _db.Queryable<User>()
            .Where(u => u.SiteID == siteId && u.ID == m.UserID)
            .Select(u => u.UserName)
            .Take(1)
            .ToListAsync(cancellationToken);
        return new UserMessageDetailDto
        {
            ID = m.ID,
            UserID = m.UserID,
            UserName = nameRows.FirstOrDefault() ?? string.Empty,
            SendID = m.SendID,
            Type = m.Type,
            Title = m.Title ?? string.Empty,
            Content = m.Content ?? string.Empty,
            IsRead = m.IsRead,
            CreateAt = m.CreateAt,
            ReadAt = m.ReadAt,
            SupportCurrency = m.SupportCurrency ?? string.Empty
        };
    }

    /// <summary>
    /// 发送站内信。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>编号</returns>
    public async Task<int> SendAsync(UserMessageSendInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (string.IsNullOrWhiteSpace(input.Title))
            throw Oops.Oh("#标题不能为空#");
        var siteId = _site.RequireSiteId();
        var userIds = await ResolveRecipientsAsync(siteId, input, cancellationToken);
        if (userIds.Count == 0)
            throw Oops.Oh("#请选择收件会员#");

        var sendId = _userManager.UserId > 0 ? (int)_userManager.UserId : 0;
        var title = input.Title.Trim();
        var content = input.Content ?? string.Empty;
        var currency = input.SupportCurrency ?? string.Empty;
        var now = DateTime.Now;
        var lastId = 0;
        foreach (var uid in userIds)
        {
            lastId = await _db.Insertable(new UserMessage
            {
                SiteID = siteId,
                UserID = uid,
                SendID = sendId,
                Type = input.Type,
                Title = title,
                Content = content,
                SupportCurrency = currency,
                IsRead = false,
                CreateAt = now,
                ReadAt = DateTime.MinValue
            }).ExecuteReturnIdentityAsync(cancellationToken);
        }

        return lastId;
    }

    /// <summary>解析收件人：UserID / 用户名 CSV / 分组 CSV（对齐旧 User + GroupID[]）。</summary>
    private async Task<List<int>> ResolveRecipientsAsync(
        int siteId,
        UserMessageSendInput input,
        CancellationToken cancellationToken)
    {
        var ids = new HashSet<int>();
        if (input.UserID > 0)
            ids.Add(input.UserID);

        var names = (input.User ?? string.Empty)
            .Split(new[] { ',', ';', '|', '，', ' ' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(200)
            .ToList();
        if (names.Count > 0)
        {
            var rows = await _db.Queryable<User>()
                .Where(u => u.SiteID == siteId && names.Contains(u.UserName))
                .Select(u => u.ID)
                .Take(500)
                .ToListAsync(cancellationToken);
            foreach (var id in rows)
                ids.Add(id);
        }

        var groupIds = (input.Groups ?? string.Empty)
            .Split(new[] { ',', ';', '|', '，' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => int.TryParse(s.Trim(), out var n) ? n : 0)
            .Where(n => n > 0)
            .Distinct()
            .Take(50)
            .ToList();
        if (groupIds.Count > 0)
        {
            var rows = await _db.Queryable<User>()
                .Where(u => u.SiteID == siteId && groupIds.Contains(u.GroupID))
                .Select(u => u.ID)
                .Take(2000)
                .ToListAsync(cancellationToken);
            foreach (var id in rows)
                ids.Add(id);
        }

        return ids.ToList();
    }

    /// <summary>
    /// 删除。
    /// </summary>
    /// <param name="id">编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        await _db.Deleteable<UserMessage>()
            .Where(m => m.SiteID == siteId && m.ID == id)
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>未读数量（对齐 /request/member/message/unreadmsgqty）。</summary>
    /// <param name="userId">会员</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>{ Qty }</returns>
    public async Task<object> UnreadQtyAsync(int userId, CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
            throw Oops.Oh("#请先登录#");
        var siteId = _site.RequireSiteId();
        var qty = await _db.Queryable<UserMessage>()
            .Where(m => m.SiteID == siteId && m.UserID == userId && !m.IsRead)
            .CountAsync(cancellationToken);
        return new { Qty = qty };
    }

    /// <summary>全部标已读（对齐 /request/member/message/read）。</summary>
    /// <param name="userId">会员</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task MarkAllReadAsync(int userId, CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
            throw Oops.Oh("#请先登录#");
        var siteId = _site.RequireSiteId();
        await _db.Updateable<UserMessage>()
            .SetColumns(m => new UserMessage { IsRead = true, ReadAt = DateTime.Now })
            .Where(m => m.SiteID == siteId && m.UserID == userId && !m.IsRead)
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>单条标已读。</summary>
    /// <param name="userId">会员</param>
    /// <param name="id">消息编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task MarkReadAsync(int userId, int id, CancellationToken cancellationToken = default)
    {
        if (userId <= 0 || id <= 0)
            throw Oops.Oh("#参数错误#");
        var siteId = _site.RequireSiteId();
        await _db.Updateable<UserMessage>()
            .SetColumns(m => new UserMessage { IsRead = true, ReadAt = DateTime.Now })
            .Where(m => m.SiteID == siteId && m.UserID == userId && m.ID == id && !m.IsRead)
            .ExecuteCommandAsync(cancellationToken);
    }

    // --- merged from UserMessageService.WaveGaps.cs ---



    /// <summary>
    /// 对齐旧 update。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> UpdateAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return UpdateCoreAsync(input, cancellationToken);
    }


    /// <summary>核心：update（对齐旧 MessageAgent.UpdateMessage）。</summary>
    private async Task<object?> UpdateCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var siteId = _site.RequireSiteId();
        var id = ReadInt(input, "ID", "Id", "MsgID");
        if (id <= 0)
            throw Oops.Oh("#编号错误#");

        var rows = await _db.Queryable<UserMessage>()
            .Where(m => m.SiteID == siteId && m.ID == id)
            .Take(1)
            .ToListAsync(cancellationToken);
        var msg = rows.FirstOrDefault() ?? throw Oops.Oh("#编号错误#");

        if (TryReadString(input, out var title, "Title", "title") && !string.IsNullOrWhiteSpace(title))
            msg.Title = title;
        if (TryReadString(input, out var content, "Content", "content", "Body") && content is not null)
            msg.Content = content;
        if (input.TryGetValue("Type", out var typeObj) && typeObj is not null
            && int.TryParse(typeObj.ToString(), out var type))
            msg.Type = type;
        if (TryReadString(input, out var currency, "SupportCurrency") && currency is not null)
            msg.SupportCurrency = currency;

        var affected = await _db.Updateable(msg)
            .UpdateColumns(m => new { m.Title, m.Content, m.Type, m.SupportCurrency })
            .Where(m => m.SiteID == siteId && m.ID == id)
            .ExecuteCommandAsync(cancellationToken);
        if (affected != 1)
            throw Oops.Oh("#保存失败#");
        return true;
    }

    private static bool TryReadString(Dictionary<string, object?> input, out string? value, params string[] keys)
    {
        value = null;
        foreach (var k in keys)
        {
            if (!input.TryGetValue(k, out var v) || v is null) continue;
            value = v.ToString();
            return true;
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
}

/// <summary>站内信分页入参。</summary>
public sealed class UserMessagePageInput
{
    /// <summary>页码。</summary>
    public int Page { get; set; } = 1;

    /// <summary>页大小。</summary>
    public int PageSize { get; set; } = 20;

    /// <summary>收件会员；0=全部。</summary>
    public int UserID { get; set; }

    /// <summary>用户名模糊。</summary>
    public string User { get; set; }

    /// <summary>标题模糊。</summary>
    public string Title { get; set; }

    /// <summary>已读；null=全部。</summary>
    public bool? IsRead { get; set; }

    /// <summary>开始时间。</summary>
    public DateTime? StartAt { get; set; }

    /// <summary>结束时间。</summary>
    public DateTime? EndAt { get; set; }
}

/// <summary>站内信分页出参。</summary>
public sealed class UserMessagePageOutput
{
    /// <summary>行。</summary>
    public List<UserMessageListItemDto> Items { get; set; }

    /// <summary>总数。</summary>
    public int Total { get; set; }
}

/// <summary>站内信列表行。</summary>
public class UserMessageListItemDto
{
    /// <summary>编号。</summary>
    public int ID { get; set; }

    /// <summary>收件会员。</summary>
    public int UserID { get; set; }

    /// <summary>收件用户名。</summary>
    public string UserName { get; set; }

    /// <summary>发送者。</summary>
    public int SendID { get; set; }

    /// <summary>类型。</summary>
    public int Type { get; set; }

    /// <summary>标题。</summary>
    public string Title { get; set; }

    /// <summary>已读。</summary>
    public bool IsRead { get; set; }

    /// <summary>创建时间。</summary>
    public DateTime CreateAt { get; set; }

    /// <summary>阅读时间。</summary>
    public DateTime ReadAt { get; set; }
}

/// <summary>站内信详情。</summary>
public sealed class UserMessageDetailDto : UserMessageListItemDto
{
    /// <summary>正文。</summary>
    public string Content { get; set; }

    /// <summary>支持货币。</summary>
    public string SupportCurrency { get; set; }
}

/// <summary>发送站内信。</summary>
public sealed class UserMessageSendInput
{
    /// <summary>收件会员编号（可选；可与 User/Groups 并用）。</summary>
    public int UserID { get; set; }

    /// <summary>收件用户名 CSV（对齐旧 User）。</summary>
    public string User { get; set; }

    /// <summary>会员分组 CSV（对齐旧 GroupID[]）。</summary>
    public string Groups { get; set; }

    /// <summary>标题。</summary>
    public string Title { get; set; }

    /// <summary>正文。</summary>
    public string Content { get; set; }

    /// <summary>类型。</summary>
    public int Type { get; set; }

    /// <summary>支持货币。</summary>
    public string SupportCurrency { get; set; }
}
