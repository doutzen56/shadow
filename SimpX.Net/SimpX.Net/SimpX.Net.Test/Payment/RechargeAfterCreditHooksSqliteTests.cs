using SimpX.Net.Application.Shared;
using SimpX.Net.Data;
using SimpX.Net.Data.Entity.Users;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 到账后站内信与 RECHARGE_LOG 入队（内存 SQLite）。
/// </summary>
public sealed class RechargeAfterCreditHooksSqliteTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;
    private readonly RecordingMoneyLedger _money = new();
    private readonly MemoryLedgerSideEffectQueues _queues = new();
    private readonly int _uid;
    private readonly RechargeOrderCheckService _svc;

    /// <summary>
    /// 建表并植入未到账订单。
    /// </summary>
    public RechargeAfterCreditHooksSqliteTests()
    {
        _db = SqliteBizSchema.CreateMemoryDb(["Users", "Site"], []);
        _db.Ado.ExecuteCommand(
            """
            CREATE TABLE IF NOT EXISTS [usr_RechargeOrder] (
              [OrderID] TEXT NOT NULL PRIMARY KEY,
              [SiteID] INTEGER NOT NULL,
              [UserID] INTEGER NOT NULL,
              [Money] REAL NOT NULL,
              [Fee] REAL NOT NULL DEFAULT 0,
              [Cost] REAL NOT NULL DEFAULT 0,
              [CreateAt] TEXT NOT NULL,
              [PayID] INTEGER NOT NULL DEFAULT 0,
              [IsPay] INTEGER NOT NULL DEFAULT 0,
              [PayAt] TEXT,
              [Paid] REAL NOT NULL DEFAULT 0,
              [SystemID] TEXT,
              [AdminID] INTEGER NOT NULL DEFAULT 0,
              [Count] INTEGER NOT NULL DEFAULT 0,
              [Remark] TEXT,
              [IP] TEXT,
              [Currency] INTEGER NOT NULL DEFAULT 0,
              [FromCurrency] INTEGER NOT NULL DEFAULT 0,
              [IsCheckFail] INTEGER NOT NULL DEFAULT 0,
              [ActiveID] TEXT,
              [IsDeleted] INTEGER NOT NULL DEFAULT 0,
              [IsProxyRecharge] INTEGER NOT NULL DEFAULT 0,
              [PayAccount] TEXT
            );
            CREATE TABLE IF NOT EXISTS [usr_Message] (
              [MsgID] INTEGER PRIMARY KEY AUTOINCREMENT,
              [SiteID] INTEGER NOT NULL,
              [UserID] INTEGER NOT NULL,
              [SendID] INTEGER NOT NULL DEFAULT 0,
              [Type] INTEGER NOT NULL DEFAULT 0,
              [CreateAt] TEXT NOT NULL,
              [ReadAt] TEXT,
              [Title] TEXT,
              [Content] TEXT,
              [IsRead] INTEGER NOT NULL DEFAULT 0,
              [SupportCurrency] TEXT
            );
            """);

        _uid = _db.Insertable(BuildUser("u_hook")).ExecuteReturnIdentity();
        _db.Insertable(new RechargeOrder
        {
            ID = "RO-HOOK-1",
            SiteID = 1001,
            UserID = _uid,
            Money = 100m,
            Fee = 0,
            Cost = 0,
            CreateAt = DateTime.UtcNow,
            PayID = 1,
            IsPay = false,
            PayAt = DateTime.UtcNow,
            Paid = 0,
            SystemID = "",
            AdminID = 0,
            Count = 0,
            Remark = "",
            IP = "1.1.1.1",
            Currency = 1,
            FromCurrency = 1,
            IsCheckFail = false,
            ActiveID = "0",
            IsDeleted = false,
            IsProxyRecharge = false,
            PayAccount = ""
        }).ExecuteCommand();

        var messages = new UserMessageNotifyService(new TestBizDb(_db));
        _svc = new RechargeOrderCheckService(
            new TestBizDb(_db),
            new FixedSiteAccessor(1001, "hook.test"),
            _money,
            _queues,
            messages,
            NoOpRechargeTelegramNotifier.Instance,
            NoOpRechargePromoGrantService.Instance);
    }

    /// <summary>
    /// 到账后写站内信并推进 RECHARGE_LOG。
    /// </summary>
    [Fact]
    public async Task Check_writes_notify_and_enqueues_recharge_log()
    {
        await _svc.CheckAsync(new RechargeOrderCheckInput
        {
            ID = "RO-HOOK-1",
            SystemID = "GW-HOOK",
            Paid = 100m
        }, adminId: 3);

        Assert.True(_queues.TryDequeueRechargeLog(out var orderId));
        Assert.Equal("RO-HOOK-1", orderId);

        var msgs = await _db.Queryable<UserMessage>().Where(m => m.UserID == _uid).ToListAsync();
        Assert.Single(msgs);
        Assert.Equal(UserMessageNotifyService.MessageTypeNotify, msgs[0].Type);
        Assert.Contains("#到帐#", msgs[0].Content);
        Assert.False(msgs[0].IsRead);
    }

    /// <inheritdoc />
    public void Dispose() => _db?.Dispose();

    private static User BuildUser(string name) => new()
    {
        SiteID = 1001,
        UserName = name,
        NickName = name,
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
        GroupID = 1,
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
        RegisteredFrom = "",
        PixelID = "",
        InitLevel = 0,
        Is_Temporary_Password = false,
        BlockGame = "",
        OldGroupID = 0
    };


    private sealed class FixedSiteAccessor : ICurrentSiteAccessor
    {
        public FixedSiteAccessor(int? siteId, string host)
        {
            SiteId = siteId;
            Host = host;
        }

        public int? SiteId { get; }
        public string Host { get; }
        public int RequireSiteId() => SiteId ?? throw new InvalidOperationException("no site");
    }
}
