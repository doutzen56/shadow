# APIJSON 收口 / Core Option / Test 矩阵 / docker 租户库说明（2026-10-27）

## 1. APIJSON

- 曾收口到 Admin-only；随后于 2026-10-27 **整块删除**（服务目录、Options、Admin 配置、前端 `api-json-api` / `jtoken` / `admin-result-jobject`）。
- 无业务页面引用，不影响登录/作业/租户主链路。

## 2. Core Option

当前 Core 只保留横切：`SnowId`、`Cluster`、`Enum`、`Cryptogram`、`LocalizationSettings`。

已归位：

| 选项 | 归属 |
| --- | --- |
| `DbConnectionOptions` | `Data` |
| `EmailOptions` / `EventBusOptions` | 对应 Feature（仍由 ProjectOptions 绑配置，多宿主告警要用） |
| `UploadOptions` | `Feature.Storage`，随 `AddFileStorage` 注册 |
| `APIJSONOptions` | Admin |
| `FileStorageOptions` / `CacheOptions` / ES / Sms / … | 各 Feature / 包自己的 Setup |

非 Admin 宿主的 `Upload.json` 已删（无上传能力点选时不必带）。

## 3. Test 引用矩阵

见业务仓 `SimpX.Net.Test/README.md`。当前只引 `Core`（Utils 单测）；Selenium 不引后端。  
验证：`SafeMath`/`DateTime` 过滤跑通 42 个。

## 4. docker 租户库

`docker/README.md` 增补「中心库 + 租户独立库」说明；`shared/Database.json`、`Secrets.json.example` 注明租户库不进 Shared 枚举、由总后台维护。
