using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 证件黑名单旧动作名。对齐 /admin/risk/device|ip/*balckkyc*。
/// </summary>
public sealed partial class BlackKycService
{
    /// <summary>
    /// 添加。对齐旧 addbalckkyc。
    /// </summary>
    /// <param name="kyc">证件号</param>
    /// <param name="demo">备注</param>
    /// <param name="isCheck">兼容旧字段</param>
    /// <param name="cancellationToken">取消令牌</param>
    public Task AddBalckKycAsync(
        string? kyc,
        string? demo = null,
        bool isCheck = false,
        CancellationToken cancellationToken = default) =>
        SaveAsync(new BlackKycSaveInput { ID = 0, BlackKyc = kyc ?? string.Empty, Demo = demo ?? string.Empty }, cancellationToken);

    /// <summary>
    /// 创建页占位。
    /// </summary>
    /// <returns>说明</returns>
    public Task<string> CreateBalckKycAsync() => Task.FromResult("#操作完成#");

    /// <summary>
    /// Excel 导入占位。
    /// </summary>
    /// <param name="url">文件地址</param>
    public Task SaveBalckKycImportAsync(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw Oops.Oh("#不能为空#");
        throw Oops.Oh("#请改用单条保存；Excel 导入待接文件服务#");
    }

    /// <summary>
    /// 列表别名。
    /// </summary>
    /// <param name="userName">关键字</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行</returns>
    public Task<List<BlackKycListItemDto>> ImportBalckKycListAsync(
        string? userName = null,
        CancellationToken cancellationToken = default) =>
        ListAsync(userName, cancellationToken);

    /// <summary>
    /// 编辑别名。
    /// </summary>
    /// <param name="input">入参</param>
    /// <param name="cancellationToken">取消令牌</param>
    public Task EditBalckKycAsync(BlackKycSaveInput input, CancellationToken cancellationToken = default) =>
        SaveAsync(input ?? new BlackKycSaveInput(), cancellationToken);

    /// <summary>
    /// 删除别名。
    /// </summary>
    /// <param name="id">编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    public Task DeleteBalckKycAsync(int id, CancellationToken cancellationToken = default) =>
        DeleteAsync(id, cancellationToken);

    /// <summary>
    /// 批量删除。
    /// </summary>
    /// <param name="ids">编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task BatchDeleteBalckKycAsync(int[]? ids, CancellationToken cancellationToken = default)
    {
        if (ids == null || ids.Length == 0)
            throw Oops.Oh("#参数错误#");
        var siteId = _site.RequireSiteId();
        await _db.Deleteable<BlackKycImport>()
            .Where(t => t.SiteID == siteId && ids.Contains(t.ID))
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 检查证件是否在黑名单。对齐旧 checkblackkyc。
    /// </summary>
    /// <param name="blackKyc">证件号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否命中</returns>
    public async Task<bool> CheckBlackKycAsync(string? blackKyc, CancellationToken cancellationToken = default)
    {
        var kyc = (blackKyc ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(kyc))
            return false;
        var siteId = _site.RequireSiteId();
        return await _db.Queryable<BlackKycImport>()
            .AnyAsync(t => t.SiteID == siteId && t.BlackKyc == kyc, cancellationToken);
    }
}
