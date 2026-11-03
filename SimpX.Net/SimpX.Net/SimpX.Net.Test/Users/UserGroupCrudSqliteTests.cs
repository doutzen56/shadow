using SimpX.Net.Application.Shared;
using SimpX.Net.Data;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 会员分组增删改查（内存 SQLite）。禁止写 BetWin30。
/// </summary>
public class UserGroupCrudSqliteTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;
    private readonly FixedSiteAccessor _site = new(1001, "crud.test");

    /// <summary>
    /// 建表。
    /// </summary>
    public UserGroupCrudSqliteTests()
    {
        _db = SqliteBizSchema.CreateMemoryDb(["usr_Group"], []);
    }

    /// <inheritdoc />
    public void Dispose() => _db?.Dispose();

    /// <summary>
    /// 增→查→改→删。
    /// </summary>
    [Fact]
    public async Task Crud_RoundTrip()
    {
        var svc = new UserGroupService(new TestBizDb(_db), _site, null!);

        await svc.SaveAsync(new UserGroupSaveInput
        {
            Name = "测试分组A",
            Sort = 1,
            IsDefault = true
        });

        var list1 = await svc.ListAsync();
        Assert.Single(list1);
        Assert.Equal("测试分组A", list1[0].Name);
        var idA = list1[0].ID;

        // 再建一个默认组，才能改掉/删除原默认（对齐旧版）
        await svc.SaveAsync(new UserGroupSaveInput
        {
            Name = "测试分组B",
            Sort = 2,
            IsDefault = true,
            IsUpdate = true
        });
        var list2 = await svc.ListAsync();
        Assert.Equal(2, list2.Count);
        var idB = list2.First(x => x.Name == "测试分组B").ID;

        var infoA = await svc.InfoAsync(idA);
        Assert.False(infoA.IsDefault);
        Assert.True((await svc.InfoAsync(idB)).IsDefault);

        await svc.SaveAsync(new UserGroupSaveInput
        {
            ID = idA,
            Name = "测试分组A2",
            Sort = 3,
            IsDefault = false,
            IsUpdate = true,
            Payment = "1,2"
        });
        var info = await svc.InfoAsync(idA);
        Assert.Equal("测试分组A2", info.Name);
        Assert.Equal("1,2", info.Payment);

        await svc.DeleteAsync(idA);
        Assert.Single(await svc.ListAsync());
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
