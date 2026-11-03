using SimpX.Net.Application.Shared;
using SimpX.Net.Data;
using SimpX.Net.Data.Entity.Reports;
using SimpX.Net.Data.Entity.Risk;
using Xunit;
using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Test;

/// <summary>
/// 风控列表：游戏盈利 / 风险会员（内存 SQLite）。禁写 BetWin30。
/// </summary>
public sealed class RiskControlSqliteTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;
    private readonly FixedSiteAccessor _site = new(1001, "crud.test");

    /// <summary>
    /// 建表并植入样本。
    /// </summary>
    public RiskControlSqliteTests()
    {
        _db = SqliteBizSchema.CreateMemoryDb(
            ["rpt_UserDateGame", "risk_UserDanger", "Users"],
            []);
        _db.Insertable(new User
        {
            SiteID = 1001,
            UserName = "u1",
            NickName = "n1",
            Password = "x",
            CreateAt = DateTime.UtcNow,
            Lock = 0,
            Level = 1,
            AgentID = 0
        }).ExecuteReturnIdentity();
        var uid = _db.Queryable<User>().Where(t => t.SiteID == 1001).Select(t => t.ID).First();
        _db.Insertable(new UserDateGame
        {
            SiteID = 1001,
            UserID = uid,
            Date = DateTime.Today,
            Type = 1,
            Category = 0,
            BetMoney = 100,
            Money = -20
        }).ExecuteCommand();
        _db.Insertable(new RiskUserDanger
        {
            SiteID = 1001,
            UserID = uid,
            Date = DateTime.Today,
            CreateAt = DateTime.Now,
            Context = "test",
            Type = "1",
            Detail = "{}"
        }).ExecuteCommand();
    }

    /// <summary>
    /// 游戏盈利汇总。
    /// </summary>
    [Fact]
    public async Task GameProfit_sums_money()
    {
        var svc = new RiskControlService(new TestBizDb(_db), _site);
        var outp = await svc.GameProfitAsync(new RiskControlQueryInput
        {
            StartAt = DateTime.Today.AddDays(-1),
            EndAt = DateTime.Today
        });
        Assert.Single(outp.Rows);
        Assert.Equal(-20, outp.TotalMoney);
        Assert.Equal("u1", outp.Rows[0].UserName);
    }

    /// <summary>
    /// 风险会员列表。
    /// </summary>
    [Fact]
    public async Task DangerUsers_lists_row()
    {
        var svc = new RiskControlService(new TestBizDb(_db), _site);
        var rows = await svc.DangerUsersAsync(new RiskControlQueryInput
        {
            StartAt = DateTime.Today,
            EndAt = DateTime.Today
        });
        Assert.Single(rows);
        Assert.Equal("u1", rows[0].UserName);
        Assert.Equal("test", rows[0].Context);
    }

    /// <inheritdoc />
    public void Dispose() => _db.Dispose();

    /// <summary>固定定站。</summary>
    private sealed class FixedSiteAccessor : ICurrentSiteAccessor
    {
        public FixedSiteAccessor(int? siteId, string host)
        {
            SiteId = siteId;
            Host = host;
        }

        public int? SiteId { get; }
        public string Host { get; }
        public int RequireSiteId() => SiteId ?? throw new InvalidOperationException("未定站");
    }
}
