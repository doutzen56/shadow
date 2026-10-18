---
summary_id: admin-token-site-binding
project_id: betwin
file_path: Library/BetWin/Agent/Sites/AdminAgent.cs
file_hash: 9b8b89c5b33fa73070d74f177a9ba06ebec9cb791795269f5769af72b4166b77
symbol: AdminAgent.GetAdminInfo
symbol_kind: method
role_scope:
  - "@@BA"
  - "@@BE"
  - reviewer
task_scope:
  - 缺陷定位
  - 代码审查
risk_tags:
  - permission
  - login
  - user-data
last_seen: 2026-10-18
---

# 站点管理员 Token 站点绑定深挖卡

## 职责

从 Cookie/Header Token 解析管理员 ID，读缓存或 DB 得到当前站点管理员。

## 契约

- 输入：`BetModule.TOKEN`。
- 输出：`SiteAdmin` 或 null。
- 失败契约：token 空或 adminId=0 返回 null。DB 路径 `GetAdminInfo(adminId)` 带 `SiteID == SiteInfo.ID`；缓存命中路径直接返回 `AdminCaching.GetAdminInfo(adminId)`，不比对当前站点。

## 依赖

- DB：`SiteAdmin`
- Redis：`AdminCaching` token 与 admin info
- ES：无
- MQ：无
- 第三方：无

## 状态与副作用

只读会话；若跨站点复用同一 adminId 缓存，可能在错误站点上下文拿到管理员对象，放大越权面。

## 验证提示

1. 站点 A 登录拿到 token 后，切换到站点 B 域名是否仍能通过 Admin 门禁。
2. 缓存命中与缓存未命中两条路径的 SiteID 校验是否一致。
3. 退出登录是否删除 token 与 admin info。

## 风险说明

- 这是权限边界缺陷候选，需结合 Handler.Admin 门禁与多站点域名解析一起验证，不能单靠本卡断定已可跨站操作。
- 与 Api 证书无鉴权同属权限族问题。

## 源码回读要求

改后台登录、token 或站点上下文前必须回读 `AdminAgent` 与 `AdminCaching`。
