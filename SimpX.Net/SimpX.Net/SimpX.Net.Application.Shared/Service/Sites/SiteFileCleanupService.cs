using SimpX.Net.Data.Entity.Sites;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 报表下载文件清理：对齐旧 DataMaintenanceJob → SiteFileAgent.DeleteFile。
/// </summary>
public interface ISiteFileCleanupService
{
    /// <summary>
    /// 删除 CreateAt 早于截止日的 site_FileDownload 行。
    /// </summary>
    /// <param name="olderThan">截止时间（通常取日期部分）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>删除行数</returns>
    Task<int> DeleteOlderThanAsync(DateTime olderThan, CancellationToken cancellationToken = default);
}

/// <summary>
/// 默认实现。不挂自动注入接口，由 Schedule 显式注册。
/// </summary>
public sealed class SiteFileCleanupService : ISiteFileCleanupService
{
    private readonly ISqlSugarClient _db;
    private readonly ILogger<SiteFileCleanupService> _logger;

    /// <summary>
    /// 注入业务库与日志。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="logger">日志</param>
    public SiteFileCleanupService(IBizDbAccessor biz, ILogger<SiteFileCleanupService> logger)
    {
        _db = biz.Db;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<int> DeleteOlderThanAsync(
        DateTime olderThan,
        CancellationToken cancellationToken = default)
    {
        var cutoff = olderThan.Date;
        try
        {
            return await _db.Deleteable<SiteFileDownload>()
                .Where(f => f.CreateAt < cutoff)
                .ExecuteCommandAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "清理 site_FileDownload 失败 cutoff={Cutoff:yyyy-MM-dd}", cutoff);
            return 0;
        }
    }
}
