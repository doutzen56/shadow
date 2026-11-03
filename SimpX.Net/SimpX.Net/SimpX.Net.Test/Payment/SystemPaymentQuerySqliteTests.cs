using SimpX.Net.Application.Shared;
using SimpX.Net.Data.Entity.Systems;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 系统充值接口只读列表（内存 SQLite）。
/// </summary>
public sealed class SystemPaymentQuerySqliteTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;

    /// <summary>
    /// 建表植入一行。
    /// </summary>
    public SystemPaymentQuerySqliteTests()
    {
        _db = SqliteBizSchema.CreateMemoryDb([], []);
        _db.Ado.ExecuteCommand(
            """
            CREATE TABLE IF NOT EXISTS [sys_Payment] (
              [SettingID] INTEGER PRIMARY KEY AUTOINCREMENT,
              [GroupID] INTEGER NOT NULL DEFAULT 0,
              [Name] TEXT,
              [IsOpen] INTEGER NOT NULL DEFAULT 1,
              [Fields] TEXT,
              [Bank] TEXT,
              [Sign] TEXT,
              [Submit] TEXT,
              [Callback] TEXT,
              [SpecialType] INTEGER NOT NULL DEFAULT 0
            );
            """);
        _db.Insertable(new SystemPayment
        {
            GroupID = 1,
            Name = "DemoPay",
            IsOpen = true,
            FieldString = "",
            BankString = "",
            SignString = "",
            SubmitString = "",
            CallbackString = "",
            SpecialType = 0
        }).ExecuteCommand();
    }

    /// <summary>
    /// 列表有 DemoPay。
    /// </summary>
    [Fact]
    public async Task List_returns_demo()
    {
        var svc = new SystemPaymentService(new TestBizDb(_db));
        var list = await svc.ListAsync();
        Assert.Contains(list, x => x.Name == "DemoPay");
    }

    /// <inheritdoc />
    public void Dispose() => _db?.Dispose();
}
