using SimpX.Net.Application.Shared;
using SimpX.Net.Data;
using SimpX.Net.Data.Entity.Users;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 银行信息只读分页（内存 SQLite）。
/// </summary>
public sealed class BankAccountReadOnlySqliteTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;
    private readonly BankAccountService _svc;

    /// <summary>
    /// 建 Users / usr_BankAccount 壳。
    /// </summary>
    public BankAccountReadOnlySqliteTests()
    {
        _db = SqliteBizSchema.CreateMemoryDb(["Users"], []);
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
            """);
        _db.Insertable(new User
        {
            SiteID = 1001,
            UserName = "u_bank",
            NickName = "u",
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
        var uid = _db.Queryable<User>().Where(u => u.UserName == "u_bank").Select(u => u.ID).First();
        _db.Insertable(new BankAccount
        {
            SiteID = 1001,
            UserID = uid,
            Type = 1,
            BankName = "测试银行",
            Name = "张三",
            NameLetters = "",
            Account = "62220001",
            AccountDetail = "分行",
            CreateAt = DateTime.UtcNow,
            UpdateAt = DateTime.UtcNow,
            IsVir = "0",
            BankInfo = "",
            Status = 0,
            Remark = "",
            Country = ""
        }).ExecuteCommand();

        _svc = new BankAccountService(
            new TestBizDb(_db),
            new FixedSiteAccessor(1001, "bank.test"),
            new UserManager(new Microsoft.AspNetCore.Http.HttpContextAccessor()));
    }

    /// <summary>列表能按用户名筛出。</summary>
    [Fact]
    public async Task Page_filters_by_user_name()
    {
        var page = await _svc.PageAsync(new BankAccountAdminPageInput
        {
            Page = 1,
            PageSize = 10,
            UserName = "u_bank"
        });
        Assert.Equal(1, page.Total);
        Assert.Equal("张三", page.Items[0].Name);
        Assert.StartsWith("'", page.Items[0].Account);

        var detail = await _svc.DetailAsync(page.Items[0].ID);
        Assert.Equal("62220001", detail.Account);
    }

    /// <inheritdoc />
    public void Dispose() => _db?.Dispose();

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
