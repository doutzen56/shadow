# SimpX.Net 前端

pnpm 工作区，两个应用：

| 应用 | 目录 | 开发端口 | 接口 |
| --- | --- | --- | --- |
| 租户管理端 admin | `apps/admin` | 8888（`apps/admin/.env` 的 `VITE_PORT`，与原来一致） | 仍走原来的 Vite 代理 |
| 总后台 super-admin | `apps/super-admin` | 8889 | `http://127.0.0.1:5004` |

常用命令（在 `Web` 目录执行）：

```bash
pnpm install
pnpm dev
pnpm dev:super-admin
pnpm --filter admin dev
pnpm --filter super-admin dev
```

`packages/shared` 的包名是 `@simpx/shared`，两个应用都依赖它。管理端本地缓存键前缀仍是 `simpx.net`。

## 总后台壳（`apps/super-admin`）

- 已对齐 Admin 框架：JWT 登录、`loginMenuTree`、侧栏 + `/menu/placeholder`；接口走 `/api/platform/*`（axios 里把 `/api/system/{auth,menu,const}` 改写过去）。
- 本地若 `pnpm install` 在 Windows 长路径下把依赖链弄断，可临时把 `apps/super-admin/node_modules` 做成指向 `apps/admin/node_modules` 的目录联接再 `pnpm --filter super-admin build` / `dev`。
- 开发：`pnpm dev:super-admin`（8889 → 反代 5004）。
