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
/// 会员功能位开/关落库（内存 SQLite）。
/// </summary>
public sealed class UserMemberFunctionSqliteTests : IDisposable
{
    /// <summary>旧 FunctionType.CloseSubActivity。</summary>
    private const int FunctionCloseSubActivity = 64;

    /// <summary>旧 FunctionType.CloseSub。</summary>
    private const int FunctionCloseSub = 32;

    /// <summary>旧 LockStatus.Login。</summary>
    private const int LockLogin = 1;

    private readonly SqlSugar.SqlSugarClient _db;
    private readonly UserMemberService _svc;
    private readonly int _userId;

    /// <summary>
    /// 建 Users + Site + usr_Depth，植入 Function=0 的会员。
    /// </summary>
    public UserMemberFunctionSqliteTests()
    {
        _db = SqliteBizSchema.CreateMemoryDb(["Users", "Site", "usr_Depth"], []);
        EnsureSite();
        _userId = _db.Insertable(BuildUser("u_fn")).ExecuteReturnIdentity();

        var site = new FixedSiteAccessor(1001, "fn.test");
        _svc = new UserMemberService(
            new TestBizDb(_db),
            site,
            new SiteAdminAuthService(new TestBizDb(_db), site, new HttpContextAccessor(), new EmptySimpXCache()),
            new FixedPermissionChecker(grantAll: true));
    }

    /// <summary>
    /// 开启功能位后落库为按位或。
    /// </summary>
    [Fact]
    public async Task UpdateFunction_open_bit_persists()
    {
        var result = await _svc.UpdateFunctionAsync(_userId, FunctionCloseSubActivity, isOpen: true);
        Assert.Equal(1, result.Success);

        var user = await _db.Queryable<User>().InSingleAsync(_userId);
        Assert.Equal(FunctionCloseSubActivity, user.Function);
    }

    /// <summary>
    /// 关闭已开启功能位后落库清掉该位。
    /// </summary>
    [Fact]
    public async Task UpdateFunction_close_bit_persists()
    {
        var open = await _svc.UpdateFunctionAsync(_userId, FunctionCloseSubActivity, isOpen: true);
        Assert.Equal(1, open.Success);

        var close = await _svc.UpdateFunctionAsync(_userId, FunctionCloseSubActivity, isOpen: false);
        Assert.Equal(1, close.Success);

        var user = await _db.Queryable<User>().InSingleAsync(_userId);
        Assert.Equal(0, user.Function);
    }

    /// <summary>
    /// 非法功能位直接失败且不改库。
    /// </summary>
    [Fact]
    public async Task UpdateFunction_invalid_bit_fails()
    {
        var result = await _svc.UpdateFunctionAsync(_userId, 3, isOpen: true);
        Assert.Equal(0, result.Success);
        Assert.Equal("#参数错误#", result.Msg);

        var user = await _db.Queryable<User>().InSingleAsync(_userId);
        Assert.Equal(0, user.Function);
    }

    /// <summary>
    /// 打开 CloseSub 时下级被加上登录锁；关闭后去掉。
    /// </summary>
    [Fact]
    public async Task UpdateFunction_CloseSub_cascades_child_login_lock()
    {
        var childId = _db.Insertable(BuildUser("u_child")).ExecuteReturnIdentity();
        _db.Insertable(new UserDepth
        {
            SiteID = 1001,
            UserID = _userId,
            ChildID = childId,
            Depth = 1
        }).ExecuteCommand();

        var open = await _svc.UpdateFunctionAsync(_userId, FunctionCloseSub, isOpen: true);
        Assert.Equal(1, open.Success);
        var childLocked = await _db.Queryable<User>().InSingleAsync(childId);
        Assert.Equal(LockLogin, childLocked.Lock & LockLogin);

        var close = await _svc.UpdateFunctionAsync(_userId, FunctionCloseSub, isOpen: false);
        Assert.Equal(1, close.Success);
        var childUnlocked = await _db.Queryable<User>().InSingleAsync(childId);
        Assert.Equal(0, childUnlocked.Lock & LockLogin);
    }

    /// <inheritdoc />
    public void Dispose() => _db?.Dispose();

    /// <summary>
    /// 写入当前站壳。
    /// </summary>
    private void EnsureSite()
    {
        if (_db.Queryable<Site>().Any(s => s.ID == 1001))
            return;
        _db.Insertable(new Site
        {
            ID = 1001,
            Name = "fn",
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
    private static User BuildUser(string userName) => new()
    {
        SiteID = 1001,
        UserName = userName,
        NickName = userName,
        AccountName = "",
        Password = "x",
        PayPassword = "",
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
        Email = "",
        IsEmail = false,
        Mobile = "",
        IsMobile = false,
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
    /// 空缓存。
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
