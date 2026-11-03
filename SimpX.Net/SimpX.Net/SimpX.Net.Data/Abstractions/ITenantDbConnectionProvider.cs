namespace SimpX.Net.Data;

/// <summary>
/// 多租户独立库：按租户 Id 取对应的数据库连接。由管理后台实现（租户及其库连接配置在后台）。
/// 宿主没注册实现时，一律用默认库连接。
/// </summary>
public interface ITenantDbConnectionProvider
{
    /// <summary>
    /// 获取租户的数据库连接。
    /// </summary>
    /// <param name="tenantId">租户 Id</param>
    /// <returns>租户库连接；租户没有独立库时返回 null，调用方继续用默认库</returns>
    SqlSugarScopeProvider GetConnectionScope(long tenantId);
}
