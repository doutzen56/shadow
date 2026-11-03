using System.Security.Cryptography;
using System.Text;
using SimpX.Net.Application.Shared;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 游戏推送 Mock：验签成功；未知类型 505；假签名失败。
/// </summary>
public sealed class GamePushMockTests
{
    /// <summary>mock 验签通过且可入账一次。</summary>
    [Fact]
    public async Task Mock_push_credits_when_signed()
    {
        var ledger = new RecordingMoneyLedger();
        var svc = new GamePushService([new MockGamePushHandler(ledger)]);
        var order = "GPO001";
        var amount = "1.50";
        var sign = Md5(order + amount + MockGamePushHandler.SandboxKey);
        var form = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["order"] = order,
            ["amount"] = amount,
            ["sign"] = sign,
            ["siteId"] = "1001",
            ["userId"] = "88"
        };

        var ok = await svc.HandleAsync("mock", "", form);
        Assert.True(ok.Ok);
        Assert.Equal(200, ok.StatusCode);
        Assert.Single(ledger.Logs);
        Assert.Equal(1.50m, ledger.Logs[0].Money);
        Assert.Equal(250, ledger.Logs[0].Type);
    }

    /// <summary>假签名 → 400。</summary>
    [Fact]
    public async Task Mock_push_rejects_bad_sign()
    {
        var svc = new GamePushService([new MockGamePushHandler(new RecordingMoneyLedger())]);
        var r = await svc.HandleAsync("mock", "", new Dictionary<string, string>
        {
            ["order"] = "X",
            ["amount"] = "1",
            ["sign"] = "bad"
        });
        Assert.False(r.Ok);
        Assert.Equal(400, r.StatusCode);
    }

    /// <summary>未知类型 → 505。</summary>
    [Fact]
    public async Task Unknown_type_returns_505()
    {
        var svc = new GamePushService([new MockGamePushHandler(new RecordingMoneyLedger())]);
        var r = await svc.HandleAsync("avia", "", new Dictionary<string, string>());
        Assert.Equal(505, r.StatusCode);
    }

    private static string Md5(string raw) =>
        Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(raw))).ToLowerInvariant();

}
