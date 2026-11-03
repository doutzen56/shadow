using SimpX.Net.Application.Shared;
using SimpX.Net.Data;
using SimpX.Net.Data.Entity.Sites;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 提现银行配置 Save→Get（内存 SQLite）。禁止写 BetWin30。
/// </summary>
public sealed class WithdrawBankSaveSqliteTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;
    private readonly FixedSiteAccessor _site = new(1001, "wd.bank");
    private readonly WithdrawBankService _svc;

    /// <summary>建 site_Config。</summary>
    public WithdrawBankSaveSqliteTests()
    {
        _db = SqliteBizSchema.CreateMemoryDb(["site_Config"], []);
        var biz = new TestBizDb(_db);
        _svc = new WithdrawBankService(biz, _site);
    }

    /// <inheritdoc />
    public void Dispose() => _db?.Dispose();

    /// <summary>无行插入后 Get 一致；再改写后 Get 一致。</summary>
    [Fact]
    public async Task Save_Then_Get_Matches()
    {
        var first = """{"banks":["ICBC","ABC"]}""";
        await _svc.SaveAsync(first);
        var got1 = await _svc.GetAsync();
        Assert.Equal(1001, got1.SiteID);
        Assert.Equal(first, got1.Bank);

        var second = """{"banks":["BOC"]}""";
        await _svc.SaveAsync(second);
        var got2 = await _svc.GetAsync();
        Assert.Equal(second, got2.Bank);

        var row = (await _db.Queryable<SiteConfig>()
            .Where(c => c.SiteID == 1001)
            .Take(1)
            .ToListAsync()).First();
        Assert.Equal(second, row.Bank);
    }

    /// <summary>固定定站。</summary>
    private sealed class FixedSiteAccessor : ICurrentSiteAccessor
    {
        private readonly int _id;
        private readonly string _host;

        /// <summary>指定站。</summary>
        public FixedSiteAccessor(int id, string host)
        {
            _id = id;
            _host = host;
        }

        /// <inheritdoc />
        public int? SiteId => _id;

        /// <inheritdoc />
        public string Host => _host;

        /// <inheritdoc />
        public int RequireSiteId() => _id;
    }
}
