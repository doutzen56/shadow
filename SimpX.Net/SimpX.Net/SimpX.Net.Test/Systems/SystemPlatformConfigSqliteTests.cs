using SimpX.Net.Application.Shared;
using SimpX.Net.Data.Entity.Systems;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 总后台代付 / 黑名单 / 标签（内存 SQLite）。禁止写 BetWin30。
/// </summary>
public sealed class SystemPlatformConfigSqliteTests : IDisposable
{
    private readonly SqlSugar.SqlSugarClient _db;
    private readonly SystemWithdrawService _withdraw;
    private readonly SystemBankBlackService _black;
    private readonly SystemTagService _tags;

    /// <summary>
    /// 建壳表。
    /// </summary>
    public SystemPlatformConfigSqliteTests()
    {
        _db = new SqlSugar.SqlSugarClient(new SqlSugar.ConnectionConfig
        {
            ConnectionString = "DataSource=:memory:",
            DbType = SqlSugar.DbType.Sqlite,
            IsAutoCloseConnection = false,
            InitKeyType = SqlSugar.InitKeyType.Attribute
        });
        _db.Ado.Open();
        _db.Ado.ExecuteCommand(
            """
            CREATE TABLE [sys_Withdraw] (
              [SettingID] INTEGER PRIMARY KEY AUTOINCREMENT,
              [Name] TEXT,
              [IsOpen] INTEGER NOT NULL DEFAULT 0,
              [Fields] TEXT,
              [Bank] TEXT,
              [Sign] TEXT,
              [Submit] TEXT,
              [Callback] TEXT
            );
            CREATE TABLE [sys_BankAccountBlackList] (
              [BlackIP] INTEGER PRIMARY KEY AUTOINCREMENT,
              [AccountName] TEXT,
              [AccountNumber] TEXT,
              [Remark] TEXT,
              [CreateAt] TEXT
            );
            CREATE TABLE [tag] (
              [TagType] INTEGER NOT NULL,
              [Code] TEXT NOT NULL,
              [TagName] TEXT,
              [Sort] INTEGER NOT NULL DEFAULT 0,
              [Setting] TEXT,
              [Image] TEXT,
              PRIMARY KEY ([TagType], [Code])
            );
            """);

        _withdraw = new SystemWithdrawService(new TestBizDb(_db));
        _black = new SystemBankBlackService(new TestBizDb(_db));
        _tags = new SystemTagService(new TestBizDb(_db));
    }

    /// <summary>代付保存并开关。</summary>
    [Fact]
    public async Task Withdraw_save_and_toggle_open()
    {
        var id = await _withdraw.SaveAsync(new SystemWithdrawSaveInput
        {
            Name = "TestPay",
            IsOpen = false,
            FieldString = "{}",
            SignString = "{}",
            SubmitString = "{}",
            CallbackString = "{}",
            BankString = "{}"
        });
        Assert.True(id > 0);

        await _withdraw.UpdateIsOpenAsync(id, true);
        var detail = await _withdraw.DetailAsync(id);
        Assert.True(detail.IsOpen);
        Assert.Equal("TestPay", detail.Name);
    }

    /// <summary>黑名单新增去重与删除。</summary>
    [Fact]
    public async Task Black_create_reject_duplicate_then_delete()
    {
        await _black.CreateAsync(new SystemBankBlackCreateInput
        {
            AccountName = "张三",
            AccountNumber = "6222000011112222",
            Remark = "t"
        });
        var list = await _black.ListAsync();
        Assert.Single(list);
        Assert.Contains("****", list[0].AccountNumber);

        await Assert.ThrowsAnyAsync<Exception>(() => _black.CreateAsync(new SystemBankBlackCreateInput
        {
            AccountName = "李四",
            AccountNumber = "6222000011112222"
        }));

        await _black.DeleteAsync(list[0].ID);
        Assert.Empty(await _black.ListAsync());
    }

    /// <summary>标签保存与删除。</summary>
    [Fact]
    public async Task Tag_save_and_delete()
    {
        await _tags.SaveAsync(new SystemTagSaveInput
        {
            Type = 3,
            Code = "Currency",
            Name = "{\"CHN\":\"币种\"}",
            Setting = "",
            Image = "",
            Sort = 10
        });
        var list = await _tags.ListAsync(new SystemTagListInput { Type = 3 });
        Assert.Single(list);
        Assert.Equal("Currency", list[0].Code);

        await _tags.DeleteAsync(3, "Currency");
        Assert.Empty(await _tags.ListAsync(new SystemTagListInput { Type = 3 }));
    }

    /// <inheritdoc />
    public void Dispose() => _db?.Dispose();
}
