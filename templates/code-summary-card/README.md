# 代码摘要卡模板

## Frontmatter

```yaml
summary_id:
project_id:
source_repo:
file_path:
file_hash:
symbol:
symbol_kind:
role_scope: []
task_scope: []
risk_tags: []
lifecycle_status: active
last_seen:
```

## 职责

说明这个文件或符号负责什么业务能力。

## 契约

- 输入：
- 输出：
- 失败契约：

## 依赖

- DB：
- Redis：
- ES：
- MQ：
- 第三方：

## 状态与副作用

说明会改哪些状态、资金、订单、缓存或外部系统。

## 验证提示

列出 `@@BE` 和 `@@DEVOPS` 可以复用的断言、命令或场景。

## 风险说明

说明资金、权限、幂等、兼容、回调、生产配置等风险。

## 源码回读要求

涉及行为判断时，必须回读当前源码；本摘要只做定位和初筛。