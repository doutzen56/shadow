# Evidence: P0 入款审核竖切

日期：2026-11-03

## 对照旧站 `check.html`

- 筛选：用户名 / 入款渠道 / 提交时间 / 订单号 / 状态；默认 Status=`None`（待审）
- 自动刷新：60s（声音提醒未迁）
- 列：编号、充值订单号、用户名、存款金额叠显、银行账户叠显、提交时间、审核时间、状态、审核人、备注、操作
- 操作：待审可挂起；待审/处理中可过/拒；抽屉详情

## 后端

- `BankRechargeCheckService` Status 筛选改 `TryParseCheckStatus`（支持 None/Failed/…）
- checklist 已吐 `Admin`/`PayName`/`Currency` 等

## 注意

- 通过/拒绝写资金账本：需人工场景验收
- Host.Admin 重载后生效
