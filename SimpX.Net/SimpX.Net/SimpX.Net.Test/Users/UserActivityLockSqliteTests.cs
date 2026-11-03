using SimpX.Net.Application.Shared;
using SimpX.Net.Data.Entity.Users;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 上级关闭下级活动（内存 SQLite）。
/// </summary>
public sealed class UserActivityLockSqliteTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;
    private readonly UserActivityLockService _svc;
    private readonly int _parentId;
    private readonly int _childId;

    /// <summary>
    /// 建父子与层级。
    /// </summary>
    public UserActivityLockSqliteTests()
    {
        _db = SqliteBizSchema.CreateMemoryDb(["Users", "usr_Depth"], []);
        _parentId = _db.Insertable(BuildUser("p", function: UserActivityLockService.FunctionCloseSubActivity))
            .ExecuteReturnIdentity();
        _childId = _db.Insertable(BuildUser("c", function: 0)).ExecuteReturnIdentity();
        _db.Insertable(new UserDepth
        {
            SiteID = 1001,
            UserID = _parentId,
            ChildID = _childId,
            Depth = 1
        }).ExecuteCommand();
        _svc = new UserActivityLockService(new TestBizDb(_db));
    }

    /// <summary>
    /// 上级关活动 → 下级被挡。
    /// </summary>
    [Fact]
    public async Task Parent_close_sub_activity_blocks_child()
    {
        Assert.True(await _svc.IsParentCloseSubActivityAsync(1001, _childId));
        Assert.True(await _svc.IsBlockedForActivityAsync(1001, _childId, userLock: 0));
    }

    /// <summary>
    /// 本人活动锁也挡。
    /// </summary>
    [Fact]
    public async Task Self_activity_lock_blocks()
    {
        Assert.True(await _svc.IsBlockedForActivityAsync(
            1001, _childId, userLock: UserActivityLockService.LockActivity));
    }

    /// <summary>
    /// 清理。
    /// </summary>
    public void Dispose() => _db?.Dispose();

    private static User BuildUser(string name, int function) => new()
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
        Function = function,
        LoginAt = DateTime.UtcNow,
        LoginIP = "",
        AgentID = 9,
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
}
