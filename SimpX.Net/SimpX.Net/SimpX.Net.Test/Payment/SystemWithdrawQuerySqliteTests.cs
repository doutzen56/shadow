using SimpX.Net.Application.Shared;
using SimpX.Net.Data.Entity.Systems;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 总后台代付接口与提现黑名单只读（内存 SQLite）。
/// </summary>
public sealed class SystemWithdrawQuerySqliteTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;

    /// <summary>
    /// 建 sys_Withdraw / sys_BankAccountBlackList 并植入样例。
    /// </summary>
    public SystemWithdrawQuerySqliteTests()
    {
        _db = SqliteBizSchema.CreateMemoryDb([], []);
        _db.Ado.ExecuteCommand(
            """
            CREATE TABLE IF NOT EXISTS [sys_Withdraw] (
              [SettingID] INTEGER PRIMARY KEY AUTOINCREMENT,
              [Name] TEXT,
              [IsOpen] INTEGER NOT NULL DEFAULT 1,
              [Fields] TEXT,
              [Bank] TEXT,
              [Sign] TEXT,
              [Submit] TEXT,
              [Callback] TEXT
            );
            CREATE TABLE IF NOT EXISTS [sys_BankAccountBlackList] (
              [BlackIP] INTEGER PRIMARY KEY AUTOINCREMENT,
              [AccountName] TEXT,
              [AccountNumber] TEXT,
              [Remark] TEXT,
              [CreateAt] TEXT
            );
            """);
        _db.Insertable(new SystemWithdraw
        {
            Name = "DemoWithdraw",
            IsOpen = true,
            FieldString = "",
            BankString = "",
            SignString = "",
            SubmitString = "",
            CallbackString = ""
        }).ExecuteCommand();
        _db.Insertable(new SystemBankAccountBlackList
        {
            AccountName = "黑户",
            AccountNumber = "6222029988776655",
            Remark = "测试",
            CreateAt = DateTime.UtcNow
        }).ExecuteCommand();
    }

    /// <summary>
    /// 代付列表有 DemoWithdraw。
    /// </summary>
    [Fact]
    public async Task Withdraw_list_returns_demo()
    {
        var svc = new SystemWithdrawService(new TestBizDb(_db));
        var list = await svc.ListAsync();
        Assert.Contains(list, x => x.Name == "DemoWithdraw" && x.IsOpen);
    }

    /// <summary>
    /// 黑名单卡号脱敏。
    /// </summary>
    [Fact]
    public async Task Black_list_masks_account()
    {
        var svc = new SystemBankBlackService(new TestBizDb(_db));
        var list = await svc.ListAsync();
        Assert.Contains(list, x => x.AccountName == "黑户" && x.AccountNumber == "6222****6655");
    }

    /// <inheritdoc />
    public void Dispose() => _db?.Dispose();
}
