namespace SimpX.Net.Application.Admin;

/// <summary>
/// 系统服务器监控服务 🧩
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "server", Order = 290, Description = "服务器监控")]
public class ServerAppService : IDynamicApiController, ITransient
{
    public ServerAppService()
    {
    }

    /// <summary>
    /// 获取服务器硬件信息
    /// </summary>
    /// <returns></returns>
    [DisplayName("获取服务器硬件信息")]
    public SystemHardwareInfo GetHardwareInfo()
    {
        return HardwareInfoManager.GetSystemHardwareInfo();
    }

    /// <summary>
    /// 获取服务器运行时信息
    /// </summary>
    /// <returns></returns>
    [DisplayName("获取服务器运行时信息")]
    public SystemRuntimeInfo GetRuntimeInfo()
    {
        return RuntimeInfoManger.GetSystemRuntimeInfo();
    }

    /// <summary>
    /// 获取框架主要程序集
    /// </summary>
    /// <returns></returns>
    [DisplayName("获取框架主要程序集")]
    public List<NuGetPackage> GetNuGetPackageInfo()
    {
        return ReflectionHelper.GetNuGetPackages("SimpX.Net");
    }
}