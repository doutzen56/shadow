# BetWin 项目档案

## 项目定位

BetWin 是一个多站点游戏平台，覆盖会员与代理前台、站点管理、平台系统配置、第三方游戏与支付对接、充提、返水返佣、活动、报表、注单检索、后台任务和消息消费。

它适合作为 SimpX 一阶段的第二个真实项目样本：比 HX.Matching 更接近长期演进的 .NET Framework 遗留单体，同时仍有明确的多宿主拆分和资金相关热路径。

## 项目风格

- 主风格：企业稳定型
- 次风格：业务交付型
- 补充风格：遗留系统改造型
- 治理标签：演进中的遗留系统

## 核心业务链路

1. 会员或代理从前台模板发起注册、充值、进游戏、提款或活动领取。
2. `Web.Handler` 把请求分发到 `Handler.Request` / `Handler.Admin` / `Handler.System` / `Handler.Api`。
3. Handler 调用 `Library/BetWin` 中的业务 Agent，读写 `BetWin.Common` 模型、缓存和外部网关。
4. 游戏、支付、代付通过 `Gateway.*` 插件对接第三方；回调进入 `Handler.Api` 或对应支付/游戏接口。
5. `JobSchedule`、`DataAnalysis`、`GameLog`、`MqServer` 处理定时任务、报表统计、注单同步和异步消息。

## 主要宿主

| 宿主 | 职责 | 关键特点 |
| --- | --- | --- |
| Web.Handler | 站点 HTTP 入口 | ASP.NET Handler 宿主，加载各 Handler 程序集 |
| Web.Admin | 站点管理端前端 | 运营后台页面，走 Handler.Admin |
| Web.System | 平台系统端前端 | 多站点配置与平台治理，走 Handler.System |
| Web.H5 / Web.T* / Web.Mobile / Web.Common | 会员与代理前台 | 多套模板与 H5，走 Handler.Request |
| JobSchedule | 后台任务调度 | Topshelf + Quartz，定时作业宿主 |
| DataAnalysis | 数据分析与对账 | 注单同步、报表、返水、风控等后台动作 |
| GameLog | 注单采集 | 游戏日志拉取与写入 |
| MqServer | 消息消费 | RabbitMQ 消费者进程 |

## 核心层

| 项目 | 职责 |
| --- | --- |
| Library/BetWin | 业务 Agent，站点、用户、资金、游戏、代理、风控的主要演进对象 |
| BetWin.Common | 实体、枚举、报表模型和 Elasticsearch 索引注册 |
| BetWin.Caching | Redis 与业务缓存 |
| Handler.Request | 会员、代理、站点前台接口 |
| Handler.Admin | 站点管理端接口 |
| Handler.System | 平台系统端接口 |
| Handler.Api | 游戏回调与运维开放接口 |
| Gateway.Game / Payment / Withdraw | 第三方游戏、代收、代付插件 |
| SP.Studio | 数据访问、Web、安全等基础设施 |

## 技术画像

- 运行时：.NET Framework 4.8
- 接口形态：ASP.NET Handler，按程序集拆请求面
- 数据访问：SP.Studio 数据层，SQL Server
- 缓存：Redis
- 消息：RabbitMQ
- 搜索：Elasticsearch，注单存在 `gamelogv2` 等索引演进
- 任务调度：Quartz，由 JobSchedule 宿主加载
- 日志：NLog
- 前端：Vue / 多套站点模板（Web.T1–T8、Web.H5、Web.Mobile）

## 工作约束

- 只做本地修改，不 git push、不提 PR。
- 提交前由人审。
- 不把说明、规则、档案写进业务仓；不改业务仓自带文件来做 SimpX 托管。Agent 全部委托 SimpX。
- 任务结束只提本仓 / SimpX / ADR 沉淀候选，不擅自写外部知识库。明确说「记住：」或「记一条 ADR」才算确认。
- 不在本仓根目录写 `AGENTS.md`、`.cursor/agents`、`.cursor/rules`、`.cursor/skills`。

## 对 SimpX 的价值

- 可以校验企业稳定型 + 遗留改造型在同一仓是否可同时使用。
- 可以沉淀 Handler 分面、Agent 层、Gateway 插件的边界方法。
- 可以形成资金、游戏、注单、代理佣金相关的回归检查习惯。
- 可以与 HX.Matching 对照：.NET 9 多宿主 WebApi 与 .NET Framework 多 Handler 宿主的不同治理方式。
