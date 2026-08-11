# Projects

每个业务仓一套 Agent 叠层，目录：`projects/<name>/`。

| 已有 | 说明 |
|------|------|
| `hx-matching/` | 撮合仓 |
| `_template/` | 新项目从此复制 |

## 新项目复用步骤

1. 复制 `_template` → `projects/my-app`
2. 改 `overlay.json` 的 `markerFile`（用来在 `E:\work` 下自动找仓）
3. 写短 `AGENTS.md` + 需要的 `.cursor/rules|skills`
4. 挂载：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File E:\shadow\scripts\sync-project-overlay.ps1 -Project my-app -TargetRepo "E:\work\my-app"
```

人生级原则/ADR 仍放在 shadow 根目录的 `principles/`、`architecture/`，各项目叠层只放**该仓**约定。
