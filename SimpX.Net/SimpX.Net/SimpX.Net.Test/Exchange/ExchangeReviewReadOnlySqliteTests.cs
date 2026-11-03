using SimpX.Net.Application.Shared;
using SimpX.Net.Data;
using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Users;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 兑币审核列表/总账/审核（内存 SQLite + 假账本）。禁止写 BetWin30。
/// </summary>
public class ExchangeReviewReadOnlySqliteTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;
    private readonly FixedSiteAccessor _site = new(1001, "crud.test");
    private readonly RecordingMoneyLedger _money = new();

    /// <summary>
    /// 建表并植入待审、已审与币种。
    /// </summary>
    public ExchangeReviewReadOnlySqliteTests()
    {
        _db = SqliteBizSchema.CreateMemoryDb(
            ["usr_CurrencyExchange", "site_Admin", "site_Currency"],
            []);
        _db.Insertable(new SiteCurrency
        {
            SiteID = 1001,
            Currency = 2,
            IsOpen = true,
            Sort = 1,
            Accuracy = 2,
            SysOpen = true,
            IsRegister = true
        }).ExecuteCommand();
        _db.Insertable(new SiteAdmin
        {
            SiteID = 1001,
            AdminName = "auditor",
            NickName = "审",
            Face = "",
            Password = "x",
            CreateAt = DateTime.UtcNow,
            Status = 1,
            GroupID = 0,
            AuditUpperLimit = 0,
            LoginAt = DateTime.UtcNow,
            LoginIP = "",
            SecretKey = Guid.Empty,
            Sign = "",
            Function = 0,
            WarningSoundString = ""
        }).ExecuteCommand();
        var adminId = _db.Queryable<SiteAdmin>().Select(a => a.ID).First();

        var day = DateTime.Now.Date.AddHours(10);
        _db.Insertable(new CurrencyExchangeOrder
        {
            SiteID = 1001,
            OrderID = "ex20260101000000001",
            UserID = 1,
            UserName = "u1",
            BeforeCurrency = 1,
            BeforeMoney = 100,
            AfterCurrency = 2,
            AfterMoney = 700,
            Money = 100,
            Point = 7,
            ExchangeMoney = 0,
            Status = 0,
            CreateAt = day,
            Remark = ""
        }).ExecuteCommand();
        _db.Insertable(new CurrencyExchangeOrder
        {
            SiteID = 1001,
            OrderID = "exdone00000000001",
            UserID = 2,
            UserName = "u2",
            BeforeCurrency = 1,
            BeforeMoney = 50,
            AfterCurrency = 2,
            AfterMoney = 350,
            Money = 50,
            Point = 7,
            ExchangeMoney = 0,
            Status = 2,
            CreateAt = day.AddHours(-1),
            CheckTime = day.AddMinutes(-30),
            Checker = adminId,
            Remark = "ok"
        }).ExecuteCommand();
    }

    /// <inheritdoc />
    public void Dispose() => _db?.Dispose();

    /// <summary>
    /// 待审/已审拆分。
    /// </summary>
    [Fact]
    public async Task PendingAndLog_SplitByStatus()
    {
        var svc = CreateSvc();
        var pending = await svc.ListPendingAsync(new ExchangeReviewListInput());
        Assert.Single(pending);
        Assert.Equal("ex20260101000000001", pending[0].OrderID);

        var log = await svc.ListLogAsync(new ExchangeReviewLogInput());
        Assert.Single(log);
        Assert.Equal("auditor", log[0].Checker);
    }

    /// <summary>
    /// F4：成功单计入总账。
    /// </summary>
    [Fact]
    public async Task Summary_CountsSuccess()
    {
        var svc = CreateSvc();
        var summary = await svc.SummaryAsync(new ExchangeSummaryInput
        {
            StartAt = DateTime.Now.Date.AddDays(-1),
            EndAt = DateTime.Now.Date
        });
        Assert.NotEmpty(summary.Items);
        Assert.True(summary.Total.ContainsKey("TOTAL"));
        Assert.True(summary.Total["TOTAL"] >= 350m);
    }

    /// <summary>
    /// F3 check 拒绝：只解锁不入账。
    /// </summary>
    [Fact]
    public async Task Check_Reject_UnlockOnly()
    {
        var svc = CreateSvc();
        var id = (await svc.ListPendingAsync(new ExchangeReviewListInput()))[0].ID;
        await svc.CheckAsync(new ExchangeReviewCheckInput { ID = id, Status = 1, Remark = "no" }, checkerId: 9);
        Assert.Single(_money.Unlocks);
        Assert.Empty(_money.Logs);
        Assert.Empty(await svc.ListPendingAsync(new ExchangeReviewListInput()));
    }

    /// <summary>
    /// F3 check 通过：解锁 + 兑出/兑入两笔。
    /// </summary>
    [Fact]
    public async Task Check_Approve_TwoMoneyLogs()
    {
        // 再插一条待审
        _db.Insertable(new CurrencyExchangeOrder
        {
            SiteID = 1001,
            OrderID = "ex20260101000000099",
            UserID = 3,
            UserName = "u3",
            BeforeCurrency = 1,
            BeforeMoney = 10,
            AfterCurrency = 2,
            AfterMoney = 70,
            Money = 10,
            Point = 7,
            ExchangeMoney = 0,
            Status = 0,
            CreateAt = DateTime.Now,
            Remark = ""
        }).ExecuteCommand();
        _money.Unlocks.Clear();
        _money.Logs.Clear();

        var svc = CreateSvc();
        var id = (await _db.Queryable<CurrencyExchangeOrder>()
            .Where(o => o.OrderID == "ex20260101000000099")
            .Select(o => o.ID)
            .FirstAsync());
        await svc.CheckAsync(new ExchangeReviewCheckInput { ID = id, Status = 2, Remark = "ok" }, checkerId: 1);
        Assert.Single(_money.Unlocks);
        Assert.Equal(2, _money.Logs.Count);
        Assert.Equal(MoneyLedgerService.MoneyTypeExchangeOut, _money.Logs[0].Type);
        Assert.Equal(MoneyLedgerService.MoneyTypeExchangeIn, _money.Logs[1].Type);
    }

    /// <summary>
    /// 未定站失败。
    /// </summary>
    [Fact]
    public async Task NoSite_Fails()
    {
        var svc = new ExchangeReviewService(new TestBizDb(_db), new FixedSiteAccessor(null, null), _money);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.ListPendingAsync(new ExchangeReviewListInput()));
    }

    /// <summary>
    /// 构造服务。
    /// </summary>
    private ExchangeReviewService CreateSvc() => new(new TestBizDb(_db), _site, _money);

    /// <summary>
    /// 记录调用的假账本。
    /// </summary>

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
