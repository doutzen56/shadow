# 模型与仓储

<!-- 由 tools/rag_index.py 生成，下次导入会覆盖；要改职责说明，改脚本里 PROJECTS 的分组定义。 -->

- project_id: `hx-matching`
- summary_group: `data-model-repository`
- host_or_layer: `data-access`
- solution_file: `HX.Matching.sln`
- last_ingestion_run: `20261018T093230Z-project-hx-matching`
- file_count: 374
- project_count: 2
- risk_tags: `cache-consistency`, `callback`, `database-schema`, `funds`, `gateway`, `idempotency`, `login`, `mq`, `order-state`, `permission`, `user-data`

## 职责摘要

实体、枚举、请求响应模型和 SqlSugar 仓储基础能力。

## 扫描根目录

- `HX.Matching.Model`
- `HX.Matching.Repository`

## 项目文件

- `HX.Matching.Model/HX.Matching.Model.csproj`
- `HX.Matching.Repository/HX.Matching.Repository.csproj`

## 定位样例

按路径里的高风险关键词优先挑选，最多列 50 个。

- `HX.Matching.Model/Enum/AuditStatusEnum.cs`：符号 `AuditStatusEnum`；风险 `database-schema, order-state, user-data`
- `HX.Matching.Model/Enum/UserMsgType.cs`：符号 `UserMsgType`；风险 `database-schema, order-state, user-data`
- `HX.Matching.Model/System/Const/PartnerOrderStatus.cs`：符号 `PartnerOrderStatus`；风险 `database-schema, order-state, user-data`
- `HX.Matching.Model/System/Model/Dto/BindGoogleAuthDto.cs`：符号 `BindGoogleAuthDto`；风险 `database-schema, order-state, permission, user-data`
- `HX.Matching.Model/System/Model/Dto/CashierOrderRecharge.cs`：符号 `CashierOrderRecharge`；风险 `database-schema, funds, order-state, user-data`
- `HX.Matching.Model/System/Model/Dto/ChannelOrderDto.cs`：符号 `ChannelOrderDto, ChannelOrderRequest, RechargeChannelOrderRequest, RechargeChannelOrderResponse, WithdrawalChannelOrderRequest, WithdrawalChannelOrderResponse, ChannelOrderBatchQueryRequest, ChannelOrderSelResponse`；风险 `database-schema, gateway, order-state, user-data`
- `HX.Matching.Model/System/Model/Dto/EmailLogDto.cs`：符号 `EmailLogQueryDto, EmailLogDto`；风险 `database-schema, order-state, user-data`
- `HX.Matching.Model/System/Model/Dto/EmailTplDto.cs`：符号 `EmailTplQueryDto, EmailTplDto`；风险 `database-schema, order-state, user-data`
- `HX.Matching.Model/System/Model/Dto/FixedMoneyPlan.cs`：符号 `FixedMoneyPlan, RemainderOf, ToMatchableAmount, IsGroupMode, ParseAmounts, ParseGroups, FilterGroupsByTotal, IsAmountAllowed`；风险 `database-schema, funds, order-state, user-data`
- `HX.Matching.Model/System/Model/Dto/HxBlacklistPayDto.cs`：符号 `HxBlacklistPayDto, HxBlacklistPayQueryDto, HxBlacklistPayImportInput`；风险 `database-schema, funds, order-state, user-data`
- `HX.Matching.Model/System/Model/Dto/HxChannelMoneyDto.cs`：符号 `HxChannelMoneyDto`；风险 `database-schema, funds, gateway, order-state, user-data`
- `HX.Matching.Model/System/Model/Dto/HxFalseOrderRecordDto.cs`：符号 `HxFalseOrderRecordDto, HxFalseOrderRecordQueryDto, HxFalseOrderRecordImportInput`；风险 `database-schema, order-state, user-data`
- `HX.Matching.Model/System/Model/Dto/HxFalseOrderUserDto.cs`：符号 `HxFalseOrderUserDto, HxFalseOrderUserQueryDto`；风险 `database-schema, order-state, user-data`
- `HX.Matching.Model/System/Model/Dto/HxMerchantCreditFundsDetailDto.cs`：符号 `HxMerchantCreditFundsDetailDto, MerchantCreditFundsDetailRequest, MerchantCreditFundsDetailResponse, MerchantCreditFundsDetailInQRequest`；风险 `database-schema, funds, order-state, user-data`
- `HX.Matching.Model/System/Model/Dto/HxMerchantFundsDetailDto.cs`：符号 `HxMerchantFundsDetailDto, MerchantFundsDetailRequest, MerchantFundsDetailQueryRequest, MerchantFundsDetailResponse, MerchantFundsDetailInQRequest, MerchantStatsInfoDto, MerchantFundsDetailResponseV2, MerchantFundsDetailChangeRequest`；风险 `database-schema, funds, order-state, user-data`
- `HX.Matching.Model/System/Model/Dto/HxMerchantUserDto.cs`：符号 `HxMerchantUserDto, HxMerchantUserQueryDto, HxMerchantUserEditDto, HxMerchantUserEditRiskDto, HxMerchantUserRiskTagChangeDto, HxMerchantUserSettlementDto, HxMerchantUserRequest, HxMerchantUserResponse`；风险 `database-schema, order-state, user-data`
- `HX.Matching.Model/System/Model/Dto/HxMerchantUserLabelDto.cs`：符号 `HxMerchantUserLabelDto, HxMerchantUserLabelQueryDto, HxMerchantUserLableDto, DeleteListDto, HxMerchantUserLabelResponse`；风险 `database-schema, order-state, user-data`
- `HX.Matching.Model/System/Model/Dto/HxMerchantUserNameLogDto.cs`：符号 `HxMerchantUserNameLogDto, HxMerchantUserNameLogQueryDto`；风险 `database-schema, order-state, user-data`
- `HX.Matching.Model/System/Model/Dto/HxMerchantUserWithdrawalDto.cs`：符号 `HxMerchantUserWithdrawalQueryDto, HxMerchantUserWithdrawalDto, HxMerchantUserWithdrawalTempDto, HxMerchantUserWithdrawalExport`；风险 `database-schema, funds, order-state, user-data`
- `HX.Matching.Model/System/Model/Dto/HxOrderMatchingSupplierDto.cs`：符号 `HxOrderMatchingSupplierDto, HxOrderMatchingSupplierQueryDto, HxOrderMatchingSupplierStatsDto, HxSupplierPageDto, HxOrderSupplierQueryDto, HxOrderSupplierSubmitDto`；风险 `database-schema, order-state, user-data`
- `HX.Matching.Model/System/Model/Dto/HxStatsMchntUserDto.cs`：符号 `HxStatsMchntUserDto, HxStatsMchntUserQueryDto`；风险 `database-schema, order-state, user-data`
- `HX.Matching.Model/System/Model/Dto/HxStatsOrderDto.cs`：符号 `HxStatsOrderDto, HxStatsOrderQueryDto, HxStatsOrderExport, OrderDailySummaryAggregateRow`；风险 `database-schema, order-state, user-data`
- `HX.Matching.Model/System/Model/Dto/HxStatsOrderLogEsDto.cs`：符号 `HxStatsOrderLogEsDto, MchntGroupKey`；风险 `database-schema, order-state, user-data`
- `HX.Matching.Model/System/Model/Dto/OrderCallbackDto.cs`：符号 `OrderCallbackRequest, OrderCallbackResponse`；风险 `callback, database-schema, order-state, user-data`
- `HX.Matching.Model/System/Model/Dto/OrderRechargeDto.cs`：符号 `OrderRechargeStatsDto, OrderRechargeDto, ManualCallbackRequest, RechargeSupplementApproveRequest, RechargeSupplementApproverDto, ManualFailedRequest, ProcessRechargeOrderRequest, ProcessRechargeOrderV2Request`；风险 `database-schema, funds, order-state, user-data`
- `HX.Matching.Model/System/Model/Dto/OrderWithdrawalDto.cs`：符号 `OrderWithdrawalDto, OrderWithdrawalRequest, MerchantOrderWithdrawalRequest, OrderWithdrawalResponse, OrderWithdrawalResponseV2, MatchingOrderData, OrderWithdrawalPaymentResponse, OrderWithdrawalCountResponse`；风险 `database-schema, funds, order-state, user-data`
- `HX.Matching.Model/System/Model/Dto/RoleUserDto.cs`：符号 `RoleUserQueryDto, RoleUsersCreateDto`；风险 `database-schema, order-state, permission, user-data`
- `HX.Matching.Model/System/Model/Dto/Telegram/TelegramBot/TelegramUpdateUserRoleDto.cs`：符号 `TelegramUpdateUserRoleDto`；风险 `database-schema, order-state, permission, user-data`
- `HX.Matching.Model/System/Model/Dto/Telegram/TelegramBot/UpdateTelegramUserRoleBatchRequest.cs`：符号 `UpdateTelegramUserRoleBatchRequest`；风险 `database-schema, order-state, permission, user-data`
- `HX.Matching.Model/System/Model/Dto/Telegram/TelegramBot/UpdateTelegramUserRoleRequest.cs`：符号 `UpdateTelegramUserRoleRequest`；风险 `database-schema, order-state, permission, user-data`
- `HX.Matching.Model/System/Model/EnterPrisePay/MatchingPayCallbackData.cs`：符号 `MatchingPayCallbackData, MatchingWithdrawCallbackData, MatchingWithdrawData, WithdrawTimeOutCallbackData, CallbackResponse`；风险 `callback, database-schema, funds, order-state, user-data`
- `HX.Matching.Model/System/Model/EnterPrisePay/WithdrawPayCallbackData.cs`：符号 `WithdrawPayCallbackData`；风险 `callback, database-schema, funds, order-state, user-data`
- `HX.Matching.Model/System/Model/Enums/RechargeSupplementApprovalStatusEnum.cs`：符号 `RechargeSupplementApprovalStatusEnum`；风险 `database-schema, funds, order-state, user-data`
- `HX.Matching.Model/System/Model/Enums/RechargeSupplementStatusEnum.cs`：符号 `RechargeSupplementStatusEnum`；风险 `database-schema, funds, order-state, user-data`
- `HX.Matching.Model/System/Model/Es/HxOrderCallbackEs.cs`：符号 `HxOrderCallbackEs`；风险 `callback, database-schema, order-state, user-data`
- `HX.Matching.Model/System/Model/Es/HxOrderRechargeAIResultEs.cs`：符号 `HxOrderRechargeAIResultEs`；风险 `database-schema, funds, order-state, user-data`
- `HX.Matching.Model/System/Model/Es/HxOrderRechargeDocEs.cs`：符号 `HxOrderRechargeDocEs`；风险 `database-schema, funds, order-state, user-data`
- `HX.Matching.Model/System/Model/Es/HxOrderWithdrawalDocEs.cs`：符号 `HxOrderWithdrawalDocEs`；风险 `database-schema, funds, order-state, user-data`
- `HX.Matching.Model/System/Model/Es/OrderRechargeQueryDocEs.cs`：符号 `OrderRechargeQueryDocEs`；风险 `database-schema, funds, order-state, user-data`
- `HX.Matching.Model/System/Model/Es/OrderWithdrawalQueryDocEs.cs`：符号 `OrderWithdrawalQueryDocEs`；风险 `database-schema, funds, order-state, user-data`
- `HX.Matching.Model/System/Model/HxFalseOrderUser.cs`：符号 `HxFalseOrderUser`；风险 `database-schema, order-state, user-data`
- `HX.Matching.Model/System/Model/HxMerchantUserOrdercOverview.cs`：符号 `HxMerchantUserOrdercOverview`；风险 `database-schema, order-state, user-data`
- `HX.Matching.Model/System/Model/HxOrderCallback.cs`：符号 `HxOrderCallback`；风险 `callback, database-schema, order-state, user-data`
- `HX.Matching.Model/System/Model/HxOrderMatchingRechargeSupplier.cs`：符号 `HxOrderMatchingRechargeSupplier`；风险 `database-schema, funds, order-state, user-data`
- `HX.Matching.Model/System/Model/HxOrderRecharge.cs`：符号 `HxOrderRecharge`；风险 `database-schema, funds, order-state, user-data`
- `HX.Matching.Model/System/Model/HxOrderRechargeAIRecognitionResult.cs`：符号 `HxOrderRechargeAIRecognitionResult`；风险 `database-schema, funds, order-state, user-data`
- `HX.Matching.Model/System/Model/HxOrderRechargeSupplementApproval.cs`：符号 `HxOrderRechargeSupplementApproval`；风险 `database-schema, funds, order-state, user-data`
- `HX.Matching.Model/System/Model/HxOrderWithdrawal.cs`：符号 `HxOrderWithdrawal`；风险 `database-schema, funds, order-state, user-data`
- `HX.Matching.Model/System/Model/SysUserLoginSession.cs`：符号 `SysUserLoginSession`；风险 `database-schema, login, order-state, user-data`
- `HX.Matching.Model/System/Model/SysUserRole.cs`：符号 `SysUserRole`；风险 `database-schema, order-state, permission, user-data`
- 还有 324 个文件只写入 SQLite 控制面；本会话没用 `tools/rag_index.py search` 或 SQL 查过时，不能当作已读取。

## 跳过项

- pruned_directory 4 项；明细只写入 `rag/init-reports/20261018T093230Z-project-hx-matching.json`。

## 使用约束

- 本摘要只用于定位和初筛，不替代当前源码。
- 判断真实行为、接口契约、状态变更或兼容逻辑时，必须回读业务仓当前源码。
- 风险标签按路径关键词自动打，可能多打或漏打；高风险判断以源码和项目档案为准。
