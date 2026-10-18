---
summary_id: channel-adapter-factory
project_id: hx-matching
file_path: HX.Matching.Channels/ChannelCoreFactory.cs
file_hash: 3db9a3ef57309cce05400fc938b3ef8536ac14c933b849083a1d2e73548c13dc
symbol: ChannelCoreFactory / ChannelCoreKnownImplementations / IChannelClient
symbol_kind: type
role_scope:
  - "@@BA"
  - "@@BE"
task_scope:
  - 代码实现
  - 方案设计
risk_tags:
  - gateway
  - callback
  - funds
  - order-state
last_seen: 2026-10-18
related_pattern: pattern-reliability-channel-adapter-registry-v1
---

# 渠道适配器工厂深挖卡

## 职责

把数十个异构支付供应商收敛为统一拉单/查单/推单契约：注册表按 `SupplierCode` 取适配器；业务只依赖 `IChannelClient` 门面。

## 契约

- 输入：`SupplierCode`、标准请求 DTO（如 `ChannelPayinRequest`）。
- 输出：标准响应 DTO；未注册抛 `ChannelNotSupportedException` 或由调用方处理 null。
- 失败契约：基类统一捕获/脱敏日志；不得把密钥写入普通日志。

## 依赖

- `RechargeChannelsBaseCore` / `WithdrawChannelsBaseCore`
- DI 单例注册各适配器类型
- 供应商表 `SupplierCode` 与类名对齐

## 新增渠道步骤

1. 实现基类方法。  
2. `ChannelCoreKnownImplementations` 登记。  
3. DI 注册。  
4. 配置供应商 `SupplierCode`。  

## 验证提示

1. 已知 SupplierCode 能解析到正确类型。  
2. 未知码失败路径明确。  
3. 回调 ACK 值与查单字段走适配器方法，而非业务散落硬编码。

## 风险说明

- 类名强绑定增加改名成本。  
- 漏注册问题常在运行时才暴露。

## 源码回读要求

接新渠道时回读工厂、已知实现表、基类与一个同类参考适配器。
