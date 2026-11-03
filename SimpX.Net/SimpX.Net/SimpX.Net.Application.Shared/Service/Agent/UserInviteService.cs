using System.Text.Json;
using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 租户后台邀请码。对齐旧版 /admin/user/agent/invite/list|info|save|delete。
/// </summary>
public sealed class UserInviteService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;

    /// <summary>
    /// 注入业务库与当前站。邀请码在 BetWin30 业务库，不能走主库 Sqlite。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站</param>
    public UserInviteService(IBizDbAccessor biz, ICurrentSiteAccessor site)
    {
        _db = biz.Db;
        _site = site;
    }

    /// <summary>
    /// 邀请码列表。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行列表</returns>
    public async Task<List<UserInviteListItemDto>> ListAsync(
        UserInviteListInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var siteId = _site.RequireSiteId();

        var query = _db.Queryable<UserInvite>().Where(t => t.SiteID == siteId);

        var inviteKey = !string.IsNullOrWhiteSpace(input.Invite) ? input.Invite.Trim()
            : (!string.IsNullOrWhiteSpace(input.ID) ? input.ID.Trim() : null);
        if (!string.IsNullOrEmpty(inviteKey))
            query = query.Where(t => t.ID != null && t.ID.Contains(inviteKey));

        if (!string.IsNullOrWhiteSpace(input.User))
        {
            var userKey = input.User.Trim();
            int userId;
            if (int.TryParse(userKey, out userId))
                query = query.Where(t => t.UserID == userId);
            else
            {
                var ids = await _db.Queryable<User>()
                    .Where(u => u.SiteID == siteId && u.UserName.Contains(userKey))
                    .Select(u => u.ID)
                    .Take(500)
                    .ToListAsync(cancellationToken);
                if (ids.Count == 0)
                    return new List<UserInviteListItemDto>();
                query = query.Where(t => ids.Contains(t.UserID));
            }
        }

        var rows = await query
            .OrderBy(t => t.CreateAt, OrderByType.Desc)
            .Take(500)
            .ToListAsync(cancellationToken);

        var userIds = rows.Select(r => r.UserID).Distinct().ToList();
        var users = userIds.Count == 0
            ? new List<User>()
            : await _db.Queryable<User>()
                .Where(u => u.SiteID == siteId && userIds.Contains(u.ID))
                .ToListAsync(cancellationToken);
        var userMap = users.ToDictionary(u => u.ID, u => u);

        IEnumerable<UserInvite> filtered = rows;
        if (!string.IsNullOrWhiteSpace(input.Type))
        {
            var typeRaw = input.Type.Trim();
            int typeFilter = -1;
            if (int.TryParse(typeRaw, out var ti)) typeFilter = ti;
            else if (string.Equals(typeRaw, "Member", StringComparison.OrdinalIgnoreCase) || typeRaw == "会员") typeFilter = 0;
            else if (string.Equals(typeRaw, "Agent", StringComparison.OrdinalIgnoreCase) || typeRaw == "代理") typeFilter = 1;
            if (typeFilter >= 0)
                filtered = filtered.Where(r => userMap.TryGetValue(r.UserID, out var u) && u.Type == typeFilter);
        }
        if (input.IsOpen.HasValue)
            filtered = filtered.Where(r => r.IsOpen == input.IsOpen.Value);

        return filtered.Select(t =>
        {
            userMap.TryGetValue(t.UserID, out var user);
            return new UserInviteListItemDto
            {
                ID = t.ID,
                UserID = t.UserID,
                UserName = user?.UserName ?? string.Empty,
                Member = t.Member,
                IsOpen = t.IsOpen,
                CreateAt = t.CreateAt,
                Type = user?.Type ?? 0
            };
        }).ToList();
    }

    /// <summary>
    /// 邀请码详情；不存在返回空壳。
    /// </summary>
    /// <param name="id">邀请码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>详情</returns>
    public async Task<UserInviteDetailDto> InfoAsync(string id, CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        UserInvite invite = null;
        if (!string.IsNullOrWhiteSpace(id))
        {
            var key = id.Trim();
            var rows = await _db.Queryable<UserInvite>()
                .Where(t => t.ID == key && t.SiteID == siteId)
                .Take(1)
                .ToListAsync(cancellationToken);
            invite = rows.FirstOrDefault();
        }

        invite ??= new UserInvite();
        var userName = string.Empty;
        if (invite.UserID > 0)
        {
            var users = await _db.Queryable<User>()
                .Where(u => u.ID == invite.UserID && u.SiteID == siteId)
                .Take(1)
                .ToListAsync(cancellationToken);
            userName = users.FirstOrDefault()?.UserName ?? string.Empty;
        }

        return new UserInviteDetailDto
        {
            ID = invite.ID ?? string.Empty,
            UserID = invite.UserID,
            UserName = userName,
            Member = invite.Member,
            IsOpen = invite.IsOpen,
            CreateAt = invite.CreateAt,
            Game = string.IsNullOrEmpty(invite.SettingString) ? "{}" : invite.SettingString
        };
    }

    /// <summary>
    /// 保存邀请码。新建自动生成 8 位数字码；Lottery 不得超过该会员游戏配置上限。
    /// </summary>
    /// <param name="input">入参</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SaveAsync(UserInviteSaveInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var siteId = _site.RequireSiteId();

        var userId = input.UserID;
        if (userId <= 0 && !string.IsNullOrWhiteSpace(input.User))
        {
            var name = input.User.Trim();
            if (int.TryParse(name, out var parsed))
                userId = parsed;
            else
            {
                var users = await _db.Queryable<User>()
                    .Where(u => u.SiteID == siteId && u.UserName == name)
                    .Take(1)
                    .ToListAsync(cancellationToken);
                userId = users.FirstOrDefault()?.ID ?? 0;
            }
        }

        if (userId <= 0)
            throw Oops.Oh("#用户#ID #错误#");

        var userOk = await _db.Queryable<User>()
            .AnyAsync(u => u.ID == userId && u.SiteID == siteId, cancellationToken);
        if (!userOk)
            throw Oops.Oh("#用户#ID #错误#");

        var cfgs = await _db.Queryable<UserGameConfig>()
            .Where(c => c.UserID == userId && c.SiteID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var cfg = cfgs.FirstOrDefault() ?? throw Oops.Oh("#游戏配置记录不存在#");

        var game = ClampLotteryInGameJson(
            string.IsNullOrWhiteSpace(input.Game) ? "{}" : input.Game.Trim(),
            cfg.Lottery);

        if (string.IsNullOrWhiteSpace(input.ID))
        {
            var newId = await CreateInviteIdAsync(cancellationToken);
            await _db.Insertable(new UserInvite
            {
                ID = newId,
                SiteID = siteId,
                UserID = userId,
                Member = 0,
                IsOpen = true,
                CreateAt = DateTime.Now,
                SettingString = game
            }).ExecuteCommandAsync(cancellationToken);
            return;
        }

        var key = input.ID.Trim();
        var rows = await _db.Queryable<UserInvite>()
            .Where(t => t.ID == key && t.SiteID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var row = rows.FirstOrDefault() ?? throw Oops.Oh("#邀请码不存在#");
        row.IsOpen = input.IsOpen;
        row.SettingString = game;
        await _db.Updateable(row).ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 把 Game JSON 里的 Lottery 压到配置上限；没有字段或 0 时写成配置值。
    /// </summary>
    /// <param name="gameJson">游戏设置 JSON</param>
    /// <param name="maxLottery">配置上限</param>
    /// <returns>写回后的 JSON</returns>
    private static string ClampLotteryInGameJson(string gameJson, int maxLottery)
    {
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(gameJson) ? "{}" : gameJson);
            var root = doc.RootElement;
            var lottery = 0;
            if (root.ValueKind == JsonValueKind.Object
                && root.TryGetProperty("Lottery", out var p)
                && p.TryGetInt32(out var n))
                lottery = n;
            if (lottery > maxLottery || lottery == 0)
                lottery = maxLottery;

            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
            {
                writer.WriteStartObject();
                if (root.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in root.EnumerateObject())
                    {
                        if (string.Equals(prop.Name, "Lottery", StringComparison.OrdinalIgnoreCase))
                            continue;
                        prop.WriteTo(writer);
                    }
                }

                writer.WriteNumber("Lottery", lottery);
                writer.WriteEndObject();
            }

            return System.Text.Encoding.UTF8.GetString(stream.ToArray());
        }
        catch (JsonException)
        {
            return $"{{\"Lottery\":{maxLottery}}}";
        }
    }

    /// <summary>
    /// 删除当前站邀请码。
    /// </summary>
    /// <param name="id">邀请码</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw Oops.Oh("#邀请码不存在#");
        var siteId = _site.RequireSiteId();
        var key = id.Trim();
        var ok = await _db.Queryable<UserInvite>()
            .AnyAsync(t => t.ID == key && t.SiteID == siteId, cancellationToken);
        if (!ok)
            throw Oops.Oh("#邀请码不存在#");
        await _db.Deleteable<UserInvite>()
            .Where(t => t.ID == key && t.SiteID == siteId)
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 生成不重复的 8 位数字邀请码。
    /// </summary>
    private async Task<string> CreateInviteIdAsync(CancellationToken cancellationToken)
    {
        var rnd = Random.Shared;
        for (var i = 0; i < 32; i++)
        {
            var candidate = rnd.Next(10000000, 99999999).ToString();
            var exists = await _db.Queryable<UserInvite>()
                .AnyAsync(t => t.ID == candidate, cancellationToken);
            if (!exists)
                return candidate;
        }

        throw Oops.Oh("#邀请码生成失败#");
    }
}
