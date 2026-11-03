using SimpX.Net.Feature.Auth;
using SimpX.Net.Hosting;

// 渠道宿主：下单、查单只接受内部签名调用，渠道回调不用登录；按域名确定租户（支付挂具体租户）
HostLauncher.Run<InternalCallAuthenticationComponent>(HostCapabilities.Channels());

