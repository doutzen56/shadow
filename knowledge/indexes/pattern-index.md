# 模式索引

## 使用方式

- 先按领域定位，再进入具体模式。
- 每次优先选择 `maturity` 高、`reuse_score` 高的模式。

## 领域入口

- cache:
  - pattern-cache-redis-bloom-dual-write-v1
- data-consistency:
  - pattern-money-agent-addmoneylog-v1
  - pattern-data-consistency-redis-shared-matching-pool-v1
- messaging:
  - pattern-messaging-webhook-mq-local-fallback-v1
- reliability:
  - pattern-reliability-azure-sql-dba-daily-inspect-v1
  - pattern-payment-callback-idempotency-checklist-v1
  - pattern-reliability-hotpath-admission-gate-v1
  - pattern-reliability-callback-ack-then-async-query-v1
  - pattern-reliability-channel-adapter-registry-v1
  - pattern-reliability-saas-tenant-init-steps-v1

## 维护规则

- 新模式入库时必须追加索引项。
- 重复模式优先合并，不新增平行条目。
