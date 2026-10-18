---
id: pattern-reliability-channel-adapter-registry-v1
title: 多供应商渠道适配器注册表 + 门面
domain: reliability
tags:
  - adapter
  - registry
  - payment-gateway
  - facade
projects:
  - hx-matching
maturity: validating
reuse_score: 88
last_verified_at: 2026-10-18
owner: "@@TL"
status: validating
lifecycle_status: confirmed
confirmed_by: "@@TL"
user_reviewed: false
role_scope:
  - "@@BA"
  - "@@BE"
task_scope:
  - 方案设计
  - 代码实现
risk_tags:
  - gateway
  - callback
  - funds
---

# 多供应商渠道适配器注册表 + 门面（模式卡）

## 抽象问题定义

- abstract_problem: 数十个外部供应商协议异构，业务服务若直接 HttpClient 散落，会导致横切（日志、超时、脱敏、错误码）失控。
- failure_of_old_way: 每个调用点复制粘贴签名与解析。
- goal_invariant: 业务只依赖稳定门面；新增供应商以「实现 + 注册」完成，不改撮合主流程。

## 能力组件拆解

- `AdapterBase`: 统一异常、取消、脱敏日志。  
- `KnownImplementationsRegistry`: 供应商码 → 类型。  
- `Factory + DI`: 解析单例适配器。  
- `IChannelClient Facade`: 业务侧唯一入口（进程内可替换为远程）。

## 核心不变量

- SupplierCode 与注册键一致。  
- 未注册失败路径明确。  
- 密钥不进普通日志。

## 算法蓝图

1. 业务给 SupplierCode + 标准 DTO。  
2. 门面/工厂解析适配器。  
3. 基类包裹调用。  
4. 返回标准响应。

## 适配点

- hx-matching: `HX.Matching.Channels` 程序集。  
- 可迁到：短信、物流、KYC 等多供应商集成。

## 权衡

- 显式注册表清晰但改名成本高；约定优于反射扫装配可减少误注册。

## 验证清单

- 注册码可解析。  
- 未知码失败明确。  
- 新适配器不改 Matchmaking 主类。

## 反模式

- 业务层 `switch(supplier)` 上百分支。  
- 反射自动扫描无白名单导致意外加载。

## 证据

- evidence_refs:
  - HX.Matching.Channels/ChannelCoreFactory.cs
  - HX.Matching.Channels/Contracts/IChannelClient.cs
  - knowledge/code-summaries/hx-matching/cards/channel-adapter-factory.md
- reusable_assets: 适配器基类 + 注册表 + 门面
- non_reuse_reason_when_created_from_gap: 公共库缺渠道注册表模式卡
