# SimpX Web

> 基于 **SimpX 通用权限开发框架** 前端（vue-next-admin 系）二次开发的企业级中后台前端工程。
> 后端为 .NET 10 + SqlSugar 的 SimpX 服务端，前后端通过 Swagger 契约自动生成 TypeScript 接口代理。

| 项目 | 说明 |
| --- | --- |
| 包名 | `SimpX.leandigital` |
| 版本 | 2.4.33（见 `package.json` / 工作台右下角版本部件） |
| 技术栈 | Vue 3 + TypeScript + Vite 8 + Element Plus 2 + VXE-Table 4 + Pinia |
| 包管理 | pnpm 12（`packageManager` 字段已锁定，推荐用 corepack） |
| Node 要求 | >= 22（Volta 锁定 22.19.0） |
| 后端默认地址 | `http://localhost:5005` |

---

## 一、技术栈与关键依赖

| 分类 | 选型 | 备注 |
| --- | --- | --- |
| 框架 | Vue 3.5（`<script setup>` 全量） | 组件名通过 `vite-plugin-vue-setup-extend` 的 `name` 属性声明 |
| 构建 | Vite 8.3 | 别名 `/@` → `src/`，生产用 terser 压缩（自动剔除 console / debugger） |
| UI | Element Plus 2.14 | 主题变量全量走 `--el-*`，项目在 `src/theme` 做二次治理 |
| 表格 | vxe-table 4.22 + vxe-pc-ui 4.20 | 列表页主力；`@vxe-ui/plugin-render-element` 让单元格直接渲染 el 组件 |
| 状态 | Pinia 4 | 7 个 store，见「六、状态管理」 |
| 路由 | vue-router 5 | Hash 模式，**后端控制路由**（`isRequestRoutes: true`） |
| 国际化 | vue-i18n 11 | 内置多语言包，菜单/页面文案走 `$t` |
| 图表 | ECharts 6 | 统一封装为 `scEcharts` 组件，主题色读 CSS 变量 |
| 其它 | LogicFlow（流程设计）、VForm3（表单设计）、wangEditor（富文本）、Monaco（代码编辑）、SignalR（在线用户推送）、MQTT、OpenLayers（地图）、@file-viewer（Office/PDF 预览） | 均为按需引入的大件 |

浏览器支持：Edge ≥ 88、Firefox ≥ 78、Chrome ≥ 87、Safari ≥ 13（Vue 3 与 Element Plus 均不再支持 IE11）。

---

## 二、环境准备与快速开始

### 1. 安装依赖

```bash
cd code/Web
pnpm install
```

> ⚠️ **必读：安装后要补顶层链接**
> 本项目用 pnpm 的严格拓扑，`pnpm install` 偶发会丢掉部分直接依赖的顶层软链，表现为
> `Could not resolve '@vitejs/plugin-vue'`、`Cannot find package 'vite-plugin-vue-setup-extend'`、
> `Rolldown failed to resolve import "@vxe-ui/core"`。安装后请执行一次：
>
> ```bash
> python ../../.workbuddy/tools/fix_top_links.py
> ```
>
> 脚本按 `package.json` 全量校验顶层链接，缺失时用 PowerShell Junction 补齐（含 `vxe-table` 的隐式依赖 `@vxe-ui/core`）。

### 2. 启动开发服务

```bash
pnpm dev        # vite --open，默认端口 8888（见 .env 的 VITE_PORT）
```

后端需先启动并可访问 `http://localhost:5005`（`.env.development` 的 `VITE_API_URL`）。
`/api`、`/upload`、`/sse` 前缀已在 `vite.config.ts` 里代理到后端，前端代码只用相对路径。

### 3. 生产构建

```bash
pnpm build      # 输出到 dist/，同时产出 .gz / .br
```

本地直接构建（避免清理旧目录被安全策略拦截）：

```bash
node ./node_modules/vite/bin/vite.js build --outDir dist --emptyOutDir=false
```

> ⚠️ **`vite build` 会重写 `public/config.js`**：`vite.config.ts` 在构建时把当前环境变量写进
> `public/config.js`，因此 `VITE_API_URL`、`VITE_DEFAULT_USER`、`VITE_DEFAULT_USER_PASSWORD`
> 三项会被 `.env.production` 的空值覆盖。开发环境下构建完**必须手动还原**这三个值，否则页面连不上后端。
>
> 该文件同时写入 `VITE_BUILD_TIMESTAMP`，`webUpdateNotice` 插件据此提示用户「系统更新，请刷新」。

### 4. 其它脚本

| 命令 | 作用 |
| --- | --- |
| `pnpm lintFix` | ESLint 自动修复 `src/` |
| `pnpm format` | Prettier 格式化 |
| `pnpm buildApi` | 调用 `script/build-api.js` 生成后端接口代理 |
| `pnpm deploy / deployWeb / deployServer` | `script/publish.js` 一键发布 |
| `pnpm translate` | `script/translate.cjs` 文案翻译 |
| `pnpm genEleIconTypes` | 生成 Element Plus 图标类型提示 |

---

## 三、目录结构

```
Web/
├─ api_build/             # 接口契约生成（swagger-codegen + 各模块 bat/sh）
│  ├─ build.bat / build.sh            # 生成系统模块接口 → src/api-services/system
│  ├─ build_leanDigital.bat           # 生成「精益数字化平台」模块接口
│  └─ swagger-codegen-cli.jar
├─ public/config.js       # 运行时配置（构建时由 vite 注入，见上文注意事项）
├─ script/                # 构建 / 发布 / 清理等 Node 脚本
├─ lang/                  # 自动 i18n 插件生成的语言包
└─ src/
   ├─ main.ts             # 入口：注册 pinia / router / i18n / ElementPlus / vxe / 第三方库
   ├─ App.vue             # 根组件：主题初始化、锁屏、布局配置抽屉、拖放兜底
   ├─ api/                # 少量手写接口（工作流等）
   ├─ api-services/       # ★ 由 Swagger 自动生成的接口代理（不要手改）
   │  ├─ system/          #    框架内置模块
   │  └─ leanDigital/     #    精益数字化平台业务模块
   ├─ assets/             # 图片、登录轮播图、错误页配图
   ├─ components/         # 通用组件（sysDict 字典、scEcharts、uploadImage、selector …）
   ├─ directive/          # 自定义指令：v-auth / v-auths / v-auth-all / v-waves / v-reclick
   ├─ hooks/              # vxe-table 初始化 + 统一表格配置（含列表页分页/排序/查询通用逻辑）
   ├─ i18n/               # 语言包（含菜单树 zh-cn.ts）
   ├─ layout/             # 布局外壳（见「五、布局与主题」）
   ├─ router/             # 路由：index.ts + backEnd.ts（后端控制）+ frontEnd.ts + route.ts
   ├─ stores/             # Pinia：themeConfig / userInfo / routesList / tagsViewRoutes …
   ├─ theme/              # ★ 全局样式治理（见「五、布局与主题」）
   ├─ types/              # 全局类型声明
   ├─ utils/              # 工具库（axios 封装、存储、format 格式化、导出 Excel、加密 …）
   └─ views/              # 页面
      ├─ home/            # 工作台（拖拽栅格 + 部件）
      ├─ login/           # 登录 / 注册 / 手机登录
      ├─ system/          # 系统管理（用户、角色、机构、菜单、字典、日志、任务、监控、报表 …）
      ├─ document/        # 文档库
      ├─ workFlow/        # 工作流（待办、流程设计、表单设计、数据审批）
      ├─ aiChat/          # AI 对话
      ├─ error/ about/ demo/
      ├─ benchmark/       # 体系对标（业务模块）
      └─ leanDigital/     # 精益数字化平台（业务模块：共创、积分、成果展示 …）
```

---

## 四、路由、菜单与权限

### 1. 两种路由模式

`src/stores/themeConfig.ts` 的 `isRequestRoutes` 决定模式，**本项目默认 `true`（后端控制路由）**：

| 模式 | 数据源 | 代码位置 |
| --- | --- | --- |
| 后端控制（默认） | `GET /api/sysMenu/loginMenuTree` → 动态 `router.addRoute` | `src/router/backEnd.ts` |
| 前端控制 | 本地 `src/router/route.ts` 写死 + `roles` 过滤 | `src/router/frontEnd.ts` |

`router.beforeEach` 的核心流程（`src/router/index.ts`）：

1. 记录当前路由、启动 NProgress；
2. 无 token → 跳 `/login?redirect=...`（带租户 `tid`）；已登录访问 `/login` 则回首页；
3. 路由表为空 → `ensureRoutesInit()` 初始化（**用 Promise 单例做并发保护**，失败则清空缓存允许重试，
   并走节流提示后跳登录，避免后端未启动时白屏）；
4. 初始化完成返回 `{ path: to.path, query: to.query }` 重新进入，避免刷新跳 404。

### 2. 按钮级权限

`v-auth` 指令直接比对 `userInfos.authApiList`（后端下发的接口权限集合），无权限时移除 DOM：

```html
<el-button v-auth="'sysUser/add'" @click="handleAdd">新增</el-button>
<el-button v-auths="'[sysUser/update, sysUser/delete]'">编辑或删除</el-button>
<el-button v-auth-all="'[a, b]'">需同时具备</el-button>
```

> 约定：指令值 = 「控制器名/动作名」，如 `sysUserRegWay/page`。新增按钮时**后端接口、菜单按钮、前端 `v-auth` 三者要同步**；
> 复制页面最容易漏改（曾出现注册方式页沿用 `sysPos/*`，导致按钮全部不显示）。

### 3. 菜单授权

新增菜单**不会自动进入 `SysRoleMenu`**，非 `superAdmin` 账号会看不到也进不去。
`/api/sysRole/grantMenu` 在部分版本返回 200 但不落库，可用脚本兜底：

```bash
python .workbuddy/tools/grant_lean_menus.py     # 接口授权 + 直写数据库补齐
```

---

## 五、布局与主题

### 1. 布局外壳

`src/layout/index.vue` 按 `themeConfig.layout` 动态切换 4 种布局：`defaults`（默认）、`classic`（经典）、
`transverse`（横向）、`columns`（分栏）。通用组成：

| 目录 | 职责 |
| --- | --- |
| `layout/main/*` | 四种布局的骨架 |
| `layout/navBars/` | 顶栏（`topBar`：面包屑、全局搜索、消息、全屏、用户菜单）、多页签 `tagsView` |
| `layout/navMenu/` | 纵向 / 横向 / 分栏菜单渲染 |
| `layout/routerView/` | `parent.vue`：路由视图 + keep-alive + iframe 刷新 |
| `layout/component/` | 侧边栏 `aside.vue`、`columnsAside.vue`、新手指引 `guideTour.vue` |
| `layout/logo/` | 站点 Logo 与标题 |

**新手指引**（`layout/component/guideTour.vue`）：登录后自动漫游一次（侧栏 → 全局搜索 → 面包屑 → 多页签 → 顶栏快捷区 → 账号菜单）。
完成 / 关闭 / ESC 都会写入 **Cookie**（`guideVersion`，有效期 365 天）—— 用 Cookie 而不是 `Local`，
是因为「退出登录 / 清理缓存」会调用 `Local.clear()` 把标记一起清掉。改 `GUIDE_VERSION` 可让所有人重新看到；
账号菜单「新手指引」可手动唤起（`mittBus.emit('openGuide')`）。

### 2. 主题样式分层（`src/theme`）

`theme/index.scss` 按序加载，**后面的文件可覆盖前面的**：

| 文件 | 定位 |
| --- | --- |
| `app.scss` | `:root` 变量（`--next-*` 与 `--el-*` 桥接）、全局滚动条、布局外壳、原子工具类 |
| `other.scss` `common/` `waves.scss` | 过渡动画、杂项 |
| `element.scss` | Element Plus 组件外观二次治理（Dialog / Drawer / Table / NavMenu / Card / Pagination …） |
| **`page.scss`** | ★ **标准页面语言**：`.card-header`、`.full-table`、`.detail-item`、Descriptions / Tabs / Empty / Loading、el-table 表头、Timeline、`pre` |
| `vxeTable.scss` | vxe-grid 表头、单元格、分页、弹层 |
| `dark.scss` | 深色模式变量映射 |
| `loading.scss` | 全屏 loading 遮罩（`z-index: 4000`，必须高于弹窗遮罩） |

**统一变量 token（写在 `app.scss :root`）**

```css
--next-gap-xs/sm/md/lg/xl            /* 4 / 8 / 12 / 16 / 24 */
--next-radius-sm/md/lg               /* 6 / 10 / 14 */
--next-shadow-card / --next-shadow-hover
--next-topbar-height / --next-logo-height / --next-tagsview-height / --next-widget-height
--next-search-control-width: 171px   /* 查询区控件宽度（App.vue 的 .el-form--inline 读取） */
--next-accent-1 ~ --next-accent-10   /* 统计卡 / 图表 / 图标底色，暗色主题可整体覆盖 */
```

### 3. 统一口径（改动样式前必读）

1. **只用主题变量**：DOM 与图表一律 `var(--el-*)` / `var(--next-*)`，禁止 `#409eff`、`rgba(0,0,0,.2)` 这类裸色值；
   主内容区背景用 `--next-bg-main-color`（不要 `--el-bg-color-page`）。
2. **改视觉优先改全局**：能写在 `theme/*.scss` 的就别给单个组件加 `!important`。
   卡片统一走 `--el-card-border-radius / --el-card-border-color`
   （**禁止 `.el-card { box-shadow: ... !important }`**，会顶掉 `shadow="never"`）。
3. **通用结构类收敛到 `page.scss`**：`.card-header`（工具条）、`.full-table`（铺满剩余高度的表格卡片，
   关键在 `min-height: 0`）、`.detail-item/.detail-label/.detail-value`。
   业务页若已在 scoped 里定义同名类，优先级更高可共存，**不要再把通用类写回各页面**。
4. **标签栏 `tagsView.vue` 不要重做形态**：框架自带多套标签样式可切换，只允许改盒模型之外的细节。
5. **键盘可达性**：已改为 `:focus { outline: none }` + `:focus-visible { 主色描边 }`，不要再全局 `outline: none !important`。

---

## 六、状态管理（Pinia）

| Store | 内容 |
| --- | --- |
| `themeConfig` | 布局、主题色、深色/灰色/色弱、组件尺寸、i18n、`isRequestRoutes`、水印、服务端时间 |
| `userInfo` | 用户信息、按钮权限 `authApiList`、字典 `dictList`、常量 `constList`、用户列配置 |
| `routesList` | 路由一维/二维结构、当前路由 |
| `tagsViewRoutes` | 多页签列表、缓存视图 |
| `keepAliveNames` | keep-alive 名称集合 |
| `quickEntry` | 快捷入口 |
| `requestOldRoutes` | 后端路由原始数据 |

规范：

- **不要在模块顶层直接解构 store**：`const { userInfos } = useUserInfo()` 会拿到重新赋值前的旧对象
  （store 里 `userInfos` 是整体赋值的），正确做法是函数内惰性取或用 `storeToRefs`，详见 `utils/useInfo.ts` 的注释；
- 同一组件内不要重复创建同一个 store 实例；
- 需要响应式就用 `storeToRefs`。

---

## 七、接口层（`src/api-services`）

### 1. 生成方式

前端不手写后端接口，全部由 **Swagger Codegen** 从后端 swagger.json 生成 `typescript-axios` 客户端：

```bash
# 系统模块（框架内置接口）
api_build/build.bat          # Windows
./api_build/build.sh         # Linux / Mac

# 精益数字化平台业务模块
api_build/build_leanDigital.bat
```

脚本要点：

- 契约地址：`http://localhost:5005/swagger/精益数字化平台/swagger.json`，
  **中文分组名必须写成 UTF-8 百分号编码**，否则 404；端口可用 `LEAN_API_PORT` 覆盖；
- Java 需绕过系统代理：`-Dhttp.nonProxyHosts="localhost|127.0.0.1"`；
- 先生成到 `.swagger-temp/`，**确认产出 `apis/` 目录后才替换正式目录**，失败不会清空已有接口；
- 生成目录为 `src/api-services/<模块>`，模块之间互不干扰。

前置条件：JDK 8+、`JAVA_HOME`、`swagger-codegen-cli.jar`（3.0.68+）放在 `api_build/` 下。

### 2. 调用方式

```ts
import { getAPI } from '/@/utils/axios-utils';
import { SysUserApi } from '/@/api-services/system/api';

const res = await getAPI(SysUserApi).apiSysUserPagePost(params);
```

- `getAPI()` 统一注入 token、租户、异常处理与 401 跳转；
- 业务模块统一引用 `/@/api-services/leanDigital/api`，**同一份契约只保留一份**，避免重复生成造成冲突；
- 改后端 DTO 后的标准动作：**后端 build → 启动服务 → 重新生成契约 → 前端 build**。

---

## 八、开发一个标准列表页（CRUD）的完整流程

目标形态：**查询卡 + 表格卡（铺满剩余高度）+ 弹窗增删改**。

### 1. 建页面

```
src/views/<模块>/<功能>/index.vue                 # 列表页
src/views/<模块>/<功能>/component/editXxx.vue     # 新增 / 编辑弹窗
```

### 2. 列表页骨架

```html
<template>
	<div class="page">                        <!-- 根容器用 flex（margin 不折叠） -->
		<el-card shadow="never">               <!-- 查询区 -->
			<el-form :inline="true" :model="state.queryParams" @keyup.enter="handleQuery(true)">
				<el-form-item label="名称"><el-input v-model="state.queryParams.name" clearable /></el-form-item>
				<el-form-item>
					<el-button type="primary" icon="ele-Search" v-auth="'xxx/page'" @click="handleQuery(true)">查询</el-button>
					<el-button icon="ele-Refresh" @click="resetQuery">重置</el-button>
				</el-form-item>
			</el-form>
		</el-card>

		<el-card class="full-table" shadow="never">   <!-- 表格区：flex:1 + min-height:0 -->
			<template #header>
				<div class="card-header">
					<span>某某列表</span>
					<el-button type="primary" icon="ele-Plus" v-auth="'xxx/add'" @click="handleAdd">新增</el-button>
				</div>
			</template>
			<vxe-grid ref="xGrid" v-bind="gridOptions" />
		</el-card>

		<EditXxx ref="editRef" @refresh="handleQuery" />
	</div>
</template>
```

- 表格统一用 `vxe-grid`，公共配置（尺寸、工具栏、导出/打印、列宽持久化）在 `src/hooks/useVxeTableOptionsHook.ts`；
- 分页、空态、loading 由统一配置注入，不要每页重复写；
- 需要自适应高度时，第二个 `el-card` **必须带 `full-table` 类**。

### 3. 列表页通用逻辑（`src/hooks/useVxeTableOptionsHook.ts`）

`useGridPageParam` / `gridPageEvents` / `useGridQuery` 与表格配置 `useVxeTable` 同处一个文件，
统一从 `/@/hooks/useVxeTableOptionsHook` 导入（同一模块只写一条 import）。

「分页/排序偏好本地持久化 + 查询刷新」这套样板原先在 29 个列表页里各写一遍，现已收敛：

```ts
// ① 必须在 state / options 之前创建：sortConfig、pagerConfig 要在 setup 阶段就拿到恢复后的值
const { key: localPageParamKey, param: localPageParam } = useGridPageParam('sysConfig', {
	pageSize: 50 as number,
	defaultSort: { field: 'orderNo', order: 'asc', descStr: 'desc' },
});
const state = reactive({ queryParams: { name: '' }, localPageParam, title: '' });

const options = useVxeTable<SysConfig>({ id: 'sysConfig', name: '参数配置', columns: [...] }, {
	proxyConfig: { autoLoad: true, ajax: { query: ({ page, sort }) => handleQueryApi(page, sort) } },
	sortConfig: { defaultSort: localPageParam.defaultSort },
	pagerConfig: { pageSize: localPageParam.pageSize },
	toolbarConfig: { export: true },
});

// ② 查询：reset=true 回到第一页（点「查询」），false 为当前页刷新（增删改后刷新列表）
const handleQuery = useGridQuery(xGrid, options);

// ③ 偏好落缓存，业务事件写在后面
const gridEvents: VxeGridListeners<SysConfig> = {
	...gridPageEvents(localPageParamKey, localPageParam),
	async cellDblclick({ row }) { ... },
};
```

不要再手写 `const localPageParamKey = 'localPageParam:xxx'` / `onMounted` 恢复 / `async pageChange` / `async sortChange`。

### 4. 弹窗

- 标题在本页 `<template #header>` 里自写：`<div style="color:#fff">` + `<el-icon size="16">`
  （弹窗头是主色底，必须白字；各弹窗标题文案不同，不抽公共组件）；
- 页脚按钮放 `<template #footer>`，主操作在右；
- 表单校验复用 `utils/formRule.ts` 里的公共规则；
- 二次确认在本页写完整 options（`confirmButtonText` / `cancelButtonText` / `type: 'warning'`），
  不同页面的按钮文案可能不同，不抽公共常量。

### 5. 字典与选人

- 字典取值统一用全局字典组件 `<GSysDict code="lean_proposal_type" v-model="xx" />`，不要各页自己拉字典；
- 选人 / 选单位用 `<lean-user-org-picker type="user" v-model="xx" />`，不要各页维护 `userOptions`。

### 6. 提交前清单

- [ ] `v-auth` 与后端接口、菜单按钮三者一致；
- [ ] 新增菜单已补角色授权（见第四节第 3 点）；
- [ ] 无硬编码色值、无魔法数字（用 `--next-gap-*` / `--next-*`）；
- [ ] 列表有分页、空态、loading；删除有二次确认；
- [ ] 跑 `validate_vue.js` 与一次 `vite build`。

---

## 九、编码规范

1. 组件用 `<script setup lang="ts" name="xxx">`，`name` 供 keep-alive 与调试使用。
2. 中文注释；关键分支写清「为什么」，不写「做了什么」。
3. **事件总线必须具名解绑**：`mittBus.off('x', () => {})` 传匿名函数等于没解绑（mitt 按引用比对），
   会随组件挂载持续叠加监听；正确写法是抽出 `const onXxx = () => {}` 后 `on` / `off` 同传。
4. 文件统一 **CRLF**（Python 脚本改完源码要补一次 LF→CRLF 转换）。
5. 改后端 DTO 后必须重新生成契约；不要手改 `api-services`。
6. 业务代码只写在 `src/views/<模块>` 下，框架底层（`src/layout`、`src/theme`、`src/utils`）的改动要保持「全局受益」，
   不能为单个页面开特例。
7. **格式化逻辑不要在各页面重复实现**：日期时间、字节/文件大小等统一用 `utils/format.ts`
   （`formatDateTime` / `formatBytes` / `formatFileSize` / `getFileTagType` / `getCurrentMonthRange`）。
   只有「展示口径确实不同」时才允许本地包一层（如上传进度要固定 2 位小数 → `formatBytes(v, { trimZeros: false })`），
   并在旁边写明原因。
8. **弹窗标题、二次确认按页面各自维护**：标题文案与 `confirmButtonText / cancelButtonText / type`
   各页可能不同，不做统一抽取；标题区保持 `<div style="color: #fff">` + `<el-icon size="16">`。
9. 提交前：`pnpm lintFix` + 页面校验 + 构建。

---

## 十、构建、部署与更新提示

- 构建产物：`dist/`，同时生成 `.gz` 与 `.br`（`vite-plugin-compression2`，> 5KB 才压缩）；
- 资源分包：Vue / Element Plus 相关归 `vue-vendor`，其余按包名拆包；
- 部署：`dist/` 交给 Nginx 静态托管，`/api`、`/upload`、`/sse` 反向代理到后端；
- 线上路径通过 `.env.production` 的 `VITE_PUBLIC_PATH` 控制；
- 前端发版后，`@plugin-web-update-notification` 会依据构建时间戳弹「系统更新，请刷新」。

---

## 十一、常用工具与排错

### 校验脚本（仓库根目录 `.workbuddy/tools/`）

| 脚本 | 用途 |
| --- | --- |
| `validate_vue.js` | 编译校验页面模板（基线 63/63） |
| `check_all_vue.js` | **全量**编译校验 340 个 `.vue`（含 `layout/` `components/`，比上面那份覆盖面全） |
| `check_fragment_root.js` | 扫「多根节点」组件——被 `v-show` / 指令作用的组件必须单一元素根 |
| `check_runtime_refs.js` | 扫模板里的「未定义引用」——白屏杀手（误报较多，只看真实字段命中） |
| `check_page_data.py` | 页面 × 关键接口体检，列出无数据的接口 |
| `fix_top_links.py` | **`pnpm install` 之后必跑**，补齐 node_modules 顶层链接 |
| `grant_lean_menus.py` | 菜单角色授权兜底 |

### 常见问题

| 现象 | 原因与处理 |
| --- | --- |
| `Could not resolve '@vitejs/plugin-vue'` / `Cannot find package 'vite-plugin-vue-setup-extend'` | `pnpm install` 打散了顶层链接 → 跑 `fix_top_links.py` |
| `Rolldown failed to resolve import "@vxe-ui/core"` | 同上，`vxe-table` 的隐式依赖缺顶层链接 |
| 构建后连不上后端 | `vite build` 清空了 `public/config.js` 的三个变量 → 手动还原 |
| 页面点不开 / 菜单看不到 | 新增菜单未进 `SysRoleMenu` → `grant_lean_menus.py` |
| 页面白屏 | 模板引用了未定义变量 → `check_runtime_refs.js`；或组件既内嵌又自带抽屉但内容没抽成 Panel |
| 契约生成报 `Unable to read URL` | 中文分组名未做百分号编码，或本机代理拦截了 localhost（脚本已带 `nonProxyHosts`） |
| 表格撑破容器、滚动条跑到页面上 | 表格卡片缺 `full-table` 类（缺 `min-height: 0` 时 flex 子项不收缩） |

---

## 十二、相关文档

- 后端仓库：`code/SimpX`（后端说明与开发流程见 `SimpX/README.md`）
- 完整教程站：`网站与教程/`（VuePress；前端手册 23 篇免费，后端手册 47 篇为会员内容）
- 教程站首页由 `网站与教程/src/.vuepress/components/TechHome.vue` 渲染，文档新增流程见 `网站与教程/README.md`
- 接口契约生成说明：`api_build/readme.md`
- SimpX 官方文档：https://admin-net.com ；Furion：https://furion.baiqian.ltd
- 变更记录：`CHANGELOG.md`
