namespace SimpX.Net.Application.Admin;

/// <summary>
/// 系统用户服务 🧩
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "user", Order = 490)]
public class UserAppService : IDynamicApiController, ITransient
{
    private readonly UserManager _userManager;
    private readonly OrgAppService _orgAppService;
    private readonly UserExtOrgManager _userExtOrgManager;
    private readonly UserRoleManager _userRoleManager;
    private readonly ConfigAppService _configAppService;
    private readonly OnlineUserAppService _onlineUserAppService;
    private readonly UserMenuAppService _userMenuAppService;
    private readonly ICacheStateStore _state;
    private readonly UserLdapManager _userLdapManager;
    private readonly SqlSugarRepository<SysUser> _sysUserRep;
    private readonly IEventPublisher _eventPublisher;

    public UserAppService(UserManager userManager,
        OrgAppService orgAppService,
        UserExtOrgManager userExtOrgManager,
        UserRoleManager userRoleManager,
        ConfigAppService configAppService,
        OnlineUserAppService onlineUserAppService,
        ICacheStateStore state,
        UserLdapManager userLdapManager,
        SqlSugarRepository<SysUser> sysUserRep,
        UserMenuAppService userMenuAppService,
        IEventPublisher eventPublisher)
    {
        _userManager = userManager;
        _orgAppService = orgAppService;
        _userExtOrgManager = userExtOrgManager;
        _userRoleManager = userRoleManager;
        _configAppService = configAppService;
        _onlineUserAppService = onlineUserAppService;
        _state = state;
        _userLdapManager = userLdapManager;
        _userMenuAppService = userMenuAppService;
        _sysUserRep = sysUserRep;
        _eventPublisher = eventPublisher;
    }

    /// <summary>
    /// 获取用户分页列表 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("获取用户分页列表")]
    public virtual async Task<SqlSugarPagedList<UserOutput>> Page(PageUserInput input)
    {
        //获取子节点Id集合(包含自己)
        var orgList = await _orgAppService.GetChildIdListWithSelfById(input.OrgId);

        return await _sysUserRep.AsQueryable()
            .LeftJoin<SysOrg>((u, a) => u.OrgId == a.Id)
            .LeftJoin<SysPos>((u, a, b) => u.PosId == b.Id)
            .Where(u => u.AccountType != AccountTypeEnum.SuperAdmin)
            .WhereIF(input.OrgId > 0, u => orgList.Contains(u.OrgId))
            .WhereIF(!_userManager.SuperAdmin, u => u.AccountType != AccountTypeEnum.SysAdmin)
            .WhereIF(_userManager.SuperAdmin && input.TenantId > 0, u => u.TenantId == input.TenantId)
            .WhereIF(!string.IsNullOrWhiteSpace(input.Account), u => u.Account.Contains(input.Account))
            .WhereIF(!string.IsNullOrWhiteSpace(input.RealName), u => u.RealName.Contains(input.RealName))
            .WhereIF(!string.IsNullOrWhiteSpace(input.PosName), (u, a, b) => b.Name.Contains(input.PosName))
            .WhereIF(!string.IsNullOrWhiteSpace(input.Phone), u => u.Phone.Contains(input.Phone))
            .OrderBy(u => new { u.OrderNo, u.Id })
            .Select((u, a, b) => new UserOutput
            {
                OrgName = a.Name,
                PosName = b.Name,
                RoleName = SqlFunc.Subqueryable<SysUserRole>().LeftJoin<SysRole>((m, n) => m.RoleId == n.Id).Where(m => m.UserId == u.Id).SelectStringJoin((m, n) => n.Name, ","),
                DomainAccount = SqlFunc.Subqueryable<SysUserLdap>().Where(m => m.UserId == u.Id).Select(m => m.Account)
            }, true)
            .ToPagedListAsync(input.Page, input.PageSize);
    }

    /// <summary>
    /// 增加用户 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [UnitOfWork]
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("增加用户")]
    public virtual async Task<long> AddUser(AddUserInput input)
    {
        var isExist = await _sysUserRep.AsQueryable().ClearFilter().AnyAsync(u => u.Account == input.Account);
        if (isExist) throw Oops.Oh(ErrorCodeEnum.D1003);

        if (!string.IsNullOrWhiteSpace(input.Phone) && await _sysUserRep.AsQueryable().ClearFilter().AnyAsync(u => u.Phone == input.Phone))
            throw Oops.Oh(ErrorCodeEnum.D1032);

        // 禁止越权新增超级管理员和系统管理员
        if (_userManager.AccountType != AccountTypeEnum.SuperAdmin && input.AccountType is AccountTypeEnum.SuperAdmin or AccountTypeEnum.SysAdmin) throw Oops.Oh(ErrorCodeEnum.D1038);

        // 若没有设置密码则取默认密码
        var password = !string.IsNullOrWhiteSpace(input.Password) ? input.Password : await _configAppService.GetConfigValue<string>(ConfigConst.SysPassword);
        var user = input.Adapt<SysUser>();
        user.Password = CryptogramUtil.Encrypt(password);
        var newUser = await _sysUserRep.AsInsertable(user).ExecuteReturnEntityAsync();

        input.Id = newUser.Id;
        await UpdateRoleAndExtOrg(input);

        // 增加域账号
        if (!string.IsNullOrWhiteSpace(input.DomainAccount))
            await _userLdapManager.AddUserLdap(newUser.TenantId!.Value, newUser.Id, newUser.Account, input.DomainAccount);

        // 发布系统用户操作事件
        await _eventPublisher.PublishAsync(SysUserEventTypeEnum.Add, new
        {
            Entity = newUser,
            Input = input
        });

        return newUser.Id;
    }

    /// <summary>
    /// 注册用户 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [NonAction]
    public virtual async Task<long> RegisterUser(AddUserInput input)
    {
        var isExist = await _sysUserRep.AsQueryable().ClearFilter().AnyAsync(u => u.Account == input.Account);
        if (isExist) throw Oops.Oh(ErrorCodeEnum.D1003);

        if (!string.IsNullOrWhiteSpace(input.Phone) && await _sysUserRep.AsQueryable().ClearFilter().AnyAsync(u => u.Phone == input.Phone))
            throw Oops.Oh(ErrorCodeEnum.D1032);

        // 禁止越权注册
        if (input.AccountType is AccountTypeEnum.SuperAdmin or AccountTypeEnum.SysAdmin) throw Oops.Oh(ErrorCodeEnum.D1038);

        if (string.IsNullOrWhiteSpace(input.Password))
        {
            var password = await _configAppService.GetConfigValue<string>(ConfigConst.SysPassword);
            input.Password = CryptogramUtil.Encrypt(password);
        }

        var user = input.Adapt<SysUser>();
        var newUser = await _sysUserRep.AsInsertable(user).ExecuteReturnEntityAsync();

        input.Id = newUser.Id;
        await UpdateRoleAndExtOrg(input);

        // 增加域账号
        if (!string.IsNullOrWhiteSpace(input.DomainAccount))
            await _userLdapManager.AddUserLdap(newUser.TenantId!.Value, newUser.Id, newUser.Account, input.DomainAccount);

        // 发布系统用户操作事件
        await _eventPublisher.PublishAsync(SysUserEventTypeEnum.Register, new
        {
            Entity = newUser,
            Input = input
        });

        return newUser.Id;
    }

    /// <summary>
    /// 更新用户 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [UnitOfWork]
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("更新用户")]
    public virtual async Task UpdateUser(UpdateUserInput input)
    {
        if (await _sysUserRep.AsQueryable().ClearFilter().AnyAsync(u => u.Account == input.Account && u.Id != input.Id))
            throw Oops.Oh(ErrorCodeEnum.D1003);

        if (!string.IsNullOrWhiteSpace(input.Phone) && await _sysUserRep.AsQueryable().ClearFilter().AnyAsync(u => u.Phone == input.Phone && u.Id != input.Id))
            throw Oops.Oh(ErrorCodeEnum.D1032);

        // 禁止越权更新超级管理员或系统管理员信息
        if (_userManager.AccountType != AccountTypeEnum.SuperAdmin && input.AccountType is AccountTypeEnum.SuperAdmin or AccountTypeEnum.SysAdmin) throw Oops.Oh(ErrorCodeEnum.D1038);

        await _sysUserRep.AsUpdateable(input.Adapt<SysUser>()).IgnoreColumns(true)
            .IgnoreColumns(u => new { u.Password, u.Status, u.TenantId }).ExecuteCommandAsync();

        await UpdateRoleAndExtOrg(input);

        // 删除用户机构缓存
        SqlSugarFilter.DeleteUserOrgCache(input.Id, _sysUserRep.Context.CurrentConnectionConfig.ConfigId.ToString());

        // 若账号的角色和组织架构发生变化,则强制下线账号进行权限更新
        var user = await _sysUserRep.AsQueryable().ClearFilter().FirstAsync(u => u.Id == input.Id);
        var roleIds = await GetOwnRoleList(input.Id);
        if (input.OrgId != user.OrgId || !input.RoleIdList.OrderBy(u => u).SequenceEqual(roleIds.OrderBy(u => u)))
            await _onlineUserAppService.ForceOffline(input.Id);
        // 更新域账号
        await _userLdapManager.AddUserLdap(user.TenantId!.Value, user.Id, user.Account, input.DomainAccount);

        // 发布系统用户操作事件
        await _eventPublisher.PublishAsync(SysUserEventTypeEnum.Update, new
        {
            Entity = user,
            Input = input
        });
    }

    /// <summary>
    /// 更新当前用户语言 🔖
    /// </summary>
    /// <param name="langCode"></param>
    /// <returns></returns>
    [UnitOfWork]
    [ApiDescriptionSettings(Name = "SetLangCode"), HttpPost]
    [DisplayName("更新当前用户语言")]
    public virtual async Task SetLangCode(string langCode)
    {
        var user = await _sysUserRep.AsQueryable().ClearFilter().FirstAsync(u => u.Id == _userManager.UserId) ?? throw Oops.Oh(ErrorCodeEnum.D1011).StatusCode(401);
        user.LangCode = langCode;
        await _sysUserRep.AsUpdateable(user).UpdateColumns(it => it.LangCode).ExecuteCommandAsync();
    }

    /// <summary>
    /// 更新角色和扩展机构
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    private async Task UpdateRoleAndExtOrg(AddUserInput input)
    {
        await GrantRole(new UserRoleInput { UserId = input.Id, RoleIdList = input.RoleIdList });

        await _userExtOrgManager.UpdateUserExtOrg(input.Id, input.ExtOrgIdList);
    }

    /// <summary>
    /// 删除用户 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [UnitOfWork]
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("删除用户")]
    public virtual async Task DeleteUser(DeleteUserInput input)
    {
        var user = await _sysUserRep.GetByIdAsync(input.Id) ?? throw Oops.Oh(ErrorCodeEnum.D0009);
        user.ValidateIsSuperAdminAccountType();
        user.ValidateIsUserId(_userManager.UserId);

        // 若账号为租户默认账号则禁止删除
        var isTenantUser = await _sysUserRep.ChangeRepository<SqlSugarRepository<SysTenant>>().IsAnyAsync(u => u.UserId == input.Id);
        if (isTenantUser) throw Oops.Oh(ErrorCodeEnum.D1029);

        // 若账号为开放接口绑定账号则禁止删除
        var isOpenAccessUser = await _sysUserRep.ChangeRepository<SqlSugarRepository<SysOpenAccess>>().IsAnyAsync(u => u.BindUserId == input.Id);
        if (isOpenAccessUser) throw Oops.Oh(ErrorCodeEnum.D1030);

        // 强制下线
        await _onlineUserAppService.ForceOffline(user.Id);

        await _sysUserRep.DeleteAsync(user);

        // 删除用户角色
        await _userRoleManager.DeleteUserRoleByUserId(input.Id);

        // 删除用户扩展机构
        await _userExtOrgManager.DeleteUserExtOrgByUserId(input.Id);

        // 删除域账号
        await _userLdapManager.DeleteUserLdapByUserId(input.Id);

        // 删除用户收藏菜单
        await _userMenuAppService.DeleteUserMenuList(input.Id);

        // 删除第三方账号绑定，否则同一第三方账号再次登录会找到已删除的用户
        await _sysUserRep.ChangeRepository<SqlSugarRepository<SysUserExternalLogin>>().DeleteAsync(u => u.UserId == input.Id);

        // 发布系统用户操作事件
        await _eventPublisher.PublishAsync(SysUserEventTypeEnum.Delete, new
        {
            Entity = user,
            Input = input
        });
    }

    /// <summary>
    /// 查看用户基本信息 🔖
    /// </summary>
    /// <returns></returns>
    [DisplayName("查看用户基本信息")]
    public virtual async Task<SysUser> GetBaseInfo()
    {
        return await _sysUserRep.AsQueryable().ClearFilter().FirstAsync(c => c.Id == _userManager.UserId);
    }

    /// <summary>
    /// 查询用户组织机构信息 🔖
    /// </summary>
    /// <returns></returns>
    [DisplayName("查询用户组织机构信息")]
    public virtual async Task<List<OrgTreeOutput>> GetOrgInfo()
    {
        return await _orgAppService.GetTree(new OrgInput { Id = 0 });
    }

    /// <summary>
    /// 更新用户基本信息 🔖
    /// </summary>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "BaseInfo"), HttpPost]
    [DisplayName("更新用户基本信息")]
    public virtual async Task<int> UpdateBaseInfo(SysUser user)
    {
        return await _sysUserRep.AsUpdateable(user)
            .IgnoreColumns(u => new { u.CreateTime, u.Account, u.Password, u.AccountType, u.OrgId, u.PosId }).ExecuteCommandAsync();
    }

    /// <summary>
    /// 设置用户状态 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [UnitOfWork]
    [DisplayName("设置用户状态")]
    public virtual async Task<int> SetStatus(UserInput input)
    {
        if (_userManager.UserId == input.Id)
            throw Oops.Oh(ErrorCodeEnum.D1026);

        var user = await _sysUserRep.GetByIdAsync(input.Id) ?? throw Oops.Oh(ErrorCodeEnum.D0009);
        user.ValidateIsSuperAdminAccountType(ErrorCodeEnum.D1015);
        if (!Enum.IsDefined(typeof(StatusEnum), input.Status))
            throw Oops.Oh(ErrorCodeEnum.D3005);

        // 账号禁用则记入黑名单，账号启用则移出。值只表示“在名单里”，不放姓名和手机号。
        if (input.Status == StatusEnum.Disable)
        {
            _state.Add(AuthCacheKeys.UserBlacklist, user.Id, "1");

            // 强制下线
            await _onlineUserAppService.ForceOffline(user.Id);
        }
        else
        {
            _state.Remove(AuthCacheKeys.UserBlacklist, user.Id);
        }

        user.Status = input.Status;
        var rows = await _sysUserRep.AsUpdateable(user).UpdateColumns(u => new { u.Status }).ExecuteCommandAsync();

        // 发布系统用户操作事件
        if (rows > 0)
            await _eventPublisher.PublishAsync(SysUserEventTypeEnum.SetStatus, new
            {
                Entity = user,
                Input = input
            });

        return rows;
    }

    /// <summary>
    /// 授权用户角色 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [UnitOfWork]
    [DisplayName("授权用户角色")]
    public async Task GrantRole(UserRoleInput input)
    {
        //var user = await _sysUserRep.GetFirstAsync(u => u.Id == input.UserId) ?? throw Oops.Oh(ErrorCodeEnum.D0009);
        //if (user.AccountType == AccountTypeEnum.SuperAdmin)
        //    throw Oops.Oh(ErrorCodeEnum.D1022);

        await _userRoleManager.GrantUserRole(input);

        // 发布系统用户操作事件
        await _eventPublisher.PublishAsync(SysUserEventTypeEnum.UpdateRole, input);
    }

    /// <summary>
    /// 修改用户密码 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("修改用户密码")]
    public virtual async Task<int> ChangePwd(ChangePwdInput input)
    {
        // 国密SM2解密（前端密码传输SM2加密后的）
        input.PasswordOld = CryptogramUtil.SM2Decrypt(input.PasswordOld);
        input.PasswordNew = CryptogramUtil.SM2Decrypt(input.PasswordNew);

        var user = await _sysUserRep.AsQueryable().ClearFilter().FirstAsync(c => c.Id == _userManager.UserId) ?? throw Oops.Oh(ErrorCodeEnum.D0009);
        if (CryptogramUtil.CryptoType == CryptogramEnum.MD5.ToString())
        {
            if (user.Password != MD5Encryption.Encrypt(input.PasswordOld))
                throw Oops.Oh(ErrorCodeEnum.D1004);
        }
        else
        {
            if (CryptogramUtil.Decrypt(user.Password) != input.PasswordOld)
                throw Oops.Oh(ErrorCodeEnum.D1004);
        }

        if (input.PasswordOld == input.PasswordNew)
            throw Oops.Oh(ErrorCodeEnum.D1028);

        // 验证密码强度
        if (CryptogramUtil.StrongPassword)
        {
            user.Password = input.PasswordNew.TryValidate(CryptogramUtil.PasswordStrengthValidation)
                ? CryptogramUtil.Encrypt(input.PasswordNew)
                : throw Oops.Oh(CryptogramUtil.PasswordStrengthValidationMsg);
        }
        else
        {
            user.Password = CryptogramUtil.Encrypt(input.PasswordNew);
        }

        var rows = await _sysUserRep.AsUpdateable(user).UpdateColumns(u => u.Password).ExecuteCommandAsync();

        // 发布系统用户操作事件
        if (rows > 0)
            await _eventPublisher.PublishAsync(SysUserEventTypeEnum.ChangePwd, new
            {
                Entity = user,
                Input = input
            });

        return rows;
    }

    /// <summary>
    /// 重置用户密码 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("重置用户密码")]
    public virtual async Task<string> ResetPwd(ResetPwdUserInput input)
    {
        var user = await _sysUserRep.GetByIdAsync(input.Id) ?? throw Oops.Oh(ErrorCodeEnum.D0009);
        string randomPassword = new(Enumerable.Repeat("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789", 6).Select(s => s[Random.Shared.Next(s.Length)]).ToArray());
        user.Password = CryptogramUtil.Encrypt(randomPassword);
        await _sysUserRep.AsUpdateable(user).UpdateColumns(u => u.Password).ExecuteCommandAsync();

        // 清空密码错误次数
        _state.Remove(AuthCacheKeys.PasswordError, user.Account);

        // 发布系统用户操作事件
        await _eventPublisher.PublishAsync(SysUserEventTypeEnum.ResetPwd, new
        {
            Entity = user,
            Input = input
        });

        return randomPassword;
    }

    /// <summary>
    /// 解除登录锁定 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("解除登录锁定")]
    public virtual async Task UnlockLogin(UnlockLoginInput input)
    {
        var user = await _sysUserRep.GetByIdAsync(input.Id) ?? throw Oops.Oh(ErrorCodeEnum.D0009);

        // 清空密码错误次数
        _state.Remove(AuthCacheKeys.PasswordError, user.Account);

        // 发布系统用户操作事件
        await _eventPublisher.PublishAsync(SysUserEventTypeEnum.UnlockLogin, new
        {
            Entity = user,
            Input = input
        });
    }

    /// <summary>
    /// 获取用户拥有角色集合 🔖
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    [DisplayName("获取用户拥有角色集合")]
    public async Task<List<long>> GetOwnRoleList(long userId)
    {
        return await _userRoleManager.GetUserRoleIdList(userId);
    }

    /// <summary>
    /// 获取用户扩展机构集合 🔖
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    [DisplayName("获取用户扩展机构集合")]
    public async Task<List<SysUserExtOrg>> GetOwnExtOrgList(long userId)
    {
        return await _userExtOrgManager.GetUserExtOrgList(userId);
    }
}