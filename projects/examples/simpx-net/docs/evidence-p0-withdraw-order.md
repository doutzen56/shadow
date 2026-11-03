# Evidence: P0 提现订单竖切

日期：2026-11-03

## 对照旧站 `withdraw/order.html`

- 筛选：订单号 / 上级 / 用户 / 金额区间 / 渠道 / 状态 / 时间；60s 自动刷新；CSV 导出
- 列：上级、分组、行政费、实际到帐、渠道、审核人、出款人、审核/出款时长
- 详情抽屉字段对齐

## 后端

- `WithdrawOrderService.MapListItemsAsync`：AgentName/Group/Channel/Admin/Cashier/Cost/RealPaid/AuditTime/DealTime/Description*
- `orderlist` Form 收 Agent；投影吐中文可用字段
- Host.Admin 需重载

## 处理提现（已接主路径）

- 详情抽屉：None → `checkorder` 通过/拒绝；Wait → `execorder` 出款/拒绝（渠道+手续费）
- 谷歌验证码、风险提示未迁

## 未迁（登记）

- 渠道统计弹层后补
