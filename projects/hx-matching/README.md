# HX.Matching 项目影子叠层

撮合仓专用的 Agent 配置**真相源**在这里（跟 `E:\shadow` 一起走），不塞进业务 Git。

## 包含

- `AGENTS.md`
- `.cursor/rules|skills|LEARNINGS`
- `docs/architecture/README.md`（短入口）

## 挂到业务仓

在业务仓目录执行（或任意位置指定路径）：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File E:\shadow\scripts\sync-hx-matching-overlay.ps1
```

默认目标：`E:\work\撮合项目`。会复制叠层文件进去，并写入该仓 **本地** `.git/info/exclude`（不进业务远程），这样：

- `git status` 干净
- 切分支不受影响
- 配置仍由 Cursor 在业务工作区读取

换电脑：clone `shadow` → 跑上面脚本即可。
