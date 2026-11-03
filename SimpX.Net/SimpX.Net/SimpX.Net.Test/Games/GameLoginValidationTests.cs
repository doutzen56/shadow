using SimpX.Net.Application.Shared;
using SimpX.Net.Data;
using SimpX.Net.Data.Entity.Games;
using SimpX.Net.Data.Entity.Sites;
using Xunit;
using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Test;

/// <summary>
/// 进游戏校验（只读连 BetWin30）。不注册厂商、不写 game_User。
/// </summary>
public class GameLoginValidationTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;

    /// <summary>
    /// 建立只读连接。
    /// </summary>
    public GameLoginValidationTests()
    {
        _db = OrmConnection.CreateClient();
    }

    /// <inheritdoc />
    public void Dispose() => _db?.Dispose();

    /// <summary>
    /// GAME-02a：缺 Category → 参数错误。
    /// </summary>
    [Fact]
    public async Task Game02a_Login_MissingCategory_Fails()
    {
        var host = await FirstHostAsync();
        var svc = CreateService(host.SiteID, host.Host, userId: null);
        var result = await svc.LoginAsync(new Dictionary<string, string>
        {
            ["Type"] = "1"
        });
        Assert.Equal(0, result.Success);
        Assert.Contains("Category", result.Msg, StringComparison.Ordinal);
    }

    /// <summary>
    /// GAME-02b：游戏未开放或不存在 → 失败。
    /// </summary>
    [Fact]
    public async Task Game02b_Login_ClosedOrMissing_Fails()
    {
        var host = await FirstHostAsync();
        var svc = CreateService(host.SiteID, host.Host, userId: 1);
        var result = await svc.LoginAsync(new Dictionary<string, string>
        {
            ["Type"] = "999999",
            ["Category"] = "1",
            ["Currency"] = "1",
            ["Language"] = "CHN"
        });
        Assert.Equal(0, result.Success);
        Assert.Contains("游戏未开放", result.Msg, StringComparison.Ordinal);
    }

    /// <summary>
    /// GAME-02c：已开启游戏但无厂商适配 → 未接入（不写库）。
    /// </summary>
    [Fact]
    public async Task Game02c_Login_OpenGame_NoVendor_NotConnected()
    {
        var host = await FirstHostAsync();
        var open = await _db.Queryable<GameSetting>()
            .Where(g => g.SiteID == host.SiteID && g.IsOpen && !g.IsDisable)
            .OrderBy(g => g.Game)
            .Take(1)
            .ToListAsync();
        Assert.NotEmpty(open);

        var members = await _db.Queryable<User>()
            .Where(u => u.SiteID == host.SiteID)
            .OrderBy(u => u.ID)
            .Take(1)
            .ToListAsync();
        Assert.NotEmpty(members);

        var svc = CreateService(host.SiteID, host.Host, userId: members[0].ID);
        var result = await svc.LoginAsync(new Dictionary<string, string>
        {
            ["Type"] = open[0].Game.ToString(),
            ["Category"] = "1",
            ["Currency"] = "1",
            ["Language"] = "CHN"
        });
        Assert.Equal(0, result.Success);
        Assert.Contains("游戏接口未接入", result.Msg, StringComparison.Ordinal);
    }

    /// <summary>
    /// 取一个可用主机。
    /// </summary>
    private async Task<SiteHost> FirstHostAsync()
    {
        var hosts = await _db.Queryable<SiteHost>()
            .Where(h => h.IsEnable && h.Host != null && h.SiteID > 0)
            .OrderBy(h => h.ID)
            .Take(1)
            .ToListAsync();
        Assert.NotEmpty(hosts);
        return hosts[0];
    }

    /// <summary>
    /// 构造服务。
    /// </summary>
    private GameLoginService CreateService(int siteId, string host, int? userId) =>
        new(
            new TestBizDb(_db),
            new FixedSiteAccessor(siteId, host),
            new FixedMemberResolver(userId),
            Array.Empty<IGameVendorLogin>());

    /// <summary>
    /// 固定定站。
    /// </summary>
    private sealed class FixedSiteAccessor : ICurrentSiteAccessor
    {
        /// <summary>
        /// 指定站。
        /// </summary>
        /// <param name="siteId">站号</param>
        /// <param name="host">主机</param>
        public FixedSiteAccessor(int siteId, string host)
        {
            SiteId = siteId;
            Host = host;
        }

        /// <inheritdoc />
        public int? SiteId { get; }

        /// <inheritdoc />
        public string Host { get; }

        /// <inheritdoc />
        public int RequireSiteId() => SiteId ?? throw new InvalidOperationException("Z1005");
    }

    /// <summary>
    /// 固定会员 Cookie 解析结果。
    /// </summary>
    private sealed class FixedMemberResolver : IMemberCookieUserIdResolver
    {
        private readonly int? _userId;

        /// <summary>
        /// 指定会员编号。
        /// </summary>
        /// <param name="userId">会员；null 未登录</param>
        public FixedMemberResolver(int? userId) => _userId = userId;

        /// <inheritdoc />
        public Task<int?> TryResolveUserIdFromCookieAsync(int siteId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_userId);
    }
}
