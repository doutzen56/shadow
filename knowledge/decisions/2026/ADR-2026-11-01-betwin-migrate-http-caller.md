---
id: adr-2026-11-01-betwin-migrate-http-caller
title: BetWin 迁移：外部 HTTP 必须用 IHttpCaller，禁止散装 HttpClient
doc_type: decision
projects:
  - simpx-net
  - betwin
lifecycle_status: confirmed
confirmed_by: user
user_reviewed: false
risk_tags:
  - public-contract
  - compatibility
evidence_refs:
  - 会话 2026-11-01：用户 @@X「迁移规则里面+不允许直接使用httpclient，直接用框架自带的http组件」「以前的都修改一下」
  - 会话 2026-11-01：用户 @@X「可以改项目规范，不要改整体cursor的规则关于httpclient的」
  - projects/examples/simpx-net/boundaries.md HTTP 出站约定
  - SimpX.Net.Feature.Http IHttpCaller / HttpCaller
---

# ADR-2026-11-01：外部 HTTP 统一 IHttpCaller

- 日期：2026-11-01
- 范围：SimpX.Net 上 BetWin 迁移及相关共享库
- 状态：生效
- 来源：用户 `@@X`

## 决定

1. 业务出站 HTTP **不得** `new HttpClient()`，**不得**在业务里用 `IHttpClientFactory` 直接 `PostAsync`/`GetAsync`。
2. 统一注入并调用 **`IHttpCaller`**（`SimpX.Net.Feature.Http`，封装 Furion HttpRemote）。表单用 `FormUrlEncodedContent` / `PostFormAsync`，JSON 用 `JsonContent` / `PostJsonAsync`。
3. 宿主之间的内部调用继续用 **`IHttpDeclarative` + 命名 HttpClient**，与本条分流。
4. 存量违规（充值/代付网关插件等）在本 ADR 确认当轮改完。

## 不做

- 不借本条改支付业务语义或回调验签算法。
- 不把 Admin 压测接口强行迁到 IHttpCaller（可保留自管 HttpClient，注释标明例外）。

## 后果

- Shared / Channels 支付相关工程须引用 `SimpX.Net.Feature.Http`。
- 单测用假 `IHttpCaller` 模拟响应，不要再包 `IHttpClientFactory`。
- 本条写在 **项目档案 / 迁移硬约束 / ADR**，**不**写入全局 Cursor `.cursor/rules/*.mdc`。
