namespace SimpX.Net.Application.Shared;

/// <summary>
/// 系统角色菜单服务
/// </summary>
public class RoleMenuManager : ITransient
{
    private readonly SqlSugarRepository<SysRoleMenu> _sysRoleMenuRep;
    private readonly CacheInvalidationBatch _invalidations;

    /// <summary>
    /// 注入角色菜单仓储和缓存失效清单。清单在事务提交成功后才执行。
    /// </summary>
    /// <param name="sysRoleMenuRep">角色菜单仓储</param>
    /// <param name="invalidations">缓存失效清单</param>
    public RoleMenuManager(SqlSugarRepository<SysRoleMenu> sysRoleMenuRep,
        CacheInvalidationBatch invalidations)
    {
        _sysRoleMenuRep = sysRoleMenuRep;
        _invalidations = invalidations;
    }

    /// <summary>
    /// 根据角色Id集合获取菜单Id集合
    /// </summary>
    /// <param name="roleIdList"></param>
    /// <returns></returns>
    public async Task<List<long>> GetRoleMenuIdList(List<long> roleIdList)
    {
        return await _sysRoleMenuRep.AsQueryable()
            .Where(u => roleIdList.Contains(u.RoleId))
            .Select(u => u.MenuId).ToListAsync();
    }

    /// <summary>
    /// 授权角色菜单 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [UnitOfWork]
    [DisplayName("授权角色菜单")]
    public async Task GrantRoleMenu(RoleMenuInput input)
    {
        await _sysRoleMenuRep.DeleteAsync(u => u.RoleId == input.Id);

        // 追加父级菜单
        var allIdList = await _sysRoleMenuRep.Context.Queryable<SysMenu>().Select(u => new { u.Id, u.Pid }).ToListAsync();
        var pIdList = allIdList.ToChildList(u => u.Pid, u => u.Id, u => input.MenuIdList.Contains(u.Id)).Select(u => u.Pid).Distinct().ToList();
        input.MenuIdList = input.MenuIdList.Concat(pIdList).Distinct().Where(u => u != 0).ToList();

        // 保存授权数据
        var menus = input.MenuIdList.Select(u => new SysRoleMenu
        {
            RoleId = input.Id,
            MenuId = u
        }).ToList();

        // 同步授权数据
        if (input.RoleIdList?.Count() > 0)
        {
            await _sysRoleMenuRep.DeleteAsync(u => input.RoleIdList.Contains(u.RoleId));
            input.RoleIdList.ForEach(u =>
            {
                menus.AddRange(input.MenuIdList.Select(v => new SysRoleMenu
                {
                    RoleId = u,
                    MenuId = v
                }));
            });
        }

        await _sysRoleMenuRep.InsertRangeAsync(menus);

        // 库写完后登记失效，事务提交成功才真正删掉全部用户按钮
        _invalidations.Add(PermissionCacheKeys.UserButton, null, CacheRemoveMode.ByName);
        _invalidations.Add(PermissionCacheKeys.AllButton, "all", CacheRemoveMode.ByKey);
    }

    /// <summary>
    /// 根据菜单Id集合删除角色菜单
    /// </summary>
    /// <param name="menuIdList"></param>
    /// <returns></returns>
    public async Task DeleteRoleMenuByMenuIdList(List<long> menuIdList)
    {
        await _sysRoleMenuRep.DeleteAsync(u => menuIdList.Contains(u.MenuId));
    }

    /// <summary>
    /// 根据角色Id删除角色菜单
    /// </summary>
    /// <param name="roleId"></param>
    /// <returns></returns>
    public async Task DeleteRoleMenuByRoleId(long roleId)
    {
        await _sysRoleMenuRep.DeleteAsync(u => u.RoleId == roleId);
    }
}