using Microsoft.AspNetCore.Http;
using SimpX.Net.Application.Shared;
using SimpX.Net.Caching;
using SimpX.Net.Data;
using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Feature.Auth;
using Xunit;
using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Test;

/// <summary>
/// 只读连 BetWin30，验 UserMemberService 分页/详情。禁止写测试库。
/// </summary>
public class UserMemberQueryReadOnlyTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;

    /// <summary>
    /// 建立只读业务库连接。
    /// </summary>
    public UserMemberQueryReadOnlyTests()
    {
        _db = OrmConnection.CreateClient();
    }

    /// <inheritdoc />
    public void Dispose() => _db?.Dispose();

    /// <summary>
    /// USER-01：定站后分页，行属当前站；字段含 ID/UserName。
    /// </summary>
    [Fact]
    public async Task User01_Page_ReturnsCurrentSiteRows_ReadOnly()
    {
        var hosts = await _db.Queryable<SiteHost>()
            .Where(h => h.IsEnable && h.Host != null && h.SiteID > 0)
            .OrderBy(h => h.ID)
            .Take(1)
            .ToListAsync();
        Assert.NotEmpty(hosts);
        var host = hosts[0];

        var hasUser = await _db.Queryable<User>().AnyAsync(u => u.SiteID == host.SiteID);
        Assert.True(hasUser);

        var site = new FixedSiteAccessor(host.SiteID, host.Host);
        var svc = new UserMemberService(
            new TestBizDb(_db),
            site,
            CreateAuth(site),
            new FixedPermissionChecker(grantAll: true));

        var page = await svc.PageAsync(new UserMemberPageInput { Page = 1, PageSize = 10 });
        Assert.True(page.RecordCount >= 1);
        Assert.Equal(page.RecordCount, page.Total);
        Assert.NotEmpty(page.Data);
        Assert.All(page.Data, row => Assert.True(row.ID > 0));
        Assert.False(string.IsNullOrEmpty(page.Data[0].UserName));
    }

    /// <summary>
    /// USER-02：详情可读；无「查看联系方式」时手机/邮箱脱敏。
    /// </summary>
    [Fact]
    public async Task User02_Detail_MasksContactWithoutPermission_ReadOnly()
    {
        var hosts = await _db.Queryable<SiteHost>()
            .Where(h => h.IsEnable && h.Host != null && h.SiteID > 0)
            .OrderBy(h => h.ID)
            .Take(1)
            .ToListAsync();
        Assert.NotEmpty(hosts);
        var host = hosts[0];

        var users = await _db.Queryable<User>()
            .Where(u => u.SiteID == host.SiteID
                        && u.Mobile != null && u.Mobile.Length >= 7
                        && u.Email != null && u.Email.Contains("@"))
            .OrderBy(u => u.ID)
            .Take(1)
            .ToListAsync();
        Assert.NotEmpty(users);
        var user = users[0];

        var site = new FixedSiteAccessor(host.SiteID, host.Host);
        var svc = new UserMemberService(
            new TestBizDb(_db),
            site,
            CreateAuth(site),
            new FixedPermissionChecker(grantAll: false));

        var detail = await svc.DetailAsync(user.ID);
        Assert.Equal(user.ID, detail.ID);
        Assert.Equal(user.UserName, detail.UserName);
        Assert.Contains("****", detail.Mobile);
        Assert.Contains("**", detail.Email);
    }

    /// <summary>
    /// USER-03：未定站时分页在查库前失败。
    /// </summary>
    [Fact]
    public async Task User03_Page_WithoutSite_FailsBeforeQuery()
    {
        var site = new FixedSiteAccessor(null, null);
        var svc = new UserMemberService(
            new TestBizDb(_db),
            site,
            CreateAuth(site),
            new FixedPermissionChecker(grantAll: true));
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.PageAsync(new UserMemberPageInput()));
        Assert.Contains("Z1005", ex.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 只读测用的后台会话（无 Cookie，写操作会走 JWT 权限）。
    /// </summary>
    /// <param name="site">当前站</param>
    /// <returns>会话服务</returns>
    private SiteAdminAuthService CreateAuth(ICurrentSiteAccessor site) =>
        new(new TestBizDb(_db), site, new HttpContextAccessor(), new EmptySimpXCache());

    /// <summary>
    /// 空缓存，只读测不走登录计数。
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
    /// 固定当前站点，仅供只读测试。
    /// </summary>
    private sealed class FixedSiteAccessor : ICurrentSiteAccessor
    {
        /// <summary>
        /// 指定站点号与主机名。
        /// </summary>
        /// <param name="siteId">站点号；null 表示未定站</param>
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
            SiteId ?? throw new InvalidOperationException("Z1005：未定站（单测替身，宿主为 Oops）");
    }

    /// <summary>
    /// 固定权限检查结果，仅供只读测试。
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
