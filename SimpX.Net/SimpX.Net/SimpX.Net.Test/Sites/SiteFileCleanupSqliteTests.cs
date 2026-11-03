using Microsoft.Extensions.Logging.Abstractions;
using SimpX.Net.Application.Shared;
using SimpX.Net.Data.Entity.Sites;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>site_FileDownload 两天前清理（内存 SQLite）。</summary>
public sealed class SiteFileCleanupSqliteTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;
    private readonly SiteFileCleanupService _svc;

    public SiteFileCleanupSqliteTests()
    {
        _db = SqliteBizSchema.CreateMemoryDb([], []);
        _db.Ado.ExecuteCommand(
            """
            CREATE TABLE IF NOT EXISTS [site_FileDownload] (
              [FileID] INTEGER PRIMARY KEY AUTOINCREMENT,
              [SiteID] INTEGER NOT NULL,
              [FileName] TEXT,
              [FilePath] TEXT,
              [FileSize] INTEGER NOT NULL DEFAULT 0,
              [Frequency] INTEGER NOT NULL DEFAULT 0,
              [CreateAt] TEXT NOT NULL
            );
            """);
        _svc = new SiteFileCleanupService(new TestBizDb(_db), NullLogger<SiteFileCleanupService>.Instance);
    }

    public void Dispose() => _db?.Dispose();

    /// <summary>只删 CreateAt 早于截止日的行，保留较新的。</summary>
    [Fact]
    public async Task DeleteOlderThan_Keeps_Recent()
    {
        _db.Insertable(new SiteFileDownload
        {
            SiteID = 1001,
            FileName = "old.csv",
            FilePath = "/tmp/old.csv",
            CreateAt = DateTime.Now.AddDays(-5)
        }).ExecuteCommand();
        _db.Insertable(new SiteFileDownload
        {
            SiteID = 1001,
            FileName = "new.csv",
            FilePath = "/tmp/new.csv",
            CreateAt = DateTime.Now.AddHours(-1)
        }).ExecuteCommand();

        var deleted = await _svc.DeleteOlderThanAsync(DateTime.Now.AddDays(-2));
        Assert.Equal(1, deleted);
        var left = await _db.Queryable<SiteFileDownload>().ToListAsync();
        Assert.Single(left);
        Assert.Equal("new.csv", left[0].FileName);
    }
}
