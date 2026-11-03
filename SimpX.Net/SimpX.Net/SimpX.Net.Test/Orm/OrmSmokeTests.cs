using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Users;
using SqlSugar;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// BetWin30 SqlSugar 冒烟：连库、读站点样板、SiteID 过滤、分页、复合主键（ORM-01～04）。
/// 只读现库，不开 CodeFirst，不调资金存储过程。
/// </summary>
public class OrmSmokeTests : IDisposable
{
    private readonly SqlSugarClient _db;

    /// <summary>
    /// 建立指向 BetWin30 的 SqlSugar 客户端（连接串来自 Host.Admin Database.json）。
    /// </summary>
    public OrmSmokeTests()
    {
        _db = OrmConnection.CreateClient();
    }

    /// <summary>
    /// ORM-01：能连上库，并按主键读到至少一条已迁实体（SiteHost 或 SiteConfig）。
    /// </summary>
    [Fact]
    public void Orm01_ConnectAndReadSiteSample_ByPrimaryKey()
    {
        // 先探测连通；失败时异常即失败
        _db.Ado.Open();
        Assert.Equal(System.Data.ConnectionState.Open, _db.Ado.Connection.State);

        var host = _db.Queryable<SiteHost>().OrderBy(h => h.ID).First();
        Assert.NotNull(host);
        Assert.True(host.ID > 0);
        Assert.True(host.SiteID > 0);

        var config = _db.Queryable<SiteConfig>().InSingle(host.SiteID);
        // 个别站可能没有 Config 行；有则校验主键一致
        if (config != null)
            Assert.Equal(host.SiteID, config.SiteID);
    }

    /// <summary>
    /// ORM-02：UserMoney 按 SiteID 过滤后，结果里不应出现其它站点。
    /// </summary>
    [Fact]
    public void Orm02_UserMoney_FilterBySiteId_ExcludesOtherSites()
    {
        var siteIds = _db.Queryable<UserMoney>()
            .Select(m => m.SiteID)
            .Distinct()
            .Take(5)
            .ToList();
        Assert.NotEmpty(siteIds);

        var siteA = siteIds[0];
        var rows = _db.Queryable<UserMoney>()
            .Where(m => m.SiteID == siteA)
            .Take(50)
            .ToList();

        Assert.NotEmpty(rows);
        Assert.All(rows, m => Assert.Equal(siteA, m.SiteID));

        if (siteIds.Count >= 2)
        {
            var siteB = siteIds[1];
            Assert.DoesNotContain(rows, m => m.SiteID == siteB);
        }
    }

    /// <summary>
    /// ORM-03：Take / 分页限制条数，顺序稳定可重复。
    /// </summary>
    [Fact]
    public void Orm03_SiteHost_TakeAndPage_LimitsCount()
    {
        const int pageSize = 5;
        var takeRows = _db.Queryable<SiteHost>()
            .OrderBy(h => h.ID)
            .Take(pageSize)
            .ToList();
        Assert.InRange(takeRows.Count, 1, pageSize);

        var pageNumber = 1;
        var total = 0;
        var pageRows = _db.Queryable<SiteHost>()
            .OrderBy(h => h.ID)
            .ToPageList(pageNumber, pageSize, ref total);

        Assert.InRange(pageRows.Count, 1, pageSize);
        Assert.True(total >= pageRows.Count);
        Assert.Equal(takeRows.Select(h => h.ID).ToArray(), pageRows.Select(h => h.ID).ToArray());
    }

    /// <summary>
    /// ORM-04：UserMoney 复合主键（SiteID + UserID + Currency）能查到唯一行。
    /// </summary>
    [Fact]
    public void Orm04_UserMoney_CompositeKey_FindsUniqueRow()
    {
        var sample = _db.Queryable<UserMoney>().Take(1).First();
        Assert.NotNull(sample);

        var found = _db.Queryable<UserMoney>()
            .Where(m => m.SiteID == sample.SiteID
                        && m.UserID == sample.UserID
                        && m.Currency == sample.Currency)
            .ToList();

        Assert.Single(found);
        Assert.Equal(sample.SiteID, found[0].SiteID);
        Assert.Equal(sample.UserID, found[0].UserID);
        Assert.Equal(sample.Currency, found[0].Currency);
        Assert.Equal(sample.TotalMoney, found[0].TotalMoney);
    }

    /// <summary>
    /// 释放 SqlSugar 客户端。
    /// </summary>
    public void Dispose()
    {
        _db.Dispose();
    }
}
