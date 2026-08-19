# 测试必须证明行为

## 来源

HX.Matching ES 迁移。曾加 `EsMigrateIdStrategyTests`：拼文档调用 `ResolveTargetId` / `ShouldReplaceDestWindow`，断言等于旁边的 `DocumentId.Build()` 或类型白名单 true/false。负责人判断：没什么用。

## 为什么没用

- 断言的是实现自己，改映射测试跟着改，绿了也不说明迁移对。
- 为了测策略把方法改成 public，污染生产 API。
- 放在 TestIntegration 里却不打 ES。

## 以后怎么做

- 默认不写这类测试。
- ES 迁移以手工 `EsIndexMigrateTests` 为准：真迁一段、两边 count / `_id` 对得上。
- 策略怎么选，看 `EsIndexMigrator` 即可，不必再铺一层单测。
