# 数据访问与定租

<!-- 由 tools/rag_index.py 生成，下次导入会覆盖；要改职责说明，改脚本里 PROJECTS 的分组定义。 -->

- project_id: `simpx-net`
- summary_group: `data-tenant`
- host_or_layer: `data-access`
- solution_file: `SimpX.Net/SimpX.Net.sln`
- last_ingestion_run: `20261028T041009Z-project-simpx-net`
- file_count: 39
- project_count: 1
- risk_tags: `cache-consistency`, `database-schema`, `user-data`

## 职责摘要

SqlSugar、仓储、SysTenant、定租解析与当前租户访问器。

## 扫描根目录

- `SimpX.Net/SimpX.Net.Data`

## 项目文件

- `SimpX.Net/SimpX.Net.Data/SimpX.Net.Data.csproj`

## 定位样例

按路径里的高风险关键词优先挑选，最多列 50 个。

- `SimpX.Net/SimpX.Net.Data/Abstractions/IDataChangeLogWriter.cs`：符号 `IDataChangeLogWriter, DataChangeLog`；风险 `cache-consistency, database-schema, user-data`
- `SimpX.Net/SimpX.Net.Data/Abstractions/IDataScopeProvider.cs`：符号 `IDataScopeProvider`；风险 `cache-consistency, database-schema, user-data`
- `SimpX.Net/SimpX.Net.Data/Abstractions/ITenantDbConnectionProvider.cs`：符号 `ITenantDbConnectionProvider`；风险 `cache-consistency, database-schema, user-data`
- `SimpX.Net/SimpX.Net.Data/Attribute/IgnoreTableAttribute.cs`：符号 `IgnoreTableAttribute`；风险 `cache-consistency, database-schema, user-data`
- `SimpX.Net/SimpX.Net.Data/Attribute/IgnoreUpdateSeedAttribute.cs`：符号 `IgnoreUpdateSeedAttribute`；风险 `cache-consistency, database-schema, user-data`
- `SimpX.Net/SimpX.Net.Data/Attribute/IgnoreUpdateSeedColumnAttribute.cs`：符号 `IgnoreUpdateSeedColumnAttribute`；风险 `cache-consistency, database-schema, user-data`
- `SimpX.Net/SimpX.Net.Data/Attribute/IncreSeedAttribute.cs`：符号 `IncreSeedAttribute`；风险 `cache-consistency, database-schema, user-data`
- `SimpX.Net/SimpX.Net.Data/Attribute/IncreTableAttribute.cs`：符号 `IncreTableAttribute`；风险 `cache-consistency, database-schema, user-data`
- `SimpX.Net/SimpX.Net.Data/Attribute/LogTableAttribute.cs`：符号 `LogTableAttribute`；风险 `cache-consistency, database-schema, user-data`
- `SimpX.Net/SimpX.Net.Data/Attribute/OwnerOrgAttribute.cs`：符号 `OwnerOrgAttribute`；风险 `cache-consistency, database-schema, user-data`
- `SimpX.Net/SimpX.Net.Data/Attribute/OwnerUserAttribute.cs`：符号 `OwnerUserAttribute`；风险 `cache-consistency, database-schema, user-data`
- `SimpX.Net/SimpX.Net.Data/Attribute/SeedDataAttribute.cs`：符号 `SeedDataAttribute`；风险 `cache-consistency, database-schema, user-data`
- `SimpX.Net/SimpX.Net.Data/Attribute/SysTableAttribute.cs`：符号 `SysTableAttribute`；风险 `cache-consistency, database-schema, user-data`
- `SimpX.Net/SimpX.Net.Data/Attribute/TableModuleAttribute.cs`：符号 `TableModuleAttribute`；风险 `cache-consistency, database-schema, user-data`
- `SimpX.Net/SimpX.Net.Data/Cache/SqlSugarCacheReadThroughScope.cs`：符号 `SqlSugarCacheReadThroughScope, Restore, Begin, Dispose`；风险 `cache-consistency, database-schema, user-data`
- `SimpX.Net/SimpX.Net.Data/Const/SqlSugarConst.cs`：符号 `SqlSugarConst`；风险 `cache-consistency, database-schema, user-data`
- `SimpX.Net/SimpX.Net.Data/Entity/EntityBase.cs`：符号 `EntityBaseId, EntityBase, EntityBaseDel, EntityBaseOrg, EntityBaseOrgDel, EntityBaseTenant, EntityBaseTenantDel, EntityBaseTenantId`；风险 `cache-consistency, database-schema, user-data`
- `SimpX.Net/SimpX.Net.Data/Entity/IEntityFilter.cs`：符号 `IDeletedFilter, ITenantIdFilter, IOrgIdFilter`；风险 `cache-consistency, database-schema, user-data`
- `SimpX.Net/SimpX.Net.Data/Entity/SysTenant.cs`：符号 `SysTenant`；风险 `cache-consistency, database-schema, user-data`
- `SimpX.Net/SimpX.Net.Data/Extension/RepositoryExtension.cs`：符号 `RepositoryExtension, RunWithoutFilter`；风险 `cache-consistency, database-schema, user-data`
- `SimpX.Net/SimpX.Net.Data/Extension/SqlSugarExtension.cs`：符号 `SqlSugarExtension, CombineFilter, GetValidFilter, CreateFilterExpression, GetStringFromJsonElement, GeValuetExpression, ChangeType, GetPropertyExpression`；风险 `cache-consistency, database-schema, user-data`
- `SimpX.Net/SimpX.Net.Data/Extension/SqlSugarFilterExtension.cs`：符号 `SqlSugarFilterExtension`；风险 `cache-consistency, database-schema, user-data`
- `SimpX.Net/SimpX.Net.Data/GlobalUsings.cs`：符号 `未提取到符号`；风险 `cache-consistency, database-schema, user-data`
- `SimpX.Net/SimpX.Net.Data/Option/DbConnectionOptions.cs`：符号 `DbConnectionOptions, DbConnectionConfig, DbSettings, TableSettings, SeedSettings, PostConfigure`；风险 `cache-consistency, database-schema, user-data`
- `SimpX.Net/SimpX.Net.Data/SimpX.Net.Data.csproj`：符号 `未提取到符号`；风险 `cache-consistency, database-schema, user-data`
- `SimpX.Net/SimpX.Net.Data/SqlSugar/ISqlSugarEntitySeedData.cs`：符号 `ISqlSugarEntitySeedData`；风险 `cache-consistency, database-schema, user-data`
- `SimpX.Net/SimpX.Net.Data/SqlSugar/ISqlSugarRepository.cs`：符号 `ISqlSugarRepository`；风险 `cache-consistency, database-schema, user-data`
- `SimpX.Net/SimpX.Net.Data/SqlSugar/ISqlSugarView.cs`：符号 `ISqlSugarView, GetQueryableSqlString`；风险 `cache-consistency, database-schema, user-data`
- `SimpX.Net/SimpX.Net.Data/SqlSugar/SqlSugarFilter.cs`：符号 `SqlSugarFilter, IEntityFilter, LoadCurrentUserOrgIds, DeleteUserOrgCache, DeleteAllUserDataScopeCache, DeleteCustomCache, SetOrgEntityFilter, SetDataScopeFilter`；风险 `cache-consistency, database-schema, user-data`
- `SimpX.Net/SimpX.Net.Data/SqlSugar/SqlSugarPagedList.cs`：符号 `SqlSugarPagedList, SqlSugarPagedExtensions`；风险 `cache-consistency, database-schema, user-data`
- `SimpX.Net/SimpX.Net.Data/SqlSugar/SqlSugarRepository.cs`：符号 `SqlSugarRepository, SplitTableInsertAsync, SplitTableUpdateAsync, SplitTableDeleteableAsync, SplitTableGetFirstAsync, SplitTableIsAnyAsync, SplitTableGetListAsync`；风险 `cache-consistency, database-schema, user-data`
- `SimpX.Net/SimpX.Net.Data/SqlSugar/SqlSugarSetup.cs`：符号 `SqlSugarSetup, AddSqlSugar, SetDbConfig, SetDbAop, SetDbDiffLog, InitView, WaitForDatabaseReady, InitDatabase`；风险 `cache-consistency, database-schema, user-data`
- `SimpX.Net/SimpX.Net.Data/SqlSugar/SqlSugarTypeProvider.cs`：符号 `SqlSugarTypeProvider, GetCustomTypes`；风险 `cache-consistency, database-schema, user-data`
- `SimpX.Net/SimpX.Net.Data/SqlSugar/SqlSugarUnitOfWork.cs`：符号 `SqlSugarUnitOfWork, BeginTransactionAsync, CommitTransactionAsync, RollbackTransactionAsync, OnCompletedAsync`；风险 `cache-consistency, database-schema, user-data`
- `SimpX.Net/SimpX.Net.Data/Tenant/HttpCurrentTenantAccessor.cs`：符号 `HttpCurrentTenantAccessor, RequireTenantId`；风险 `cache-consistency, database-schema, user-data`
- `SimpX.Net/SimpX.Net.Data/Tenant/ICurrentTenantAccessor.cs`：符号 `ICurrentTenantAccessor`；风险 `cache-consistency, database-schema, user-data`
- `SimpX.Net/SimpX.Net.Data/Tenant/ITenantHostResolver.cs`：符号 `ITenantHostResolver`；风险 `cache-consistency, database-schema, user-data`
- `SimpX.Net/SimpX.Net.Data/Tenant/TenantHostResolver.cs`：符号 `TenantHostResolver, ResolveTenantIdAsync, ResolveTenantAsync`；风险 `cache-consistency, database-schema, user-data`
- `SimpX.Net/SimpX.Net.Data/Utils/SqlSecurityHelper.cs`：符号 `SqlSecurityHelper, IsValidIdentifier, ValidateIdentifier, IsValidProcedureName, ValidateProcedureName, IsSafeSqlExpression, ValidateSqlExpression, ValidateDefaultValue`；风险 `cache-consistency, database-schema, user-data`

## 跳过项

- pruned_directory 2 项；明细只写入 `rag/init-reports/20261028T041009Z-project-simpx-net.json`。

## 使用约束

- 本摘要只用于定位和初筛，不替代当前源码。
- 判断真实行为、接口契约、状态变更或兼容逻辑时，必须回读业务仓当前源码。
- 风险标签按路径关键词自动打，可能多打或漏打；高风险判断以源码和项目档案为准。
