using SimpX.Net.Application.Shared;
using SimpX.Net.Data;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 内容区域增删改查（内存 SQLite）。
/// </summary>
public sealed class SiteRegionCrudSqliteTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;
    private readonly FixedSiteAccessor _site = new(1001, "region.test");
    private readonly SiteRegionService _svc;

    /// <summary>
    /// 建表。
    /// </summary>
    public SiteRegionCrudSqliteTests()
    {
        _db = SqliteBizSchema.CreateMemoryDb([], []);
        _db.Ado.ExecuteCommand(
            """
            CREATE TABLE IF NOT EXISTS [site_Region] (
              [RegionID] INTEGER PRIMARY KEY AUTOINCREMENT,
              [SiteID] INTEGER NOT NULL,
              [RegionName] TEXT,
              [CreateAt] TEXT,
              [Description] TEXT,
              [Content] TEXT,
              [Type] INTEGER NOT NULL DEFAULT 0,
              [IsSiteGif] INTEGER NOT NULL DEFAULT 0,
              [GifType] INTEGER NOT NULL DEFAULT 0
            );
            CREATE TABLE IF NOT EXISTS [site_RegionContent] (
              [AdId] INTEGER PRIMARY KEY AUTOINCREMENT,
              [SiteID] INTEGER NOT NULL,
              [RegionID] INTEGER NOT NULL,
              [AdNo] TEXT,
              [Title] TEXT,
              [Content] TEXT,
              [Remark] TEXT,
              [Url] TEXT,
              [Sort] INTEGER NOT NULL DEFAULT 0,
              [Pic] TEXT,
              [IsShow] INTEGER NOT NULL DEFAULT 1,
              [ClickCount] INTEGER NOT NULL DEFAULT 0,
              [ExpireAt] TEXT,
              [CreateAt] TEXT,
              [UrlType] INTEGER NOT NULL DEFAULT 0
            );
            """);
        _svc = new SiteRegionService(new TestBizDb(_db), _site);
    }

    /// <inheritdoc />
    public void Dispose() => _db?.Dispose();

    /// <summary>
    /// 区域 CRUD + 内容增删。
    /// </summary>
    [Fact]
    public async Task Region_and_content_round_trip()
    {
        await _svc.SaveAsync(new SiteRegionSaveInput
        {
            ID = 0,
            Name = "轮播区",
            Description = "测",
            Content = "{\"Interval\":0,\"Items\":[]}",
            Type = 1
        });

        var page = await _svc.PageAsync(new SiteRegionPageInput { Page = 1, PageSize = 20 });
        Assert.Equal(1, page.RecordCount);
        var regionId = page.Data[0].ID;

        await _svc.ContentSaveAsync(new SiteRegionContentSaveInput
        {
            ID = 0,
            RegionID = regionId,
            Title = "图1",
            Content = "c",
            Sort = 1,
            IsShow = true
        });

        var contents = await _svc.ContentListAsync(regionId);
        Assert.Single(contents);
        Assert.Equal("图1", contents[0].Title);

        await _svc.ContentDeleteAsync(contents[0].ID);
        Assert.Empty(await _svc.ContentListAsync(regionId));

        await _svc.DeleteAsync(regionId);
        var page2 = await _svc.PageAsync(new SiteRegionPageInput());
        Assert.Equal(0, page2.RecordCount);
    }

    /// <summary>
    /// 固定定站。
    /// </summary>
    private sealed class FixedSiteAccessor : ICurrentSiteAccessor
    {
        /// <summary>
        /// 指定站。
        /// </summary>
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
            SiteId ?? throw new InvalidOperationException("Z1005：未定站");
    }
}
