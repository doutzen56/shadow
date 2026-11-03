using SimpX.Net.Application.Shared;
using SimpX.Net.Data;
using SimpX.Net.Data.Entity.Sites;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 提现渠道增删改（内存 SQLite）。禁止写 BetWin30。
/// </summary>
public sealed class WithdrawChannelOperateSqliteTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;
    private readonly FixedSiteAccessor _site = new(1001, "wd.ch");
    private readonly WithdrawChannelOperateService _svc;
    private readonly WithdrawChannelService _query;

    /// <summary>建表。</summary>
    public WithdrawChannelOperateSqliteTests()
    {
        _db = SqliteBizSchema.CreateMemoryDb([], []);
        _db.Ado.ExecuteCommand(
            """
            CREATE TABLE IF NOT EXISTS [site_WithdrawChannel] (
              [ChannelID] INTEGER PRIMARY KEY AUTOINCREMENT,
              [SiteID] INTEGER NOT NULL,
              [SysID] INTEGER NOT NULL DEFAULT 0,
              [Name] TEXT,
              [Setting] TEXT,
              [QuerySetting] TEXT,
              [IsOpen] INTEGER NOT NULL DEFAULT 1,
              [Sort] INTEGER NOT NULL DEFAULT 0,
              [CreateAt] TEXT,
              [IsDeleted] INTEGER NOT NULL DEFAULT 0,
              [Cost] REAL NOT NULL DEFAULT 0,
              [MinCost] REAL NOT NULL DEFAULT 0,
              [MaxCost] REAL NOT NULL DEFAULT 0,
              [Currency] INTEGER NOT NULL DEFAULT 0
            );
            """);
        var biz = new TestBizDb(_db);
        _svc = new WithdrawChannelOperateService(biz, _site);
        _query = new WithdrawChannelService(biz, _site);
    }

    /// <inheritdoc />
    public void Dispose() => _db?.Dispose();

    /// <summary>新增→详情→改名→软删后列表空。</summary>
    [Fact]
    public async Task Save_Info_Delete_Soft()
    {
        await _svc.SaveAsync(new WithdrawChannelSaveInput
        {
            ID = 0,
            Name = "通道甲",
            SysID = 0,
            IsOpen = true,
            Cost = 0.01m,
            Currency = 1,
            Setting = "{}",
            QuerySetting = ""
        });

        var list1 = await _query.ListAsync();
        Assert.Single(list1);
        Assert.Equal(1, list1[0].Sort);
        var id = list1[0].ID;

        var info = await _svc.InfoAsync(id);
        Assert.Equal("通道甲", info.Name);
        Assert.Equal(0.01m, info.Cost);

        await _svc.SaveAsync(new WithdrawChannelSaveInput
        {
            ID = id,
            Name = "通道乙",
            SysID = 2,
            IsOpen = false,
            Cost = 0.02m,
            MinCost = 1,
            MaxCost = 9,
            Currency = 2,
            Setting = "{\"k\":1}",
            QuerySetting = "{\"q\":1}"
        });

        var info2 = await _svc.InfoAsync(id);
        Assert.Equal("通道乙", info2.Name);
        Assert.Equal(2, info2.SysID);
        Assert.False(info2.IsOpen);
        Assert.Equal(0.02m, info2.Cost);
        Assert.Equal(1m, info2.MinCost);
        Assert.Equal(9m, info2.MaxCost);
        Assert.Equal(2, info2.Currency);
        Assert.Contains("k", info2.Setting);

        await _svc.DeleteAsync(id);
        Assert.Empty(await _query.ListAsync());

        var soft = (await _db.Queryable<WithdrawChannel>()
            .Where(c => c.ID == id).Take(1).ToListAsync()).First();
        Assert.True(soft.IsDeleted);
    }

    /// <summary>空名称拒绝。</summary>
    [Fact]
    public async Task Save_EmptyName_Throws()
    {
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _svc.SaveAsync(new WithdrawChannelSaveInput { Name = "  " }));
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
