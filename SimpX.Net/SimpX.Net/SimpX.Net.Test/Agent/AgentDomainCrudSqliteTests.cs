using SimpX.Net.Application.Shared;
using SimpX.Net.Data;
using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Users;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 代理域名增删改查（内存 SQLite）。禁止写 BetWin30。
/// </summary>
public class AgentDomainCrudSqliteTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;
    private readonly FixedSiteAccessor _site = new(1001, "crud.test");
    private readonly int _userId;
    private readonly string _inviteId = "87654321";

    /// <summary>
    /// 建表并植入站点、会员与邀请码。
    /// </summary>
    public AgentDomainCrudSqliteTests()
    {
        _db = SqliteBizSchema.CreateMemoryDb(["usr_Domain", "usr_Invite", "Users", "Site"], []);
        _db.Insertable(new Site
        {
            ID = 1001,
            Name = "默认站名"
        }).ExecuteCommand();
        _userId = _db.Insertable(new User
        {
            SiteID = 1001,
            UserName = "domain-agent",
            NickName = "a",
            AccountName = "a",
            Password = "x",
            CreateAt = DateTime.Now,
            ActiveAt = DateTime.Now,
            LoginAt = DateTime.Now,
            Type = 1
        }).ExecuteReturnIdentity();

        _db.Insertable(new UserInvite
        {
            ID = _inviteId,
            SiteID = 1001,
            UserID = _userId,
            Member = 0,
            IsOpen = true,
            CreateAt = DateTime.Now,
            SettingString = "{}"
        }).ExecuteCommand();
    }

    /// <inheritdoc />
    public void Dispose() => _db?.Dispose();

    /// <summary>
    /// 增→查→改→删。
    /// </summary>
    [Fact]
    public async Task Crud_RoundTrip()
    {
        var svc = new AgentDomainService(new TestBizDb(_db), _site);

        await svc.SaveAsync(new AgentDomainSaveInput
        {
            ID = 0,
            Name = "agent-crud.example",
            InviteID = _inviteId,
            SiteName = "测站"
        });

        var page1 = await svc.PageAsync(new AgentDomainPageInput
        {
            StartAt = DateTime.Now.AddDays(-1),
            EndAt = DateTime.Now.AddDays(1),
            Page = 1,
            PageSize = 20
        });
        Assert.Equal(1, page1.RecordCount);
        Assert.Equal("agent-crud.example", page1.Data[0].Name);
        var id = page1.Data[0].ID;

        var detail = await svc.DetailAsync(id);
        Assert.Equal(_inviteId, detail.InviteID);
        Assert.Equal("domain-agent", detail.UserName);

        await svc.SaveAsync(new AgentDomainSaveInput
        {
            ID = id,
            Name = "agent-crud.example",
            InviteID = _inviteId,
            SiteName = "测站2"
        });

        var detail2 = await svc.DetailAsync(id);
        Assert.Equal("测站2", detail2.SiteName);

        await svc.DeleteAsync(id);
        var page2 = await svc.PageAsync(new AgentDomainPageInput
        {
            StartAt = DateTime.Now.AddDays(-1),
            EndAt = DateTime.Now.AddDays(1)
        });
        Assert.Equal(0, page2.RecordCount);
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
