using SimpX.Net.Application.Shared;
using SimpX.Net.Caching;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 旧队列 Redis 连接串改写与创建规则（不连真 Redis）。
/// </summary>
public sealed class LegacyLedgerRedisFactoryTests
{
    /// <summary>
    /// 已有 db 段时只改库号，其它段保留。
    /// </summary>
    [Fact]
    public void ConfigurationWithDb_replaces_existing_db()
    {
        var input = "server=127.0.0.1:6379;password=x;db=5;";
        var got = LegacyLedgerRedisFactory.ConfigurationWithDb(input, 10);
        Assert.Equal("server=127.0.0.1:6379;password=x;db=10;", got);
    }

    /// <summary>
    /// 没有 db 段时追加目标库号。
    /// </summary>
    [Fact]
    public void ConfigurationWithDb_appends_when_missing()
    {
        var input = "server=127.0.0.1:6379;password=x";
        var got = LegacyLedgerRedisFactory.ConfigurationWithDb(input, 1);
        Assert.Equal("server=127.0.0.1:6379;password=x;db=1;", got);
    }

    /// <summary>
    /// 非 Redis 缓存类型不创建客户端。
    /// </summary>
    [Fact]
    public void TryCreate_returns_null_when_not_redis()
    {
        var opt = new CacheOptions { CacheType = "Memory" };
        Assert.Null(LegacyLedgerRedisFactory.TryCreate(opt, LegacyLedgerRedisFactory.ReportDbIndex));
    }

    /// <summary>
    /// 库号常量对齐旧站。
    /// </summary>
    [Fact]
    public void Db_indexes_match_legacy()
    {
        Assert.Equal(10, LegacyLedgerRedisFactory.ReportDbIndex);
        Assert.Equal(1, LegacyLedgerRedisFactory.MoneyDbIndex);
    }
}
