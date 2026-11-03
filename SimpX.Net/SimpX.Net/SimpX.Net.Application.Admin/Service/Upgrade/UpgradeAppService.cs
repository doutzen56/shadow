namespace SimpX.Net.Application.Admin;

/// <summary>
/// 系统更新日志占位（对齐 Pro 顶栏 lastUnRead）。
/// SimpX 暂无升级公告表；返回空，避免前端 404，以后有表再补实现。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "upgrade", Order = 390, Description = "系统更新日志")]
public class UpgradeAppService : IDynamicApiController, ITransient
{
    /// <summary>
    /// 获取当前用户未读的最新更新日志。
    /// </summary>
    /// <returns>始终 null（功能未启用）</returns>
    [DisplayName("获取最新未读系统更新日志")]
    public Task<object?> GetLastUnRead() => Task.FromResult<object?>(null);
}
