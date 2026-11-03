namespace SimpX.Net.Application.SuperAdmin;

/// <summary>
/// 总后台常量列表占位。第一波壳层会调 const/list，先回空表，避免白屏。
/// </summary>
[ApiDescriptionSettings(Module = "platform", Name = "const", Order = 280)]
public class PlatformConstAppService : IDynamicApiController, ITransient
{
    /// <summary>
    /// 获取常量列表（暂空）。
    /// </summary>
    /// <returns>空列表</returns>
    [DisplayName("获取所有常量列表")]
    public Task<List<object>> GetList()
    {
        EnsurePlatformHost();
        return Task.FromResult(new List<object>());
    }

    /// <summary>
    /// 只允许总后台进程调用。
    /// </summary>
    private static void EnsurePlatformHost()
    {
        if (!HostTenantMode.IsPlatformHost)
            throw Oops.Oh(ErrorCodeEnum.SA001);
    }
}
