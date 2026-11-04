# Evidence: P0 会员帐变竖切

日期：2026-11-03

## 对照旧站 `report/user/moneylog.html`

- 默认今日；快捷今日/近7天
- 筛选：用户、包含下级、类型、时间、备注
- 列中文；金额着色；Type/Currency 显示名

## 后端

- `BelongUser=1` 含直属下级
- `TypeValue` + `CurrencyCode` 投影

## DoD

- `/admin/report/money/log` 本机有数即可验收
