# 总后台与租户后台宿主

<!-- 由 tools/rag_index.py 生成，下次导入会覆盖；要改职责说明，改脚本里 PROJECTS 的分组定义。 -->

- project_id: `simpx-net`
- summary_group: `platform-hosts`
- host_or_layer: `web-host`
- solution_file: `SimpX.Net/SimpX.Net.sln`
- last_ingestion_run: `20261028T041009Z-project-simpx-net`
- file_count: 221
- project_count: 4
- risk_tags: `cache-consistency`, `elasticsearch`, `login`, `order-state`, `permission`, `user-data`

## 职责摘要

总后台与租户后台入口：域名定租、登录、权限、租户注册与租户内管理。

## 扫描根目录

- `SimpX.Net/SimpX.Net.Host.SuperAdmin`
- `SimpX.Net/SimpX.Net.Host.Admin`
- `SimpX.Net/SimpX.Net.Application.SuperAdmin`
- `SimpX.Net/SimpX.Net.Application.Admin`

## 项目文件

- `SimpX.Net/SimpX.Net.Host.SuperAdmin/SimpX.Net.Host.SuperAdmin.csproj`
- `SimpX.Net/SimpX.Net.Host.Admin/SimpX.Net.Host.Admin.csproj`
- `SimpX.Net/SimpX.Net.Application.SuperAdmin/SimpX.Net.Application.SuperAdmin.csproj`
- `SimpX.Net/SimpX.Net.Application.Admin/SimpX.Net.Application.Admin.csproj`

## 定位样例

按路径里的高风险关键词优先挑选，最多列 50 个。

- `SimpX.Net/SimpX.Net.Application.Admin/Attribute/DictAttribute.cs`：符号 `DictAttribute, IsValid, GetImporterHeaderName`；风险 `login, permission, user-data`
- `SimpX.Net/SimpX.Net.Application.Admin/Dto/Auth/LoginInput.cs`：符号 `LoginInput, LoginPhoneInput, UserRegistrationInput`；风险 `login, permission, user-data`
- `SimpX.Net/SimpX.Net.Application.Admin/Dto/Auth/LoginOutput.cs`：符号 `LoginOutput`；风险 `login, permission, user-data`
- `SimpX.Net/SimpX.Net.Application.Admin/Dto/Auth/LoginUserOutput.cs`：符号 `LoginUserOutput`；风险 `login, permission, user-data`
- `SimpX.Net/SimpX.Net.Application.Admin/Dto/Log/LogInput.cs`：符号 `PageOpLogInput, PageExLogInput, PageVisLogInput, PageLogInput, LogInput`；风险 `login, permission, user-data`
- `SimpX.Net/SimpX.Net.Application.Admin/Dto/OnlineUser/OnlineUserInput.cs`：符号 `PageOnlineUserInput`；风险 `login, permission, user-data`
- `SimpX.Net/SimpX.Net.Application.Admin/Dto/User/UserExtOrgInput.cs`：符号 `UserExtOrgInput`；风险 `login, permission, user-data`
- `SimpX.Net/SimpX.Net.Application.Admin/Dto/User/UserInput.cs`：符号 `UserInput, PageUserInput, AddUserInput, UpdateUserInput, DeleteUserInput, ResetPwdUserInput, ChangePwdInput, UnlockLoginInput`；风险 `login, permission, user-data`
- `SimpX.Net/SimpX.Net.Application.Admin/Dto/User/UserMenuInput.cs`：符号 `UserMenuInput`；风险 `login, permission, user-data`
- `SimpX.Net/SimpX.Net.Application.Admin/Dto/User/UserOutput.cs`：符号 `UserOutput`；风险 `login, permission, user-data`
- `SimpX.Net/SimpX.Net.Application.Admin/Dto/User/UserRegWayInput.cs`：符号 `PageUserRegWayInput, AddUserRegWayInput, UpdateUserRegWayInput`；风险 `login, permission, user-data`
- `SimpX.Net/SimpX.Net.Application.Admin/Dto/User/UserRegWayOutput.cs`：符号 `UserRegWayOutput`；风险 `login, permission, user-data`
- `SimpX.Net/SimpX.Net.Application.Admin/Dto/User/UserRoleInput.cs`：符号 `UserRoleInput`；风险 `login, permission, user-data`
- `SimpX.Net/SimpX.Net.Application.Admin/Entity/SysNoticeUser.cs`：符号 `SysNoticeUser`；风险 `login, permission, user-data`
- `SimpX.Net/SimpX.Net.Application.Admin/Entity/SysOnlineUser.cs`：符号 `SysOnlineUser`；风险 `login, permission, user-data`
- `SimpX.Net/SimpX.Net.Application.Admin/Entity/SysUser.cs`：符号 `SysUser, ValidateIsSuperAdminAccountType, ValidateIsUserId`；风险 `login, permission, user-data`
- `SimpX.Net/SimpX.Net.Application.Admin/Entity/SysUserConfig.cs`：符号 `SysUserConfig`；风险 `login, permission, user-data`
- `SimpX.Net/SimpX.Net.Application.Admin/Entity/SysUserConfigData.cs`：符号 `SysUserConfigData`；风险 `login, permission, user-data`
- `SimpX.Net/SimpX.Net.Application.Admin/Entity/SysUserExtOrg.cs`：符号 `SysUserExtOrg`；风险 `login, permission, user-data`
- `SimpX.Net/SimpX.Net.Application.Admin/Entity/SysUserExternalLogin.cs`：符号 `SysUserExternalLogin`；风险 `login, permission, user-data`
- `SimpX.Net/SimpX.Net.Application.Admin/Entity/SysUserLdap.cs`：符号 `SysUserLdap`；风险 `login, permission, user-data`
- `SimpX.Net/SimpX.Net.Application.Admin/Entity/SysUserMenu.cs`：符号 `SysUserMenu`；风险 `login, permission, user-data`
- `SimpX.Net/SimpX.Net.Application.Admin/Entity/SysUserRegWay.cs`：符号 `SysUserRegWay`；风险 `login, permission, user-data`
- `SimpX.Net/SimpX.Net.Application.Admin/Entity/SysUserRole.cs`：符号 `SysUserRole`；风险 `login, permission, user-data`
- `SimpX.Net/SimpX.Net.Application.Admin/Entity/TestViewSysUser.cs`：符号 `TestViewSysUser, GetQueryableSqlString`；风险 `login, permission, user-data`
- `SimpX.Net/SimpX.Net.Application.Admin/Enum/FinishStatusEnum.cs`：符号 `FinishStatusEnum`；风险 `login, order-state, permission, user-data`
- `SimpX.Net/SimpX.Net.Application.Admin/Enum/NoticeStatusEnum.cs`：符号 `NoticeStatusEnum`；风险 `login, order-state, permission, user-data`
- `SimpX.Net/SimpX.Net.Application.Admin/Enum/NoticeUserStatusEnum.cs`：符号 `NoticeUserStatusEnum`；风险 `login, order-state, permission, user-data`
- `SimpX.Net/SimpX.Net.Application.Admin/Enum/SysUserEventTypeEnum.cs`：符号 `SysUserEventTypeEnum`；风险 `login, permission, user-data`
- `SimpX.Net/SimpX.Net.Application.Admin/EventBus/SysUserEventSubscriber.cs`：符号 `SysUserEventSubscriber, AddUser, RegisterUser, UpdateUser, DeleteUser, SetUserStatus, UpdateUserRole, UnlockUserLogin`；风险 `login, permission, user-data`
- `SimpX.Net/SimpX.Net.Application.Admin/Hub/Dto/OnlineUserHubInput.cs`：符号 `OnlineUserHubInput`；风险 `login, permission, user-data`
- `SimpX.Net/SimpX.Net.Application.Admin/Hub/Dto/OnlineUserHubOutput.cs`：符号 `OnlineUserList`；风险 `login, permission, user-data`
- `SimpX.Net/SimpX.Net.Application.Admin/Hub/IOnlineUserHub.cs`：符号 `IOnlineUserHub`；风险 `login, permission, user-data`
- `SimpX.Net/SimpX.Net.Application.Admin/Hub/OnlineUserHub.cs`：符号 `OnlineUserHub, OnConnectedAsync, OnDisconnectedAsync`；风险 `login, permission, user-data`
- `SimpX.Net/SimpX.Net.Application.Admin/Initialization/UserBlacklistWarmup.cs`：符号 `UserBlacklistWarmup, WarmupAsync`；风险 `login, permission, user-data`
- `SimpX.Net/SimpX.Net.Application.Admin/Security/ConfigTokenLifetimeProvider.cs`：符号 `ConfigTokenLifetimeProvider, GetAccessTokenExpireAsync, GetRefreshTokenExpireAsync`；风险 `login, permission, user-data`
- `SimpX.Net/SimpX.Net.Application.Admin/SeedData/SysUserExtOrgSeedData.cs`：符号 `SysUserExtOrgSeedData, HasData`；风险 `login, permission, user-data`
- `SimpX.Net/SimpX.Net.Application.Admin/SeedData/SysUserRoleSeedData.cs`：符号 `SysUserRoleSeedData, HasData`；风险 `login, permission, user-data`
- `SimpX.Net/SimpX.Net.Application.Admin/SeedData/SysUserSeedData.cs`：符号 `SysUserSeedData, HasData`；风险 `login, permission, user-data`
- `SimpX.Net/SimpX.Net.Application.Admin/Service/Config/UserConfigAppService.cs`：符号 `UserConfigAppService, Page, List, AddConfig, UpdateConfig, DeleteConfig, BatchDeleteConfig, GetDetail`；风险 `login, permission, user-data`
- `SimpX.Net/SimpX.Net.Application.Admin/Service/ExternalLogin/ExternalLoginAppService.cs`：符号 `ExternalLoginAppService, ExternalLoginState, SignIn, Callback, FindOrCreateUserAsync, ResolveProvider, EnsureRedirectAllowed, BuildCallbackUrl`；风险 `login, permission, user-data`
- `SimpX.Net/SimpX.Net.Application.Admin/Service/Message/EmailAppService.cs`：符号 `EmailAppService, SendEmail`；风险 `login, permission, user-data`
- `SimpX.Net/SimpX.Net.Application.Admin/Service/OnlineUser/OnlineUserAppService.cs`：符号 `OnlineUserAppService, Page, ForceOffline, PublicNotice, SingleLogin`；风险 `login, permission, user-data`
- `SimpX.Net/SimpX.Net.Application.Admin/Service/User/UserAppService.cs`：符号 `UserAppService, Page, AddUser, RegisterUser, UpdateUser, SetLangCode, UpdateRoleAndExtOrg, DeleteUser`；风险 `login, permission, user-data`
- `SimpX.Net/SimpX.Net.Application.Admin/Service/User/UserExtOrgManager.cs`：符号 `UserExtOrgManager, GetUserExtOrgList, UpdateUserExtOrg, DeleteUserExtOrgByOrgIdList, DeleteUserExtOrgByUserId, HasUserOrg, HasUserPos`；风险 `login, permission, user-data`
- `SimpX.Net/SimpX.Net.Application.Admin/Service/User/UserLdapManager.cs`：符号 `UserLdapManager, InsertUserLdapList, AddUserLdap, DeleteUserLdapByUserId`；风险 `login, permission, user-data`
- `SimpX.Net/SimpX.Net.Application.Admin/Service/User/UserManager.cs`：符号 `UserManager`；风险 `login, permission, user-data`
- `SimpX.Net/SimpX.Net.Application.Admin/Service/User/UserMenuAppService.cs`：符号 `UserMenuAppService, AddUserMenu, DeleteUserMenu, GetUserMenuList, GetUserMenuIdList, DeleteUserMenuList, DeleteMenuList`；风险 `login, permission, user-data`
- `SimpX.Net/SimpX.Net.Application.Admin/Service/User/UserRegWayAppService.cs`：符号 `UserRegWayAppService, List, Add, Update, CheckData, Delete`；风险 `login, permission, user-data`
- `SimpX.Net/SimpX.Net.Application.Admin/Service/User/UserRoleManager.cs`：符号 `UserRoleManager, GrantUserRole, DeleteUserRoleByRoleId, DeleteUserRoleByUserId, GetUserRoleList, GetUserRoleIdList, GetUserIdList`；风险 `login, permission, user-data`
- 还有 171 个文件只写入 SQLite 控制面；本会话没用 `tools/rag_index.py search` 或 SQL 查过时，不能当作已读取。

## 跳过项

- pruned_directory 8 项、sensitive_file 6 项；明细只写入 `rag/init-reports/20261028T041009Z-project-simpx-net.json`。

## 使用约束

- 本摘要只用于定位和初筛，不替代当前源码。
- 判断真实行为、接口契约、状态变更或兼容逻辑时，必须回读业务仓当前源码。
- 风险标签按路径关键词自动打，可能多打或漏打；高风险判断以源码和项目档案为准。
