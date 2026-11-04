# Evidence: 入款审核声音提醒

日期：2026-11-03

## 对照旧站

- `money/recharge/check.html`：挂起文案「不再声音提醒」；全局 `controls/notify` 对 `Transfer`（BankRecharge 待审）播 `/admin/site/admin/sound`
- 本页落地页内开关，避免等全局角标壳

## 实现

- `check.vue`：声音提醒开关（默认开）
- 查询结果含 `Status=待审/None` 时播 `sound.Transfer`；同批 ID 去重
- 无自定义 URL 时短蜂鸣兜底（浏览器可能拦截无手势自动播）

## 残余

- 布局级 notify 角标轮询/多类型音轨仍未迁（非本页 DoD）
