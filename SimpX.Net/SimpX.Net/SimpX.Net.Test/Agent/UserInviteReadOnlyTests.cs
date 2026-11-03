using SimpX.Net.Application.Shared;
using SimpX.Net.Data;
using SimpX.Net.Data.Entity.Sites;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>邀请码只读。禁止写库。</summary>
public class UserInviteReadOnlyTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;

    /// <summary>建立只读连接。</summary>
    public UserInviteReadOnlyTests() => _db = OrmConnection.CreateClient();

    /// <inheritdoc />
    public void Dispose() => _db?.Dispose();

    /// <summary>PROXY-P2：定站列表可读；未定站失败。</summary>
    [Fact]
    public async Task Invite01_List_ReadOnly()
    {
        var host = await FirstHostAsync();
        var svc = new UserInviteService(new TestBizDb(_db), new FixedSiteAccessor(host.SiteID, host.Host));
        var list = await svc.ListAsync(new UserInviteListInput());
        Assert.NotNull(list);

        var noSite = new UserInviteService(new TestBizDb(_db), new FixedSiteAccessor(null, null));
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            noSite.ListAsync(new UserInviteListInput()));
        Assert.Contains("Z1005", ex.Message, StringComparison.Ordinal);
    }

    private async Task<SiteHost> FirstHostAsync()
    {
        var hosts = await _db.Queryable<SiteHost>()
            .Where(h => h.IsEnable && h.Host != null && h.SiteID > 0)
            .OrderBy(h => h.ID).Take(1).ToListAsync();
        Assert.NotEmpty(hosts);
        return hosts[0];
    }

    private sealed class FixedSiteAccessor : ICurrentSiteAccessor
    {
        public FixedSiteAccessor(int? siteId, string host) { SiteId = siteId; Host = host; }
        public int? SiteId { get; }
        public string Host { get; }
        public int RequireSiteId() => SiteId ?? throw new InvalidOperationException("Z1005：未定站");
    }
}
