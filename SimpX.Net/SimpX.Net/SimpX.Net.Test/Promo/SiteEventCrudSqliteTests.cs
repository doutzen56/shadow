using SimpX.Net.Application.Shared;
using SimpX.Net.Data;
using SimpX.Net.Data.Entity.Sites;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 优惠活动增删改查（内存 SQLite）。禁止写 BetWin30。
/// </summary>
public class SiteEventCrudSqliteTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;
    private readonly FixedSiteAccessor _site = new(1001, "crud.test");

    /// <summary>
    /// 建表并植入栏目。
    /// </summary>
    public SiteEventCrudSqliteTests()
    {
        _db = SqliteBizSchema.CreateMemoryDb(["site_Event", "site_EventColumn"], []);
        _db.Insertable(new SiteEventColumn
        {
            SiteID = 1001,
            Name = "测栏",
            Sort = 1,
            EventCount = 0
        }).ExecuteCommand();
    }

    /// <inheritdoc />
    public void Dispose() => _db?.Dispose();

    /// <summary>
    /// 增→查→改→删，栏目计数联动。
    /// </summary>
    [Fact]
    public async Task Crud_RoundTrip()
    {
        var cols = await _db.Queryable<SiteEventColumn>().ToListAsync();
        Assert.Single(cols);
        var colId = cols[0].ID;

        var svc = new SiteEventService(new TestBizDb(_db), _site);
        await svc.SaveAsync(new SiteEventSaveInput
        {
            ID = 0,
            ColID = colId,
            Title = "{\"zh-CN\":\"活动1\"}",
            Content = "{\"zh-CN\":\"正文1\"}",
            StartAt = DateTime.Now.Date,
            ExpireAt = DateTime.Now.AddDays(7).Date,
            IsVisible = true,
            IsOpen = true
        });

        var list1 = await svc.ListAsync(new SiteEventListInput());
        Assert.Single(list1);
        var id = list1[0].ID;

        var colAfterAdd = (await _db.Queryable<SiteEventColumn>()
            .Where(c => c.ID == colId).Take(1).ToListAsync()).First();
        Assert.Equal(1, colAfterAdd.EventCount);

        var info = await svc.InfoAsync(id);
        Assert.Contains("活动1", info.Title);

        await svc.SaveAsync(new SiteEventSaveInput
        {
            ID = id,
            ColID = colId,
            Title = "{\"zh-CN\":\"活动2\"}",
            Content = "{\"zh-CN\":\"正文2\"}",
            StartAt = DateTime.Now.Date,
            ExpireAt = DateTime.Now.AddDays(7).Date,
            IsVisible = true,
            IsOpen = false
        });

        var info2 = await svc.InfoAsync(id);
        Assert.Contains("活动2", info2.Title);
        Assert.False(info2.IsOpen);

        await svc.DeleteAsync(id);
        Assert.Empty(await svc.ListAsync(new SiteEventListInput()));
        var colAfterDel = (await _db.Queryable<SiteEventColumn>()
            .Where(c => c.ID == colId).Take(1).ToListAsync()).First();
        Assert.Equal(0, colAfterDel.EventCount);
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
