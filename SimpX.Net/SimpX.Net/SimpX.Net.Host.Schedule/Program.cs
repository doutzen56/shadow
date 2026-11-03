using SimpX.Net.Feature.Auth;
using SimpX.Net.Hosting;

// 调度宿主：只接受内部签名调用；不挂验证码、图片、SignalR、IP 内存库
HostLauncher.Run<InternalCallAuthenticationComponent>(HostCapabilities.Worker());
