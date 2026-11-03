using SimpX.Net.Application.Shared;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 充值出站路由：Stub 回落与未知插件失败。
/// </summary>
public sealed class RechargeGatewayRouterTests
{
    /// <summary>
    /// 无 Plugin 时走 Stub。
    /// </summary>
    [Fact]
    public async Task Router_falls_back_to_stub_when_no_plugin()
    {
        var router = new RechargeGatewayRouter(new StubRechargeGatewayPlugin(), Array.Empty<IRechargeGatewayPlugin>());
        var result = await router.SubmitAsync(new RechargeGatewayRequest
        {
            OrderID = "ORD1",
            Money = 10m,
            ChannelSetting = ""
        });
        Assert.True(result.Success);
        Assert.Contains("ORD1", result.PayUrl);
    }

    /// <summary>
    /// 声明了未知插件且无实现时失败。
    /// </summary>
    [Fact]
    public async Task Router_fails_when_named_plugin_missing()
    {
        var router = new RechargeGatewayRouter(new StubRechargeGatewayPlugin(), Array.Empty<IRechargeGatewayPlugin>());
        var result = await router.SubmitAsync(new RechargeGatewayRequest
        {
            OrderID = "ORD2",
            Money = 10m,
            ChannelSetting = "Plugin=no-such"
        });
        Assert.False(result.Success);
        Assert.Contains("no-such", result.ErrorMessage);
    }

    /// <summary>
    /// http-json-md5 在 Gateway/Key 齐全时认领。
    /// </summary>
    [Fact]
    public void HttpJsonMd5_can_handle_when_configured()
    {
        var plugin = new HttpJsonMd5RechargeGatewayPlugin(FakeHttpCaller.Noop());
        Assert.True(plugin.CanHandle(new RechargeGatewayRequest
        {
            ChannelSetting = """{"Plugin":"http-json-md5","Gateway":"https://pay.example/submit","Key":"k"}"""
        }));
        Assert.False(plugin.CanHandle(new RechargeGatewayRequest
        {
            ChannelSetting = """{"Plugin":"http-json-md5","Gateway":"","Key":"k"}"""
        }));
    }
}
