using SimpX.Net.Application.Shared;
using SimpX.Net.Data.Entity.Systems;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 系统更新通知只读（内存 SQLite）。
/// </summary>
public sealed class SystemUpdateNewsSqliteTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;
    private readonly SystemUpdateNewsService _svc;

    /// <summary>
    /// 建表并植入两条不同语种。
    /// </summary>
    public SystemUpdateNewsSqliteTests()
    {
        _db = SqliteBizSchema.CreateMemoryDb([], []);
        _db.Ado.ExecuteCommand(
            """
            CREATE TABLE IF NOT EXISTS [sys_UpdateNews] (
              [NewsID] INTEGER PRIMARY KEY AUTOINCREMENT,
              [Title] TEXT,
              [Language] INTEGER NOT NULL DEFAULT 0,
              [Content] TEXT,
              [CreateAt] TEXT,
              [Ident] TEXT
            );
            """);
        _db.Insertable(new SystemUpdateNews
        {
            Title = "中文通知",
            Language = 0,
            Content = "内容A",
            CreateAt = DateTime.Now.AddDays(-1),
            Ident = "a"
        }).ExecuteCommand();
        _db.Insertable(new SystemUpdateNews
        {
            Title = "EN notice",
            Language = 2,
            Content = "body",
            CreateAt = DateTime.Now,
            Ident = "b"
        }).ExecuteCommand();
        _svc = new SystemUpdateNewsService(new TestBizDb(_db));
    }

    /// <inheritdoc />
    public void Dispose() => _db?.Dispose();

    /// <summary>
    /// 按语种过滤；不传则全部。
    /// </summary>
    [Fact]
    public async Task List_filters_by_language()
    {
        var all = await _svc.ListAsync();
        Assert.Equal(2, all.Count);

        var zh = await _svc.ListAsync(0);
        Assert.Single(zh);
        Assert.Equal("中文通知", zh[0].Title);

        var en = await _svc.ListAsync(2);
        Assert.Single(en);
        Assert.Equal("EN notice", en[0].Title);
    }
}
