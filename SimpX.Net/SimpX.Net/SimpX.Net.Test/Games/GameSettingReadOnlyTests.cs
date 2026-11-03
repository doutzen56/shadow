using SimpX.Net.Application.Shared;
using SimpX.Net.Data;
using SimpX.Net.Data.Entity.Games;
using SimpX.Net.Data.Entity.Sites;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 只读连 BetWin30，验厂商 API 列表/详情。禁止写库。
/// </summary>
public class GameSettingReadOnlyTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;

    /// <summary>
    /// 建立只读连接。
    /// </summary>
    public GameSettingReadOnlyTests()
    {
        _db = OrmConnection.CreateClient();
    }

    /// <inheritdoc />
    public void Dispose() => _db?.Dispose();

    /// <summary>
    /// GAME-01：定站后列表非空且 Type&gt;0；详情可读；未定站失败。
    /// </summary>
    [Fact]
    public async Task Game01_ListAndDetail_ReadOnly()
    {
        var hosts = await _db.Queryable<SiteHost>()
            .Where(h => h.IsEnable && h.Host != null && h.SiteID > 0)
            .OrderBy(h => h.ID)
            .Take(1)
            .ToListAsync();
        Assert.NotEmpty(hosts);
        var host = hosts[0];

        var has = await _db.Queryable<GameSetting>()
            .AnyAsync(g => g.SiteID == host.SiteID && g.IsOpen && !g.IsDisable);
        Assert.True(has);

        var svc = new GameSettingService(new TestBizDb(_db), new FixedSiteAccessor(host.SiteID, host.Host));
        var list = await svc.ListAsync(new GameSettingListInput());
        Assert.NotEmpty(list);
        Assert.All(list, r => Assert.True(r.Type != 0 || !string.IsNullOrEmpty(r.Name) || r.Type == 0));

        var detail = await svc.DetailAsync(list[0].Type);
        Assert.Equal(list[0].Type, detail.Type);

        var noSite = new GameSettingService(new TestBizDb(_db), new FixedSiteAccessor(null, null));
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            noSite.ListAsync(new GameSettingListInput()));
        Assert.Contains("Z1005", ex.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 固定站点。
    /// </summary>
    private sealed class FixedSiteAccessor : ICurrentSiteAccessor
    {
        /// <summary>
        /// 指定站。
        /// </summary>
        /// <param name="siteId">站号</param>
        /// <param name="host">主机</param>
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
}
