using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 手机黑名单旧动作名。对齐 /admin/risk/device|ip/*balckmobile*。
/// </summary>
public sealed partial class BlackMobileService
{
    /// <summary>
    /// 添加。对齐旧 addbalckmobile（字段 Mobile）。
    /// </summary>
    /// <param name="mobile">手机号</param>
    /// <param name="demo">备注</param>
    /// <param name="cancellationToken">取消令牌</param>
    public Task AddBalckMobileAsync(string? mobile, string? demo = null, CancellationToken cancellationToken = default) =>
        SaveAsync(new BlackMobileSaveInput { ID = 0, BlackMobile = mobile ?? string.Empty, Demo = demo ?? string.Empty }, cancellationToken);

    /// <summary>
    /// 创建页占位。对齐旧 createbalckmobile。
    /// </summary>
    /// <returns>说明</returns>
    public Task<string> CreateBalckMobileAsync() => Task.FromResult("#操作完成#");

    /// <summary>
    /// Excel 导入占位（无文件下载时拒绝）。对齐旧 savebalckmobile。
    /// </summary>
    /// <param name="url">文件地址</param>
    public Task SaveBalckMobileImportAsync(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw Oops.Oh("#不能为空#");
        throw Oops.Oh("#请改用单条保存；Excel 导入待接文件服务#");
    }

    /// <summary>
    /// 列表别名。对齐旧 importbalckmobilelist。
    /// </summary>
    /// <param name="userName">手机号关键字</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行</returns>
    public Task<List<BlackMobileListItemDto>> ImportBalckMobileListAsync(
        string? userName = null,
        CancellationToken cancellationToken = default) =>
        ListAsync(userName, cancellationToken);

    /// <summary>
    /// 编辑别名。对齐旧 editbalckmobile。
    /// </summary>
    /// <param name="input">入参</param>
    /// <param name="cancellationToken">取消令牌</param>
    public Task EditBalckMobileAsync(BlackMobileSaveInput input, CancellationToken cancellationToken = default) =>
        SaveAsync(input ?? new BlackMobileSaveInput(), cancellationToken);

    /// <summary>
    /// 删除别名。
    /// </summary>
    /// <param name="id">编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    public Task DeleteBalckMobileAsync(int id, CancellationToken cancellationToken = default) =>
        DeleteAsync(id, cancellationToken);

    /// <summary>
    /// 批量删除。
    /// </summary>
    /// <param name="ids">编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task BatchDeleteBalckMobileAsync(int[]? ids, CancellationToken cancellationToken = default)
    {
        if (ids == null || ids.Length == 0)
            throw Oops.Oh("#参数错误#");
        var siteId = _site.RequireSiteId();
        await _db.Deleteable<BlackMobileImport>()
            .Where(t => t.SiteID == siteId && ids.Contains(t.ID))
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 批量禁止。对齐旧 batchlockbalckmobile。
    /// </summary>
    /// <param name="ids">编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task BatchLockBalckMobileAsync(int[]? ids, CancellationToken cancellationToken = default)
    {
        if (ids == null || ids.Length == 0)
            throw Oops.Oh("#参数错误#");
        var siteId = _site.RequireSiteId();
        await _db.Updateable<BlackMobileImport>()
            .SetColumns(t => new BlackMobileImport { IsLock = "1" })
            .Where(t => t.SiteID == siteId && ids.Contains(t.ID))
            .ExecuteCommandAsync(cancellationToken);
    }
}
