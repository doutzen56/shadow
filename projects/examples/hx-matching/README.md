# HX.Matching 项目档案

## 项目定位

HX.Matching 是一个支付撮合平台，覆盖商户代收、商户代付、合作方互通、渠道对接、订单撮合、后台管理、异步回调、统计报表和生产运维。

它适合作为 SimpX 一阶段的第一个真实项目样本，因为它同时具备业务复杂度、生产稳定性诉求、多宿主部署、外部接口契约、消息队列、缓存、搜索和真实事故复盘。

## 项目风格

- 主风格：企业稳定型
- 次风格：业务交付型
- 补充风格：性能优先型
- 治理标签：演进中的遗留系统

## 核心业务链路

1. 商户或合作方发起订单。
2. 开放接口完成验签、风控要素校验和订单创建。
3. 核心服务根据渠道、供应商、买卖单状态执行撮合或拉单。
4. 后台任务和消息消费者处理超时、回调、统计、导出和异步补偿。
5. 管理端和商户后台提供运营、配置、报表和人工处理能力。

## 五个宿主

| 宿主 | 职责 | 关键特点 |
| --- | --- | --- |
| HX.Matching.Admin.WebApi | 平台管理端 | 管理商户、渠道、供应商、订单、统计、通知和系统配置 |
| HX.Matching.MerchantAdmin.WebApi | 商户后台 | 商户侧登录、订单、余额、报表和配置查看 |
| HX.Matching.MerchantApi | 商户开放接口 | 代收、代付、查单、收银台，是商户热路径 |
| HX.Matching.PartnerApi | 合作方互通接口 | 合作方下单、查收款账户、卖家挂单，与撮合链路关系更紧 |
| HX.Matching.Quartz.Job | 后台任务进程 | 定时任务、消息消费、回调、统计、超时处理和 Telegram 通知 |

## 核心层

| 项目 | 职责 |
| --- | --- |
| HX.Matching.ServiceCore | 业务核心，包含撮合、渠道、统计、缓存、搜索和后台服务 |
| HX.Matching.Model | 实体、枚举、请求响应模型和搜索文档模型 |
| HX.Matching.Repository | SqlSugar 仓储基础能力 |
| HX.Matching.Infrastructure | 缓存、消息、认证、限流、工具和横切基础设施 |
| HX.Matching.Tasks | Quartz 定时任务定义 |

## 技术画像

- 运行时：.NET 9
- 接口框架：ASP.NET Core，管理端以控制器为主，开放接口以最小接口为主
- 数据访问：SqlSugar，包含分表和多库配置
- 缓存：Redis，业务缓存以 CSRedis 为主，部分框架能力使用 StackExchange.Redis
- 消息：RabbitMQ
- 搜索：Elasticsearch
- 任务调度：Quartz
- 日志：NLog
- 配置：AgileConfig
- 对象存储：S3

## 工作约束

- 只做本地修改，不 git push、不提 PR。
- 提交前由人审。
- 任务结束只提本仓 / SimpX / ADR 沉淀候选，不擅自写外部知识库。明确说「记住：」或「记一条 ADR」才算确认。

## 对 SimpX 的价值

- 可以校验企业稳定型项目风格是否真实可用。
- 可以沉淀多宿主 .NET 系统的边界分析方法。
- 可以形成支付撮合领域术语、接口契约和渠道扩展方法。
- 可以把线程池饥饿事故变成性能与稳定性治理样本。
- 无感 Turnstile：`E:\simpx\knowledge\security\hx-matching-turnstile-bot-protect.md`
