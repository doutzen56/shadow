namespace SimpX.Net.Application.Shared;

/// <summary>
/// IP 黑名单旧动作名。对齐 /admin/risk/ip/blacklist|blackinfo|saveblackip|...
/// </summary>
public sealed partial class IpBlackService
{
    /// <summary>
    /// 列表别名。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行</returns>
    public Task<List<IpBlackListItemDto>> BlackListAsync(string? keyword = null, CancellationToken cancellationToken = default) =>
        ListAsync(keyword, cancellationToken);

    /// <summary>
    /// 详情别名。
    /// </summary>
    /// <param name="id">编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>详情</returns>
    public Task<IpBlackListItemDto> BlackInfoAsync(int id, CancellationToken cancellationToken = default) =>
        DetailAsync(id, cancellationToken);

    /// <summary>
    /// 编辑页详情。对齐旧 editbalckip。
    /// </summary>
    /// <param name="id">编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>详情</returns>
    public Task<IpBlackListItemDto> EditBalckIpAsync(int id, CancellationToken cancellationToken = default) =>
        DetailAsync(id, cancellationToken);

    /// <summary>
    /// 保存别名。
    /// </summary>
    /// <param name="input">入参</param>
    /// <param name="cancellationToken">取消令牌</param>
    public Task SaveBlackIpAsync(IpBlackSaveInput input, CancellationToken cancellationToken = default) =>
        SaveAsync(input ?? new IpBlackSaveInput(), cancellationToken);

    /// <summary>
    /// 删除别名。
    /// </summary>
    /// <param name="id">编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    public Task DeleteBlackIpAsync(int id, CancellationToken cancellationToken = default) =>
        DeleteAsync(id, cancellationToken);

    /// <summary>
    /// 批量保存。对齐旧 batchsaveblackip（URL 导入占位；有列表则逐条 Save）。
    /// </summary>
    /// <param name="items">行</param>
    /// <param name="url">文件地址（有则拒绝，待接文件服务）</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task BatchSaveBlackIpAsync(
        List<IpBlackSaveInput>? items,
        string? url = null,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(url) && (items == null || items.Count == 0))
            throw Oops.Oh("#请改用逐条保存；Excel 导入待接文件服务#");
        if (items == null || items.Count == 0)
            throw Oops.Oh("#参数错误#");
        foreach (var item in items)
            await SaveAsync(item, cancellationToken);
    }
}
