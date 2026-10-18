# 测试项目

project_id: hx-matching
summary_group: tests
host_or_layer: test
source_repo: F:/HX.MatchingApi
last_ingestion_run: 20261018T070046Z-project-hx-matching
file_count: 140
project_count: 3
risk_tags: [verification]

## 职责摘要

渠道、V2 和集成测试入口，用于后续验证定位。

## 扫描根目录

- HX.Matching.Channels.Tests
- HX.Matching.V2.Tests
- src/TestIntegration

## 项目文件

- HX.Matching.Channels.Tests/HX.Matching.Channels.Tests.csproj
- HX.Matching.V2.Tests/HX.Matching.V2.Tests.csproj
- src/TestIntegration/TestIntegration.csproj

## 定位样例

- HX.Matching.Channels.Tests/BaWangPayInRechargeTests.cs
- HX.Matching.Channels.Tests/ChannelCancellationTests.cs
- HX.Matching.Channels.Tests/ChannelClientTests.cs
- HX.Matching.Channels.Tests/ChannelContractMapperTests.cs
- HX.Matching.Channels.Tests/ChannelContractParityTests.cs
- HX.Matching.Channels.Tests/ChannelDiWiringTests.cs
- HX.Matching.Channels.Tests/ChannelEnumParityTests.cs
- HX.Matching.Channels.Tests/ChannelInvocationContextTests.cs
- HX.Matching.Channels.Tests/ChannelLogMaskerTests.cs
- HX.Matching.Channels.Tests/ChannelSignExtensionsTests.cs
- HX.Matching.Channels.Tests/ChannelStatusPipelineTests.cs
- HX.Matching.Channels.Tests/FakeChannelHttpClient.cs

## 使用约束

- 本摘要只用于 RAG 定位和初筛，不替代当前源码。
- 判断真实行为、接口契约、状态变更或兼容逻辑时，必须回读业务仓当前源码。
- 摘要生成时跳过 bin、obj、.git、logs、证书、密钥和 secrets 文件。
