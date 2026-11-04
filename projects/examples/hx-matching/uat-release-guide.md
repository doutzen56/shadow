# HX.Matching UAT 发布指南

> 依据：业务仓 `deploy/jenkins/`、`Sql/`、项目档案 `flows-multitenant-and-review.md`、当前源码。  
> 知识只写 SimpX；业务仓不落本文件副本。  
> 密钥、口令、连接串只在 AgileConfig / 运维密钥库配置，本文用占位符，不写真实密文。

## 0. 发布前先确认

| 项 | 说明 |
| --- | --- |
| 目标环境 | `uat`（本项目只有 `dev` / `uat`，没有单独 test 环境名） |
| 源码仓 | `HX.Matching.sln` 所在业务仓；Jenkins 源：`http://47.83.190.30:6610/Hx/HX.MatchingApi.git` |
| 分支 | 发 UAT 前确认要发的业务分支已合入流水线检出的分支（常见 `master` 或约定发版分支） |
| 本轮是否含 AdminV2 | **注意**：`Jenkinsfile.orchestrator` **不**推 AdminV2；AdminV2 要用 Docker 热更脚本或单独发布通道 |
| 权限 | 需要 Jenkins 凭据、AgileConfig UAT 环境编辑权、SQL Server / Redis / ES / MQ 运维口 |

### 本轮建议重点验证的改动（若本次包里包含）

1. `UpDownRulelist`：V2 `data` 对齐 V1 Task 形态（标签在 `data.result`）。
2. `GET /api/v2/telegrambotconfig/{id}`：编辑机器人详情；总后台须带 `TenantCode` 或 `X-Tenant-Code`。
3. Quartz 多租户 Job：切租户前懒加载注册 `t_{TenantCode}` 连接（避免 `ConfigId was not found t_T0002`）。

---

## 1. 宿主与发布产物对照

| 宿主 | 项目 | AgileConfig appId（见各宿主 appsettings） | UAT 部署仓 / 方式 | 典型端口（测试 Docker 参考） |
| --- | --- | --- | --- | --- |
| 管理端 V1 | `HX.Matching.Admin.WebApi` | `HX.Matching.Admin.WebApi` | GitHub `irenixf/HxAdminApi` → 分支 `uat` | 8888 |
| 管理端 V2 | `HX.Matching.AdminV2.WebApi` | `HX.Matching.AdminV2.WebApi` | **不在 orchestrator**；用 `jenkins-fast-deploy.sh` 或等价 Docker 发布 | 9000 |
| 商户后台 | `HX.Matching.MerchantAdmin.WebApi` | `HX.Matching.MerchantAdmin.WebApi` | `irenixf/HxMerchantAdminApi` → `uat` | 8889 |
| 商户开放口 | `HX.Matching.MerchantApi` | `HX.Matching.MerchantApi` | `irenixf/HxMerchantOpenApi` → `uat` | 8890 |
| 合作方口 | `HX.Matching.PartnerApi` | `HX.Matching.MerchantApi`（与开放口共配置族，以仓内 appsettings 为准） | `irenixf/HXMatchingPartnerApi` → `uat` | 8892 |
| 定时任务 / Bot | `HX.Matching.Quartz.Job` | 当前仓内挂 `HX.Matching.Admin.WebApi`（与 V1 管理端同 app；改 env 时与运维确认） | `irenixf/HxMatchingQuartzJob` → `uat` | 8891 |

流水线证据：`deploy/jenkins/Jenkinsfile.orchestrator`（推 `uat`）、`Jenkinsfile.promote`（`uat`→`master` 上线）、`deploy/jenkins/README.md`、`README.docker-deploy.md`。

---

## 2. 完整发布流程（UAT）

### 阶段 A：准备（发版前 1 天～当天上午）

1. **冻结范围**  
   - 列出要发的宿主：至少 AdminV1 / MerchantAdmin / MerchantApi / PartnerApi / Quartz；若用多租户后台，必须单独安排 **AdminV2**。  
   - 确认前端 Admin Web 是否同步发（前端仓不在本后端 orchestrator 里）。

2. **AgileConfig 切到 UAT 配置**  
   - 各 appId 在配置中心准备好 **UAT** 环境配置（或把进程 `AgileConfig:env` 指到 UAT）。  
   - 本地 `appsettings.json` 里的 `env` 只是默认；UAT 容器/部署包应以环境变量或配置中心为准，避免仍指向 DEV。

3. **基础设施连通**（`@@DEVOPS`）  
   - SQL Server：公共库 + 首租户库 + 已 Ready 租户库  
   - Redis、RabbitMQ、Elasticsearch、对象存储（若用）  
   - 记录连通证据：能连上、能读写最小探针

4. **数据库脚本**（按本轮是否开多租户 / 是否有增量）  
   - 公共库：`Sql/public_baseline.sql`、`Sql/public_seed.sql`（及本轮新增 public 脚本）  
   - 租户业务库：`Sql/tenant_baseline.sql`、`Sql/tenant_seed.sql`、`Sql/tenant_deploy.sql`（增量）  
   - 任务种子：`Sql/Job_V2_sys_tasks.sql` 等（确认 `tenant-biz-stats-sync`、`tenant-stats-daily` 等 IsStart）  
   - **禁止**对公共库误跑 `tenant_deploy.sql`（脚本会拦）  
   - 生产/UAT 默认勿自动 DDL：`AllowAutoApplyDdl` 保持关，除非运维明确允许

5. **多租户与域名**（开了 `MultiTenant:Enabled=true` 时必做）  
   - 公共库 `hx_domain_binding`：总后台 / 租户后台 / 商户后台 / 商户下单 / 收银台域名齐套  
   - 渠道回调站点根绑在「商户下单」域名上（不拼路径租户段）  
   - `hx_tenant_db_route`：每个 Ready 租户有正确 `ConfigId`（如 `t_T0002`）、`DbName`  
   - UAT 若 `ASPNETCORE_ENVIRONMENT=Development`，确认 **`MultiTenant:DevHostOverride` 为空**，否则所有 Host 被盖住

6. **实例与 CAP**  
   - 各容器 `INSTANCE_WORK_ID` **全局不重复**（AdminV2 默认参考 16，勿与 V1/开放口冲突）  
   - V2 策略：`MqRequireTenantHeader` 保持 false（缺头回落首租户）

### 阶段 B：构建推 UAT（后端五仓）

1. Jenkins Job（Pipeline from SCM）：`deploy/jenkins/Jenkinsfile.orchestrator`  
2. 凭据：`gitlab-hx-matching`、`github-deploy-token`  
3. 行为：编译 + `dotnet publish`，并行推五个部署仓的 **`uat` 分支**（不动 `master`）  
4. 版本：Build 注入 `InformationalVersion=yyyy.MM.dd.{BUILD_NUMBER}`；部署后查 `GET /version`  
5. 忽略：`world.xdb` 不进部署 Git；服务器应用根目录需自备 `world.xdb`

### 阶段 C：AdminV2（若本轮要发）

任选其一（与运维现状对齐）：

- **测试机 Docker 热更同源脚本**（UAT 若也是 Docker）：  
  `bash deploy/jenkins/jenkins-fast-deploy.sh`  
  关注变量：`ADMIN_V2_API_CONTAINERS`、`ADMIN_V2_API_HOST_PORTS`、`ADMIN_V2_API_WORK_IDS`、`ADMIN_V2_API_ASPNETCORE_ENV`、`ADMIN_V2_API_NETWORK`、`ADMIN_V2_API_MOUNTS`  
  V2 默认每次删容器重建，1Panel 里手改的环境变量下次会被覆盖，要改 Jenkins 变量。

- **若 UAT 也是「部署仓 + 拉镜像/拉文件」**：需运维补 AdminV2 部署仓或手工发布包（当前 orchestrator 未登记）。

### 阶段 D：拉起 / 滚动进程

1. 按部署仓 `uat` 更新各 API 与 Quartz 进程（具体是 K8s / Docker / IIS 以 UAT 现网为准）。  
2. 重启顺序建议：  
   - 先公共依赖（无变更可跳过）  
   - 再 Quartz（避免旧 Job 打新库）  
   - 再开放口 / Partner  
   - 再 MerchantAdmin / AdminV1 / AdminV2  
3. 确认各进程已读到 **UAT** AgileConfig，且 `MultiTenant:Enabled` 与库一致。

### 阶段 E：冒烟（最短集）

| # | 检查 | 期望 |
| --- | --- | --- |
| 1 | 各宿主 `GET /version` | 与当次构建版本一致 |
| 2 | 总后台域名登录 AdminV2 | 200，能进；未绑域则 404「当前域名尚未开通」 |
| 3 | 平台带 `TenantCode`/`X-Tenant-Code` 调租户接口 | 不再 403「请先选择租户」；切到正确库 |
| 4 | `GET /api/v2/hx/UpDownRule/UpDownRulelist?TenantCode=T00xx` | `data.result` 为标签数组 |
| 5 | `GET /api/v2/telegrambotconfig/{id}?TenantCode=T00xx` | 非 404；能回填编辑 |
| 6 | MerchantApi 下单域 Host 定租户 | 商户下单 / 回调落正确租户库 |
| 7 | Quartz：`Job_TenantBizStatsSync` 等分租户 Job | 非首租户不再报 `ConfigId was not found t_Txxxx` |
| 8 | 健康：Redis/MQ/ES 无持续报错 | 日志无连接风暴 |

### 阶段 F：收口

1. 记录：构建号、InformationalVersion、发版人、时间、AgileConfig 变更项、SQL 脚本清单。  
2. 未通过项：环境问题归 `@@DEVOPS`，业务断言归 `@@QA`，超 2 轮升级用户。  
3. **不要**在未经验收时跑 `Jenkinsfile.promote`（那是 `uat`→`master` 生产上线）。

---

## 3. AgileConfig / 进程必配清单（核对表）

每个宿主在 **UAT** 环境至少核对：

| 配置键 / 项 | 说明 |
| --- | --- |
| `AgileConfig:env` | 指向 UAT |
| `MultiTenant:Enabled` | UAT 是否开多租户；与库结构一致 |
| `MultiTenant:PublicDb:ConfigId` / `Conn` | 公共库 |
| `MultiTenant:LegacyTenantCode` / `LegacyConfigId` | 默认 T0001 / `0` |
| `MultiTenant:TenantDbConnectionTemplate` | 含 `{DbName}`，用于懒加载 `t_Txxxx` |
| `MultiTenant:DevHostOverride` | UAT 正式验收应为空 |
| `MultiTenant:AllowAutoApplyDdl` | UAT/生产默认 false |
| DB / Redis / MQ / ES 连接 | 指向 UAT 实例，勿指 DEV |
| CAP / Rabbit 交换机队列 | 与 UAT 隔离，勿打到生产 |
| `INSTANCE_WORK_ID` | 容器环境变量，全局唯一 |
| 日志 / NLog 目标 | UAT 目录或采集端点 |

AdminV2 与 AdminV1 **两个 appId**，配置不要混改。

---

## 4. 数据库与种子（执行顺序建议）

1. 备份 UAT 公共库 + 相关租户库。  
2. 公共库：baseline → seed → 本轮增量。  
3. 首租户 / 各租户库：baseline（新库）或 `tenant_deploy.sql`（存量增量）→ seed。  
4. 核对 `sys_tasks`：需要跑的 Job `IsStart=1`（含租户业务日统计、租户会员日统计等）。  
5. 核对 `hx_tenant.InitStatus=Ready` 与 `hx_tenant_db_route`。  
6. 回滚脚本参考：`Sql/tenant_deploy_rollback.sql`（先停多租户代码或保持 V1）。

---

## 5. Jenkins 与凭据

| 项 | 值 / 说明 |
| --- | --- |
| Orchestrator | `deploy/jenkins/Jenkinsfile.orchestrator` → 推部署仓 `uat` |
| Promote（生产） | `deploy/jenkins/Jenkinsfile.promote` → `uat` 合并到 `master`；**UAT 验收通过前不要跑** |
| 源码凭据 | `gitlab-hx-matching` |
| 部署仓凭据 | `github-deploy-token` |
| 节点 | .NET 9 SDK；需要时 `libicu` 或 `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1` |
| Docker 快速发（偏测试机） | `deploy/jenkins/jenkins-fast-deploy.sh` |

---

## 6. 回滚

| 场景 | 动作 |
| --- | --- |
| 仅代码问题 | 部署仓 `uat` 回退到上一成功提交并重新拉起进程；或 Docker 回上一镜像标签 |
| 脚本已改库 | 按对应 `*_rollback.sql` / 备份还原；先停 Job 与写流量 |
| 配置误指 | AgileConfig UAT 配置回滚发布；重启进程 |
| AdminV2 单独回滚 | 用上一版发布包 / 镜像重建 `hx-matching-admin-v2-api`（或等价服务名） |

回滚后重做阶段 E 冒烟 1～3、7。

---

## 7. 责任分工（SimpX）

| 角色 | 职责 |
| --- | --- |
| `@@DEVOPS` | 环境、配置、部署、连通、冒烟、回滚 |
| `@@QA` | 业务断言、场景验收（资金/订单/权限/登录等） |
| `@@TL` | 看两边证据决定是否放行 UAT / 是否允许后续 promote |

---

## 8. 证据索引（写本文时用过的路径）

- `deploy/jenkins/README.md`
- `deploy/jenkins/README.docker-deploy.md`
- `deploy/jenkins/Jenkinsfile.orchestrator`
- `deploy/jenkins/Jenkinsfile.promote`
- `Sql/tenant_deploy.sql`
- `projects/examples/hx-matching/flows-multitenant-and-review.md`
- `projects/examples/hx-matching/envs.yaml`
- 各宿主 `appsettings.json` 中 `AgileConfig.appId`
