---
summary_id: api-cert-down
project_id: betwin
file_path: Library/Handler/BetWin.Handler.Api/server/cert.cs
file_hash: 54415a82bdd320d279fcfab6bddaf890ee4795df0c4048834599a6ef14bbdd8b
symbol: cert.down
symbol_kind: method
role_scope:
  - "@@BA"
  - "@@BE"
  - reviewer
  - "@@DEVOPS"
task_scope:
  - 缺陷定位
  - 事故处理
  - 代码审查
risk_tags:
  - permission
  - production-config
  - secret
last_seen: 2026-10-18
---

# Api 证书下载深挖卡

## 职责

按 Name/ID 查询 `SiteCert`，按 Type 输出证书正文或私钥；默认分支还可输出内嵌资源。

## 契约

- 输入：`Name`、`ID`、`Type`（cert/key/其他）。
- 输出：证书 PEM/内容或私钥明文写入 Response。
- 失败契约：找不到证书返回 404；方法上未见登录、权限、IP 白名单约束。

## 依赖

- DB：`SiteCert`
- Redis：无
- ES：无
- MQ：无
- 第三方：无

## 状态与副作用

只读数据库并写出敏感材料；无写库，但私钥一旦输出即为泄露。

## 验证提示

1. 未登录请求 `Type=key` 是否仍能拿到私钥。
2. 仅知 Name 或 ID 是否可枚举下载。
3. Api 面分发层是否有统一鉴权（见 ResultInterface `case "Api"`）。

## 风险说明

- Api 请求面分发为空实现，本接口无额外鉴权。
- 关联事故：`inc-2026-10-18-betwin-api-cert-unauth`。
- 同族：管理员 token 缓存不校验站点；管理日志可能含敏感 POST。

## 源码回读要求

改证书管理、Api 鉴权或站点私钥存放前必须回读本文件与 `ResultInterface`。
