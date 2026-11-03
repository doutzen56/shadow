using SimpX.Net.Application.Shared;
using SimpX.Net.Data;
using SimpX.Net.Data.Entity.Agents;
using SimpX.Net.Data.Entity.Users;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 代理审核（内存 SQLite）。禁止写 BetWin30。
/// </summary>
public sealed class AgentApplySqliteTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;
    private readonly FixedSiteAccessor _site = new(1001, "apply.test");
    private readonly AgentApplyService _svc;
    private readonly int _userId;

    /// <summary>
    /// 建表并植入待审申请。
    /// </summary>
    public AgentApplySqliteTests()
    {
        _db = SqliteBizSchema.CreateMemoryDb(["Users"], []);
        _db.Ado.ExecuteCommand(
            """
            CREATE TABLE IF NOT EXISTS [usr_AgentApply] (
              [ApplyID] INTEGER PRIMARY KEY AUTOINCREMENT,
              [SiteID] INTEGER NOT NULL,
              [UserID] INTEGER NOT NULL,
              [Skype] TEXT,
              [QQ] TEXT,
              [Area] TEXT,
              [Phone] TEXT,
              [Telegram] TEXT,
              [Line] TEXT,
              [WhatsApp] TEXT,
              [Content] TEXT,
              [CreateAt] TEXT NOT NULL,
              [Status] INTEGER NOT NULL DEFAULT 0
            );
            CREATE TABLE IF NOT EXISTS [agent_User] (
              [AgentID] INTEGER PRIMARY KEY,
              [SiteID] INTEGER NOT NULL,
              [ParentID] INTEGER NOT NULL DEFAULT 0,
              [CreateAt] TEXT,
              [Status] INTEGER NOT NULL DEFAULT 0,
              [CommissionID] INTEGER NOT NULL DEFAULT 0,
              [ReturnID] INTEGER NOT NULL DEFAULT 0,
              [Permission] INTEGER NOT NULL DEFAULT 0
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

        _userId = _db.Insertable(new User
        {
            SiteID = 1001,
            UserName = "apply-member",
            NickName = "a",
            AccountName = "a",
            Password = "x",
            CreateAt = DateTime.Now,
            ActiveAt = DateTime.Now,
            LoginAt = DateTime.Now,
            Type = AgentApplyService.UserTypeMember,
            AgentID = 0
        }).ExecuteReturnIdentity();

        _db.Insertable(new AgentApply
        {
            SiteID = 1001,
            UserID = _userId,
            Skype = "",
            QQ = "10001",
            Area = "",
            Phone = "",
            Telegram = "",
            Line = "",
            WhatsApp = "",
            Content = "推广简介",
            CreateAt = DateTime.Now,
            Status = AgentApplyService.StatusNone
        }).ExecuteCommand();

        var messages = new UserMessageNotifyService(new TestBizDb(_db));
        _svc = new AgentApplyService(new TestBizDb(_db), _site, messages);
    }

    /// <inheritdoc />
    public void Dispose() => _db?.Dispose();

    /// <summary>
    /// 待审列表可见。
    /// </summary>
    [Fact]
    public async Task Page_lists_pending()
    {
        var page = await _svc.PageAsync(new AgentApplyPageInput
        {
            Status = AgentApplyService.StatusNone,
            Page = 1,
            PageSize = 20
        });
        Assert.Equal(1, page.RecordCount);
        Assert.Equal("apply-member", page.Data[0].UserName);
        Assert.Equal(AgentApplyService.StatusNone, page.Data[0].Status);
    }

    /// <summary>
    /// 通过：写 agent_User，User.Type=Agent。
    /// </summary>
    [Fact]
    public async Task Pass_creates_agent_user_and_sets_type()
    {
        var pending = await _svc.PageAsync(new AgentApplyPageInput { Status = AgentApplyService.StatusNone });
        var id = pending.Data[0].ID;

        await _svc.CheckAsync(new AgentApplyCheckInput
        {
            ID = id,
            Status = AgentApplyService.StatusPass
        });

        var user = await _db.Queryable<User>().InSingleAsync(_userId);
        Assert.Equal(AgentApplyService.UserTypeAgent, user.Type);

        var agent = await _db.Queryable<AgentUser>().InSingleAsync(_userId);
        Assert.Equal(AgentUser.AgentStatus.Normal, agent.Status);
        Assert.Equal(0, agent.ParentID);
        Assert.Equal(1001, agent.SiteID);

        var apply = await _db.Queryable<AgentApply>().InSingleAsync(id);
        Assert.Equal(AgentApplyService.StatusPass, apply.Status);
    }

    /// <summary>
    /// 拒绝后不可再审。
    /// </summary>
    [Fact]
    public async Task Reject_cannot_recheck()
    {
        var pending = await _svc.PageAsync(new AgentApplyPageInput { Status = AgentApplyService.StatusNone });
        var id = pending.Data[0].ID;

        await _svc.CheckAsync(new AgentApplyCheckInput
        {
            ID = id,
            Status = AgentApplyService.StatusReject
        });

        var user = await _db.Queryable<User>().InSingleAsync(_userId);
        Assert.Equal(AgentApplyService.UserTypeMember, user.Type);
        Assert.False(await _db.Queryable<AgentUser>().AnyAsync(a => a.ID == _userId));

        // 无 Furion 宿主时 Oops.Oh 可能包一层类型初始化异常；只断言不能再审。
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _svc.CheckAsync(new AgentApplyCheckInput
            {
                ID = id,
                Status = AgentApplyService.StatusPass
            }));
    }

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
