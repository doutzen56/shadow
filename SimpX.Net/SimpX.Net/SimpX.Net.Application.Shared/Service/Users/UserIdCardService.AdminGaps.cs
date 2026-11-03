using System.Text.Json.Serialization;
using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 证件信息缺口：详情、审核、重置、查会员、创建别名。
/// </summary>
public sealed partial class UserIdCardService
{
    /// <summary>旧 CheckStatus.Success。</summary>
    public const int StatusSuccess = 2;

    /// <summary>
    /// 证件详情。对齐旧 info。
    /// </summary>
    /// <param name="id">证件编号</param>
    /// <param name="userId">会员编号（可与 id 二选一）</param>
    /// <param name="status">状态过滤</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>详情</returns>
    public async Task<UserIdCardDetailDto> InfoAsync(
        int id,
        int userId,
        int status,
        CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var query = _db.Queryable<UserIdCard>().Where(t => t.SiteID == siteId);
        if (id > 0)
            query = query.Where(t => t.ID == id);
        if (userId > 0)
            query = query.Where(t => t.UserID == userId);
        if (status != 0)
            query = query.Where(t => t.Status == status);

        var rows = await query.Take(1).ToListAsync(cancellationToken);
        var model = rows.FirstOrDefault() ?? throw Oops.Oh("证件不存在!");
        var sentinel = DateTime.Parse("1900-01-01");
        return new UserIdCardDetailDto
        {
            ID = model.ID,
            CardName = model.CardName ?? string.Empty,
            CardNumber = model.CardNumber ?? string.Empty,
            Remark = model.Remark ?? string.Empty,
            CardFront = model.CardFront ?? string.Empty,
            CardBack = model.CardBack ?? string.Empty,
            Status = model.Status,
            CardCity = model.CardCity ?? string.Empty,
            CardCreateAt = model.CardCreateAt <= sentinel ? "N/A" : model.CardCreateAt.ToShortDateString(),
            CardBirthday = model.CardBirthday <= sentinel ? "N/A" : model.CardBirthday.ToShortDateString(),
            CardType = model.CardType.ToString(),
            Gender = model.Gender.ToString(),
            Country = model.Country ?? string.Empty
        };
    }

    /// <summary>
    /// 审核证件。对齐旧 updateinfo。
    /// </summary>
    /// <param name="input">入参</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task UpdateInfoAsync(UserIdCardUpdateInfoInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.ID <= 0)
            throw Oops.Oh("#编号错误#");
        var siteId = _site.RequireSiteId();

        if (input.Status == StatusSuccess)
        {
            var cardNumber = (input.CardNumber ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(cardNumber))
                throw Oops.Oh("CardNumber is Error");
            var dup = await _db.Queryable<UserIdCard>()
                .AnyAsync(
                    t => t.SiteID == siteId
                         && t.ID != input.ID
                         && t.CardNumber == cardNumber
                         && t.Status == StatusSuccess,
                    cancellationToken);
            if (dup)
                throw Oops.Oh($"CardNumber [{cardNumber}] is Exist");
        }

        var rows = await _db.Queryable<UserIdCard>()
            .Where(t => t.ID == input.ID && t.SiteID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var row = rows.FirstOrDefault() ?? throw Oops.Oh("#证件不存在!#");
        row.Status = input.Status;
        row.Remark = input.Remark ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(input.CardNumber))
            row.CardNumber = input.CardNumber.Trim();
        row.CheckDateTime = DateTime.Now;
        await _db.Updateable(row)
            .UpdateColumns(t => new { t.Status, t.Remark, t.CardNumber, t.CheckDateTime })
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 按会员清空证件。对齐旧 reset。
    /// </summary>
    /// <param name="userId">会员</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task ResetAsync(int userId, CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
            throw Oops.Oh("#参数错误#");
        var siteId = _site.RequireSiteId();
        await _db.Deleteable<UserIdCard>()
            .Where(t => t.SiteID == siteId && t.UserID == userId)
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 按用户名查会员编号。对齐旧 verifyuser。
    /// </summary>
    /// <param name="userName">用户名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>编号</returns>
    public async Task<UserIdCardVerifyUserOutput> VerifyUserAsync(
        string userName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userName))
            throw Oops.Oh("#用户不存在#");
        var siteId = _site.RequireSiteId();
        var key = userName.Trim();
        var rows = await _db.Queryable<User>()
            .Where(u => u.SiteID == siteId && u.UserName == key)
            .Select(u => u.ID)
            .Take(1)
            .ToListAsync(cancellationToken);
        var id = rows.FirstOrDefault();
        if (id == 0)
            throw Oops.Oh("#用户不存在#");
        return new UserIdCardVerifyUserOutput { Id = id };
    }

    /// <summary>
    /// 新建证件（createidcard 别名，委托 Save）。
    /// </summary>
    /// <param name="input">入参</param>
    /// <param name="cancellationToken">取消令牌</param>
    public Task CreateIdCardAsync(UserIdCardSaveInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        input.ID = 0;
        return SaveAsync(input, cancellationToken);
    }
}

/// <summary>证件详情。</summary>
public sealed class UserIdCardDetailDto
{
    /// <summary>编号。</summary>
    [JsonPropertyName("ID")]
    public int ID { get; set; }

    /// <summary>姓名。</summary>
    public string CardName { get; set; }

    /// <summary>证件号。</summary>
    public string CardNumber { get; set; }

    /// <summary>备注。</summary>
    public string Remark { get; set; }

    /// <summary>正面。</summary>
    public string CardFront { get; set; }

    /// <summary>背面。</summary>
    public string CardBack { get; set; }

    /// <summary>状态。</summary>
    public int Status { get; set; }

    /// <summary>城市。</summary>
    public string CardCity { get; set; }

    /// <summary>发证日文案。</summary>
    public string CardCreateAt { get; set; }

    /// <summary>生日文案。</summary>
    public string CardBirthday { get; set; }

    /// <summary>类型文案。</summary>
    public string CardType { get; set; }

    /// <summary>性别文案。</summary>
    public string Gender { get; set; }

    /// <summary>国家。</summary>
    public string Country { get; set; }
}

/// <summary>审核证件入参。</summary>
public sealed class UserIdCardUpdateInfoInput
{
    /// <summary>编号。</summary>
    public int ID { get; set; }

    /// <summary>状态。</summary>
    public int Status { get; set; }

    /// <summary>备注。</summary>
    public string Remark { get; set; }

    /// <summary>证件号（成功时校验）。</summary>
    public string CardNumber { get; set; }
}

/// <summary>证件查会员结果。</summary>
public sealed class UserIdCardVerifyUserOutput
{
    /// <summary>会员编号。</summary>
    [JsonPropertyName("Id")]
    public int Id { get; set; }
}
