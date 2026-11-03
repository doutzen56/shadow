using SimpX.Net.Application.SuperAdmin;
using SimpX.Net.Hosting;

// 总后台宿主：不按域名确定租户；后台用户令牌 + 内部签名 + 开放接口签名
HostLauncher.Run<SuperAdminAuthenticationComponent>(HostCapabilities.SuperAdmin());
