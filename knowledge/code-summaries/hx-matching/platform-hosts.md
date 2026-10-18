# 平台与商户后台宿主

<!-- 由 tools/rag_index.py 生成，下次导入会覆盖；要改职责说明，改脚本里 PROJECTS 的分组定义。 -->

- project_id: `hx-matching`
- summary_group: `platform-hosts`
- host_or_layer: `web-host`
- solution_file: `HX.Matching.sln`
- last_ingestion_run: `20261018T083109Z-project-hx-matching`
- file_count: 333
- project_count: 3
- risk_tags: `cache-consistency`, `callback`, `funds`, `gateway`, `login`, `order-state`, `permission`, `user-data`

## 职责摘要

管理端和商户后台入口，承载运营、权限、报表和后台管理动作。

## 扫描根目录

- `HX.Matching.Admin.WebApi`
- `HX.Matching.AdminV2.WebApi`
- `HX.Matching.MerchantAdmin.WebApi`

## 项目文件

- `HX.Matching.Admin.WebApi/HX.Matching.Admin.WebApi.csproj`
- `HX.Matching.AdminV2.WebApi/HX.Matching.AdminV2.WebApi.csproj`
- `HX.Matching.MerchantAdmin.WebApi/HX.Matching.MerchantAdmin.WebApi.csproj`

## 定位样例

按路径里的高风险关键词优先挑选，最多列 50 个。

- `HX.Matching.Admin.WebApi/Controllers/System/CallBack/ChannelCallback.cs`：符号 `ChannelCallback, Notify, NotifyForTenant, VerifySessionRecharge, VerifySessionRechargeForTenant, VerifySessionRechargeGet, VerifySessionRechargeGetForTenant, RunAsCallbackTenantAsync`；风险 `callback, gateway, login, permission, user-data`
- `HX.Matching.Admin.WebApi/Controllers/System/ChannelOrderController.cs`：符号 `HxChannelOrderController, ChannelOrderRechargeList, ChannelOrderRechargeBatchQuery, RechargeInfo, ChannelOrderWithdrawalList, ChannelOrderWithdrawalBatchQuery, WithdrawalInfo, ChannelOrderRechargeExport`；风险 `gateway, login, order-state, permission, user-data`
- `HX.Matching.Admin.WebApi/Controllers/System/HxBlacklistPayController.cs`：符号 `HxBlacklistPayController, List, AddUser, Info, ChangeEnable, Remove, GetBlacklistTypeEnum, ImportData`；风险 `funds, login, permission, user-data`
- `HX.Matching.Admin.WebApi/Controllers/System/HxChannelMoneyController.cs`：符号 `HxChannelMoneyController, ListR, ListW, Info, ChannelAddR, ChannelAddW, ChannelEditR, ChannelEditW`；风险 `funds, gateway, login, permission, user-data`
- `HX.Matching.Admin.WebApi/Controllers/System/HxFalseOrderRecordController.cs`：符号 `HxFalseOrderRecordController, List, Info, Add, DeleteFalseOrderRecord, ImportData, ImportTemplateExcel, WithdrawalList`；风险 `login, order-state, permission, user-data`
- `HX.Matching.Admin.WebApi/Controllers/System/HxFalseOrderUserController.cs`：符号 `HxFalseOrderUserController, List, Remove, Info, AddUser, GetOrderPunishTypeEnum, GetPunishRuleEnum`；风险 `login, order-state, permission, user-data`
- `HX.Matching.Admin.WebApi/Controllers/System/HxMerchantUserController.cs`：符号 `HxMerchantUserController, List, Export, QueryExport, GetExport, SatasList, SettlementList, Info`；风险 `login, permission, user-data`
- `HX.Matching.Admin.WebApi/Controllers/System/HxMerchantUserLabelController.cs`：符号 `HxMerchantUserLabelController, List, MerchantList, Info, AddUser, AddUserList, EditUserList, EditUser`；风险 `login, permission, user-data`
- `HX.Matching.Admin.WebApi/Controllers/System/HxMerchantUserNameLogController.cs`：符号 `HxMerchantUserNameLogController, List, Detail, GetFormerName`；风险 `login, permission, user-data`
- `HX.Matching.Admin.WebApi/Controllers/System/HxMerchantUserWithdrawalController.cs`：符号 `HxMerchantUserWithdrawalController, List, Export, QueryExport, GetExport`；风险 `funds, login, permission, user-data`
- `HX.Matching.Admin.WebApi/Controllers/System/HxOrderLifeController.cs`：符号 `HxOrderLifeController, GetOrderLifeList, GetResultContentList, GetOrderStateEnum, GetNxhErrorEnum, GetRechargeOrderStateEnum`；风险 `login, order-state, permission, user-data`
- `HX.Matching.Admin.WebApi/Controllers/System/HxOrderMatchingController.cs`：符号 `HxOrderMatchingController, MerchantPagelist, OrderLog, OrderPaymentInfo, OrdercertificateInfo, ReviewQuery, ReviewFirstQuery, TransferFirst`；风险 `login, order-state, permission, user-data`
- `HX.Matching.Admin.WebApi/Controllers/System/HxOrderMatchingSupplierController.cs`：符号 `HxOrderMatchingSupplierController, Pagelist, SupplierOrderLog, SupplierOrderPaymentInfo, Export, QueryExport, GetExportFile`；风险 `login, order-state, permission, user-data`
- `HX.Matching.Admin.WebApi/Controllers/System/HxOrderRechargeController.cs`：符号 `HxOrderRechargeController, Pagelist, List, Info, OrderLog, OrderPaymentInfo, OrderCertificateInfo, ManualCallback`；风险 `funds, login, order-state, permission, user-data`
- `HX.Matching.Admin.WebApi/Controllers/System/HxOrderWithdrawalController.cs`：符号 `HxOrderWithdrawalController, Pagelist, PagePrioritylist, TgPostPagelist, Info, OrderLog, OrderPaymentInfo, ManualCallback`；风险 `funds, login, order-state, permission, user-data`
- `HX.Matching.Admin.WebApi/Controllers/System/HxStatsOrderController.cs`：符号 `HxStatsOrderController, List, Export, QueryExport, GetExport`；风险 `login, order-state, permission, user-data`
- `HX.Matching.Admin.WebApi/Controllers/System/HxStatsSupplierRechargeController.cs`：符号 `HxStatsSupplierRechargeController, List, Export, QueryExport, GetExport`；风险 `funds, login, permission, user-data`
- `HX.Matching.Admin.WebApi/Controllers/System/HxStatsSupplierWithdrawalController.cs`：符号 `HxStatsSupplierWithdrawalController, List, Export, QueryExport, GetExport`；风险 `funds, login, permission, user-data`
- `HX.Matching.Admin.WebApi/Controllers/System/HxSupplierRechargeController.cs`：符号 `HxSupplierRechargeController, List, Info, EditUser, ChangeEnable, GetChannelPayType, Dropdownlist, SupList`；风险 `funds, login, permission, user-data`
- `HX.Matching.Admin.WebApi/Controllers/System/HxSupplierWithdrawalController.cs`：符号 `HxSupplierWithdrawalController, List, Info, EditUser, ChangeEnable, Dropdownlist, MatchingConfig, EditMatchingConfig`；风险 `funds, login, permission, user-data`
- `HX.Matching.Admin.WebApi/Controllers/System/HxUserAccountLinkController.cs`：符号 `HxUserAccountLinkController, List, ListValue, GetRiskTypeEnum`；风险 `login, permission, user-data`
- `HX.Matching.Admin.WebApi/Controllers/System/SysLoginController.cs`：符号 `SysLoginController, Login, EvaluateAccountSecurityAsync, IsBlockedAsync, ClearLoginFailedAsync, LogOut, GetUserInfo, GetRouters`；风险 `login, permission, user-data`
- `HX.Matching.Admin.WebApi/Controllers/System/SysLoginErrorController.cs`：符号 `SysLoginErrorController, ListLoginErrorsList, RemoveLoginError, ClearAllLoginErrors`；风险 `login, permission, user-data`
- `HX.Matching.Admin.WebApi/Controllers/System/SysUserController.cs`：符号 `SysUserController, List, GetInfo, AddUser, UpdateUser, ClearGoogleKey, ChangeStatus, SetForceLauncher`；风险 `login, permission, user-data`
- `HX.Matching.Admin.WebApi/Controllers/System/SysUserMsgController.cs`：符号 `SysUserMsgController, QuerySysUserMsg, QueryMySysUserMsg, QueryMyUnReadMsg, GetSysUserMsg, AddSysUserMsg, DeleteSysUserMsg, ReadMsg`；风险 `login, permission, user-data`
- `HX.Matching.Admin.WebApi/Controllers/System/SysUserRoleController.cs`：符号 `SysUserRoleController, GetList, Create, Delete, GetExcludeUsers`；风险 `login, permission, user-data`
- `HX.Matching.Admin.WebApi/Controllers/System/monitor/SysLogininforController.cs`：符号 `SysLogininforController, LoignLogList, QueryMyLoignLogList, CleanLoginInfo, Remove, Export, QueryStatiLoginLog`；风险 `login, permission, user-data`
- `HX.Matching.Admin.WebApi/Controllers/System/monitor/SysUserOnlineController.cs`：符号 `SysUserOnlineController, Index, Force, BatchforceLogout`；风险 `login, permission, user-data`
- `HX.Matching.Admin.WebApi/Controllers/System/monitor/UserOnlineLogController.cs`：符号 `UserOnlineLogController, QueryUserOnlineLog, DeleteUserOnlineLog, Export`；风险 `login, permission, user-data`
- `HX.Matching.AdminV2.WebApi/Application/BlacklistPayV2ApplicationService.cs`：符号 `BlacklistPayV2ApplicationService, ListAsync, Info, AddAsync, ChangeEnable, RemoveBatchAsync, GetBlacklistTypeEnum, ImportDataAsync`；风险 `funds, login, permission, user-data`
- `HX.Matching.AdminV2.WebApi/Application/ChannelMoneyV2ApplicationService.cs`：符号 `ChannelMoneyV2ApplicationService, ListRAsync, ListWAsync, InfoAsync, AddRAsync, AddWAsync, EditRAsync, EditWAsync`；风险 `funds, gateway, login, permission, user-data`
- `HX.Matching.AdminV2.WebApi/Application/ChannelOrderV2ApplicationService.cs`：符号 `ChannelOrderV2ApplicationService, RechargeListAsync, WithdrawalListAsync, RechargeInfoAsync, WithdrawalInfoAsync, RechargeBatchQueryAsync, WithdrawalBatchQueryAsync, CreateExportAsync`；风险 `gateway, login, order-state, permission, user-data`
- `HX.Matching.AdminV2.WebApi/Application/FalseOrderRecordV2ApplicationService.Withdrawal.cs`：符号 `FalseOrderRecordV2ApplicationService, WithdrawalListAsync, WithdrawalOrderInfoAsync, AddWithdrawalAsync, DeleteWithdrawalAsync`；风险 `funds, login, order-state, permission, user-data`
- `HX.Matching.AdminV2.WebApi/Application/FalseOrderRecordV2ApplicationService.cs`：符号 `FalseOrderRecordV2ApplicationService, ListAsync, RechargeOrderInfoAsync, AddRechargeAsync, DeleteRechargeAsync, ImportDataAsync, ParseIds`；风险 `login, order-state, permission, user-data`
- `HX.Matching.AdminV2.WebApi/Application/FalseOrderUserV2ApplicationService.cs`：符号 `FalseOrderUserV2ApplicationService, ListAsync, Remove, Info, AddAsync, GetOrderPunishTypeEnum, GetPunishRuleEnum, ValidateAdd`；风险 `login, order-state, permission, user-data`
- `HX.Matching.AdminV2.WebApi/Application/HxPlatformTenantWalletV2ApplicationService.cs`：符号 `HxPlatformTenantWalletV2ApplicationService, AdjustAsync, LedgerListAsync`；风险 `funds, login, permission, user-data`
- `HX.Matching.AdminV2.WebApi/Application/HxStatsOrderV2ApplicationService.cs`：符号 `HxStatsOrderV2ApplicationService, ListAsync, ExportAsync, QueryExport, DownloadAsync`；风险 `login, order-state, permission, user-data`
- `HX.Matching.AdminV2.WebApi/Application/HxStatsSupplierRechargeV2ApplicationService.cs`：符号 `HxStatsSupplierRechargeV2ApplicationService, ListAsync, ExportAsync, QueryExport, DownloadAsync`；风险 `funds, login, permission, user-data`
- `HX.Matching.AdminV2.WebApi/Application/HxStatsSupplierWithdrawalV2ApplicationService.cs`：符号 `HxStatsSupplierWithdrawalV2ApplicationService, ListAsync, ExportAsync, QueryExport, DownloadAsync`；风险 `funds, login, permission, user-data`
- `HX.Matching.AdminV2.WebApi/Application/LoginV2ApplicationService.cs`：符号 `LoginV2ApplicationService, LoginAsync, LogoutAsync, GetInfoAsync, GetRoutersAsync, CaptchaImageAsync, SlideCaptchaGenAsync, SlideCaptchaCheckAsync`；风险 `login, permission, user-data`
- `HX.Matching.AdminV2.WebApi/Application/MerchantUserLabelV2ApplicationService.cs`：符号 `MerchantUserLabelV2ApplicationService, ListAsync, MerchantListAsync, Info, AddUser, AddUserListAsync, EditUserListAsync, EditUser`；风险 `login, permission, user-data`
- `HX.Matching.AdminV2.WebApi/Application/MerchantUserWithdrawalV2ApplicationService.cs`：符号 `MerchantUserWithdrawalV2ApplicationService, ListAsync, ExportAsync, QueryExport, DownloadAsync`；风险 `funds, login, permission, user-data`
- `HX.Matching.AdminV2.WebApi/Application/OrderRechargeV2ApplicationService.Ops.cs`：符号 `OrderRechargeV2ApplicationService, CertificateAsync, ProcessAsync, CancelAsync, PlatformEnumAsync, PlatformEnumV1Async, UserOrdersAsync, UploadVoucherAsync`；风险 `funds, login, order-state, permission, user-data`
- `HX.Matching.AdminV2.WebApi/Application/OrderRechargeV2ApplicationService.cs`：符号 `OrderRechargeV2ApplicationService, PageListAsync, ApplyEsSupplementFlags, LabelsAsync, InfoAsync, LogAsync, PaymentAsync, CallbackAsync`；风险 `funds, login, order-state, permission, user-data`
- `HX.Matching.AdminV2.WebApi/Application/OrderWithdrawalV2ApplicationService.Ops.cs`：符号 `OrderWithdrawalV2ApplicationService, PageListAsync, PriorityListAsync, TgPostListAsync, InfoAsync, LogAsync, PaymentAsync, CallbackAsync`；风险 `funds, login, order-state, permission, user-data`
- `HX.Matching.AdminV2.WebApi/Application/OrderWithdrawalV2ApplicationService.cs`：符号 `OrderWithdrawalV2ApplicationService`；风险 `funds, login, order-state, permission, user-data`
- `HX.Matching.AdminV2.WebApi/Controllers/Hx/HxFalseOrderUserController.cs`：符号 `HxFalseOrderUserController, List, Remove, Info, AddUser, GetOrderPunishTypeEnum, GetPunishRuleEnum`；风险 `login, order-state, permission, user-data`
- `HX.Matching.AdminV2.WebApi/Controllers/Hx/HxMerchantUserWithdrawalController.cs`：符号 `HxMerchantUserWithdrawalController, List, Export, QueryExport, Download`；风险 `funds, login, permission, user-data`
- `HX.Matching.AdminV2.WebApi/Controllers/Hx/HxOrderRechargeController.cs`：符号 `HxOrderRechargeController, List, Labels, Info, Log, Payment, Certificate, Callback`；风险 `funds, login, order-state, permission, user-data`
- `HX.Matching.AdminV2.WebApi/Controllers/Hx/HxOrderWithdrawalController.cs`：符号 `HxOrderWithdrawalController, List, PriorityList, TgPostList, Info, Log, Payment, Callback`；风险 `funds, login, order-state, permission, user-data`
- 还有 283 个文件只写入 SQLite 控制面；本会话没用 `tools/rag_index.py search` 或 SQL 查过时，不能当作已读取。

## 跳过项

- pruned_directory 6 项、sensitive_file 6 项；明细只写入 `rag/init-reports/20261018T083109Z-project-hx-matching.json`。

## 使用约束

- 本摘要只用于定位和初筛，不替代当前源码。
- 判断真实行为、接口契约、状态变更或兼容逻辑时，必须回读业务仓当前源码。
- 风险标签按路径关键词自动打，可能多打或漏打；高风险判断以源码和项目档案为准。
