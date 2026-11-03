using Microsoft.AspNetCore.Http;
using SimpX.Net.Data;
using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Users;
using SqlSugar;
using System.Text.RegularExpressions;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 会员注册：写入当前站点；邀请码链邀请链路跟旧版 AddUser 主流程一致（InviteCode → Setting.InviteCode → AgentID）。
/// </summary>
public sealed class MemberRegisterService : ITransient
{
    /// <summary>旧版 RegMethod.UserCreate 对应值。</summary>
    public const int RegFlagUserCreate = 0;

    /// <summary>会员类型（UserType.Member）。</summary>
    public const int UserTypeMember = 0;

    /// <summary>用户名规则：英数下划线横杠，4～16 位（跟旧版用户名规则一样）。</summary>
    private static readonly Regex UserNameRule = new(
        @"^[0-9a-z\-_]{4,16}$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly MemberAuthService _memberAuth;

    /// <summary>
    /// 注入依赖。会员表在业务库。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站点</param>
    /// <param name="httpContextAccessor">HTTP</param>
    /// <param name="memberAuth">登录（注册成功后种会话）</param>
    public MemberRegisterService(
        IBizDbAccessor biz,
        ICurrentSiteAccessor site,
        IHttpContextAccessor httpContextAccessor,
        MemberAuthService memberAuth)
    {
        _db = biz.Db;
        _site = site;
        _httpContextAccessor = httpContextAccessor;
        _memberAuth = memberAuth;
    }

    /// <summary>
    /// 注册会员：SiteID 只用当前站点编号；邀请码决定上级代理。
    /// </summary>
    /// <param name="userName">用户名</param>
    /// <param name="password">明文密码</param>
    /// <param name="password2">确认密码</param>
    /// <param name="inviteCode">邀请码；空则尝试站点 Setting.InviteCode</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>旧版返回格式；成功时 info 带 Token</returns>
    public async Task<OldApiResult> RegisterAsync(
        string userName,
        string password,
        string password2,
        string inviteCode,
        CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        userName = userName?.Trim();
        if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))
            return OldApiResult.Fail("#用户名/密码错误#");

        if (!string.Equals(password, password2, StringComparison.Ordinal))
            return OldApiResult.Fail("#两次输入的密码不相同#");

        var lowerUser = userName.ToLowerInvariant();
        var lowerPass = password.Trim().ToLowerInvariant();
        if (lowerPass == "a123456")
            return OldApiResult.Fail("#密码过于简单#");
        if (lowerUser == lowerPass)
            return OldApiResult.Fail("#用户名密码不得相同#");
        if (lowerUser.Contains(lowerPass))
            return OldApiResult.Fail("#用户名不得包含密码字符串#");
        if (lowerPass.Contains(lowerUser))
            return OldApiResult.Fail("#密码包含用户名字符串#");

        if (!UserNameRule.IsMatch(userName))
            return OldApiResult.Fail("#用户名错误#");

        if (password.Length < 5 || password.Length > 16)
            return OldApiResult.Fail("#密码长度在5到16位之间#");

        var exists = await _db.Queryable<User>()
            .Where(u => u.SiteID == siteId && u.UserName == userName)
            .AnyAsync(cancellationToken);
        if (exists)
            return OldApiResult.Fail("#该用户名已被注册#");

        var defaultGroups = await _db.Queryable<UserGroup>()
            .Where(g => g.SiteID == siteId && g.IsDefault)
            .OrderBy(g => g.ID)
            .Take(1)
            .ToListAsync(cancellationToken);
        var defaultGroup = defaultGroups.FirstOrDefault();
        if (defaultGroup == null)
            return OldApiResult.Fail("#系统错误,未配置默认分组#");

        var siteRows = await _db.Queryable<Site>()
            .Where(s => s.ID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var site = siteRows.FirstOrDefault();
        if (site == null)
            return OldApiResult.Fail("#站点不存在#");

        var setting = ParseInviteSetting(site.SettingString);
        inviteCode = string.IsNullOrWhiteSpace(inviteCode) ? setting.InviteCode : inviteCode.Trim();

        // 开启邀请时必须有码；未开启也尽量走默认邀请码，文案跟旧版「上级错误」一致
        if (setting.IsInvite && string.IsNullOrWhiteSpace(inviteCode))
            return OldApiResult.Fail("#邀请码错误#");

        if (string.IsNullOrWhiteSpace(inviteCode) && !string.IsNullOrWhiteSpace(setting.InviteCode))
            inviteCode = setting.InviteCode;

        UserInvite invite = null;
        var agentId = 0;
        var inviteUserId = 0;

        if (!string.IsNullOrWhiteSpace(inviteCode))
        {
            var invites = await _db.Queryable<UserInvite>()
                .Where(i => i.SiteID == siteId && i.ID == inviteCode)
                .Take(1)
                .ToListAsync(cancellationToken);
            invite = invites.FirstOrDefault();
            if (invite == null)
                return OldApiResult.Fail("#邀请码错误#");

            var inviteUsers = await _db.Queryable<User>()
                .Where(u => u.ID == invite.UserID && u.SiteID == siteId)
                .Take(1)
                .ToListAsync(cancellationToken);
            var inviteUser = inviteUsers.FirstOrDefault();
            if (inviteUser == null)
            {
                // 邀请人失效时退到站点默认邀请码
                if (!string.IsNullOrWhiteSpace(setting.InviteCode) && setting.InviteCode != inviteCode)
                {
                    var fallback = await _db.Queryable<UserInvite>()
                        .Where(i => i.SiteID == siteId && i.ID == setting.InviteCode)
                        .Take(1)
                        .ToListAsync(cancellationToken);
                    invite = fallback.FirstOrDefault();
                    if (invite == null)
                        return OldApiResult.Fail("#上级错误#");
                    agentId = invite.UserID;
                }
                else
                {
                    return OldApiResult.Fail("#上级错误#");
                }
            }
            else if (inviteUser.Type == UserTypeMember)
            {
                // 会员邀会员：记 InviteID；上级代理取邀请人的 Agent 或站点默认邀请码
                inviteUserId = inviteUser.ID;
                if (inviteUser.AgentID != 0)
                {
                    agentId = inviteUser.AgentID;
                }
                else if (!string.IsNullOrWhiteSpace(setting.InviteCode))
                {
                    var def = await _db.Queryable<UserInvite>()
                        .Where(i => i.SiteID == siteId && i.ID == setting.InviteCode)
                        .Take(1)
                        .ToListAsync(cancellationToken);
                    var defInvite = def.FirstOrDefault();
                    if (defInvite == null)
                        return OldApiResult.Fail("#上级错误#");
                    agentId = defInvite.UserID;
                    invite = defInvite;
                }
                else
                {
                    return OldApiResult.Fail("#上级错误#");
                }
            }
            else
            {
                agentId = inviteUser.ID;
            }
        }

        if (agentId == 0)
            return OldApiResult.Fail("#上级错误#");

        var agentRows = await _db.Queryable<User>()
            .Where(u => u.ID == agentId && u.SiteID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var agentUser = agentRows.FirstOrDefault();
        if (agentUser == null || agentUser.Type == UserTypeMember)
            return OldApiResult.Fail("#上级必须为代理#");

        var http = _httpContextAccessor.HttpContext;
        var now = DateTime.Now;
        var empty = string.Empty;
        var regHost = http?.Request.Headers["X-Forwarded-HostX"].ToString();
        if (string.IsNullOrWhiteSpace(regHost))
            regHost = http?.Request.Host.Host ?? empty;

        var user = new User
        {
            SiteID = siteId,
            UserName = userName,
            NickName = userName,
            AccountName = empty,
            Password = Md5PasswordHasher.Hash(password),
            PayPassword = empty,
            Face = empty,
            Email = empty,
            Mobile = empty,
            Telegram = empty,
            Line = empty,
            Google = empty,
            Answer = empty,
            Info = empty,
            DeviceId = empty,
            PixelID = empty,
            Address = empty,
            WashingRatio = empty,
            DisableCurrency = empty,
            BlockGame = empty,
            RegisteredFrom = empty,
            CreateAt = now,
            LoginAt = now,
            ActiveAt = now,
            UpdateAt = now,
            Birthday = new DateTime(1970, 1, 1),
            RegIP = http?.Connection.RemoteIpAddress?.ToString() ?? empty,
            LoginIP = http?.Connection.RemoteIpAddress?.ToString() ?? empty,
            RegDomain = regHost,
            RegFlag = RegFlagUserCreate,
            LastReturn = now.Date.AddDays(-1),
            SecretKey = Guid.Empty,
            GroupID = defaultGroup.ID,
            AgentID = agentId,
            InviteID = inviteUserId,
            Depth = agentUser.Depth + 1,
            Type = UserTypeMember
        };

        await _db.Ado.BeginTranAsync();
        try
        {
            var newId = await _db.Insertable(user).ExecuteReturnIdentityAsync(cancellationToken);
            if (newId <= 0)
            {
                await _db.Ado.RollbackTranAsync();
                return OldApiResult.Fail("#注册失败#");
            }

            await _db.Updateable<UserGroup>()
                .SetColumns(g => g.Member == g.Member + 1)
                .Where(g => g.ID == defaultGroup.ID && g.SiteID == siteId)
                .ExecuteCommandAsync(cancellationToken);

            if (invite != null)
            {
                await _db.Updateable<UserInvite>()
                    .SetColumns(i => i.Member == i.Member + 1)
                    .Where(i => i.ID == invite.ID && i.SiteID == siteId)
                    .ExecuteCommandAsync(cancellationToken);
            }

            await _db.Ado.CommitTranAsync();
        }
        catch
        {
            await _db.Ado.RollbackTranAsync();
            throw;
        }

        return await _memberAuth.LoginAsync(userName, password, cancellationToken);
    }

    /// <summary>
    /// 用户名是否符合旧版英数规则（供单测）。
    /// </summary>
    /// <param name="userName">用户名</param>
    /// <returns>是否合法</returns>
    public static bool IsValidUserName(string userName) =>
        !string.IsNullOrEmpty(userName) && UserNameRule.IsMatch(userName);

    /// <summary>
    /// 从站点 Setting（JSON 或旧版 querystring）读邀请相关字段。
    /// </summary>
    private static (bool IsInvite, string InviteCode) ParseInviteSetting(string raw)
    {
        var map = RequestSiteService.ParseSettingMap(raw);
        var inviteCode = map.TryGetValue("InviteCode", out var code) ? (code ?? string.Empty) : string.Empty;
        var isInvite = false;
        if (map.TryGetValue("IsInvite", out var flag) && !string.IsNullOrWhiteSpace(flag))
        {
            if (bool.TryParse(flag, out var b))
                isInvite = b;
            else if (int.TryParse(flag, out var n))
                isInvite = n != 0;
        }

        return (isInvite, inviteCode);
    }
}
