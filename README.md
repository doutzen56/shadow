# shadow

Tzen 的可迁移个人影子库：偏好、原则、ADR、可复用技能。与业务仓物理分离。

## 本机路径

默认：`KB_ROOT=E:\shadow`（换机改 User Rules 里这一行）。

## 安装 Cursor skills（个人，全项目）

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\install-cursor-skills.ps1
```

会把 `cursor-skills/*` 安装到 `%USERPROFILE%\.cursor\skills\`（不删除你已有的其它 skill）。

## 挂撮合仓叠层（切分支友好）

撮合专用配置真相源在 `projects/hx-matching/`，**不要提交进业务 Git**。挂载：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\sync-hx-matching-overlay.ps1
```

会复制到业务仓，并写入该仓本地 `.git/info/exclude`，`git status` 保持干净、切分支不受影响。

## 口令

- `人生级记住：…` → 写入本仓（视为已确认）
- `本仓记住：…` → 写入当前业务仓 `.cursor/`
- `记一条 ADR：…` / `提炼成架构模式：…`
- 重要任务收尾：Agent 先问要不要沉淀；**未确认不写本仓**

## Token 预算

入口短、库存厚、按需读。不要把本仓长文贴进 User Rules / alwaysApply。
