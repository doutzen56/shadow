# Evidence: 去掉 OldApiJson 手包

日期：2026-11-03

## 改动

- 删除 `OldApiJson.cs`
- 新增 `OldApiContract`（声明名序列化选项 + 结果改写）
- `OldApiAuthAttribute` 在 `next()` 后统一把 `OldApiResult` 写成声明名 JSON
- Admin/WebApi 方法改为直接 `return OldApiResult` / `Task<OldApiResult>`

## 验证

- `Application.Admin` / `Application.WebApi` / `Host.Admin`：0 error
- 重载后：`User=dl111` → `success=1`，`info.RecordCount=93`，`info.list` 长度 5，字段名仍为 `RecordCount`/`list`（未驼峰）
