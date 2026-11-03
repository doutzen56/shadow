using SimpX.Net.Application.Shared;
using SimpX.Net.Data;
using SimpX.Net.Data.Entity.Sites;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 帮助文章增删改查（内存 SQLite）。禁止写 BetWin30。
/// </summary>
public class SiteHelpCrudSqliteTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;
    private readonly FixedSiteAccessor _site = new(1001, "crud.test");

    /// <summary>
    /// 建表并植入栏目。
    /// </summary>
    public SiteHelpCrudSqliteTests()
    {
        _db = SqliteBizSchema.CreateMemoryDb(["site_Help", "site_HelpColumn"], []);
        _db.Insertable(new HelpColumn
        {
            SiteID = 1001,
            Name = "{\"zh-CN\":\"测栏\"}",
            CreateAt = DateTime.Now,
            Count = 0,
            Sort = 1,
            Url = "",
            IsFooter = false
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
        var cols = await _db.Queryable<HelpColumn>().ToListAsync();
        Assert.Single(cols);
        var colId = cols[0].ID;

        var svc = new SiteHelpService(new TestBizDb(_db), _site);
        await svc.SaveAsync(new SiteHelpSaveInput
        {
            ID = 0,
            ColID = colId,
            Title = "{\"zh-CN\":\"标题1\"}",
            Content = "{\"zh-CN\":\"正文1\"}",
            Sort = 1
        });

        var list1 = await svc.ListAsync();
        Assert.Single(list1);
        var id = list1[0].ID;

        var colAfterAdd = (await _db.Queryable<HelpColumn>()
            .Where(c => c.ID == colId).Take(1).ToListAsync()).First();
        Assert.Equal(1, colAfterAdd.Count);

        var info = await svc.InfoAsync(id);
        Assert.Contains("标题1", info.Title);

        await svc.SaveAsync(new SiteHelpSaveInput
        {
            ID = id,
            ColID = colId,
            Title = "{\"zh-CN\":\"标题2\"}",
            Content = "{\"zh-CN\":\"正文2\"}",
            Sort = 2
        });

        var info2 = await svc.InfoAsync(id);
        Assert.Contains("标题2", info2.Title);

        await svc.DeleteAsync(id);
        Assert.Empty(await svc.ListAsync());
        var colAfterDel = (await _db.Queryable<HelpColumn>()
            .Where(c => c.ID == colId).Take(1).ToListAsync()).First();
        Assert.Equal(0, colAfterDel.Count);
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
