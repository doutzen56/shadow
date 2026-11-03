using Microsoft.AspNetCore.Http;
using SimpX.Net.Application.Shared;
using SimpX.Net.Caching;
using SimpX.Net.Data;
using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Users;
using SimpX.Net.Feature.Auth;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 会员重置字段（内存 SQLite）：Mobile / PayPassword / 未知 Field。
/// </summary>
public sealed class UserMemberResetSqliteTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;
    private readonly UserMemberService _svc;
    private readonly int _userId;

    /// <summary>
    /// 建 Users + Site，植入带手机与资金密码的会员。
    /// </summary>
    public UserMemberResetSqliteTests()
    {
        _db = SqliteBizSchema.CreateMemoryDb(["Users", "Site"], []);
        EnsureSite();
        _userId = _db.Insertable(BuildUser(
                userName: "u_reset",
                mobile: "13800138000",
                isMobile: true,
                payPassword: "pay-hash"))
            .ExecuteReturnIdentity();

        var site = new FixedSiteAccessor(1001, "reset.test");
        _svc = new UserMemberService(
            new TestBizDb(_db),
            site,
            new SiteAdminAuthService(new TestBizDb(_db), site, new HttpContextAccessor(), new EmptySimpXCache()),
            new FixedPermissionChecker(grantAll: true));
    }

    /// <summary>
    /// Mobile：清空手机与已验证标记。
    /// </summary>
    [Fact]
    public async Task Reset_Mobile_clears_mobile_and_flag()
    {
        var result = await _svc.ResetAsync(_userId, "Mobile");
        Assert.Equal(1, result.Success);

        var user = await _db.Queryable<User>().InSingleAsync(_userId);
        Assert.Equal(string.Empty, user.Mobile);
        Assert.False(user.IsMobile);
    }

    /// <summary>
    /// PayPassword：清空资金密码。
    /// </summary>
    [Fact]
    public async Task Reset_PayPassword_clears_pay_password()
    {
        var result = await _svc.ResetAsync(_userId, "PayPassword");
        Assert.Equal(1, result.Success);

        var user = await _db.Queryable<User>().InSingleAsync(_userId);
        Assert.Equal(string.Empty, user.PayPassword);
    }

    /// <summary>
    /// 未知 Field：返回暂不支持。
    /// </summary>
    [Fact]
    public async Task Reset_unknown_field_fails()
    {
        var result = await _svc.ResetAsync(_userId, "Telegram");
        Assert.Equal(0, result.Success);
        Assert.Equal("#暂不支持该字段#", result.Msg);

        var user = await _db.Queryable<User>().InSingleAsync(_userId);
        Assert.Equal("13800138000", user.Mobile);
        Assert.Equal("pay-hash", user.PayPassword);
    }

    /// <inheritdoc />
    public void Dispose() => _db?.Dispose();

    /// <summary>
    /// 写入当前站壳（Setting 可空；本测不覆盖 Password）。
    /// </summary>
    private void EnsureSite()
    {
        if (_db.Queryable<Site>().Any(s => s.ID == 1001))
            return;
        _db.Insertable(new Site
        {
            ID = 1001,
            Name = "reset",
            CreateAt = DateTime.UtcNow,
            NotifyAt = DateTime.UtcNow,
            Status = 0,
            Currency = 1,
            Wallet = 1,
            IsCurrencySelect = false,
            APIKey = Guid.NewGuid(),
            SettingString = "",
            APPSettingString = "",
            PCSettingString = "",
            GameCredit = 0,
            NotifyCredit = 0,
            AdminDomain = "",
            HomeTemplateName = ""
        }).ExecuteCommand();
    }

    /// <summary>
    /// 构造会员行。
    /// </summary>
    private static User BuildUser(string userName, string mobile, bool isMobile, string payPassword) => new()
    {
        SiteID = 1001,
        UserName = userName,
        NickName = userName,
        AccountName = "",
        Password = "x",
        PayPassword = payPassword,
        CreateAt = DateTime.UtcNow,
        Face = "",
        RegIP = "",
        Lock = 0,
        Function = 0,
        LoginAt = DateTime.UtcNow,
        LoginIP = "",
        AgentID = 0,
        Depth = 0,
        GroupID = 1,
        Type = 0,
        Email = "a@b.com",
        IsEmail = true,
        Mobile = mobile,
        IsMobile = isMobile,
        Question = 0,
        Answer = "",
        SecretKey = Guid.Empty,
        Currency = 1,
        Money = 0,
        LockMoney = 0,
        GameMoney = 0,
        IsOnline = false,
        Flag = 0,
        Level = 0,
        ActiveAt = DateTime.UtcNow,
        Exper = 0,
        RegFlag = 0,
        RegDomain = "",
        LoginPlatform = 0,
        RegPlatform = 0,
        Unbalanced = 0,
        InviteID = 0,
        UpdateAt = DateTime.UtcNow,
        ProxyRate = 0,
        WashingRatio = "",
        LastReturn = DateTime.UtcNow,
        Coin = 0,
        DisableCurrency = "",
        Address = "",
        Birthday = DateTime.UtcNow,
        Telegram = "",
        Line = "",
        Google = "",
        Info = "",
        DeviceId = "",
        RegisteredFrom = "",
        PixelID = "",
        InitLevel = 0,
        Is_Temporary_Password = false,
        BlockGame = "",
        OldGroupID = 0
    };

    /// <summary>
    /// 空缓存，写测不走登录计数。
    /// </summary>
    private sealed class EmptySimpXCache : ISimpXCache
    {
        /// <inheritdoc />
        public void Add<T>(string cacheName, T value, TimeSpan expire)
        {
        }

        /// <inheritdoc />
        public void Add<T>(string cacheName, object id, T? value, TimeSpan expire)
        {
        }

        /// <inheritdoc />
        public Task<T?> GetOrAddAsync<T>(string cacheName, object? id, Func<CancellationToken, Task<T?>> factory, TimeSpan expire, CancellationToken cancellationToken = default) =>
            factory(cancellationToken);

        /// <inheritdoc />
        public T? GetOrAdd<T>(string cacheName, object? id, Func<T?> factory, TimeSpan expire) => factory();

        /// <inheritdoc />
        public T? Get<T>(string cacheName, object? id) => default;

        /// <inheritdoc />
        public void Remove(string cacheName, object? id)
        {
        }

        /// <inheritdoc />
        public void RemoveEntity<T>(string cacheName, T value)
        {
        }

        /// <inheritdoc />
        public void RemoveAll(string cacheName)
        {
        }
    }

    /// <summary>
    /// 固定当前站点。
    /// </summary>
    private sealed class FixedSiteAccessor : ICurrentSiteAccessor
    {
        /// <summary>
        /// 指定站点号与主机名。
        /// </summary>
        /// <param name="siteId">站点号</param>
        /// <param name="host">主机名</param>
        public FixedSiteAccessor(int? siteId, string host)
        {
            SiteId = siteId;
            Host = host;
        }

        /// <inheritdoc />
        public int? SiteId { get; }

        /// <inheritdoc />
        public string Host { get; }

        /// <inheritdoc />
        public int RequireSiteId() =>
            SiteId ?? throw new InvalidOperationException("Z1005：未定站（单测替身）");
    }

    /// <summary>
    /// 固定权限检查结果。
    /// </summary>
    private sealed class FixedPermissionChecker : IPermissionChecker
    {
        private readonly bool _grantAll;

        /// <summary>
        /// 指定是否一律放行。
        /// </summary>
        /// <param name="grantAll">true 表示有全部权限</param>
        public FixedPermissionChecker(bool grantAll) => _grantAll = grantAll;

        /// <inheritdoc />
        public Task<bool> IsGrantedAsync(string permissionCode) => Task.FromResult(_grantAll);
    }
}
