# Evidence: P0 充值订单竖切

日期：2026-11-03

## 前端

- `money/recharge/order.vue`：对照 `order.html`
- 默认近 7 天 + 今日/近7天/近30天快捷
- `runAdminVxeQuery` → `/admin/recharge/orderlist`
- 列：订单号/网关单号/用户/上级/存入金额叠显/充值时间/到帐/代客/审核人
- 详情抽屉（非纯 JSON）；CSV 导出

## 后端

- `RechargeOrderListItemDto` 补 SystemID/Cost/Count/IsProxyRecharge/AgentName/Admin
- Form 收 Status/IsPay、MoneyFrom/To、Agent

## 本机验

- 登录后近 7 天 `orderlist`：`success=1`，有 `RecordCount`，行含 `AgentName`/`SystemID`/`Cost`

## 对照旧站补齐（第二刀）

- 列：支付（ThirdParty/Payment）、存入金额叠显绿字手续费/行政费、到帐隐藏 1900 时间
- 顶栏：渠道统计占位、代客充值弹层（create）
- 详情：对齐 orderinfo 字段；未到账可「确认入账 / 删除订单」
- 筛选：上级代理、渠道、代客充值、金额区间
- 订单号 Trim；列表补 Payment/ThirdParty/RealPaid

## 多行裁切（第三刀）

根因：`rowConfig.height: 78` → vxe 打 `col--rs-height`，单元格 `overflow:hidden`，第三行「实充」被裁。
修法：去掉固定行高；关 `scrollY/virtualY`；CSS `min-height:96` + `overflow:visible`（`admin-page.scss` `.vxe-multiline`）。

## 审核人 / 处理时长（第四刀）

- `RechargeOrderService`：批量查 `SiteAdmin.AdminName` 填 `Admin`；`DealTime` = 到账耗时 `H:mm:ss`
- 前端列「处理时长」→ `x小时y分z秒`；审核人不再裸 ID
- Host.Admin 需重载后生效

## 展示语义（第五刀）

- 存入金额：手续费/行政费分两行（禁「费：a/b」）
- 币种：唯一源 `Data.Currency` + `OldApiEnumDisplay.Currency`；前端 `ensureCurrencyEnum`←`/admin/site/currency/report`（禁硬编码表）
- 到账 Count：`第N次` + tip
- 多行单元格上下居中
