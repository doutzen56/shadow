# BetWin 系统边界

## 边界总览

BetWin 是单仓多宿主系统。前台、站点后台、平台系统端、游戏回调和后台进程共享 `Library/BetWin` 与 `BetWin.Common`，但请求面和运行时职责不同。边界分析的目标不是拆成微服务，而是避免把会员热路径、管理报表、第三方回调和异步作业混在一次改动里。

## 宿主边界

### 站点请求入口

路径：`E:\work\BetWin20250303\WebSite\Web.Handler`

职责：

- ASP.NET 站点 HTTP 宿主。
- 加载 Handler 程序集，对外提供会员、代理、管理、系统、回调入口。

边界：

- 不在此项目堆业务规则。
- 改入口配置时不要顺手改 Handler 业务。

### 会员与代理前台接口

路径：`E:\work\BetWin20250303\Library\BetWin.Handler.Request`

职责：

- 会员登录、资金、游戏、活动、代理团队。
- 站点内容、支付与部分第三方回调入口。

边界：

- 是会员侧热路径，优先保证正确性和可用性。
- 不承担站点管理报表和平台级配置。
- 对应前端主要在 `Web.H5`、`Web.T*`、`Web.Mobile`、`Web.Common`。

### 站点管理端

路径：`E:\work\BetWin20250303\Library\BetWin.Handler.Admin`

前端：`E:\work\BetWin20250303\WebSite\Web.Admin`

职责：

- 单站点运营：会员、代理、充提、游戏、活动、报表、风控。

边界：

- 允许较重的查询、审核和导出。
- 不应改成会员下单或进游戏热路径。
- 与 `Handler.System` 的平台配置职责分开，不把多站点治理塞进站点后台。

### 平台系统端

路径：`E:\work\BetWin20250303\Library\BetWin.Handler.System`

前端：`E:\work\BetWin20250303\WebSite\Web.System`

职责：

- 多站点与平台级配置：域名、游戏 API、支付、服务、缓存、监控。

边界：

- 面向平台治理，不直接服务会员点击路径。
- 改配置类接口时要评估缓存失效和多站点副作用。

### 游戏回调与运维接口

路径：`E:\work\BetWin20250303\Library\Handler\BetWin.Handler.Api`

职责：

- 第三方游戏回调。
- 部分运维能力：节点、CDN、证书、监控。

边界：

- 回调契约以供应商为准，不能按内部字段名随意统一。
- 运维脚本和证书资源不当普通业务需求改。

### 后台任务调度

路径：`E:\work\BetWin20250303\Services\JobSchedule`

职责：

- Topshelf Windows 服务。
- 加载 Quartz 作业，执行定时业务。

边界：

- 作业实现多在 `Library/BetWin` 的 Jobs 目录，宿主只负责调度。
- 停用作业时要同时考虑代码特性和 Quartz 持久化残留。

### 数据分析与注单

路径：

- `E:\work\BetWin20250303\Services\DataAnalysis`
- `E:\work\BetWin20250303\Services\GameLog`

职责：

- 注单同步、报表统计、返水、活跃、风控等后台动作。
- 游戏日志采集。

边界：

- 读写 Elasticsearch 与统计表，不承担前台同步请求。
- 注单索引演进（如 `gamelogv2`）与旧别名并存时，先确认读写路径再改作业。

### 消息消费

路径：`E:\work\BetWin20250303\Services\MqServer`

职责：

- RabbitMQ 消费者。

边界：

- 不在 Web 进程里抢消费。
- 生产者与消费者契约变更必须成对评估。

## 核心层边界

### 业务 Agent

`Library/BetWin` 是事实上的业务核心，按 Sites、Users、Games、Proxy、Reports、Risk 等目录组织。

边界风险：该类过多，资金、游戏、代理规则都在这里。后续治理应先沿现有 Agent 边界改，而不是先拆项目。

### 数据模型

`BetWin.Common` 承载实体、枚举、报表视图和 ES 索引注册。

边界风险：模型层不要继续吸收基础设施细节；改 ES 别名属于横切变更，影响读写作业和查询。

### 外部网关

`Gateway.Game`、`Gateway.Payment`、`Gateway.Withdraw` 及 Special 项目是第三方插件。

边界风险：每个供应商的签名、状态码、金额精度、回调时机都可能故意不同，不能为了抽象而统一行为。

### 基础设施

`SP.Studio` 提供数据访问、Web、安全、MQ 基础能力；`BetWin.Caching` 提供业务缓存。

边界风险：新增功能时不要把业务规则下沉到 SP.Studio。

### 站点静态与证书

`WebSite/Web.Sites` 存放分站点静态资源和证书。

边界：不当业务逻辑改；不要把证书和密钥写进 SimpX 档案。

## 边界治理原则

- 改会员热路径先看 `Handler.Request` 和对应 `BetWin` Agent。
- 改站点运营先看 `Handler.Admin` 与 `Web.Admin`。
- 改平台配置先看 `Handler.System` 与 `Web.System`。
- 改第三方对接先看对应 `Gateway.*` 和 `Handler.Api` 回调。
- 改异步、对账、注单先看 `JobSchedule`、`DataAnalysis`、`GameLog`、`MqServer`。
- 涉及资金、余额、订单状态、回调时，`@@QA` 必须给出回归清单。
- 不把业务仓库的真实代码搬进 SimpX，只沉淀边界、方法和经验。
