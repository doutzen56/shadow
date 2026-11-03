# docker 部署

四个宿主（Admin、Schedule、WebApi、Channels）加 nginx、SQL Server、Redis。四个宿主共用一台 SQL Server、一个 Redis。

## 库怎么分（中心库 + 租户库）

产品约定是：**中心库（总后台）+ 凡租户独立库**（`TenantType=Db`），不是按域/按宿主分库。

| 库 | 谁用 | docker 默认怎么配 |
| --- | --- | --- |
| 中心库 | 四个宿主都连这一条（租户元数据、作业、系统表等） | `shared/Secrets.json` 的 `DbConnection.ConnectionConfigs[0]`，示例库名 `SimpX` |
| 租户库 | 按租户在总后台创建/维护连接串，运行时动态挂上 | **不必**写进 compose 的 Shared 配置；Admin 建租户选「独立库」后自动建库/写连接 |

本仓库 compose 只起 **一个** SQL Server 实例：开发时中心库和租户库可以都建在这台实例上（不同 Database 名）。生产可换成多实例，只要把各租户连接串指到对应库即可。

种子默认租户类型是独立库；若暂时仍想「同库按租户 Id 隔离」，可在总后台把该租户改成 Id 模式（兼容保留，不是推荐默认）。

## 目录

| 目录或文件 | 用途 |
| --- | --- |
| `.env.example` | compose 用的密码模板，复制成 `.env` 再填 |
| `shared/` | 四个宿主共用的部署配置，挂到容器的 `/app/Deploy/Shared` |
| `shared/Database.json` | 中心库类型与连接占位；默认关 Init/Seed（只连不初始化） |
| `shared/Secrets.json.example` | 密钥和连接串模板，复制成 `shared/Secrets.json` 再填 |
| `admin/Database.json` | Admin 覆盖：对中心库开 Init/Table/Seed（唯一初始化者） |
| `admin/`、`schedule/`、`channels/` | 各宿主自己的部署配置，挂到容器的 `/app/Deploy/Host` |
| `nginx/`、`redis/` | nginx、Redis 配置 |
| `wait-for-it.sh` | 等前一个服务的端口通了再启动 |

宿主启动时依次读取 `Configuration`（编译输出里的完整配置）、`Deploy/Shared`、`Deploy/Host`，后读到的同名配置盖掉前面的，同名文件也会盖。所以部署目录里只写要改的那几项。

`.env` 和 `shared/Secrets.json` 不进 git。

## 第一次部署

1. 复制 `.env.example` 为 `.env`，填 SQL Server 的 sa 密码和 Redis 密码。
2. 复制 `shared/Secrets.json.example` 为 `shared/Secrets.json`，把每一项都换成自己生成的值，数据库和 Redis 密码要和 `.env` 一致。
   - 生产环境还在用仓库默认的 JWT、国密、内部调用、看板密钥时，宿主会直接启动失败。
   - 国密密钥要成对生成，公钥同步改到 `Web/.env` 的 `VITE_SM_PUBLIC_KEY`，再编译前端。
3. 按需改 `channels/Payment.json` 的 `NotifyBaseUrl`，填外网能访问到的地址。
4. 编译：
   - 后端：`docker compose -f docker-compose-builder.yml up net-builder`
   - 前端：`docker compose -f docker-compose-builder.yml up web-builder`
   - 编完清掉编译容器：`docker compose -f docker-compose-builder.yml down`
5. 启动：`docker compose up -d`。四个宿主按 Admin、Schedule、WebApi、Channels 的顺序依次启动，库表和种子数据由第一个启动的 Admin 建好。

## 注意

- 容器里编译后端会改写 `obj` 目录，回到本机开发前先执行一次 `dotnet restore`。
- 四个宿主不直接对外开放端口，统一经 nginx 访问：`http://ip:9100`、`https://ip:9103`。调度宿主只经 nginx 开放调度看板。
- `nginx/key` 里是示例证书，正式环境换成自己的证书。
- SQL Server 默认是 Developer 版，只能用于开发和测试。
- 前端 `pnpm build` 默认产物在 `Web/apps/admin/dist`；`deploy-test.sh` 会同步到 `Web/dist` 供 nginx 挂载。总后台 super-admin 与 SuperAdmin 宿主尚未进本 compose，后续单独加。

## GitLab 自动部署（测试环境 / `dev` 分支）

推送 `dev` 后，测试机上的 Runner 跑仓库根目录的 `.gitlab-ci.yml` → `docker/deploy-test.sh`：编译 → 同步前端 → `docker compose up -d` → 健康检查 → Telegram 通知。

### 测试机一次性准备

1. 安装 Docker（含 Compose V2）、curl、git；把本仓库 clone 到固定目录（或交给 Runner 自动拉）。
2. 在 `docker/` 下按「第一次部署」准备好 `.env` 和 `shared/Secrets.json`（只留在机器上，不进 Git）。
3. 注册 **GitLab Runner（shell 执行器）**，打标签 `simpx-test`，Runner 用户要能跑 `docker` / `docker compose`。
4. 在 GitLab 项目 **Settings → CI/CD → Variables** 增加（敏感项勾 Masked / Protected 按你库策略）：

| 变量 | 必填 | 说明 |
| --- | --- | --- |
| `TELEGRAM_BOT_TOKEN` | 是 | BotFather 发的 bot token |
| `TELEGRAM_CHAT_ID` | 是 | 目标群/频道 id（群多为负数） |
| `DEPLOY_BASE_URL` | 否 | 通知里展示的访问根，如 `http://测试机IP:9100` |
| `DEPLOY_HEALTH_PATH` | 否 | 健康检查路径，默认 `/` |

5. 确认默认分支策略：只有 `dev` 会进流水线（见 `.gitlab-ci.yml` 的 `workflow.rules`）。

### 本机手动跑同一套脚本

```bash
# 在仓库根目录；需已 export TELEGRAM_*（可选）和 DEPLOY_BASE_URL（可选）
chmod +x docker/deploy-test.sh docker/notify-telegram.sh
./docker/deploy-test.sh
```

### 回滚

在测试机把仓库切到上一成功提交（或 `git reset --hard <sha>`），再执行一次 `./docker/deploy-test.sh`。数据库数据不随脚本回滚；若要回滚库表需另做备份策略。
