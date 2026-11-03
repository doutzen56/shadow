namespace SimpX.Net.Core;

/// <summary>
/// 某个用户的数据范围：能看哪些机构，以及角色里最大的数据范围。
/// </summary>
public sealed class UserDataScopeCache
{
    /// <summary>
    /// 机构 Id 列表。
    /// </summary>
    public List<long> OrgIds { get; set; } = new();

    /// <summary>
    /// 最大数据范围，取值和 <see cref="DataScopeEnum"/> 一致。0 表示还没算出来。
    /// </summary>
    public int MaxDataScope { get; set; }
}
