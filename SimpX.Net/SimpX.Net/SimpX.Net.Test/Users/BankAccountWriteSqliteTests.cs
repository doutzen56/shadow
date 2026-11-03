using Microsoft.AspNetCore.Http;
using SimpX.Net.Application.Shared;
using SimpX.Net.Data;
using SimpX.Net.Data.Entity.Users;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 银行信息写路径（内存 SQLite）。禁止写 BetWin30。
/// </summary>
public sealed class BankAccountWriteSqliteTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;
    private readonly BankAccountService _svc;
    private readonly int _userId;

    /// <summary>
    /// 建表并植入会员。
    /// </summary>
    public BankAccountWriteSqliteTests()
    {
        _db = SqliteBizSchema.CreateMemoryDb(["Users", "usr_Flag"], []);
        _db.Ado.ExecuteCommand(
            """
            CREATE TABLE IF NOT EXISTS [usr_BankAccount] (
              [AccountID] INTEGER PRIMARY KEY AUTOINCREMENT,
              [SiteID] INTEGER NOT NULL,
              [UserID] INTEGER NOT NULL,
              [Type] INTEGER NOT NULL DEFAULT 0,
              [BankName] TEXT,
              [Name] TEXT,
              [NameLetters] TEXT,
              [Account] TEXT,
              [AccountDetail] TEXT,
              [CreateAt] TEXT,
              [UpdateAt] TEXT,
              [IsVir] TEXT,
              [BankInfo] TEXT,
              [Status] INTEGER NOT NULL DEFAULT 0,
              [Remark] TEXT,
              [CheckDateTime] TEXT,
              [Checker] INTEGER,
              [Country] TEXT
            );
            CREATE TABLE IF NOT EXISTS [usr_WithdrawOrder] (
              [OrderID] INTEGER PRIMARY KEY AUTOINCREMENT,
              [SiteID] INTEGER NOT NULL,
              [UserID] INTEGER NOT NULL,
              [AccountNumber] TEXT,
              [Status] INTEGER NOT NULL DEFAULT 0
            );
            CREATE TABLE IF NOT EXISTS [sys_BankAccountBlackList] (
              [BlackIP] INTEGER PRIMARY KEY AUTOINCREMENT,
              [AccountName] TEXT,
              [AccountNumber] TEXT,
              [Remark] TEXT,
              [CreateAt] TEXT
            );
            """);

        _db.Insertable(new User
        {
            SiteID = 1001,
            UserName = "u_write",
            NickName = "w",
            AccountName = "",
            Password = "x",
            PayPassword = "",
            CreateAt = DateTime.UtcNow,
            Face = "",
            RegIP = "",
            Lock = 0,
            Function = 0,
            LoginAt = DateTime.UtcNow,
            LoginIP = "",
            AgentID = 0,
            Depth = 0,
            GroupID = 0,
            Type = 0,
            Email = "",
            IsEmail = false,
            Mobile = "",
            IsMobile = false,
            Question = 0,
            Answer = "",
            SecretKey = Guid.Empty,
            Currency = 1,
            Money = 0,
            LockMoney = 0,
            GameMoney = 0,
            IsOnline = false,
            Flag = 0,
            Level = 0,
            ActiveAt = DateTime.UtcNow,
            Exper = 0,
            RegFlag = 0,
            RegDomain = "",
            LoginPlatform = 0,
            RegPlatform = 0,
            Unbalanced = 0,
            InviteID = 0,
            UpdateAt = DateTime.UtcNow,
            ProxyRate = 0,
            WashingRatio = "",
            LastReturn = DateTime.UtcNow,
            Coin = 0,
            DisableCurrency = "",
            Address = "",
            Birthday = DateTime.UtcNow,
            Telegram = "",
            Line = "",
            Google = "",
            Info = "",
            DeviceId = "",
            OldGroupID = 0
        }).ExecuteCommand();
        _userId = _db.Queryable<User>().Where(u => u.UserName == "u_write").Select(u => u.ID).First();

        _svc = new BankAccountService(
            new TestBizDb(_db),
            new FixedSiteAccessor(1001, "bank-w.test"),
            new UserManager(new HttpContextAccessor()));
    }

    /// <summary>新增 → 审核成功 → 删。</summary>
    [Fact]
    public async Task Create_update_delete_round_trip()
    {
        var verified = await _svc.VerifyUserAsync("u_write");
        Assert.Equal(_userId, verified.Id);

        await _svc.CreateAsync(new BankAccountCreateInput
        {
            ID = _userId,
            UserName = "u_write",
            IsVir = "0",
            Type = 10,
            Name = "李四",
            Account = "62229999",
            AccountDetail = "支行",
            BankAddress = "地址",
            BranchCode = "001"
        });

        var page = await _svc.PageAsync(new BankAccountAdminPageInput { UserName = "u_write" });
        Assert.Equal(1, page.Total);
        var id = page.Items[0].ID;
        Assert.Equal(BankAccountService.StatusNone, page.Items[0].Status);

        await _svc.UpdateStatusAsync(new BankAccountUpdateStatusInput
        {
            ID = id,
            Status = BankAccountService.StatusSuccess,
            Remark = "ok"
        });
        var detail = await _svc.DetailAsync(id);
        Assert.Equal(BankAccountService.StatusSuccess, detail.Status);
        Assert.Equal("ok", detail.Remark);

        var flag = await _db.Queryable<UserFlag>().FirstAsync(f => f.UserID == _userId);
        Assert.True(flag.IsCard);

        await _svc.DeleteAsync(id);
        Assert.Equal(0, await _db.Queryable<BankAccount>().CountAsync());
    }

    /// <summary>有未完结提现单时不能删。</summary>
    [Fact]
    public async Task Delete_blocked_when_pending_withdraw()
    {
        await _svc.CreateAsync(new BankAccountCreateInput
        {
            ID = _userId,
            UserName = "u_write",
            IsVir = "1",
            VCurrency = 250,
            VAgreement = "TRC20",
            VAddress = "Txxx"
        });
        var id = (await _svc.PageAsync(new BankAccountAdminPageInput { UserName = "u_write" })).Items[0].ID;
        _db.Ado.ExecuteCommand(
            "INSERT INTO [usr_WithdrawOrder]([SiteID],[UserID],[AccountNumber],[Status]) VALUES(1001,@uid,'Txxx',0)",
            new { uid = _userId });

        await Assert.ThrowsAnyAsync<Exception>(() => _svc.DeleteAsync(id));
    }

    /// <inheritdoc />
    public void Dispose() => _db?.Dispose();

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
        public int RequireSiteId() => SiteId ?? throw new InvalidOperationException("未定站");
    }
}
