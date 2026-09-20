# HX.Matching 线程池饥饿事故响应案例

## 适用场景

用于处理“主机资源看似正常，但请求大量超时、缓存和消息队列同时报错”的生产故障。

## 初始判断

当出现以下组合时，应考虑线程池饥饿：

- CPU 空闲或不高。
- 内存充足。
- 请求大量超时。
- Redis 客户端报整池不可用。
- RabbitMQ 心跳丢失或恢复失败。
- 网关出现大量 499、502、504。

## 排查顺序

1. 看线程池排队延迟，而不是先下结论为 Redis 或 RabbitMQ 故障。
2. 看是否有高并发路径上的同步等待。
3. 看是否有锁内数据库或缓存访问。
4. 看后台任务进程是否停止消费。
5. 看服务端超时是否生效。
6. 看健康检查是否能反映线程池延迟。

## 应急原则

- 先止血，避免请求无限堆积。
- 优先摘除不就绪实例。
- 限制高风险入口并发。
- 减少锁内同步调用。
- 保留旧缓存值优先于请求线程回源。
- 对单实例后台任务进程单独告警。

## 复盘输出

事故结束后至少沉淀：

- 现象。
- 根因链。
- 被误导的信号。
- 真正有效的观测指标。
- 修复清单。
- 以后检查项。

## 关联知识

- 经验教训：`F:\shadow\knowledge\lessons-learned\hx-matching-threadpool-starvation.md`
- 多宿主架构：`F:\shadow\knowledge\architecture\hx-matching-multi-host.md`
- 数据库分表约定：`F:\shadow\knowledge\engineering\sqlsugar-and-splittable-pattern.md`
