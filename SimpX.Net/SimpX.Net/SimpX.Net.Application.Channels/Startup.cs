using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace SimpX.Net.Application.Channels;

/// <summary>
/// Payment 应用启动：支付渠道实现。认证由宿主选择的认证组件注册（下单、查单只认内部签名，回调不用登录）。
/// </summary>
[AppStartup(100)]
public class Startup : AppStartup
{
    /// <summary>
    /// 注册支付渠道。接入新渠道时，在这里追加一行 IPaymentChannel 的注册即可。
    /// demo：纯内存；mock-http：用 IHttpCaller 调本机模拟网关（接真实通道照这个写）。
    /// </summary>
    /// <param name="services">服务集合</param>
    public void ConfigureServices(IServiceCollection services)
    {
        // 回调成功可走账本（依赖宿主已配 BetWin30Biz）
        services.AddSharedChannelsMoneyServices();
        services.AddTransient<IPaymentChannel, DemoPaymentChannel>();
        services.AddTransient<IPaymentChannel, MockHttpPaymentChannel>();
        // 可配置真实/沙箱 HTTP：配齐 Payment:HttpJson:CreateUrl 后使用 channelCode=http-json
        services.AddTransient<IPaymentChannel, HttpJsonPaymentChannel>();
    }

    /// <summary>
    /// 请求处理链由 Hosting 统一配置，这里不追加。
    /// </summary>
    /// <param name="app">应用构建器</param>
    /// <param name="env">宿主环境</param>
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
    }
}
