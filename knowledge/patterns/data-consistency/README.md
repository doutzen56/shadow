# data-consistency 模式

资金、订单、缓存与数据库一致性相关模式。

- `money-agent-addmoneylog-v1.md`：统一加减钱入口（先查流水再写存储过程）
- `redis-shared-matching-pool-v1.md`：Redis 共享匹配池（Hash + 分组有序索引）
