using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 会员分组缺口：批量改组、刷新人数、默认组、排序。
/// </summary>
public sealed partial class UserGroupService
{
    /// <summary>
    /// 批量改会员分组。对齐旧 batchedit。
    /// </summary>
    /// <param name="userCsv">用户名逗号串</param>
    /// <param name="groupId">目标分组</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task BatchEditAsync(
        string userCsv,
        int groupId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userCsv))
            throw Oops.Oh("#请输入会员#");
        if (groupId <= 0)
            throw Oops.Oh("#分组编号错误#");

        var siteId = _site.RequireSiteId();
        var groupOk = await _db.Queryable<UserGroup>()
            .AnyAsync(g => g.ID == groupId && g.SiteID == siteId, cancellationToken);
        if (!groupOk)
            throw Oops.Oh("#分组编号错误#");

        var names = userCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (names.Length == 0)
            throw Oops.Oh("#请输入会员#");

        var users = await _db.Queryable<User>()
            .Where(u => u.SiteID == siteId && names.Contains(u.UserName))
            .ToListAsync(cancellationToken);
        if (users.Count == 0)
            throw Oops.Oh("#请输入会员#");

        foreach (var u in users)
            u.GroupID = groupId;
        await _db.Updateable(users).UpdateColumns(u => new { u.GroupID }).ExecuteCommandAsync(cancellationToken);
        await UpdateMemberAsync(groupId, cancellationToken);
    }

    /// <summary>
    /// 刷新组内人数。对齐旧 updatemember。
    /// </summary>
    /// <param name="groupId">分组</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>人数</returns>
    public async Task<int> UpdateMemberAsync(int groupId, CancellationToken cancellationToken = default)
    {
        if (groupId <= 0)
            throw Oops.Oh("#编号错误#");
        var siteId = _site.RequireSiteId();
        var count = await _db.Queryable<User>()
            .Where(u => u.SiteID == siteId && u.GroupID == groupId)
            .CountAsync(cancellationToken);
        var n = await _db.Updateable<UserGroup>()
            .SetColumns(g => g.Member == count)
            .Where(g => g.ID == groupId && g.SiteID == siteId)
            .ExecuteCommandAsync(cancellationToken);
        if (n <= 0)
            throw Oops.Oh("#编号错误#");
        return count;
    }

    /// <summary>
    /// 设为默认分组。对齐旧 updategroupisdefault。
    /// </summary>
    /// <param name="groupId">分组</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task UpdateGroupIsDefaultAsync(int groupId, CancellationToken cancellationToken = default)
    {
        if (groupId <= 0)
            throw Oops.Oh("#编号错误#");
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<UserGroup>()
            .Where(g => g.ID == groupId && g.SiteID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var group = rows.FirstOrDefault() ?? throw Oops.Oh("#编号错误#");

        await _db.Updateable<UserGroup>()
            .SetColumns(g => g.IsDefault == false)
            .Where(g => g.SiteID == siteId && g.IsDefault)
            .ExecuteCommandAsync(cancellationToken);

        group.IsDefault = true;
        await _db.Updateable(group).UpdateColumns(g => new { g.IsDefault }).ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 保存排序。对齐旧 savesort（ID 数组从前端到后端，旧版会 Reverse）。
    /// </summary>
    /// <param name="ids">分组编号顺序</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SaveSortAsync(int[] ids, CancellationToken cancellationToken = default)
    {
        if (ids == null || ids.Length == 0)
            throw Oops.Oh("#参数错误#");
        var siteId = _site.RequireSiteId();
        var ordered = ids.Reverse().ToArray();
        short sort = 0;
        foreach (var id in ordered)
        {
            if (id <= 0)
                continue;
            await _db.Updateable<UserGroup>()
                .SetColumns(g => g.Sort == sort)
                .Where(g => g.ID == id && g.SiteID == siteId)
                .ExecuteCommandAsync(cancellationToken);
            sort++;
        }
    }
}
