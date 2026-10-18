# 后台任务与异步处理

project_id: hx-matching
summary_group: background-jobs
host_or_layer: background-job
source_repo: F:/HX.MatchingApi
last_ingestion_run: 20261018T070046Z-project-hx-matching
file_count: 72
project_count: 2
risk_tags: [mq, callback, async, retry, order-state]

## 职责摘要

定时任务、消息消费、回调、统计、超时处理和异步补偿入口。

## 扫描根目录

- HX.Matching.Quartz.Job
- HX.Matching.Tasks

## 项目文件

- HX.Matching.Quartz.Job/HX.Matching.Quartz.Job.csproj
- HX.Matching.Tasks/HX.Matching.Tasks.csproj

## 定位样例

- HX.Matching.Quartz.Job/BackService/Telegram/BalanceCommandParser.cs
- HX.Matching.Quartz.Job/BackService/Telegram/BalanceCommandResult.cs
- HX.Matching.Quartz.Job/BackService/Telegram/BalanceCommandType.cs
- HX.Matching.Quartz.Job/BackService/Telegram/BalanceReceiptContext.cs
- HX.Matching.Quartz.Job/BackService/Telegram/BalanceReceiptMarkdownV1Builder.cs
- HX.Matching.Quartz.Job/BackService/Telegram/EnumPayLedgerAmountType.cs
- HX.Matching.Quartz.Job/BackService/Telegram/ManualBalanceTelegramNotifyHelper.cs
- HX.Matching.Quartz.Job/BackService/Telegram/MediaGroupData.cs
- HX.Matching.Quartz.Job/BackService/Telegram/MultiTelegramBotHostedService.cs
- HX.Matching.Quartz.Job/BackService/Telegram/OkxResultBase.cs
- HX.Matching.Quartz.Job/BackService/Telegram/OkxResultData.cs
- HX.Matching.Quartz.Job/BackService/Telegram/OkxResultSell.cs

## 使用约束

- 本摘要只用于 RAG 定位和初筛，不替代当前源码。
- 判断真实行为、接口契约、状态变更或兼容逻辑时，必须回读业务仓当前源码。
- 摘要生成时跳过 bin、obj、.git、logs、证书、密钥和 secrets 文件。
