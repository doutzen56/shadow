# 模式索引

## 使用方式

- 先按领域定位，再进入具体模式。
- 每次优先选择 `maturity` 高、`reuse_score` 高的模式。

## 领域入口

- cache:
  - pattern-cache-redis-bloom-dual-write-v1

## 维护规则

- 新模式入库时必须追加索引项。
- 重复模式优先合并，不新增平行条目。
