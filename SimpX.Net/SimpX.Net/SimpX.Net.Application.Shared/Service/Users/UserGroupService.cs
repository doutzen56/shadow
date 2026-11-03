using System.Text.Json.Serialization;
using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Users;
using SqlSugar;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 租户后台会员分组（JWT）。对齐旧版 /admin/user/member/group/list 行字段；权限开关留给权限系统。
/// </summary>
public sealed partial class UserGroupService : ITransient
{
    /// <summary>会员分组权限 GUID。</summary>
    public const string GroupListPermissionId = "30301d7ff5224fddbdecf18b6c1f4a65";

    /// <summary>修改会员分组权限 GUID。</summary>
    public const string GroupEditPermissionId = "84997cf7c202dfec9ecf6b9bc7c3a3cb";

    /// <summary>删除会员分组权限 GUID。</summary>
    public const string GroupDeletePermissionId = "bab2e8fe0bb20d9e8ba45ba245e5af99";

    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;
    private readonly SiteAdminAuthService _adminAuth;

    /// <summary>
    /// 注入业务库、当前站与后台会话。分组表在业务库。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站</param>
    /// <param name="adminAuth">站点后台会话</param>
    public UserGroupService(IBizDbAccessor biz, ICurrentSiteAccessor site, SiteAdminAuthService adminAuth)
    {
        _db = biz.Db;
        _site = site;
        _adminAuth = adminAuth;
    }

    /// <summary>
    /// 当前站会员分组列表（旧版返回）。对应 /admin/user/member/group/list。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>旧版返回</returns>
    public async Task<OldApiResult> ListOldApiAsync(CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        // 权限已在 OldApiAuth（Cookie GUID 或 JWT 新码）拦过；这里只组列表数据。
        // 编辑/删除标记：有 Cookie 管理员时按旧分组串；纯 JWT 时给前端可编辑位（按钮仍靠 v-auth）。
        var canEdit = true;
        var canDelete = true;
        var canView = true;
        var adminId = _adminAuth.TryResolveAdminIdFromCookie(siteId);
        if (adminId != null)
        {
            var admins = await _db.Queryable<SiteAdmin>()
                .Where(a => a.ID == adminId.Value && a.SiteID == siteId)
                .Take(1)
                .ToListAsync(cancellationToken);
            var admin = admins.FirstOrDefault();
            if (admin != null)
            {
                var groupCsv = await LoadGroupPermissionCsvAsync(admin, cancellationToken);
                canEdit = OldApiPermissionEvaluator.SiteAdminHasAll(admin.AdminName, admin.GroupID, groupCsv, [GroupEditPermissionId]);
                canDelete = OldApiPermissionEvaluator.SiteAdminHasAll(admin.AdminName, admin.GroupID, groupCsv, [GroupDeletePermissionId]);
                canView = OldApiPermissionEvaluator.SiteAdminHasAll(admin.AdminName, admin.GroupID, groupCsv, [GroupListPermissionId]);
            }
        }

        var sites = await _db.Queryable<Site>()
            .Where(s => s.ID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        // 旧枚举：1=SINGLE，其它=MULTI
        var walletCode = (sites.FirstOrDefault()?.Wallet ?? 0) == 1 ? "SINGLE" : "MULTI";

        var rows = await _db.Queryable<UserGroup>()
            .Where(g => g.SiteID == siteId)
            .OrderBy(g => g.Sort)
            .OrderBy(g => g.ID, OrderByType.Desc)
            .Take(500)
            .ToListAsync(cancellationToken);

        var info = rows.Select(g => new
        {
            g.ID,
            g.Name,
            g.CreateAt,
            g.Member,
            g.Sort,
            IsUpdate = g.IsUpdate ? 1 : 0,
            IsDefault = g.IsDefault ? 1 : 0,
            Wallet = walletCode,
            IsEdit = canEdit ? "1" : "0",
            IsDelete = canDelete ? "1" : "0",
            IsView = canView ? "1" : "0"
        }).ToList();

        return OldApiResult.Ok(string.Empty, OldApiShowResult.OfList(info));
    }

    /// <summary>
    /// 读取管理员所属分组的权限串；最高权限返回空串交给判定器当全权限。
    /// </summary>
    private async Task<string> LoadGroupPermissionCsvAsync(SiteAdmin admin, CancellationToken cancellationToken)
    {
        if (string.Equals(admin.AdminName, "admin", StringComparison.OrdinalIgnoreCase) || admin.GroupID == 0)
            return string.Empty;

        var groups = await _db.Queryable<SiteAdminGroup>()
            .Where(g => g.SiteID == admin.SiteID && g.ID == admin.GroupID)
            .Take(1)
            .ToListAsync(cancellationToken);
        return groups.FirstOrDefault()?.PermissionString ?? string.Empty;
    }
    /// <summary>
    /// 当前站分组列表。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行列表</returns>
    public async Task<List<UserGroupListItemDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<UserGroup>()
            .Where(t => t.SiteID == siteId)
            .OrderBy(t => t.Sort)
            .OrderBy(t => t.ID, OrderByType.Desc)
            .ToListAsync(cancellationToken);

        return rows.Select(t => new UserGroupListItemDto
        {
            ID = t.ID,
            Name = t.Name ?? string.Empty,
            CreateAt = t.CreateAt,
            Member = t.Member,
            Sort = t.Sort,
            IsUpdate = t.IsUpdate,
            IsDefault = t.IsDefault,
            Wallet = 0,
            IsEdit = "0",
            IsDelete = "0",
            IsView = "1"
        }).ToList();
    }

    /// <summary>
    /// 详情。
    /// </summary>
    /// <param name="id">编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>详情</returns>
    public async Task<UserGroupDetailDto> InfoAsync(int id, CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        if (id <= 0)
        {
            return new UserGroupDetailDto { Name = string.Empty, Payment = string.Empty };
        }

        var rows = await _db.Queryable<UserGroup>()
            .Where(t => t.ID == id && t.SiteID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var g = rows.FirstOrDefault() ?? new UserGroup();
        return new UserGroupDetailDto
        {
            ID = g.ID,
            Name = g.Name ?? string.Empty,
            Sort = g.Sort,
            IsUpdate = g.IsUpdate,
            IsDefault = g.IsDefault,
            IsNight = g.IsNight,
            Member = g.Member,
            Payment = g.Payment ?? string.Empty,
            Withdraw = g.WithdrawString ?? string.Empty,
            Condition = g.ConditionString ?? string.Empty,
            SettingInfo = g.SettingInfo ?? string.Empty
        };
    }

    /// <summary>
    /// 保存。对齐旧 SaveGroup：无默认时新建强制默认；改掉唯一默认须先设别的默认；新默认会清掉旧默认。
    /// </summary>
    /// <param name="input">入参</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SaveAsync(UserGroupSaveInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (string.IsNullOrWhiteSpace(input.Name))
            throw Oops.Oh("#请输入分组名称#");

        var siteId = _site.RequireSiteId();
        var otherDefault = await GetOtherDefaultAsync(siteId, input.ID, cancellationToken);

        if (input.ID == 0)
        {
            var isDefault = input.IsDefault || otherDefault == null;
            if (isDefault && otherDefault != null)
            {
                otherDefault.IsDefault = false;
                await _db.Updateable(otherDefault).UpdateColumns(g => new { g.IsDefault })
                    .ExecuteCommandAsync(cancellationToken);
            }

            var entity = new UserGroup
            {
                SiteID = siteId,
                Name = input.Name.Trim(),
                Sort = input.Sort,
                IsUpdate = input.IsUpdate,
                IsDefault = isDefault,
                IsNight = input.IsNight,
                Payment = input.Payment ?? string.Empty,
                WithdrawString = input.WithdrawString ?? string.Empty,
                ConditionString = input.ConditionString ?? string.Empty,
                SettingInfo = input.SettingInfo ?? string.Empty,
                CreateAt = DateTime.Now,
                Member = 0
            };
            var newId = await _db.Insertable(entity).ExecuteReturnIdentityAsync(cancellationToken);
            // 对齐旧版：Condition.GroupID = 新建编号后写回 ConditionString（若是 JSON 且含 GroupID）
            if (!string.IsNullOrWhiteSpace(entity.ConditionString) && newId > 0)
            {
                var patched = PatchConditionGroupId(entity.ConditionString, newId);
                if (!string.Equals(patched, entity.ConditionString, StringComparison.Ordinal))
                {
                    await _db.Updateable<UserGroup>()
                        .SetColumns(g => new UserGroup { ConditionString = patched })
                        .Where(g => g.ID == newId && g.SiteID == siteId)
                        .ExecuteCommandAsync(cancellationToken);
                }
            }

            return;
        }

        var existing = await _db.Queryable<UserGroup>()
            .Where(t => t.ID == input.ID && t.SiteID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var row = existing.FirstOrDefault() ?? throw Oops.Oh("#编号错误#");

        if (otherDefault == null && !input.IsDefault)
            throw Oops.Oh("#请先设定另外的分组为默认分组#");

        if (input.IsDefault && otherDefault != null)
        {
            otherDefault.IsDefault = false;
            await _db.Updateable(otherDefault).UpdateColumns(g => new { g.IsDefault })
                .ExecuteCommandAsync(cancellationToken);
        }

        row.Name = input.Name.Trim();
        row.Sort = input.Sort;
        row.IsUpdate = input.IsUpdate;
        row.IsDefault = input.IsDefault;
        row.IsNight = input.IsNight;
        row.Payment = input.Payment ?? string.Empty;
        if (input.WithdrawString != null)
            row.WithdrawString = input.WithdrawString;
        if (input.ConditionString != null)
            row.ConditionString = PatchConditionGroupId(input.ConditionString, row.ID);
        if (input.SettingInfo != null)
            row.SettingInfo = input.SettingInfo;
        await _db.Updateable(row).ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 删除。默认分组不可删（对齐旧 DeleteGroup）。
    /// </summary>
    /// <param name="id">编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw Oops.Oh("#编号错误#");
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<UserGroup>()
            .Where(t => t.ID == id && t.SiteID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var row = rows.FirstOrDefault() ?? throw Oops.Oh("#编号错误#");
        if (row.IsDefault)
            throw Oops.Oh("#不能删除默认分组#");
        if (row.Member > 0)
            throw Oops.Oh("该分组有会员，不能删除");
        await _db.Deleteable<UserGroup>().Where(t => t.ID == id && t.SiteID == siteId)
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 取站内另一个默认分组（排除指定编号）。
    /// </summary>
    private async Task<UserGroup> GetOtherDefaultAsync(
        int siteId,
        int excludeId,
        CancellationToken cancellationToken)
    {
        var q = _db.Queryable<UserGroup>().Where(t => t.SiteID == siteId && t.IsDefault);
        if (excludeId > 0)
            q = q.Where(t => t.ID != excludeId);
        var list = await q.Take(1).ToListAsync(cancellationToken);
        return list.FirstOrDefault();
    }

    /// <summary>
    /// 若 Condition JSON 有 GroupID，写成当前分组编号。
    /// </summary>
    private static string PatchConditionGroupId(string conditionJson, int groupId)
    {
        if (string.IsNullOrWhiteSpace(conditionJson) || groupId <= 0)
            return conditionJson ?? string.Empty;
        var trimmed = conditionJson.Trim();
        if (!trimmed.StartsWith('{'))
            return conditionJson;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(trimmed);
            if (doc.RootElement.ValueKind != System.Text.Json.JsonValueKind.Object)
                return conditionJson;
            using var stream = new System.IO.MemoryStream();
            using (var writer = new System.Text.Json.Utf8JsonWriter(stream))
            {
                writer.WriteStartObject();
                var wrote = false;
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    if (string.Equals(prop.Name, "GroupID", StringComparison.OrdinalIgnoreCase))
                    {
                        writer.WriteNumber("GroupID", groupId);
                        wrote = true;
                    }
                    else
                        prop.WriteTo(writer);
                }

                if (!wrote)
                    writer.WriteNumber("GroupID", groupId);
                writer.WriteEndObject();
            }

            return System.Text.Encoding.UTF8.GetString(stream.ToArray());
        }
        catch (System.Text.Json.JsonException)
        {
            return conditionJson;
        }
    }
}

/// <summary>会员分组列表行。</summary>
public sealed class UserGroupListItemDto
{
    /// <summary>分组编号。</summary>
    [JsonPropertyName("ID")]
    public int ID { get; set; }

    /// <summary>分组名。</summary>
    [JsonPropertyName("Name")]
    public string Name { get; set; }

    /// <summary>创建时间。</summary>
    [JsonPropertyName("CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>组内人数（库字段）。</summary>
    [JsonPropertyName("Member")]
    public int Member { get; set; }

    /// <summary>排序。</summary>
    [JsonPropertyName("Sort")]
    public short Sort { get; set; }

    /// <summary>是否自动升级。</summary>
    [JsonPropertyName("IsUpdate")]
    public bool IsUpdate { get; set; }

    /// <summary>是否默认组。</summary>
    [JsonPropertyName("IsDefault")]
    public bool IsDefault { get; set; }

    /// <summary>站点钱包模式。</summary>
    [JsonPropertyName("Wallet")]
    public int Wallet { get; set; }

    /// <summary>是否可改（字符串 0/1）。</summary>
    [JsonPropertyName("IsEdit")]
    public string IsEdit { get; set; }

    /// <summary>是否可删（字符串 0/1）。</summary>
    [JsonPropertyName("IsDelete")]
    public string IsDelete { get; set; }

    /// <summary>是否可看（字符串 0/1）。</summary>
    [JsonPropertyName("IsView")]
    public string IsView { get; set; }
}
