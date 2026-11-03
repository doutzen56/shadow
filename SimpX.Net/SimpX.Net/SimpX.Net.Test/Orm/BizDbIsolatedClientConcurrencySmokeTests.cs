using SqlSugar;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 真库冒烟：业务库按客户端隔离后，并发查询不应再撞 DataReader/MARS。
/// 对齐 BizDbAccessor「每作用域 new 独立客户端」的修法。
/// </summary>
public sealed class BizDbIsolatedClientConcurrencySmokeTests
{
    /// <summary>
    /// 16 路并行各用独立 SqlSugarClient 查一条标量，全部成功。
    /// </summary>
    [Fact]
    public async Task Parallel_scalar_on_isolated_clients_succeeds()
    {
        string cs;
        try
        {
            cs = OrmConnection.LoadBetWin30ConnectionString();
        }
        catch (Exception ex)
        {
            Assert.Fail($"读不到 BetWin30 连接串：{ex.Message}");
            return;
        }

        ConnectionConfig MakeCfg() => new()
        {
            ConnectionString = cs,
            DbType = DbType.SqlServer,
            IsAutoCloseConnection = true,
            InitKeyType = InitKeyType.Attribute,
            ConfigId = "BetWin30Biz"
        };

        try
        {
            using var probe = new SqlSugarClient(MakeCfg());
            await probe.Ado.GetScalarAsync("SELECT 1");
        }
        catch (Exception ex)
        {
            Assert.Fail($"BetWin30 不可达：{ex.Message}");
            return;
        }

        var errors = new System.Collections.Concurrent.ConcurrentBag<string>();
        var tasks = Enumerable.Range(0, 16).Select(async _ =>
        {
            try
            {
                // 与修法一致：每路独立客户端，不共用同一 SqlSugarClient
                using var db = new SqlSugarClient(MakeCfg());
                var n = await db.Ado.GetScalarAsync("SELECT TOP 1 GroupID FROM usr_Group ORDER BY GroupID");
                if (n == null || n == DBNull.Value)
                    errors.Add("usr_Group 无数据");
            }
            catch (Exception ex)
            {
                errors.Add(ex.Message);
            }
        });

        await Task.WhenAll(tasks);
        Assert.True(errors.IsEmpty, "并发隔离客户端仍失败：" + string.Join(" | ", errors.Take(5)));
    }
}
