---
summary_id: result-interface-api-auth
project_id: betwin
file_path: Library/BetWin.Handler/HandlerBase/ResultInterface.cs
file_hash: 88dcb58891cc2c3e0178082039ee60878280af62478b4085d4ac6c1e4b824bc8
symbol: ResultInterface.Execute / case Api
symbol_kind: method
role_scope:
  - "@@BA"
  - "@@BE"
  - reviewer
task_scope:
  - 缺陷定位
  - 方案设计
  - 代码审查
risk_tags:
  - permission
  - login
  - callback
last_seen: 2026-10-18
---

# ResultInterface Api 面鉴权深挖卡

## 职责

按 Handler 程序集类型（Request/Admin/System/Api）做登录与权限门禁，再调用具体方法；可选页面缓存。

## 契约

- 输入：当前请求上下文、目标方法上的 Guest/权限特性。
- 输出：通过门禁则执行方法；未登录/无权限则 ShowError。
- 失败契约：Request/Admin/System 有明确登录与权限分支；`case "Api":` 为空，等于 Api 面默认放行，依赖各方法自行保护。

## 依赖

- DB：间接（各 Handler/Agent）
- Redis：`PageCaching`（可选）
- ES：无
- MQ：无
- 第三方：游戏/支付回调常走 Api 面

## 状态与副作用

本文件本身不改业务状态；门禁缺失会放大下游敏感接口（证书、运维节点、回调）风险。

## 验证提示

1. 列出 Api 程序集全部公开方法，逐个确认是否自带验签/IP/密钥。
2. 无鉴权方法是否可从公网或内网任意调用。
3. Guest 特性是否被误标在资金或运维方法上。

## 风险说明

- Api 默认放行是平台级权限边界问题，不只是单个 cert 接口。
- 分发基座位于 `Library/BetWin.Handler`，已纳入 handlers 摘要分组 roots。

## 源码回读要求

新增 Api 方法或改分发门禁前必须回读本文件。
