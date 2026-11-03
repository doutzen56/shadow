using SimpX.Net.Application.Shared;
using SimpX.Net.Data;
using SimpX.Net.Data.Entity.Users;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 邀请码增删改查（内存 SQLite）。禁止写 BetWin30。
/// </summary>
public class UserInviteCrudSqliteTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;
    private readonly FixedSiteAccessor _site = new(1001, "crud.test");
    private readonly int _userId;

    /// <summary>
    /// 建表并植入会员。
    /// </summary>
    public UserInviteCrudSqliteTests()
    {
        _db = SqliteBizSchema.CreateMemoryDb(["usr_Invite", "Users", "usr_GameConfig"], []);
        _userId = _db.Insertable(new User
        {
            SiteID = 1001,
            UserName = "invite-agent",
            NickName = "a",
            AccountName = "a",
            Password = "x",
            CreateAt = DateTime.Now,
            ActiveAt = DateTime.Now,
            LoginAt = DateTime.Now,
            Type = 1
        }).ExecuteReturnIdentity();
        _db.Insertable(new UserGameConfig
        {
            UserID = _userId,
            SiteID = 1001,
            Lottery = 10
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
        var svc = new UserInviteService(new TestBizDb(_db), _site);

        await svc.SaveAsync(new UserInviteSaveInput
        {
            UserID = _userId,
            IsOpen = true,
            Game = "{\"Lottery\":1}"
        });

        var list1 = await svc.ListAsync(new UserInviteListInput());
        Assert.Single(list1);
        Assert.Equal(_userId, list1[0].UserID);
        var id = list1[0].ID;
        Assert.False(string.IsNullOrEmpty(id));

        var info = await svc.InfoAsync(id);
        Assert.Equal("invite-agent", info.UserName);

        await svc.SaveAsync(new UserInviteSaveInput
        {
            ID = id,
            UserID = _userId,
            IsOpen = false,
            Game = "{\"Lottery\":2}"
        });

        var info2 = await svc.InfoAsync(id);
        Assert.False(info2.IsOpen);
        Assert.Contains("2", info2.Game);

        await svc.DeleteAsync(id);
        Assert.Empty(await svc.ListAsync(new UserInviteListInput()));
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
