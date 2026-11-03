# Evidence: 会员列表查不出数据（空表）

日期：2026-11-03

## 现象

`/user/member/list` 查询后表格永远空；后端 `POST /admin/user/member/info/list` 实际有数据。

## 根因

`useVxeTable` 默认 `proxyConfig.response` 只认框架形态：

- `result: 'data.result.items'`
- `total: 'data.result.total'`

业务页 `handleQueryApi` 返回的是 `{ page: { total }, result: [] }`。
vxe 用 `XEUtils.get(rest, 'data.result.items')` → `undefined` → 空表。

后端当时正常（本机验证）：

- `User=dl111` → `RecordCount=93`，`listLen=20`，`first=dl111`
- `IsLimit=0` → `RecordCount=1674`

## 修复

1. **`useVxeTableOptionsHook.ts`**：`response.result/total/list` 改为函数，同时兼容
   - `{ data: { result: { items, total } } }`（系统页）
   - `{ page: { total }, result: [] }`（业务 /admin 页）
2. **`admin-api.ts`**：`toVxeProxyPage` / `readAdminList` / `readAdminRecordCount`
3. **`list.vue`**：查询返回 `toVxeProxyPage(...)`；失败弹 `ElMessage`（不再吞错变空表）
4. **后端**：`UserDateMoney` 汇总包 try/catch；`Currency` 计入 hasFilter

## 构建

- `SimpX.Net.Application.Shared`：0 error（本次改动）
- 前端：Vite :8888 热更新 hook / list.vue
