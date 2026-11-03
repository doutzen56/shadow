using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 租户后台电销会员列表（联系方式脱敏，不发短信）。对齐旧 /user/customer/tel 列表面。
/// </summary>
public sealed class CustomerTelService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;

    /// <summary>
    /// 注入依赖。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站</param>
    public CustomerTelService(IBizDbAccessor biz, ICurrentSiteAccessor site)
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
    public async Task<CustomerTelPageOutput> PageAsync(
        CustomerTelPageInput input,
        CancellationToken cancellationToken = default)
    {
        input ??= new CustomerTelPageInput();
        var siteId = _site.RequireSiteId();
        var page = input.Page <= 0 ? 1 : input.Page;
        var pageSize = input.PageSize <= 0 ? 20 : Math.Min(input.PageSize, 100);

        var query = _db.Queryable<User>().Where(u => u.SiteID == siteId);
        if (!string.IsNullOrWhiteSpace(input.UserName))
            query = query.Where(u => u.UserName.Contains(input.UserName.Trim()));
        if (input.GroupID > 0)
            query = query.Where(u => u.GroupID == input.GroupID);
        if (input.HasMobile == true)
            query = query.Where(u => u.Mobile != null && u.Mobile != string.Empty);
        if (input.HasMobile == false)
            query = query.Where(u => u.Mobile == null || u.Mobile == string.Empty);

        RefAsync<int> total = 0;
        var rows = await query.OrderBy(u => u.ID, OrderByType.Desc)
            .Select(u => new CustomerTelListItemDto
            {
                ID = u.ID,
                UserName = u.UserName,
                Mobile = u.Mobile,
                Email = u.Email,
                GroupID = u.GroupID,
                CreateAt = u.CreateAt
            })
            .ToPageListAsync(page, pageSize, total, cancellationToken);

        foreach (var row in rows)
        {
            row.Mobile = MaskMobile(row.Mobile);
            row.Email = MaskEmail(row.Email);
        }

        return new CustomerTelPageOutput { Items = rows, Total = total };
    }

    private static string MaskMobile(string mobile)
    {
        if (string.IsNullOrWhiteSpace(mobile) || mobile.Length < 7)
            return string.IsNullOrWhiteSpace(mobile) ? string.Empty : "****";
        return mobile[..3] + "****" + mobile[^2..];
    }

    private static string MaskEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            return string.IsNullOrWhiteSpace(email) ? string.Empty : "****";
        var at = email.IndexOf('@');
        return at <= 1 ? "****" + email[at..] : email[0] + "****" + email[at..];
    }
}

/// <summary>电销列表筛选。</summary>
public sealed class CustomerTelPageInput
{
    /// <summary>页码。</summary>
    public int Page { get; set; } = 1;

    /// <summary>页大小。</summary>
    public int PageSize { get; set; } = 20;

    /// <summary>用户名模糊。</summary>
    public string UserName { get; set; }

    /// <summary>分组；0=全部。</summary>
    public int GroupID { get; set; }

    /// <summary>是否有手机；null=全部。</summary>
    public bool? HasMobile { get; set; }
}

/// <summary>电销分页出参。</summary>
public sealed class CustomerTelPageOutput
{
    /// <summary>行。</summary>
    public List<CustomerTelListItemDto> Items { get; set; }

    /// <summary>总数。</summary>
    public int Total { get; set; }
}

/// <summary>电销列表行。</summary>
public sealed class CustomerTelListItemDto
{
    /// <summary>编号。</summary>
    public int ID { get; set; }

    /// <summary>用户名。</summary>
    public string UserName { get; set; }

    /// <summary>手机（脱敏）。</summary>
    public string Mobile { get; set; }

    /// <summary>邮箱（脱敏）。</summary>
    public string Email { get; set; }

    /// <summary>分组。</summary>
    public int GroupID { get; set; }

    /// <summary>注册时间。</summary>
    public DateTime CreateAt { get; set; }
}
