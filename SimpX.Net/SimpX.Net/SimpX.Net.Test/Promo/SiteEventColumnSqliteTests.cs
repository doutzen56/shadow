using SimpX.Net.Application.Shared;
using SimpX.Net.Data;
using SimpX.Net.Data.Entity.Sites;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 活动栏目增删（内存 SQLite）。禁止写 BetWin30。
/// </summary>
public sealed class SiteEventColumnSqliteTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;
    private readonly FixedSiteAccessor _site = new(1001, "col.test");
    private readonly SiteEventColumnService _svc;

    /// <summary>建栏目与活动表。</summary>
    public SiteEventColumnSqliteTests()
    {
        _db = SqliteBizSchema.CreateMemoryDb(["site_EventColumn", "site_Event"], []);
        _svc = new SiteEventColumnService(new TestBizDb(_db), _site);
    }

    /// <inheritdoc />
    public void Dispose() => _db?.Dispose();

    /// <summary>新增→列表→改名排序→空栏可删。</summary>
    [Fact]
    public async Task Save_List_Delete_Empty()
    {
        await _svc.SaveAsync(new SiteEventColumnSaveInput
        {
            ID = 0,
            Name = "栏目A",
            Sort = 5
        });

        var list1 = await _svc.ColumnListAsync();
        Assert.Single(list1);
        Assert.Equal("栏目A", list1[0].Name);
        Assert.Equal((short)5, list1[0].Sort);
        var id = list1[0].ID;

        await _svc.SaveAsync(new SiteEventColumnSaveInput
        {
            ID = id,
            Name = "栏目B",
            Sort = 9
        });

        var list2 = await _svc.ColumnListAsync();
        Assert.Equal("栏目B", list2[0].Name);
        Assert.Equal((short)9, list2[0].Sort);

        await _svc.DeleteAsync(id);
        Assert.Empty(await _svc.ColumnListAsync());
    }

    /// <summary>EventCount&gt;0 时拒绝删除。</summary>
    [Fact]
    public async Task Delete_WhenEventCount_Throws()
    {
        await _svc.SaveAsync(new SiteEventColumnSaveInput { Name = "有计数", Sort = 1 });
        var id = (await _svc.ColumnListAsync())[0].ID;
        var row = (await _db.Queryable<SiteEventColumn>().Where(c => c.ID == id).Take(1).ToListAsync()).First();
        row.EventCount = 2;
        await _db.Updateable(row).UpdateColumns(c => new { c.EventCount }).ExecuteCommandAsync();

        await Assert.ThrowsAnyAsync<Exception>(() => _svc.DeleteAsync(id));
        Assert.Single(await _svc.ColumnListAsync());
    }

    /// <summary>仍有活动引用 ColID 时拒绝删除。</summary>
    [Fact]
    public async Task Delete_WhenSiteEventRefs_Throws()
    {
        await _svc.SaveAsync(new SiteEventColumnSaveInput { Name = "有引用", Sort = 1 });
        var id = (await _svc.ColumnListAsync())[0].ID;

        _db.Insertable(new SiteEvent
        {
            SiteID = 1001,
            ColID = id,
            Title = "{}",
            SubTitle = "{}",
            Tag = "{}",
            Cover = "{}",
            CreateAt = DateTime.Now,
            StartAt = DateTime.Now.Date,
            ExpireAt = DateTime.Now.AddDays(1).Date,
            Description = "{}",
            Content = "{}",
            Member = 0,
            SettingString = "",
            Type = 0,
            IsVisible = true,
            IsOpen = true,
            IsLoginAfterOpen = false,
            IsLoginBeforeOpen = false,
            IsQuickEntrance = false,
            JumpType = 0,
            Status = 0,
            Views = "",
            Skin = "",
            Sort = 0,
            Currency = 0,
            FilterGame = "",
            SupportCurrency = "",
            FilterPlatform = "",
            IsNewerTask = false,
            Demo = "",
            IsDetail = false
        }).ExecuteCommand();

        await Assert.ThrowsAnyAsync<Exception>(() => _svc.DeleteAsync(id));
        Assert.Single(await _svc.ColumnListAsync());
    }

    /// <summary>空名称拒绝。</summary>
    [Fact]
    public async Task Save_EmptyName_Throws()
    {
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _svc.SaveAsync(new SiteEventColumnSaveInput { Name = "" }));
    }

    /// <summary>固定定站。</summary>
    private sealed class FixedSiteAccessor : ICurrentSiteAccessor
    {
        /// <summary>指定站。</summary>
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
