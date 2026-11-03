using SimpX.Net.Data.Entity.Systems;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 总后台客户留言。对齐旧 /service/feedback。
/// </summary>
public sealed class SystemFeedbackService : ITransient
{
    private readonly ISqlSugarClient _db;

    /// <summary>
    /// 注入业务库。总后台留言在 BetWin30。
    /// </summary>
    /// <param name="biz">业务库</param>
    public SystemFeedbackService(IBizDbAccessor biz) => _db = biz.Db;

    /// <summary>
    /// 列表（最多 200）。
    /// </summary>
    /// <param name="name">称呼模糊</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行列表</returns>
    public async Task<List<SystemFeedbackListItemDto>> ListAsync(
        string name = null,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Queryable<SystemFeedback>();
        if (!string.IsNullOrWhiteSpace(name))
            query = query.Where(t => t.Name.Contains(name.Trim()));
        var rows = await query.OrderBy(t => t.CreateAt, OrderByType.Desc).Take(200)
            .ToListAsync(cancellationToken);
        return rows.Select(t => new SystemFeedbackListItemDto
        {
            ID = t.ID,
            Name = t.Name ?? string.Empty,
            Company = t.Company ?? string.Empty,
            Email = MaskEmail(t.Email),
            Mobile = MaskMobile(t.Mobile),
            Content = Truncate(t.Content, 120),
            CreateAt = t.CreateAt,
            IP = t.IP ?? string.Empty
        }).ToList();
    }

    /// <summary>
    /// 详情（联系方式完整，供后台处理）。
    /// </summary>
    /// <param name="id">编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>详情</returns>
    public async Task<SystemFeedbackDetailDto> DetailAsync(int id, CancellationToken cancellationToken = default)
    {
        var rows = await _db.Queryable<SystemFeedback>().Where(t => t.ID == id).Take(1)
            .ToListAsync(cancellationToken);
        var t = rows.FirstOrDefault() ?? throw Oops.Oh("#记录不存在#");
        return new SystemFeedbackDetailDto
        {
            ID = t.ID,
            Name = t.Name ?? string.Empty,
            Company = t.Company ?? string.Empty,
            Email = t.Email ?? string.Empty,
            Mobile = t.Mobile ?? string.Empty,
            Content = t.Content ?? string.Empty,
            CreateAt = t.CreateAt,
            IP = t.IP ?? string.Empty,
            IPAddress = t.IPAddress ?? string.Empty
        };
    }

    /// <summary>
    /// 删除。
    /// </summary>
    /// <param name="id">编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw Oops.Oh("#参数错误#");
        await _db.Deleteable<SystemFeedback>().In(id).ExecuteCommandAsync(cancellationToken);
    }

    private static string MaskEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            return string.IsNullOrWhiteSpace(email) ? string.Empty : "****";
        var at = email.IndexOf('@');
        return at <= 1 ? "****" + email[at..] : email[0] + "****" + email[at..];
    }

    private static string MaskMobile(string mobile)
    {
        if (string.IsNullOrWhiteSpace(mobile) || mobile.Length < 7)
            return string.IsNullOrWhiteSpace(mobile) ? string.Empty : "****";
        return mobile[..3] + "****" + mobile[^2..];
    }

    private static string Truncate(string s, int max)
    {
        if (string.IsNullOrEmpty(s) || s.Length <= max)
            return s ?? string.Empty;
        return s[..max] + "…";
    }
}

/// <summary>客户留言列表行（联系方式已脱敏）。</summary>
public sealed class SystemFeedbackListItemDto
{
    /// <summary>编号。</summary>
    public int ID { get; set; }

    /// <summary>称呼。</summary>
    public string Name { get; set; }

    /// <summary>公司。</summary>
    public string Company { get; set; }

    /// <summary>邮箱（脱敏）。</summary>
    public string Email { get; set; }

    /// <summary>手机（脱敏）。</summary>
    public string Mobile { get; set; }

    /// <summary>内容摘要。</summary>
    public string Content { get; set; }

    /// <summary>创建时间。</summary>
    public DateTime CreateAt { get; set; }

    /// <summary>IP。</summary>
    public string IP { get; set; }
}

/// <summary>客户留言详情。</summary>
public sealed class SystemFeedbackDetailDto
{
    /// <summary>编号。</summary>
    public int ID { get; set; }

    /// <summary>称呼。</summary>
    public string Name { get; set; }

    /// <summary>公司。</summary>
    public string Company { get; set; }

    /// <summary>邮箱。</summary>
    public string Email { get; set; }

    /// <summary>手机。</summary>
    public string Mobile { get; set; }

    /// <summary>内容。</summary>
    public string Content { get; set; }

    /// <summary>创建时间。</summary>
    public DateTime CreateAt { get; set; }

    /// <summary>IP。</summary>
    public string IP { get; set; }

    /// <summary>IP 归属。</summary>
    public string IPAddress { get; set; }
}
