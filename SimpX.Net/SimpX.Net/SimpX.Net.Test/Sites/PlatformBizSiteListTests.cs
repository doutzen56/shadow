using SimpX.Net.Application.Shared;
using SimpX.Net.Data;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>总后台站点列表只读。禁止写库。</summary>
public class PlatformBizSiteListTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;

    /// <summary>建立只读连接。</summary>
    public PlatformBizSiteListTests() => _db = OrmConnection.CreateClient();

    /// <inheritdoc />
    public void Dispose() => _db?.Dispose();

    /// <summary>PLAT-01：不传 SiteID 可列出站点。</summary>
    [Fact]
    public async Task Plat01_ListItems_ReadOnly()
    {
        var svc = new SystemSiteService(new TestBizDb(_db));
        var list = await svc.ListItemsAsync(0, null, null);
        Assert.NotNull(list);
        Assert.True(list.Count >= 0);
        if (list.Count > 0)
            Assert.True(list[0].ID > 0);
    }
}
