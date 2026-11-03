using SimpX.Net.Data;
using SimpX.Net.Data.Entity.Users;
using SqlSugar;
using System.Text.Json.Serialization;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 会员本人资料（getinfo）与后台会员详情（detail）的精简实现。
/// </summary>
public sealed class MemberProfileService : ITransient
{
    /// <summary>「查看联系方式」权限。</summary>
    public const string ViewContactPermissionId = "266641ba250f4fe4952ffdb59c3d43b6";

    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;
    private readonly MemberAuthService _memberAuth;
    private readonly SiteAdminAuthService _adminAuth;

    /// <summary>
    /// 注入依赖。会员表在业务库。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站点</param>
    /// <param name="memberAuth">会员会话</param>
    /// <param name="adminAuth">后台会话（详情联系方式权限）</param>
    public MemberProfileService(
        IBizDbAccessor biz,
        ICurrentSiteAccessor site,
        MemberAuthService memberAuth,
        SiteAdminAuthService adminAuth)
    {
        _db = biz.Db;
        _site = site;
        _memberAuth = memberAuth;
        _adminAuth = adminAuth;
    }

    /// <summary>
    /// 当前登录会员资料（脱敏）。对应旧版 /request/user/getinfo。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>旧版返回格式</returns>
    public async Task<OldApiResult> GetSelfInfoAsync(CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var userId = await _memberAuth.TryResolveUserIdFromCookieAsync(siteId, cancellationToken);
        if (userId == null)
            return OldApiResult.NeedLogin();

        var rows = await _db.Queryable<User>()
            .Where(u => u.ID == userId.Value && u.SiteID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var user = rows.FirstOrDefault();
        if (user == null)
            return OldApiResult.NeedLogin();

        return OldApiResult.Ok(string.Empty, new
        {
            ID = user.ID.ToString(System.Globalization.CultureInfo.InvariantCulture),
            UserName = user.UserName,
            NickName = user.NickName ?? string.Empty,
            CreateAt = OldApiMoneyDisplay.FormatDateTime(user.CreateAt),
            AccountName = ContactMask.MaskName(user.AccountName),
            Email = ContactMask.MaskEmail(user.Email),
            Mobile = ContactMask.MaskMobile(user.Mobile),
            IsEmail = user.IsEmail ? 1 : 0,
            IsMobile = user.IsMobile ? 1 : 0,
            Avatar = string.IsNullOrWhiteSpace(user.Face) ? string.Empty : user.Face,
            Address = user.Address ?? string.Empty
        });
    }

    /// <summary>
    /// 会话信息。对应旧版 /request/user/info。
    /// </summary>
    public async Task<OldApiResult> GetSessionInfoAsync(CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var userId = await _memberAuth.TryResolveUserIdFromCookieAsync(siteId, cancellationToken);
        if (userId == null)
            return OldApiResult.Ok("#游客#");

        var rows = await _db.Queryable<User>()
            .Where(u => u.ID == userId.Value && u.SiteID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var user = rows.FirstOrDefault();
        if (user == null)
            return OldApiResult.Ok("#游客#");

        // Lock 位展开：旧站 Activity=128
        var lockObj = new Dictionary<string, int>(StringComparer.Ordinal);
        if ((user.Lock & 128) != 0) lockObj["Activity"] = 1;

        // 余额走 usr_Money（Users.Money 列常为过时镜像）
        var moneyRows = await _db.Queryable<UserMoney>()
            .Where(m => m.SiteID == siteId && m.UserID == user.ID && m.Currency == user.Currency)
            .Take(1)
            .ToListAsync(cancellationToken);
        var wallet = moneyRows.FirstOrDefault();
        var totalMoney = wallet?.TotalMoney ?? user.Money;
        var lockMoney = wallet?.LockMoney ?? user.LockMoney;

        return OldApiResult.Ok(string.Empty, new
        {
            ID = user.ID.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Name = user.UserName,
            UserName = user.UserName,
            NickName = user.NickName ?? string.Empty,
            Avatar = string.IsNullOrWhiteSpace(user.Face) ? string.Empty : user.Face,
            Type = user.Type == 0 ? "Member" : user.Type.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Lock = lockObj,
            Currency = OldApiMoneyDisplay.FormatCurrency(user.Currency),
            Level = user.Level.ToString(System.Globalization.CultureInfo.InvariantCulture),
            levelBadge = string.Empty,
            Msg = "0",
            UserVipName = user.Level > 0 ? $"VIP{user.Level}" : string.Empty,
            AccountName = ContactMask.MaskName(user.AccountName),
            Mobile = ContactMask.MaskMobile(user.Mobile),
            Money = OldApiMoneyDisplay.FormatMoneyTrim(totalMoney),
            LockMoney = OldApiMoneyDisplay.FormatMoneyTrim(lockMoney),
            Withdraw = OldApiMoneyDisplay.FormatMoneyTrim(totalMoney - lockMoney),
            Coin = OldApiMoneyDisplay.FormatMoneyTrim(user.Coin),
            Birthday = user.Birthday == default
                ? "1900-01-01"
                : user.Birthday.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            Exper = user.Exper.ToString(System.Globalization.CultureInfo.InvariantCulture),
            CreateAt = OldApiMoneyDisplay.FormatDateTime(user.CreateAt),
            FirstBindingNeedVerification = 0,
            USDTVerification = 0,
            USDTSecurity = 0,
            Function = new Dictionary<string, int> { [""] = 1 },
            Group = new { NoWithdraw = 0, IsDifferent = 0 },
            LastReturn = "0001/1/1 0:00:00",
            isAgent = "0",
            isDefaultPassword = user.Is_Temporary_Password ? "1" : "0",
            Address = user.Address ?? string.Empty,
            DeviceId = user.DeviceId ?? string.Empty,
            RegisteredFrom = string.Empty,
            PixelID = string.Empty,
            Info = new { }
        });
    }

    /// <summary>
    /// 银行卡列表。对应旧版 /request/user/getbanklist。
    /// 未传 IsVir 时旧站返回空列表；传 IsVir=0/1 才按类型过滤。
    /// </summary>
    public async Task<OldApiResult> GetBankListAsync(string? isVir, CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var userId = await _memberAuth.TryResolveUserIdFromCookieAsync(siteId, cancellationToken);
        if (userId == null)
            return OldApiResult.NeedLogin();

        if (string.IsNullOrWhiteSpace(isVir))
            return OldApiResult.Ok(string.Empty, OldApiShowResult.OfList(Array.Empty<object>()));

        var vir = isVir.Trim();
        var userRows = await _db.Queryable<User>()
            .Where(u => u.ID == userId.Value && u.SiteID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var user = userRows.FirstOrDefault();
        var currency = user == null ? string.Empty : OldApiMoneyDisplay.FormatCurrency(user.Currency);

        var rows = await _db.Queryable<BankAccount>()
            .Where(b => b.SiteID == siteId && b.UserID == userId.Value && b.IsVir == vir)
            .OrderBy(b => b.ID, OrderByType.Desc)
            .ToListAsync(cancellationToken);
        var list = rows.Select(t =>
        {
            var acc = t.Account ?? string.Empty;
            var parts = (t.AccountDetail ?? string.Empty).Split(',');
            var bankName = !string.IsNullOrWhiteSpace(t.BankName)
                ? t.BankName
                : (BankTypeNameMap.Names.TryGetValue(t.Type, out var bn) ? bn : t.Type.ToString());
            var tail = acc.Length > 4 ? acc[^4..] : acc;
            var maskedName = ContactMask.MaskName(t.Name ?? user?.AccountName ?? string.Empty);
            return new
            {
                ID = t.ID.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Type = bankName,
                IsVir = t.IsVir ?? string.Empty,
                Name = t.Name ?? string.Empty,
                Status = OldApiMoneyDisplay.FormatCheckStatus(t.Status),
                Remark = t.Remark ?? string.Empty,
                Bank = bankName,
                BankID = t.Type.ToString(System.Globalization.CultureInfo.InvariantCulture),
                currency,
                Account = $"{maskedName} {bankName} #尾号#: {tail}".Trim(),
                AccountNumber = HiddenAccount(acc),
                AccountTail = tail,
                IsWithdraw = 1,
                BranchCode = parts.Length > 2 ? parts[2] : "",
                BankAddress = parts.Length > 2 ? parts[1] : "",
                AccountDetail = parts.Length > 2 ? parts[0] : (t.AccountDetail ?? string.Empty)
            };
        });
        return OldApiResult.Ok(string.Empty, OldApiShowResult.OfList(list));
    }

    private static string HiddenAccount(string account)
    {
        if (string.IsNullOrEmpty(account) || account.Length <= 8)
            return account ?? string.Empty;
        return account[..4] + new string('*', Math.Max(0, account.Length - 8)) + account[^4..];
    }

    /// <summary>
    /// 当前登录会员安全项布尔位。对应旧版 /request/user/safeinfo。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>旧版返回格式</returns>
    public async Task<OldApiResult> GetSafeInfoAsync(CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var userId = await _memberAuth.TryResolveUserIdFromCookieAsync(siteId, cancellationToken);
        if (userId == null)
            return OldApiResult.NeedLogin();

        var rows = await _db.Queryable<User>()
            .Where(u => u.ID == userId.Value && u.SiteID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var user = rows.FirstOrDefault();
        if (user == null)
            return OldApiResult.NeedLogin();

        return OldApiResult.Ok(string.Empty, new MemberSafeInfoDto
        {
            IsPayPassword = !string.IsNullOrWhiteSpace(user.PayPassword),
            IsQuestion = user.Question != 0,
            IsEmail = !string.IsNullOrWhiteSpace(user.Email),
            IsLine = !string.IsNullOrWhiteSpace(user.Line),
            IsGoole = !string.IsNullOrWhiteSpace(user.Google),
            IsTelegram = !string.IsNullOrWhiteSpace(user.Telegram),
            IsMobile = user.IsMobile,
            IsSecretKey = user.SecretKey != Guid.Empty,
            Is_Temporary_Password = user.Is_Temporary_Password,
            IsAddress = !string.IsNullOrWhiteSpace(user.Address)
        });
    }

    /// <summary>
    /// 当前登录会员改登录密码（先不做滑块/短信校验）。对应旧版 /request/user/updatepassword 核心校验。
    /// </summary>
    /// <param name="oldPassword">当前密码</param>
    /// <param name="newPassword1">新密码</param>
    /// <param name="newPassword2">确认新密码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>旧版返回格式</returns>
    public async Task<OldApiResult> UpdatePasswordAsync(
        string oldPassword,
        string newPassword1,
        string newPassword2,
        CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var userId = await _memberAuth.TryResolveUserIdFromCookieAsync(siteId, cancellationToken);
        if (userId == null)
            return OldApiResult.NeedLogin();

        if (!string.Equals(newPassword1, newPassword2, StringComparison.Ordinal))
            return OldApiResult.Fail("#两次输入的密码不相同#");
        if (string.Equals((newPassword1 ?? string.Empty).Trim(), "a123456", StringComparison.OrdinalIgnoreCase))
            return OldApiResult.Fail("#密码过于简单#");
        if (string.IsNullOrEmpty(newPassword1) || newPassword1.Length < 5 || newPassword1.Length > 16)
            return OldApiResult.Fail("#密码长度在5到16位之间#");

        var rows = await _db.Queryable<User>()
            .Where(u => u.ID == userId.Value && u.SiteID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var user = rows.FirstOrDefault();
        if (user == null)
            return OldApiResult.NeedLogin();

        if (!Md5PasswordHasher.Matches(oldPassword, user.Password))
            return OldApiResult.Fail("#当前密码输入错误#");
        if (Md5PasswordHasher.Matches(newPassword1, user.Password))
            return OldApiResult.Fail("#新密码不能跟旧密码相同#");

        var hash = Md5PasswordHasher.Hash(newPassword1);
        await _db.Updateable<User>()
            .SetColumns(u => new User { Password = hash, Is_Temporary_Password = false })
            .Where(u => u.ID == user.ID && u.SiteID == siteId)
            .ExecuteCommandAsync(cancellationToken);

        return OldApiResult.Ok("#操作成功#");
    }

    /// <summary>
    /// 站点后台会员详情（当前站）。对应旧版 /admin/user/member/info/detail 精简字段。
    /// </summary>
    /// <param name="userId">会员编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>旧版返回格式</returns>
    public async Task<OldApiResult> AdminDetailAsync(int userId, CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
            return OldApiResult.Fail("#用户不存在#");

        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<User>()
            .Where(u => u.ID == userId && u.SiteID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var user = rows.FirstOrDefault();
        if (user == null)
            return OldApiResult.Fail("#用户不存在#");

        var canViewContact = await CanViewContactAsync(cancellationToken);
        var email = user.Email ?? string.Empty;
        var mobile = user.Mobile ?? string.Empty;
        if (!canViewContact)
        {
            email = ContactMask.MaskEmail(email);
            mobile = ContactMask.MaskMobile(mobile);
        }

        return OldApiResult.Ok(string.Empty, new UserMemberDetailDto
        {
            ID = user.ID,
            UserName = user.UserName,
            NickName = user.NickName,
            AccountName = user.AccountName,
            CreateAt = user.CreateAt,
            RegIP = user.RegIP,
            RegDomain = user.RegDomain,
            LoginAt = user.LoginAt,
            LoginIP = user.LoginIP,
            ActiveAt = user.ActiveAt,
            Lock = user.Lock,
            Function = user.Function,
            Currency = user.Currency,
            Money = user.Money,
            AgentID = user.AgentID,
            InviteID = user.InviteID,
            GroupID = user.GroupID,
            Level = user.Level,
            InitLevel = user.InitLevel,
            Type = user.Type,
            Depth = user.Depth,
            Email = email,
            Mobile = mobile
        });
    }

    /// <summary>
    /// 当前后台管理员是否有「查看联系方式」权限。
    /// </summary>
    private async Task<bool> CanViewContactAsync(CancellationToken cancellationToken)
    {
        var admin = await _adminAuth.TryGetCurrentAdminAsync(cancellationToken);
        if (admin == null)
            return false;
        if (string.Equals(admin.AdminName, "admin", StringComparison.OrdinalIgnoreCase) || admin.GroupID == 0)
            return true;

        var groups = await _db.Queryable<SimpX.Net.Data.Entity.Sites.SiteAdminGroup>()
            .Where(g => g.SiteID == admin.SiteID && g.ID == admin.GroupID)
            .Take(1)
            .ToListAsync(cancellationToken);
        var csv = groups.FirstOrDefault()?.PermissionString ?? string.Empty;
        return OldApiPermissionEvaluator.SiteAdminHasAll(
            admin.AdminName,
            admin.GroupID,
            csv,
            [ViewContactPermissionId]);
    }

}

/// <summary>会员安全信息布尔位 DTO（字段名跟旧版一样，保留 IsGoole 这个拼写）。</summary>
public sealed class MemberSafeInfoDto
{
    /// <summary>是否已设资金密码。</summary>
    [JsonPropertyName("IsPayPassword")]
    public bool IsPayPassword { get; set; }

    /// <summary>是否已设密保问题。</summary>
    [JsonPropertyName("IsQuestion")]
    public bool IsQuestion { get; set; }

    /// <summary>是否已填邮箱。</summary>
    [JsonPropertyName("IsEmail")]
    public bool IsEmail { get; set; }

    /// <summary>是否已绑 Line。</summary>
    [JsonPropertyName("IsLine")]
    public bool IsLine { get; set; }

    /// <summary>是否已绑谷歌（旧版字段名 IsGoole）。</summary>
    [JsonPropertyName("IsGoole")]
    public bool IsGoole { get; set; }

    /// <summary>是否已绑 Telegram。</summary>
    [JsonPropertyName("IsTelegram")]
    public bool IsTelegram { get; set; }

    /// <summary>手机是否验证。</summary>
    [JsonPropertyName("IsMobile")]
    public bool IsMobile { get; set; }

    /// <summary>是否已绑谷歌验证器密钥。</summary>
    [JsonPropertyName("IsSecretKey")]
    public bool IsSecretKey { get; set; }

    /// <summary>是否临时密码。</summary>
    [JsonPropertyName("Is_Temporary_Password")]
    public bool Is_Temporary_Password { get; set; }

    /// <summary>是否已填地址。</summary>
    [JsonPropertyName("IsAddress")]
    public bool IsAddress { get; set; }
}

/// <summary>会员本人资料 DTO。</summary>
public sealed class MemberSelfInfoDto
{
    /// <summary>编号。</summary>
    [JsonPropertyName("ID")]
    public int ID { get; set; }

    /// <summary>用户名。</summary>
    [JsonPropertyName("UserName")]
    public string UserName { get; set; }

    /// <summary>昵称。</summary>
    [JsonPropertyName("NickName")]
    public string NickName { get; set; }

    /// <summary>注册时间。</summary>
    [JsonPropertyName("CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>真实姓名（脱敏）。</summary>
    [JsonPropertyName("AccountName")]
    public string AccountName { get; set; }

    /// <summary>邮箱（脱敏）。</summary>
    [JsonPropertyName("Email")]
    public string Email { get; set; }

    /// <summary>手机（脱敏）。</summary>
    [JsonPropertyName("Mobile")]
    public string Mobile { get; set; }

    /// <summary>邮箱是否验证。</summary>
    [JsonPropertyName("IsEmail")]
    public bool IsEmail { get; set; }

    /// <summary>手机是否验证。</summary>
    [JsonPropertyName("IsMobile")]
    public bool IsMobile { get; set; }

    /// <summary>头像。</summary>
    [JsonPropertyName("Avatar")]
    public string Avatar { get; set; }

    /// <summary>地址。</summary>
    [JsonPropertyName("Address")]
    public string Address { get; set; }
}

/// <summary>后台会员详情精简 DTO。</summary>
public sealed class UserMemberDetailDto
{
    /// <summary>编号。</summary>
    [JsonPropertyName("ID")]
    public int ID { get; set; }

    /// <summary>用户名。</summary>
    [JsonPropertyName("UserName")]
    public string UserName { get; set; }

    /// <summary>昵称。</summary>
    [JsonPropertyName("NickName")]
    public string NickName { get; set; }

    /// <summary>提现账户名。</summary>
    [JsonPropertyName("AccountName")]
    public string AccountName { get; set; }

    /// <summary>注册时间。</summary>
    [JsonPropertyName("CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>注册 IP。</summary>
    [JsonPropertyName("RegIP")]
    public string RegIP { get; set; }

    /// <summary>注册域名。</summary>
    [JsonPropertyName("RegDomain")]
    public string RegDomain { get; set; }

    /// <summary>登录时间。</summary>
    [JsonPropertyName("LoginAt")]
    public DateTime LoginAt { get; set; }

    /// <summary>登录 IP。</summary>
    [JsonPropertyName("LoginIP")]
    public string LoginIP { get; set; }

    /// <summary>活动时间。</summary>
    [JsonPropertyName("ActiveAt")]
    public DateTime ActiveAt { get; set; }

    /// <summary>锁定。</summary>
    [JsonPropertyName("Lock")]
    public int Lock { get; set; }

    /// <summary>功能开放位。</summary>
    [JsonPropertyName("Function")]
    public int Function { get; set; }

    /// <summary>币种。</summary>
    [JsonPropertyName("Currency")]
    public int Currency { get; set; }

    /// <summary>余额。</summary>
    [JsonPropertyName("Money")]
    public decimal Money { get; set; }

    /// <summary>上级。</summary>
    [JsonPropertyName("AgentID")]
    public int AgentID { get; set; }

    /// <summary>邀请人。</summary>
    [JsonPropertyName("InviteID")]
    public int InviteID { get; set; }

    /// <summary>分组。</summary>
    [JsonPropertyName("GroupID")]
    public int GroupID { get; set; }

    /// <summary>等级。</summary>
    [JsonPropertyName("Level")]
    public byte Level { get; set; }

    /// <summary>初始等级。</summary>
    [JsonPropertyName("InitLevel")]
    public byte InitLevel { get; set; }

    /// <summary>用户类型。</summary>
    [JsonPropertyName("Type")]
    public int Type { get; set; }

    /// <summary>代理深度。</summary>
    [JsonPropertyName("Depth")]
    public int Depth { get; set; }

    /// <summary>邮箱。</summary>
    [JsonPropertyName("Email")]
    public string Email { get; set; }

    /// <summary>手机。</summary>
    [JsonPropertyName("Mobile")]
    public string Mobile { get; set; }
}
