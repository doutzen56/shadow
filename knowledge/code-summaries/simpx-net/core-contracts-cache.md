# 内核、契约与缓存

<!-- 由 tools/rag_index.py 生成，下次导入会覆盖；要改职责说明，改脚本里 PROJECTS 的分组定义。 -->

- project_id: `simpx-net`
- summary_group: `core-contracts-cache`
- host_or_layer: `infrastructure`
- solution_file: `SimpX.Net/SimpX.Net.sln`
- last_ingestion_run: `20261028T041009Z-project-simpx-net`
- file_count: 146
- project_count: 4
- risk_tags: `cache-consistency`, `elasticsearch`, `idempotency`, `login`, `mq`, `order-state`, `permission`, `user-data`

## 职责摘要

常量枚举错误码、作业契约、缓存抽象与可选 ES。

## 扫描根目录

- `SimpX.Net/SimpX.Net.Core`
- `SimpX.Net/SimpX.Net.Contracts.Jobs`
- `SimpX.Net/SimpX.Net.Caching`
- `SimpX.Net/SimpX.Net.ElasticSearch`

## 项目文件

- `SimpX.Net/SimpX.Net.Core/SimpX.Net.Core.csproj`
- `SimpX.Net/SimpX.Net.Contracts.Jobs/SimpX.Net.Contracts.Jobs.csproj`
- `SimpX.Net/SimpX.Net.Caching/SimpX.Net.Caching.csproj`
- `SimpX.Net/SimpX.Net.ElasticSearch/SimpX.Net.ElasticSearch.csproj`

## 定位样例

按路径里的高风险关键词优先挑选，最多列 50 个。

- `SimpX.Net/SimpX.Net.Caching/Abstractions/ICacheInvalidationExecutor.cs`：符号 `ICacheInvalidationExecutor`；风险 `cache-consistency, elasticsearch`
- `SimpX.Net/SimpX.Net.Caching/Abstractions/ICacheReadThroughScope.cs`：符号 `ICacheReadThroughScope`；风险 `cache-consistency, elasticsearch`
- `SimpX.Net/SimpX.Net.Caching/Abstractions/ICacheStateStore.cs`：符号 `ICacheStateStore`；风险 `cache-consistency, elasticsearch`
- `SimpX.Net/SimpX.Net.Caching/Abstractions/ICacheWarmup.cs`：符号 `ICacheWarmup`；风险 `cache-consistency, elasticsearch`
- `SimpX.Net/SimpX.Net.Caching/Abstractions/ISimpXCache.cs`：符号 `ISimpXCache`；风险 `cache-consistency, elasticsearch`
- `SimpX.Net/SimpX.Net.Caching/Attributes/CacheRemoveAttribute.cs`：符号 `CacheRemoveAttribute`；风险 `cache-consistency, elasticsearch`
- `SimpX.Net/SimpX.Net.Caching/Attributes/CacheRemoveMode.cs`：符号 `CacheRemoveMode`；风险 `cache-consistency, elasticsearch`
- `SimpX.Net/SimpX.Net.Caching/CacheOptions.cs`：符号 `CacheOptions, RedisOption, PostConfigure`；风险 `cache-consistency, elasticsearch`
- `SimpX.Net/SimpX.Net.Caching/CacheServiceCollectionExtensions.cs`：符号 `CacheServiceCollectionExtensions, AddCache, RegisterSimpXCacheLayer`；风险 `cache-consistency, elasticsearch`
- `SimpX.Net/SimpX.Net.Caching/CacheTypeEnum.cs`：符号 `CacheTypeEnum`；风险 `cache-consistency, elasticsearch`
- `SimpX.Net/SimpX.Net.Caching/Definitions/CacheDefinition.cs`：符号 `CacheDefinition`；风险 `cache-consistency, elasticsearch`
- `SimpX.Net/SimpX.Net.Caching/Definitions/CacheKind.cs`：符号 `CacheKind`；风险 `cache-consistency, elasticsearch`
- `SimpX.Net/SimpX.Net.Caching/Definitions/CacheRegistry.cs`：符号 `CacheRegistry, Register, GetRequired`；风险 `cache-consistency, elasticsearch`
- `SimpX.Net/SimpX.Net.Caching/Definitions/Modules/AuthCacheKeys.cs`：符号 `AuthCacheKeys, Register`；风险 `cache-consistency, elasticsearch, permission`
- `SimpX.Net/SimpX.Net.Caching/Definitions/Modules/CommonCacheKeys.cs`：符号 `CommonCacheKeys, Register`；风险 `cache-consistency, elasticsearch`
- `SimpX.Net/SimpX.Net.Caching/Definitions/Modules/ConfigCacheKeys.cs`：符号 `ConfigCacheKeys, Register`；风险 `cache-consistency, elasticsearch`
- `SimpX.Net/SimpX.Net.Caching/Definitions/Modules/DictCacheKeys.cs`：符号 `DictCacheKeys, Register`；风险 `cache-consistency, elasticsearch`
- `SimpX.Net/SimpX.Net.Caching/Definitions/Modules/LangCacheKeys.cs`：符号 `LangCacheKeys, Register`；风险 `cache-consistency, elasticsearch`
- `SimpX.Net/SimpX.Net.Caching/Definitions/Modules/PermissionCacheKeys.cs`：符号 `PermissionCacheKeys, Register`；风险 `cache-consistency, elasticsearch, permission`
- `SimpX.Net/SimpX.Net.Caching/Definitions/Modules/TenantCacheKeys.cs`：符号 `TenantCacheKeys, Register`；风险 `cache-consistency, elasticsearch`
- `SimpX.Net/SimpX.Net.Caching/Services/CacheAttributeArgumentResolver.cs`：符号 `CacheAttributeArgumentResolver, Resolve`；风险 `cache-consistency, elasticsearch`
- `SimpX.Net/SimpX.Net.Caching/Services/CacheInvalidationBatch.cs`：符号 `CacheInvalidationBatch, Add, ExecuteAsync, Discard`；风险 `cache-consistency, elasticsearch`
- `SimpX.Net/SimpX.Net.Caching/Services/CacheInvalidationExecutor.cs`：符号 `CacheInvalidationExecutor, ExecuteAsync`；风险 `cache-consistency, elasticsearch`
- `SimpX.Net/SimpX.Net.Caching/Services/CacheKeyBuilder.cs`：符号 `CacheKeyBuilder, Build, BuildNamePrefix`；风险 `cache-consistency, elasticsearch`
- `SimpX.Net/SimpX.Net.Caching/Services/CacheStateStore.cs`：符号 `CacheStateStore, AcquireLock, Increment, SetExpire, Exists, Remove, HashRemove, EnsurePositive`；风险 `cache-consistency, elasticsearch`
- `SimpX.Net/SimpX.Net.Caching/Services/SimpXCache.cs`：符号 `SimpXCache, Remove, RemoveAll, TryLock, BeginReadThrough, SearchKeys, EnsureData, IsEmptyCollection`；风险 `cache-consistency, elasticsearch`
- `SimpX.Net/SimpX.Net.Contracts.Jobs/Entity/SysJobCluster.cs`：符号 `SysJobCluster`；风险 `cache-consistency, elasticsearch`
- `SimpX.Net/SimpX.Net.Contracts.Jobs/Entity/SysJobDetail.cs`：符号 `SysJobDetail`；风险 `cache-consistency, elasticsearch`
- `SimpX.Net/SimpX.Net.Contracts.Jobs/Entity/SysJobTrigger.cs`：符号 `SysJobTrigger`；风险 `cache-consistency, elasticsearch`
- `SimpX.Net/SimpX.Net.Contracts.Jobs/Entity/SysJobTriggerRecord.cs`：符号 `SysJobTriggerRecord`；风险 `cache-consistency, elasticsearch`
- `SimpX.Net/SimpX.Net.Contracts.Jobs/IAdminMaintenanceApi.cs`：符号 `IAdminMaintenanceApi`；风险 `cache-consistency, elasticsearch, permission`
- `SimpX.Net/SimpX.Net.Contracts.Jobs/IJobManagementApi.cs`：符号 `IJobManagementApi`；风险 `cache-consistency, elasticsearch`
- `SimpX.Net/SimpX.Net.Contracts.Jobs/IScheduleDashboardLogin.cs`：符号 `IScheduleDashboardLogin`；风险 `cache-consistency, elasticsearch, login`
- `SimpX.Net/SimpX.Net.Contracts.Jobs/JobCreateTypeEnum.cs`：符号 `JobCreateTypeEnum`；风险 `cache-consistency, elasticsearch`
- `SimpX.Net/SimpX.Net.Contracts.Jobs/JobDetailInput.cs`：符号 `JobDetailInput, PageJobDetailInput, AddJobDetailInput, UpdateJobDetailInput, DeleteJobDetailInput`；风险 `cache-consistency, elasticsearch`
- `SimpX.Net/SimpX.Net.Contracts.Jobs/JobDetailOutput.cs`：符号 `JobDetailOutput`；风险 `cache-consistency, elasticsearch`
- `SimpX.Net/SimpX.Net.Contracts.Jobs/JobManagementRoutes.cs`：符号 `JobManagementRoutes`；风险 `cache-consistency, elasticsearch`
- `SimpX.Net/SimpX.Net.Contracts.Jobs/JobStatusEnum.cs`：符号 `JobStatusEnum`；风险 `cache-consistency, elasticsearch, order-state`
- `SimpX.Net/SimpX.Net.Contracts.Jobs/JobTriggerInput.cs`：符号 `JobTriggerInput, AddJobTriggerInput, UpdateJobTriggerInput, DeleteJobTriggerInput`；风险 `cache-consistency, elasticsearch`
- `SimpX.Net/SimpX.Net.Contracts.Jobs/JobTriggerRecordInput.cs`：符号 `PageJobTriggerRecordInput`；风险 `cache-consistency, elasticsearch`
- `SimpX.Net/SimpX.Net.Core/Attribute/CommonValidationAttribute.cs`：符号 `CommonValidationAttribute, ModelInput, IsValid, CreateCondition`；风险 `cache-consistency, elasticsearch`
- `SimpX.Net/SimpX.Net.Core/Attribute/IdempotentAttribute.cs`：符号 `IdempotentAttribute, ResponseData, OnActionExecutionAsync`；风险 `cache-consistency, elasticsearch, idempotency`
- `SimpX.Net/SimpX.Net.Core/Cache/UserDataScopeCache.cs`：符号 `UserDataScopeCache`；风险 `cache-consistency, elasticsearch, user-data`
- `SimpX.Net/SimpX.Net.Core/Enum/LoginModeEnum.cs`：符号 `LoginModeEnum`；风险 `cache-consistency, elasticsearch, login`
- `SimpX.Net/SimpX.Net.Core/Enum/LoginTypeEnum.cs`：符号 `LoginTypeEnum`；风险 `cache-consistency, elasticsearch, login`
- `SimpX.Net/SimpX.Net.Core/Enum/StatusEnum.cs`：符号 `StatusEnum`；风险 `cache-consistency, elasticsearch, order-state`
- `SimpX.Net/SimpX.Net.Core/Utils/AdminResult.cs`：符号 `AdminResult`；风险 `cache-consistency, elasticsearch, permission`
- `SimpX.Net/SimpX.Net.Core/Utils/AdminResults.cs`：符号 `AdminResults, Ok, Error, Build`；风险 `cache-consistency, elasticsearch, permission`
- `SimpX.Net/SimpX.Net.Core/Utils/BaseStatusInput.cs`：符号 `BaseStatusInput`；风险 `cache-consistency, elasticsearch, order-state`
- `SimpX.Net/SimpX.Net.ElasticSearch/ElasticSearchAuthTypeEnum.cs`：符号 `ElasticSearchAuthTypeEnum`；风险 `cache-consistency, elasticsearch, permission`
- 还有 96 个文件只写入 SQLite 控制面；本会话没用 `tools/rag_index.py search` 或 SQL 查过时，不能当作已读取。

## 跳过项

- pruned_directory 8 项；明细只写入 `rag/init-reports/20261028T041009Z-project-simpx-net.json`。

## 使用约束

- 本摘要只用于定位和初筛，不替代当前源码。
- 判断真实行为、接口契约、状态变更或兼容逻辑时，必须回读业务仓当前源码。
- 风险标签按路径关键词自动打，可能多打或漏打；高风险判断以源码和项目档案为准。
