using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 租户后台短信邮件发送日志。对齐旧 /user/customer/sms。手机号列表脱敏。
/// </summary>
public sealed class SmsLogService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;

    /// <summary>
    /// 注入依赖。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站</param>
    public SmsLogService(IBizDbAccessor biz, ICurrentSiteAccessor site)
    {
        _db = biz.Db;
        _site = site;
    }

    /// <summary>
    /// 分页列表。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页</returns>
    public async Task<SmsLogPageOutput> PageAsync(
        SmsLogPageInput input,
        CancellationToken cancellationToken = default)
    {
        input ??= new SmsLogPageInput();
        var siteId = _site.RequireSiteId();
        var page = input.Page <= 0 ? 1 : input.Page;
        var pageSize = input.PageSize <= 0 ? 20 : Math.Min(input.PageSize, 100);

        var query = _db.Queryable<SMSLog, User>((s, u) => new JoinQueryInfos(
                JoinType.Left, s.UserID == u.ID && s.SiteID == u.SiteID))
            .Where((s, u) => s.SiteID == siteId);

        if (!string.IsNullOrWhiteSpace(input.User))
        {
            var key = input.User.Trim();
            if (int.TryParse(key, out var uid))
                query = query.Where((s, u) => s.UserID == uid || u.UserName.Contains(key));
            else
                query = query.Where((s, u) => u.UserName.Contains(key));
        }

        if (!string.IsNullOrWhiteSpace(input.Mobile))
            query = query.Where((s, u) => s.Mobile.Contains(input.Mobile.Trim()));
        if (input.IsSuccess.HasValue)
            query = query.Where((s, u) => s.IsSuccess == input.IsSuccess.Value);
        if (input.StartAt.HasValue)
            query = query.Where((s, u) => s.CreateAt >= input.StartAt.Value);
        if (input.EndAt.HasValue)
            query = query.Where((s, u) => s.CreateAt <= input.EndAt.Value);

        RefAsync<int> total = 0;
        var rows = await query.OrderBy((s, u) => s.CreateAt, OrderByType.Desc)
            .Select((s, u) => new SmsLogListItemDto
            {
                ID = s.ID,
                UserID = s.UserID,
                UserName = u.UserName,
                Mobile = s.Mobile,
                Content = s.Content,
                IsSuccess = s.IsSuccess,
                ResultMsg = s.ResultMsg,
                CreateAt = s.CreateAt
            })
            .ToPageListAsync(page, pageSize, total, cancellationToken);

        foreach (var row in rows)
            row.Mobile = MaskContact(row.Mobile);

        return new SmsLogPageOutput { Items = rows, Total = total };
    }

    /// <summary>
    /// 写入一条电销短信日志。对齐旧 AddSMSLog。
    /// </summary>
    public async Task AddLogAsync(
        int userId,
        string mobile,
        string content,
        bool success,
        string resultMsg,
        CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        await _db.Insertable(new SMSLog
        {
            SiteID = siteId,
            UserID = userId,
            Mobile = mobile ?? string.Empty,
            Content = content ?? string.Empty,
            IsSuccess = success,
            ResultMsg = resultMsg ?? string.Empty,
            CreateAt = DateTime.Now
        }).ExecuteCommandAsync(cancellationToken);
    }

    private static string MaskContact(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;
        if (value.Contains('@'))
        {
            var at = value.IndexOf('@');
            return at <= 1 ? "****" + value[at..] : value[0] + "****" + value[at..];
        }

        if (value.Length < 7)
            return "****";
        return value[..3] + "****" + value[^2..];
    }
}

/// <summary>短信日志分页入参。</summary>
public sealed class SmsLogPageInput
{
    /// <summary>页码。</summary>
    public int Page { get; set; } = 1;

    /// <summary>页大小。</summary>
    public int PageSize { get; set; } = 20;

    /// <summary>用户编号或用户名。</summary>
    public string User { get; set; }

    /// <summary>手机/邮箱模糊。</summary>
    public string Mobile { get; set; }

    /// <summary>是否成功；null=全部。</summary>
    public bool? IsSuccess { get; set; }

    /// <summary>开始时间。</summary>
    public DateTime? StartAt { get; set; }

    /// <summary>结束时间。</summary>
    public DateTime? EndAt { get; set; }
}

/// <summary>短信日志分页出参。</summary>
public sealed class SmsLogPageOutput
{
    /// <summary>行。</summary>
    public List<SmsLogListItemDto> Items { get; set; }

    /// <summary>总数。</summary>
    public int Total { get; set; }
}

/// <summary>短信日志行。</summary>
public sealed class SmsLogListItemDto
{
    /// <summary>编号。</summary>
    public int ID { get; set; }

    /// <summary>会员编号。</summary>
    public int UserID { get; set; }

    /// <summary>用户名。</summary>
    public string UserName { get; set; }

    /// <summary>手机/邮箱（脱敏）。</summary>
    public string Mobile { get; set; }

    /// <summary>内容。</summary>
    public string Content { get; set; }

    /// <summary>是否成功。</summary>
    public bool IsSuccess { get; set; }

    /// <summary>网关返回。</summary>
    public string ResultMsg { get; set; }

    /// <summary>发送时间。</summary>
    public DateTime CreateAt { get; set; }
}
