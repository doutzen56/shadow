using SimpX.Net.Application.Shared;
using SimpX.Net.Data;
using SimpX.Net.Data.Entity.Sites;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 新闻公告增删改查（内存 SQLite）。禁止写 BetWin30。
/// </summary>
public class SiteNewsCrudSqliteTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;
    private readonly FixedSiteAccessor _site = new(1001, "crud.test");

    /// <summary>
    /// 建表并植入栏目。
    /// </summary>
    public SiteNewsCrudSqliteTests()
    {
        _db = SqliteBizSchema.CreateMemoryDb(["site_News", "site_NewsColumn", "site_Language"], []);
        _db.Insertable(new NewsColumn
        {
            SiteID = 1001,
            Name = "测栏",
            Code = "t",
            Sort = 1
        }).ExecuteCommand();
    }

    /// <inheritdoc />
    public void Dispose() => _db?.Dispose();

    /// <summary>
    /// 增→查→改→删。
    /// </summary>
    [Fact]
    public async Task Crud_RoundTrip()
    {
        var cols = await _db.Queryable<NewsColumn>().ToListAsync();
        Assert.Single(cols);
        var colId = cols[0].ID;

        var svc = new SiteNewsService(new TestBizDb(_db), _site);
        await svc.SaveAsync(new SiteNewsSaveInput
        {
            ID = 0,
            ColID = colId,
            Title = "{\"zh-CN\":\"新闻1\"}",
            Content = "{\"zh-CN\":\"内容1\"}",
            Cover = "{}",
            CreateAt = DateTime.Now,
            StartAt = DateTime.Now.Date,
            EndAt = DateTime.Now.AddDays(7).Date,
            Sort = 0
        });

        var page1 = await svc.PageAsync(new SiteNewsPageInput { Page = 1, PageSize = 20 });
        Assert.Equal(1, page1.RecordCount);
        var id = page1.Data[0].ID;

        await svc.SaveAsync(new SiteNewsSaveInput
        {
            ID = id,
            ColID = colId,
            Title = "{\"zh-CN\":\"新闻2\"}",
            Content = "{\"zh-CN\":\"内容2\"}",
            Cover = "{}",
            CreateAt = DateTime.Now,
            StartAt = DateTime.Now.Date,
            EndAt = DateTime.Now.AddDays(7).Date,
            Sort = 1
        });

        var detail = await svc.DetailAsync(id);
        Assert.Contains("新闻2", detail.Title);

        await svc.DeleteAsync(id);
        var page2 = await svc.PageAsync(new SiteNewsPageInput { Page = 1, PageSize = 20 });
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
