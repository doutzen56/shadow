# 租户后台 Admin 前端迁移说明（Web.Admin → SimpX Web/apps/admin）

> 状态：W0 工程约定已落地（2026-11-03）；**2026-11-03 纠偏：停止横向铺壳，改 P0 竖切**  
> 纠偏主档：[`admin-frontend-migration-reset.md`](admin-frontend-migration-reset.md)（**本档冲突时以纠偏档为准**）  
> 入口：`@@X`  
> 旧版源码：`F:/website/Web.Admin`（工作区可读）  
> 新壳：`SimpX.Net/Web/apps/admin`（Admin.NET.Pro 壳 + SimpX 登录/菜单）  
> 冲突登记：[`admin-frontend-migration-conflicts.md`](admin-frontend-migration-conflicts.md)

## 1. 用户拍板的迁移原则（本档为准）

| # | 原则 | 落地含义 |
|---|------|----------|
| 1 | **业务行为 1:1** | 字段、按钮、校验、状态流以 Web.Admin 对应页为准，不擅自删减 |
| 2 | **视觉/交互用新壳** | 布局、vxe、查询条、操作列、弹出层跟当前 SimpX（Pro 壳），不复刻 layui 皮肤 |
| 3 | **接口路径与出入参 1:1 旧版** | 继续调 `/admin/...`；请求参数名/形态、响应 `success/msg/info` 跟旧版；**菜单与按钮权限用新体系**（`site:…` / 新菜单种子） |
| 4 | **接口有问题优先靠拢旧版** | 行为冲突先对齐旧版；新壳或新权限与旧行为打架时 **停下来由用户拍板** |
| 5 | **做的过程必须回旧站核对**（2026-11-03） | 每页/每功能实现前、实现中、验收时都要打开 `Web.Admin` 对应 html（及关联弹层），核对：**功能清单、操作流程、字段与校验、接口与参数、状态变化**。禁止只凭印象或只看新壳/后端现成接口就写页 |

冲突裁决顺序（本任务）：**用户拍板 > 本档原则 > 旧版页面行为 > 现 SimpX 后端实现 > Pro 默认实现**。

---

## 2. 旧版 Web.Admin 深度画像

### 2.1 技术形态

- **壳**：layuiAdmin Pro（`src/index.js` + `config.js`）。
- **页面**：约 **446** 个 `src/views/**/*.html`（扫描 446；目录枚举约 448）。
- **逻辑**：大量写在 html 内联脚本；`controller` 几乎无独立业务 JS；依赖 `layui.betwin` / `betwin.admin.req` / `admin.req`。
- **鉴权与结果约定**（`config.js`）：
  - Token 字段名：`token`
  - 成功：`success === 1`（或 truthy）
  - 文案：`msg`
  - 数据：`info`
  - 登录失效：`!success && info.ErrorType === "Login"`

### 2.2 业务域（按 views 目录页数）

| 目录 | html 约数 | 说明 |
|------|-----------|------|
| site | 86 | 站点/配置/VIP/域名等 |
| game | 69 | 游戏设定、转账、日志等 |
| user | 65 | 会员、代理、资料弹层 |
| events | 59 | 活动/奖励相关 |
| report | 45 | 报表 |
| money | 37 | 充提、加减款、返水佣金 |
| risk | 26 | 风控 |
| voucher | 9 | 优惠券 |
| exchange | 9 | 汇率 |
| proxy | 9 | 代理制 |
| shop | 8 | 商城 |
| 其他 | <10 | ticket / survey / system / main / page … |

### 2.3 接口面（从 views 静态抽取）

- 页面里出现的 **`admin/...` 唯一路径约 711 条**（含少量带 query 的脏字符串，需人工清洗）。
- 按路径第二段域粗分（接口条数，非页数）：

| 域 | 约数 | 域 | 约数 |
|----|------|----|------|
| site | 189 | user | 112 |
| game | 88 | money | 67 |
| award | 47 | risk | 36 |
| report | 35 | recharge | 26 |
| shop / voucher | ~21 | exchange | 17 |
| proxy | 12 | 其他 | 更少 |

高频公共接口示例：`admin/site/currency/show`、`admin/system/upload`、`admin/user/member/group/list` 等。

### 2.4 典型页面结构（以会员列表为例）

路径：`src/views/user/member/list.html`

- 查询区：搜索类型下拉 + 关键字、上级代理、用户类型、状态等多条件。
- 工具按钮：导入备注、添加总代等（`data-popup` 弹层）。
- 表格：layui table，行内操作进详情/锁号等。
- 请求：`admin/user/member/...` 形态；参数多为 Form 字段名（如 `SearchType`、`User`）。

迁到新壳时：**同一套字段与按钮语义**，换成 vxe + Element Plus 查询条/对话框。

---

## 3. 新壳与菜单/权限现状（SimpX）

### 3.1 前端壳

- `Web/apps/admin`：Pro 布局 + 动态路由；登录/菜单已接 SimpX JWT + `loginMenuTree`。
- 业务 `component` 路径以 **`SiteAdminMenuSeedData`** 为准（约 **133** 个 `Menu` 叶子），例如：
  - 会员列表：`Component="/user/member/list"`，`Permission="site:user:member:list"`
  - 充值订单：`Component="/money/recharge/order"`，`Permission="site:money:…"`
- **原则 3**：页面 `v-auth` / 菜单只认这些 **新权限码**；不要再绑旧版权限 GUID 到前端按钮（后端旧接口上的 `OldApiPermission` 另论）。

### 3.2 后端旧路径兼容层（重要缺口）

SimpX 已有旧版返回封装：

- `OldApiResult` / `OldApiShowResult`（`success/msg/info`）
- 部分接口 `[NonUnify]` + `[HttpPost("/admin/...")]`

**后端旧路径覆盖（2026-11-03 续）**：Admin `/admin/...` 累计约 **~350 / ~790 ≈ 44%**（site/risk/report/award/agent/shop/voucher/points/exchange/customer 已挂高频并对打）；Request `/request/...` 约 **~46 / ~471 ≈ 10%**（同域 money/user/game + content/member/invite/agent-team/events 读）。详见 `F:/upgrade docs/evidence/2026-11-03-api-hub-coverage.md` 与各波 evidence。

结论：

- 迁前端时 **优先调用已存在的 `/admin/...` + 旧版返回**。
- 若旧页依赖的接口后端没有：按原则 4 **先补后端靠拢旧版**（路径、入参、`OldApiResult`），再写页面；不要改成只调 `/api/system/*` 冒充完成（除非用户拍板）。
- **后端第二波未过域 DoD 前**，对应菜单页不要宣称「可迁完」。

### 3.3 双轨返回格式

| 场景 | 格式 |
|------|------|
| 新框架接口（登录、菜单、系统用户等） | `{ code, result, … }` |
| 旧业务兼容接口 | `{ success, msg, info }`（`NonUnify`） |

**已拍板（2026-11-03）**：新壳默认拦截器继续吃 `{ code, result }`；对 `/admin/...` 走 **单独请求层**（前端模块命名用域，如 `form-api` / `formPost` / `sitePost`，**不要用 old\*，也不要再叠无意义的 Admin/Request 类名前缀**），只解 `success/msg/info`，两套不要混在同一段解析逻辑里。  
**HTTP 路径仍 1:1 旧版**：`/admin/...`、`/request/...` 是 v1 路由本身，不是多余前缀；Swagger/`ApiDescriptionSettings` 与 AppService 类名已去掉 `Admin*`/`Request*` 叠词（撞 JWT 时用 `*FormAppService`）。

### 3.4 `msg` 里的 `#xxx#`（语言包，禁止瞎扒）

| 标记 | 含义 | 旧站怎么处理 |
|------|------|----------------|
| `#操作成功#` 等 | **语言包 key**，后端常这样回 msg | `Utils.getDemo(msg)`：正则抠 `#…#` → 查 `scripts/trlang.json`；有词条用译文，没有才退回去掉 `#` 的原文 |
| `~保存~` 等 | 页面/模板文案标记 | `Utils.getField` 等会再处理；`trlang.json` 的 value 里也常见 `~…~` |

**硬约束**：新壳拦截器 / `resolveAdminApiMsg` **不得**为了“好看”直接 `replace` 掉 `#`。未接语言包前 **原样保留 msg**；接语言包时对齐 `getDemo`/`getField` + 词条表，单独开任务。

证据：`Web.Admin/scripts/index.js` 的 `getDemo` / `getField`；词条 `Web.Admin/scripts/trlang.json`（另有 `scripts/lang/eng.js` 等语种）。

---

## 4. 路径与组件映射约定

| 旧版 | 新壳 |
|------|------|
| 路由视图 `views/{a}/{b}/{c}.html` | 优先 `src/views/{a}/{b}/{c}.vue`（与菜单 `Component` 一致） |
| `data-popup="user/agent/agent-add"` | 同业务弹层组件，或路由子页 |
| `admin/xxx/yyy` | 原样请求 `/admin/xxx/yyy`（经 Vite 反代到 Admin :5005） |
| 登录态 | **一律新 JWT Bearer**；不再使用旧站 `token` 头做鉴权 |

菜单已用新 Path/Component 时：**以菜单 Component 为文件落点**；旧 html 路径仅作行为规格来源。若两者不一致，记入「待拍板」表，不擅自改种子。

---

## 5. 单页迁移作业标准（执行清单）

每迁一页（一个菜单叶子）必须产出：

1. **规格摘录**（来自旧 html，**必读旧站**）  
   - 查询字段表、列表列、按钮、弹层、**完整操作流程**、调用的 `admin/...` 列表  
   - 写清旧站路径：`Web.Admin/src/views/...`（含 `data-popup` 弹层）  
2. **接口核对**  
   - 逐个确认 SimpX 是否已有同路径 + 旧版返回；缺失或参数不对则开后端任务（靠拢旧版）  
3. **新页实现**（实现中遇不确定再回读旧 html，不猜）  
   - Vue3 + vxe/查询条/操作列；`v-auth` 用菜单新权限  
   - 请求走 `adminPost`/`adminGet`；解析 `success/msg/info`  
4. **验收**  
   - 主路径按旧站流程走一遍：查询 → 列表 → 至少一写操作（若旧页有）  
   - 对比旧站同操作结果（字段级抽查）  
5. **冲突**  
   - 新旧行为不一致 → 记入 `admin-frontend-migration-conflicts.md`，**不自行取舍**，交用户拍板  

禁止：整目录拷贝 html；为省事改调新 REST 且改出入参；用 Pro 系统页冒充业务页；**不打开旧站就写业务页**。

---

## 6. 推荐迁移波次（前端）

结合菜单种子、旧页密度、资金风险：

| 波次 | 范围 | 理由 |
|------|------|------|
| **W0** | 工程约定 | `admin-api`（`success/info`）、`/admin` 反代、占位页模板、冲突登记表 |
| **W1** | 用户 / 会员主路径 | `user/member/list` 及详情相关弹层；后端 `/admin/user/member/*` 已有一批 |
| **W2** | 资金只读/审核类 | 充值订单、入款审核、提现订单等（高风险，页少先读后写） |
| **W3** | 代理 / 返佣设定 | 与会员强相关 |
| **W4** | 站点配置类 | `site/*` 页多，可平行拆 |
| **W5** | 游戏 / 报表 / 活动 / 风控 | 页与接口最多，靠后 |

资金、订单状态相关页走高风险验证（场景 3～7，不能只看接口 200）。

---

## 7. 冲突与拍板（2026-11-03 已定）

| ID | 冲突 | 结论 |
|----|------|------|
| C1 | 旧站 token vs 新 JWT | **一律新 JWT**；旧 token 头不做鉴权 |
| C2 | 旧权限 GUID vs 新 `site:…` | **只用新权限码**（库已写入）；旧 GUID / `OldApiPermission` 不用 |
| C3 | 菜单 Component 与旧 html 路径不完全同名 | 以菜单为准落文件；旧页当规格 |
| C4 | 缺接口 / 参数不对 | **后端该补补、该改改**，靠拢旧 `/admin` 路径与入参；前端不改调新契约冒充完成 |
| C5 | 传输加密 / DPoP / 设备绑定 | **不纳入本迁移波次** |
| C6 | 两套返回格式怎么拆 | **`admin-api` 独立拦截器**解 `success/msg/info`（命名不用 old）；新壳默认拦截器继续解 `code/result` |

---

## 8. 指挥话术（之后怎么召唤）

```text
@@X 按 admin-frontend-migration 迁「会员列表」
旧版：Web.Admin/src/views/user/member/list.html
新壳落点：菜单 Component /user/member/list
原则：行为旧、皮肤新、/admin 路径+旧返回、权限新
缺接口靠拢旧版；冲突列给我拍板
```

---

## 9. 证据与扫描说明

- 旧页/API 统计：对 `Web.Admin/src/views` 静态扫描（html 内 `admin/` 字符串），非运行时覆盖率；含噪声需页级清洗。  
- SimpX `/admin` 路由：对 `*.cs` 中 `HttpPost/Get("/admin/...")` 扫描，**动态 API 其它挂法可能未计入**；开页前仍以实际 Swagger/抓包为准。  
- 本档只约束 **租户后台 admin 前端迁移**；总后台 `super-admin`、会员 H5 另档。

---

## 10. W0 落地清单与下一步

### W0 已落地（2026-11-03）

| 项 | 位置 |
|----|------|
| `/admin` 请求层 | `Web/apps/admin/src/utils/admin-api.ts`（`adminPost`/`adminGet`，JWT + `success/msg/info`） |
| 开发反代 | `Web/apps/admin/vite.config.ts` → `^/admin` |
| 列表页模板 | `Web/apps/admin/src/views/_templates/admin-list.vue`（复制后改，勿直接挂菜单） |
| 冲突表 | `docs/admin-frontend-migration-conflicts.md` |
| `#xxx#` | 语言包 key，见 §3.4；未接词条前原样保留 |

### W1 会员域 + 代理域 + W2 资金主列表（2026-11-03 续）

| 域 | 已落页面（主列表） |
|----|-------------------|
| 会员 | list / group / bank / kyc / return |
| 代理 | domain / invite / check / commission / return / gradesetting / usragentinfo |
| 客户 | message / sms |
| 资金 | recharge order·check·checkfail·setting；withdraw order·setting·bank |

策略：有 `/admin` 的先落查询主路径；弹层/次要列/导入记冲突表，不阻塞下一页。资金审核按钮已接后端，须人工复核账务。

### 进度（菜单叶子，2026-11-03 本会话）

- 业务菜单 **133** 叶：**vue 文件已齐 133/133**（覆盖菜单 Component）。  
- 深度不一：用户/代理/资金主列表做过旧站对照；报表/游戏/站点等多为列表壳 + 猜测接口路径，需逐页回旧站精修列与弹层。  
- 导入类 6 页（会员三导入、电销、活动会员导入、号码导入）为占位空态，冲突表已记。

### 弹层接通（2026-11-03 本段）

- 会员分组：save/delete 后端已挂；添加/编辑弹层（主字段+部分提现）。
- 银行/证件：添加 + 审核弹层；银行 `updateinfo`/reset 已接通；证件正反面上传+`createidcard` 全字段；银行添加：凭证上传 + 银行/虚拟/电子/超商 Tab + 超商级联（IsVir=4）。
- 返水设定：GroupID 筛选、returntype、分组/等级多选、SettingString 键值%网格；厂商 GameGroups 元数据网格 / 自助规则（savereturnrule stub）未迁。
- 代理域名/邀请：添加/编辑弹层；域名 `save` 后端已挂。

### W1 会员列表精修（2026-11-03 本波）

- 查询下拉：佣金/返点/等级/币种/站点类型已挂旧路径。
- 工具：添加总代（`/admin/user/agent/info/add`）、备注导入 Excel/CSV（前端 ExcelJS→`batchaddmark` Items）。
- 列：总存款/总取款已接后端 `rpt_UserDateMoney` 汇总（未做汇率折算）；余额可展开 `detailmoney`；开户方式文案对齐常见 RegMethod。
- 导出：Form `/admin/user/member/info/export` + 前端 CSV（≤5000）；服务端 Url 下载体 Excel 可选后补。

### 本波续作（2026-11-03）

- 会员 list：`TotalRechargeMoney`/`TotalWithdrawMoney`（`rpt_UserDateMoney`）+ Form `info/export` + 前端 CSV；备注 Excel/CSV。
- 代理：返点方案弹层、层级新增/设代理/锁定；`grade/save` 支持 UserName/ParentName；审核状态中文；返佣 SettingString 键值%网格。
- 银行/证件：审核全字段+正反面上传；分组：提现全字段 + 自动跳组 Condition；电子钱包 IsVir=2 类型下拉。
- 返水：GroupID 筛选、returntype、分组/等级多选、SettingString；自助反水 `savereturnrule`→`AutoReturnRule`。
- 帐变：分组帐变类型下拉；导入：会员三页+手机/证件黑名单 Excel；风控壳中文列。
- 报表：余额汇总列对齐钱包汇总 DTO；userlog/signin/pointlog/balance `OfPage`/`OfList`；非 P0 图表+生成壳诚实空态。
- 风控：`updatestatus` 批量处理；风险会员明细抽屉；control list/profit/game `OfList`。
- 客服：站内信/短信 `OfPage` + `runAdminVxeQuery`；站内信发送弹层。
- 提现银行勾选、充值/提现通道主字段编辑弹层。
- 提现 `channelsys` SysID 下拉；返水/代理返点返佣 GameGroups|game/info/get 厂商网格；帐变 `MoneyTypeCatalog`；风控明细结构化；站内信 User/Groups；电子钱包国家级联。
- 款项目发放 loglist 筛选/审核；会员详情·余额·充提通道·比对失败·反馈详情结构化；银行 IsVir 分类型；Host :5005 持续重载。
- 佣金详情 JSON 结构化；佣金/返点/返水/救援金币种筛选；会员日志 Platform 中文；返水设定 Groups/Levels 显示名；Host :5005。
- 充提订单/帐变/入款审核/比对失败 Currency；会员锁定位 formatUserLockFlags；银行 IsVir 类型列；Host :5005。

### 下一步（纠偏后）

见 [`admin-frontend-migration-reset.md`](admin-frontend-migration-reset.md)：  
**冻结非 P0**；共用壳（`admin-page.scss` / `runAdminVxeQuery`）；只竖切会员列表→充提订单→入款审核→帐变→银行。

### 11. 展示层硬约束（枚举 / 备注 / 费用）— 2026-11-03 踩坑必读

迁资金与会员相关页时**强制**遵守；再出现 `实充：47.50 2`、`备注：||5975 TWD`、列头英文、静默空表，按本条返工。

| # | 规则 | 正确做法 | 禁止 |
|---|------|----------|------|
| E1 | **枚举唯一源** | 后端 `SimpX.Net.Data` 枚举（如 `Currency`/`MoneyCheckStatus`/`WithdrawStatus`）+ `OldApiEnumDisplay.Name&lt;T&gt;` / `.Currency` / `.CheckStatus` / `.WithdrawStatus` | 各服务自建 `CurrencyNames[]`、大段 `switch (currency)`、前后端各抄一份表 |
| E2 | **金额旁币种** | `OldApiMoneyDisplay.FormatMoneyCurrency(paid, currency)`；历史串走 `NormalizeCurrencySuffix`（`47.50 2`→`47.50 TWD`） | 字符串拼接 `paid + " " + currencyInt`；列表/详情直接吐库里的裸整型 |
| E3 | **充值 Remark 管道** | 库格式 `备注A\|备注B\|实充 币种`；展示用 `ParseRechargeRemark` / 前端 `formatRechargeRemark`；实充单独字段 RealPaid | 详情「备注」原样显示 `\|\|5975.00 TWD` |
| E4 | **前端枚举** | `admin-enum.ensureCurrencyEnum`（优先 `exchange/rate/getcurrencyinfo`）+ `formatCurrency` / `formatRealPaidDisplay` / `formatMoneyCheckStatus` / `formatWithdrawStatus`；**查询前 await** | 页面内硬编码币种数组；未预热就格式化导致仍显示 `2`/`17` |
| E5 | **费用语义** | 手续费 / 行政费分列或分两行标签 | 「费：a/b」无语义缩写 |
| E6 | **次数** | 到账次数写「第N次」并 tip | 裸数字贴在金额旁像币种 |
| E7 | **状态文案** | 审核/提现状态走统一 formatter；兼容旧拼写 `Faild` | 每页复制一份 status map，且只认整型或只认英文名 |
| E8 | **列表契约** | `runAdminVxeQuery` + 中文列头；失败 `ElMessage` | `catch { return [] }` 静默空表；列标题用 `UserName`/`CreateAt` |
| E9 | **中文文件编码** | 改 `.vue/.ts/.md` 用编辑器/Write/`python` UTF-8；`admin-page` 只加在**根页面 div** | PowerShell `Set-Content`/`Out-File` 默认编码把中文写成 `????`；正则把 `admin-page` 洒到 toolbar/actions |
| E10 | **非 P0 报表** | 图表类（`data-chartname`）诚实空态「未验收」；不解冻前禁止猜 `/admin/report/*` 冒充列表 | 把手续费统计等壳绑到错误 list API 当交付 |
| E11 | **会员锁定位** | `formatUserLockFlags`：Login=1/Recharge=2/Game=32/Point=64/Activity=128/Upgrade=256 中文标签 | 详情直接显示裸整型 `Lock` |

落地文件：

- 后端：`OldApiEnumDisplay.cs`、`OldApiMoneyDisplay.cs`、`Data/Enum/Currency.cs` 等  
- 前端：`utils/admin-enum.ts`、`utils/admin-display.ts`  
- 冲突登记：见 conflicts 表「展示层枚举/缩写」「充值 Remark 管道」

### 契约清理（2026-11-03）

- 已去掉逐接口 `OldApiJson.Result`；由 `OldApiAuth` + `OldApiContract` 统一声明名序列化。

- MoneyReview.Action：**1=加款 / 2=扣款**（勿把 0/1 当方向）；金额扣款为负。
- 日期展示：`formatDateTime` 把 CLR `0001-01-01` / `1900-01-01` 哨兵当成空（`-`），审核时间未审勿显示假日期。
- 充值/提现通道 Fee、Cost：库内为小数费率；后台编辑与列表「%」列按 ×100 / ÷100 换算。
- 汇总 data.Currency：**禁止硬编码 TWD/CNY**；单币种用枚举名，多币种留空。

### 金额/费率/排序展示（@@X 续）
- 待审类页（入款/提现订单/加减款/比对失败/发放/代理审核/银行/证件）宜提供 60s「自动刷新」开关，离开页清理 timer；充值订单同。
- 金额色：入款/发放/存款类绿色 `money-in`；出款/锁定/扣款类红色 `money-out`；盈亏按正负。
- 费率：库内小数费率，列表/编辑按百分比（×100 展示，保存 /100）；提现通道 Cost 同充值 Fee/Cost。
- 代理返佣方案 Award/Return/Cost/MaxRate 同费率换算；返水/返佣 Setting 游戏比例同。
- 通道 Sort：充值/提现通道列表展示并可编辑保存。
- 用户名筛选：资金/会员相关列表优先 **Contains 模糊**（纯数字仍按 UserID 精确）；上级 Agent 同理。
- 加减款审核：补 **流水号 SourceID** 查询框；方向 Action 1加/2扣；列展示 PayName（PayID→SitePayment）。
- 代理信息列表：补齐 AgentName/Mode/ReturnMode/Member/Invite/Domain/BlockGame；支持 Status/Agent/BelongUser/日期；返回 OfPage。
- 代理层级：账号类型 Type 筛选 + Tag。

### Host 本地启动（@@X）
- `Host.Admin/Configuration/Database.json`：日常 **EnableInitDb / EnableInitTable / EnableInitSeed = false**（`SimpX.Net.db` 已存在）。
- 空库首次建 Sys* 壳才临时改 true，跑通后立刻改回；业务段 `BetWin30Biz` 永远禁 CodeFirst/Seed。
- 运行目录是 `bin/Debug/...` 时，改源配置后需同步到输出 `Configuration/Database.json`（或重新 build）。

### 错误提示（@@X）
- 网络/HTTP 错误只弹一次：`showNetworkErrorThrottled`（中文）+ `__toastShown`。
- 业务 catch / `runAdminVxeQuery` 用 `showCaughtError`，**禁止**再 `ElMessage.error(e?.message)`（Axios 英文会叠第二条）。
- 请求拦截失败也只走节流，勿再 `ElMessage.error(error)` 叠一条。
- 充提订单筛：对齐旧站补 **FromCurrency**（充值=入款币种 / 提现=出款币种）。
- 充提订单时间筛：对齐旧站 **DateType**=CreateAt|PayAt。
- 比对失败：`/admin/recharge/orderlistfail` 接通 `RechargeCheckFailService`（IsCheckFail），支持 Currency/日期。
- request2 等遗留路径也改 showNetworkErrorThrottled，禁止 ElMessage.error(error) 叠英文。
- 提现订单：对齐旧站补 **审核时长/出款时长** 分钟筛（StartAudit/EndAudit/StartCash/EndCash）。
- 充值接口：名称/币种筛 + 批量启用/停用 + 删除；提现接口补删除。
- 银行信息币种筛；银行帐号/证件号码 Contains。
- 充值接口 settinglist 回全字段（Fee/Cost/Currency/SysName 等）；补 updatepaymentstatus；列表 Switch 启停。
- 提现接口：创建时间列、币种/启用筛、Switch 启停（savechannel Cost 按%传入）。
- 帐变 SourceID Contains；返点/佣金发放用户名 Contains。
- 提现订单 Status 筛：对齐旧站枚举名 None/Wait/Finish/Faild/InPayment（BE 兼容 int + Failed/Fail→Faild）。
- 邀请码列表 IsOpen 改 Switch 直调 save；风控详情失败走 showCaughtError。
- 返点/佣金发放 Status 筛兼容 CheckStatus 枚举名。

- 发放类 Status 筛统一枚举名 None/Faild/Success（返水/返点/佣金/救援）；救援 BE 兼容枚举名。

- 比对失败补 IsPay 到帐筛（对齐旧站）。

- 比对失败列表补上级 AgentName、到帐次数 Count、PayAt；IsPay 筛。

- 登录日志 userlog：Platform/IPAddress/IsLast 必须落在请求体对象内（曾出现语法断裂导致筛失效）。

- 代理域名 Invite/Domain 筛改为 Contains 模糊。

- 会员列表 InviterName：用户名 Contains / 纯数字按 InviteID。

- 邀请码 ID 筛 Contains；IP 黑名单 User 关键字筛 Name/Remark/IP；站内信/短信默认可查今日。

- 充值订单补 KeyWord（订单号/网关单号 Contains），对齐旧站。

- 提现订单补 SystemID（网关单号）Contains 筛。
