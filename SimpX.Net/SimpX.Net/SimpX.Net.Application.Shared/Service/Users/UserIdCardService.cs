using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 租户后台证件信息后台。对齐旧版 /admin/user/member/idcard/list；不写库、不返回证件照路径。
/// </summary>
public sealed partial class UserIdCardService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;

    /// <summary>
    /// 注入业务库与当前站。证件表在业务库。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站</param>
    public UserIdCardService(IBizDbAccessor biz, ICurrentSiteAccessor site)
    {
        _db = biz.Db;
        _site = site;
    }

    /// <summary>
    /// 当前站证件分页。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页</returns>
    public async Task<UserIdCardAdminPageOutput> PageAsync(
        UserIdCardAdminPageInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var siteId = _site.RequireSiteId();
        var pageSize = Math.Clamp(input.PageSize <= 0 ? 20 : input.PageSize, 1, 100);
        var page = Math.Max(1, input.Page <= 0 ? 1 : input.Page);

        var query = _db.Queryable<UserIdCard>().Where(t => t.SiteID == siteId);

        if (!string.IsNullOrWhiteSpace(input.StartAt) && DateTime.TryParse(input.StartAt.Trim(), out var startAt))
            query = query.Where(t => t.CreateAt >= startAt);

        if (!string.IsNullOrWhiteSpace(input.EndAt) && DateTime.TryParse(input.EndAt.Trim(), out var endAt))
            query = query.Where(t => t.CreateAt <= endAt);

        if (!string.IsNullOrWhiteSpace(input.UserNumber))
            query = query.Where(t => t.CardNumber != null && t.CardNumber.Contains(input.UserNumber.Trim()));

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
                return new UserIdCardAdminPageOutput { Data = [], Total = 0 };
            query = query.Where(t => ids.Contains(t.UserID));
        }

        if (OldApiMoneyDisplay.TryParseCheckStatus(input.Status, out var status))
            query = query.Where(t => t.Status == status);

        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderBy(t => t.CreateAt, OrderByType.Desc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var userIds = rows.Select(r => r.UserID).Distinct().ToList();
        var nameMap = new Dictionary<int, string>();
        if (userIds.Count > 0)
        {
            var users = await _db.Queryable<User>()
                .Where(u => u.SiteID == siteId && userIds.Contains(u.ID))
                .Select(u => new { u.ID, u.UserName })
                .ToListAsync(cancellationToken);
            foreach (var u in users)
                nameMap[u.ID] = u.UserName ?? string.Empty;
        }

        var data = rows.Select(t => new UserIdCardAdminListItemDto
        {
            ID = t.ID,
            UserID = t.UserID,
            UserName = nameMap.TryGetValue(t.UserID, out var n) && !string.IsNullOrEmpty(n)
                ? n
                : (t.UserName ?? string.Empty),
            CardName = t.CardName ?? string.Empty,
            CardNumber = t.CardNumber ?? string.Empty,
            CardCity = t.CardCity ?? string.Empty,
            CardCreateAt = t.CardCreateAt,
            CardBirthday = t.CardBirthday,
            CardType = t.CardType,
            Status = t.Status,
            Remark = t.Remark ?? string.Empty,
            CreateAt = t.CreateAt,
            Checker = t.Checker,
            CheckDateTime = t.CheckDateTime
        }).ToList();

        return new UserIdCardAdminPageOutput
        {
            Data = data,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// 保存。对齐旧 createidcard：正反面/国家/姓名/证件号/会员必填；黑名单证件禁止。
    /// </summary>
    /// <param name="input">入参</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SaveAsync(UserIdCardSaveInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (string.IsNullOrWhiteSpace(input.CardNumber))
            throw Oops.Oh("#请输入证件号#");

        var siteId = _site.RequireSiteId();
        if (input.ID == 0)
        {
            if (input.UserID <= 0
                || string.IsNullOrWhiteSpace(input.CardFront)
                || string.IsNullOrWhiteSpace(input.CardBack)
                || string.IsNullOrWhiteSpace(input.CardName)
                || string.IsNullOrWhiteSpace(input.Country)
                || string.IsNullOrWhiteSpace(input.UserName))
                throw Oops.Oh("资料不全");

            var cardNumber = input.CardNumber.Trim();
            var dup = await _db.Queryable<UserIdCard>()
                .AnyAsync(t => t.SiteID == siteId && t.CardNumber == cardNumber, cancellationToken);
            if (dup)
                throw Oops.Oh("证件已经被使用,禁止绑定");

            var black = await _db.Queryable<BlackKycImport>()
                .AnyAsync(t => t.BlackKyc == cardNumber, cancellationToken);
            if (black)
                throw Oops.Oh("黑名单证件,禁止绑定");

            var entity = new UserIdCard
            {
                SiteID = siteId,
                UserID = input.UserID,
                UserName = input.UserName.Trim(),
                CardName = input.CardName.Trim(),
                CardNumber = cardNumber,
                CardType = input.CardType == 0 ? 1 : input.CardType,
                Status = input.Status,
                Remark = input.Remark ?? string.Empty,
                CardCity = input.CardCity ?? string.Empty,
                Country = input.Country.Trim(),
                Gender = input.Gender,
                CardFront = input.CardFront.Trim(),
                CardBack = input.CardBack.Trim(),
                CardCreateAt = input.CardCreateAt ?? DateTime.Parse("1900-01-01"),
                CardBirthday = input.CardBirthday ?? DateTime.Parse("1900-01-01"),
                CreateAt = DateTime.Now
            };
            await _db.Insertable(entity).ExecuteCommandAsync(cancellationToken);
            return;
        }

        var existing = await _db.Queryable<UserIdCard>()
            .Where(t => t.ID == input.ID && t.SiteID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var row = existing.FirstOrDefault() ?? throw Oops.Oh("#证件不存在!#");
        row.CardName = input.CardName ?? row.CardName;
        row.CardNumber = input.CardNumber.Trim();
        row.CardType = input.CardType;
        row.Status = input.Status;
        row.Remark = input.Remark ?? string.Empty;
        row.CardCity = input.CardCity ?? row.CardCity;
        row.Country = input.Country ?? row.Country;
        row.Gender = input.Gender;
        if (!string.IsNullOrWhiteSpace(input.CardFront))
            row.CardFront = input.CardFront.Trim();
        if (!string.IsNullOrWhiteSpace(input.CardBack))
            row.CardBack = input.CardBack.Trim();
        if (input.CardCreateAt.HasValue)
            row.CardCreateAt = input.CardCreateAt.Value;
        if (input.CardBirthday.HasValue)
            row.CardBirthday = input.CardBirthday.Value;
        row.CheckDateTime = DateTime.Now;
        await _db.Updateable(row).ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 删除。
    /// </summary>
    /// <param name="id">编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw Oops.Oh("#编号错误#");
        var siteId = _site.RequireSiteId();
        var ok = await _db.Queryable<UserIdCard>()
            .AnyAsync(t => t.ID == id && t.SiteID == siteId, cancellationToken);
        if (!ok)
            throw Oops.Oh("#编号错误#");
        await _db.Deleteable<UserIdCard>().Where(t => t.ID == id && t.SiteID == siteId)
            .ExecuteCommandAsync(cancellationToken);
    }
}
