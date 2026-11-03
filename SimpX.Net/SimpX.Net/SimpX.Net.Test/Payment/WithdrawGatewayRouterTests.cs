using SimpX.Net.Application.Shared;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 代付路由：未配置插件走 Stub；显式 http-md5 才认领。
/// </summary>
public sealed class WithdrawGatewayRouterTests
{
    /// <summary>无 Setting → Stub 受理。</summary>
    [Fact]
    public async Task Router_falls_back_to_stub_without_plugin_setting()
    {
        var router = new WithdrawGatewayRouter(new StubWithdrawGateway(), Array.Empty<IWithdrawGatewayPlugin>());
        var r = await router.SubmitAsync(new WithdrawGatewayRequest
        {
            OrderID = 7,
            Money = 10,
            SysID = 9,
            ChannelSetting = null
        });
        Assert.Equal(WithdrawGatewayStatus.Paymenting, r.Status);
        Assert.StartsWith("STUB", r.SystemID);
    }

    /// <summary>Plugin=http-md5 但无 Gateway → 不认领，仍 Stub。</summary>
    [Fact]
    public async Task Http_plugin_does_not_claim_incomplete_config()
    {
        var plugin = new HttpMd5FormWithdrawGatewayPlugin(FakeHttpCaller.Noop());
        Assert.False(plugin.CanHandle(new WithdrawGatewayRequest
        {
            ChannelSetting = """{"Plugin":"http-md5","Key":"k"}"""
        }));
        var router = new WithdrawGatewayRouter(new StubWithdrawGateway(), [plugin]);
        var r = await router.SubmitAsync(new WithdrawGatewayRequest
        {
            OrderID = 8,
            ChannelSetting = """{"Plugin":"http-md5","Key":"k"}"""
        });
        Assert.StartsWith("STUB", r.SystemID);
    }
}
